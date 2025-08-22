using System;
using System.Collections.Generic;
using System.IO;

namespace GE_Ranger_Programmer
{
    /// <summary>
    /// Reads/writes .RGR files and exposes channel fields.
    /// - Frequencies decoding is the same as before (unchanged).
    /// - Tones: 6-bit indices, stored across the 16 big-endian nibbles per channel.
    ///   * TX index uses the "gold" table (ToneLock.TxMenu).
    ///   * RX index uses the derived RX map (ToneLock.RxIndexToDisplay).
    /// </summary>
    public static class RgrCodec
    {
        public sealed class Channel
        {
            public int Number { get; set; }            // 1..16
            public double TxMHz { get; set; }
            public double RxMHz { get; set; }

            public int TxToneIndex { get; set; }       // 0..63 (0 means "0" no tone). -1 = unknown/bad
            public int RxToneIndex { get; set; }       // 0..63 (0 means "0" no tone). -1 = unknown/bad

            public byte Cct { get; set; }              // left column of digits (0..9 used)
            public bool Ste { get; set; }              // Y/N (true/false)

            // Convenience display (computed)
            public string TxToneDisplay => ToneLock.TxIndexToDisplay(TxToneIndex);
            public string RxToneDisplay
            {
                get
                {
                    var s = ToneLock.RxIndexToDisplay(RxToneIndex, out var _);
                    return s;
                }
            }
        }

        // ===== Public API =====
        public static List<Channel> Load(string path)
        {
            var raw = File.ReadAllBytes(path);
            return DecodeChannels(raw);
        }

        public static void Save(string path, IReadOnlyList<Channel> channels)
        {
            // Load current file as template, replace the 16-nibble blocks & control bits.
            var raw = File.ReadAllBytes(path);
            EncodeChannels(raw, channels);
            File.WriteAllBytes(path, raw);
        }

        // ===== Low-level helpers =====

        private static List<Channel> DecodeChannels(byte[] raw)
        {
            var list = new List<Channel>(16);
            for (int ch = 0; ch < 16; ch++)
            {
                var chan = new Channel { Number = ch + 1 };

                // --- 8 bytes (16 nibbles) per channel, big-endian nibble order
                int byteOffset = ChannelByteOffset(ch);
                Span<byte> block = raw.AsSpan(byteOffset, 8);

                // Big-endian nibbles -> 64-bit
                ulong bits = NibblesBigEndianToU64(block);

                // Frequencies: reuse existing (unchanged) extractors.
                chan.TxMHz = FreqExtract_Tx(bits);
                chan.RxMHz = FreqExtract_Rx(bits);

                // Tones: 6-bit windows at the known positions.
                int txIdx = (int)ExtractBits(bits, TX_INDEX_START, 6);
                int rxIdx = (int)ExtractBits(bits, RX_INDEX_START, 6);

                // Control flags
                chan.Cct = ExtractCctDigit(bits);
                chan.Ste = ExtractSteFlag(bits);

                // Bounds/sanity
                chan.TxToneIndex = txIdx is >= 0 and < 64 ? txIdx : -1;
                chan.RxToneIndex = rxIdx is >= 0 and < 64 ? rxIdx : -1;

                list.Add(chan);
            }
            return list;
        }

        private static void EncodeChannels(byte[] raw, IReadOnlyList<Channel> channels)
        {
            for (int ch = 0; ch < Math.Min(16, channels.Count); ch++)
            {
                var c = channels[ch];

                int byteOffset = ChannelByteOffset(ch);
                Span<byte> block = raw.AsSpan(byteOffset, 8);

                ulong bits = NibblesBigEndianToU64(block);

                // Put back Tx/Rx frequencies using existing writers.
                bits = FreqWrite_Tx(bits, c.TxMHz);
                bits = FreqWrite_Rx(bits, c.RxMHz);

                // Write TX tone index (6 bits). If unknown -> leave as-is.
                if (c.TxToneIndex is >= 0 and < 64)
                    bits = WriteBits(bits, TX_INDEX_START, 6, (uint)c.TxToneIndex);

                // Write RX tone index (6 bits). If unknown -> leave as-is.
                if (c.RxToneIndex is >= 0 and < 64)
                    bits = WriteBits(bits, RX_INDEX_START, 6, (uint)c.RxToneIndex);

                // Control flags
                bits = WriteCctDigit(bits, c.Cct);
                bits = WriteSteFlag(bits, c.Ste);

                // Back to 8 bytes (big-endian nibbles)
                U64ToNibblesBigEndian(bits, block);
            }
        }

        // ====== Constants for bit windows ======
        private const int TX_INDEX_START = 19; // 6-bit window start (0 = MSB of 64-bit bitset)
        private const int RX_INDEX_START = 43; // 6-bit window start

        // ====== Frequency extraction/writing (unchanged behavior) ======
        // These stubs call into the same logic you had working before.
        private static double FreqExtract_Tx(ulong bits) => FreqLock.ExtractTxMHz(bits);
        private static double FreqExtract_Rx(ulong bits) => FreqLock.ExtractRxMHz(bits);
        private static ulong  FreqWrite_Tx(ulong bits, double mhz) => FreqLock.WriteTxMHz(bits, mhz);
        private static ulong  FreqWrite_Rx(ulong bits, double mhz) => FreqLock.WriteRxMHz(bits, mhz);

        // ====== Control fields ======
        private static byte ExtractCctDigit(ulong bits) => CctLock.ExtractCct(bits);
        private static bool ExtractSteFlag(ulong bits)  => CctLock.ExtractSte(bits);
        private static ulong WriteCctDigit(ulong bits, byte v) => CctLock.WriteCct(bits, v);
        private static ulong WriteSteFlag(ulong bits, bool v)  => CctLock.WriteSte(bits, v);

        // ====== Bit/Nibble helpers ======
        private static int ChannelByteOffset(int ch) => 0x000 + (ch * 8); // adjust if base differs

        private static ulong NibblesBigEndianToU64(ReadOnlySpan<byte> eight)
        {
            ulong acc = 0;
            for (int i = 0; i < 8; i++)
            {
                byte b = eight[i];
                byte hi = (byte)(b >> 4);
                byte lo = (byte)(b & 0x0F);

                acc = (acc << 4) | (uint)hi;
                acc = (acc << 4) | (uint)lo;
            }
            return acc;
        }

        private static void U64ToNibblesBigEndian(ulong bits, Span<byte> eight)
        {
            for (int i = 7; i >= 0; i--)
            {
                byte lo = (byte)(bits & 0x0F); bits >>= 4;
                byte hi = (byte)(bits & 0x0F); bits >>= 4;
                eight[i] = (byte)((hi << 4) | lo);
            }
        }

        private static uint ExtractBits(ulong src, int startBit, int bitCount)
        {
            int shift = 64 - (startBit + bitCount);
            ulong mask = ((1UL << bitCount) - 1UL) << shift;
            return (uint)((src & mask) >> shift);
        }

        private static ulong WriteBits(ulong dst, int startBit, int bitCount, uint value)
        {
            int shift = 64 - (startBit + bitCount);
            ulong mask = ((1UL << bitCount) - 1UL) << shift;
            dst &= ~mask;
            dst |= ((ulong)(value & ((1u << bitCount) - 1u)) << shift);
            return dst;
        }
    }
}
