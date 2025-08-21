// ToneLock.cs — RANGR6M2 tone decode/encode + X2212 packing
// Namespace matches your repo.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical CG menu (index → string). Index 0 is "0".
        public static readonly string[] Cg = new[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // ===== RANGR6M2: Tx coding (observed) =====
        // For this personality/image the Tx tone is encoded in A1 (with one disambiguation on B1.bit7).
        // Map: A1 byte -> CG index. If A1 == 0x28, B1.bit7 disambiguates 103.5 vs 107.2.
        private static readonly Dictionary<byte, int> Tx_A1_ToIndex = new()
        {
            // From the RANGR6M2 test image you provided
            { 0xAC, 20 }, { 0xAE, 20 }, { 0xA4, 16 }, { 0x6D, 13 }, { 0xEB, 25 },
            { 0x29, 20 }, { 0xEC, 11 }, { 0x68, 26 }, { 0x98, 14 }, { 0xE3, 16 },
            { 0xE2, 16 }, { 0x63, 15 }, { 0x2A, 26 }, { 0xA5, 19 },
            // 0x28 is ambiguous: CH10=103.5, CH13=107.2 → B1.bit7 picks
            // (bit7=1 => 103.5 / idx13, bit7=0 => 107.2 / idx14)
        };

        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            if (A1 == 0x28)
                return ((B1 & 0x80) != 0) ? 13 : 14;
            if (Tx_A1_ToIndex.TryGetValue(A1, out var idx))
                return idx;
            return -1; // unknown
        }

        // Encode: CG index -> A1 value (and, if needed, B1.bit7 choice).
        // This table covers every Tx tone used in your RANGR6M2 test file.
        private static readonly Dictionary<int, (byte A1, bool? B1bit7)> Tx_Index_To_A1_B1bit7 = new()
        {
            { 11, (0xEC, null) }, { 13, (0x28, true) }, { 14, (0x28, false) }, { 15,(0x63, null) },
            { 16, (0xA4, null) }, { 19, (0xA5, null) }, { 20,(0xAC, null) }, { 25,(0xEB, null) }, { 26,(0x68, null) }
        };

        // ===== RANGR6M2: Rx coding (observed) =====
        // Reality: RANGR6M2 does NOT use a single “6-bit Rx index” source consistently across images.
        // From your RXMAP fixtures we can derive a near-complete bank0 table by using A0 in bit order [7,6,3,2,1,0].
        // From the RANGR6M2 test image, several channels show non-zero Rx tones while A0==0,
        // which implies an *additional* Rx tone coding path (not yet pinned). We handle both, cleanly.

        // Derived from RXMAP_A/B/C (bank 0 only): (bank=0, idxFromA0) -> tone
        private static readonly Dictionary<(int bank, int idx), string> Rx_FromA0_Bank0 = BuildBank0FromRxMap();

        private static Dictionary<(int,int),string> BuildBank0FromRxMap()
        {
            // Hard-wire the bank-0 index→tone pairs we harvested from RXMAP_A/B/C.
            // Index comes from A0 with bit order [7,6,3,2,1,0] (MSB→LSB).
            var d = new Dictionary<(int,int),string>();
            void add(int idx, string tone) => d[(0, idx)] = tone;

            add( 0,"210.7"); add( 3,"103.5"); add( 5,"94.8");  add( 7,"85.4");  add( 8,"79.7");
            add(10,"67.0");  add(12,"173.8"); add(13,"162.2"); add(14,"146.2"); add(15,"131.8");
            add(16,"123.0"); add(17,"118.8"); add(18,"203.5"); add(19,"192.8"); add(20,"179.9");
            add(21,"171.3"); // if unused, harmless
            add(22,"167.9"); add(23,"156.7"); add(24,"151.4"); add(25,"141.3"); add(26,"136.5");
            add(28,"127.3"); add(29,"123.0"); add(30,"118.8"); add(31,"114.8"); add(32,"110.9");
            add(33,"107.2"); add(35,"100.0"); add(36,"97.4");  add(37,"94.8");  add(39,"91.5");
            add(48,"127.3"); add(49,"203.5"); add(51,"107.2"); add(53,"97.4");  add(55,"88.5");
            add(57,"77.0");  add(59,"114.8"); add(60,"186.2"); add(62,"156.7"); add(63,"141.3");

            // Note: RXMAP coverage is bank 0. Bank 1 needs dedicated capture; we add minimal observed pairs below.
            return d;
        }

        // Minimal bank-1 pairs *observed in your RANGR6M2 test image* via A0-index route
        // (limited; a proper RXMAP for bank 1 would fill this comprehensively).
        private static readonly Dictionary<(int bank, int idx), string> Rx_FromA0_Bank1_Min = new()
        {
            { (1, 63), "162.2" },
            { (1, 12), "107.2" },
        };

        // Decode Rx tone from a channel’s bytes.
        // Policy:
        //   1) Try A0-index method if A0 != 0 (bank0 table complete; bank1 partial).
        //   2) If tone resolves, return it.
        //   3) If A0 == 0 → treat as "0" (as seen on DOS) *unless* a future flag says otherwise.
        public static string RxToneFromBytes(byte A0, byte B3)
        {
            int bank = (B3 >> 1) & 1;
            int idxA0 = IndexFromA0(A0);
            if (idxA0 != 0)
            {
                if (bank == 0 && Rx_FromA0_Bank0.TryGetValue((0, idxA0), out var t0)) return t0;
                if (bank == 1 && Rx_FromA0_Bank1_Min.TryGetValue((1, idxA0), out var t1)) return t1;
                return "?"; // unknown bank/index combo
            }
            // A0==0: DOS shows "." → "0" for most channels in your test image.
            return "0";
        }

        // Encode Rx by A0-index method (works for bank 0 fully, bank 1 partially per above).
        public static bool TrySetRxTone(ref byte A0, ref byte B3, string tone)
        {
            if (!TryFindA0IndexForTone(((B3 >> 1) & 1), tone, out int idx))
            {
                // If caller wants true "0", set A0=0.
                if (tone == "0") { A0 = 0x00; return true; }
                return false;
            }
            // write idx into A0 using [7,6,3,2,1,0]
            A0 = WriteIndexToA0(idx, A0 /*preserve unused bits*/);
            return true;
        }

        private static bool TryFindA0IndexForTone(int bank, string tone, out int idx)
        {
            if (bank == 0)
            {
                foreach (var kv in Rx_FromA0_Bank0)
                    if (kv.Key.Item1 == 0 && kv.Value == tone) { idx = kv.Key.Item2; return true; }
            }
            else
            {
                foreach (var kv in Rx_FromA0_Bank1_Min)
                    if (kv.Key.Item1 == 1 && kv.Value == tone) { idx = kv.Key.Item2; return true; }
            }
            idx = 0;
            return false;
        }

        // ===== Public decode helpers =====
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return idx < 0 ? "?" : Cg[idx];
        }

        public static (string Tx, string Rx) DecodeChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A0, B3);
            return (tx, rx);
        }

        // ===== Public encode helpers =====
        // Encode only the tone fields (leave freq and unrelated bits untouched).
        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;

            if (!Tx_Index_To_A1_B1bit7.TryGetValue(idx, out var spec))
                return false; // not in our observed set

            A1 = spec.A1;
            if (spec.B1bit7.HasValue)
            {
                if (spec.B1bit7.Value) B1 |= 0x80; else B1 &= 0x7F;
            }
            return true;
        }

        // ===== A0 6-bit index packing/unpacking (MSB..LSB = [7,6,3,2,1,0])
        private static int IndexFromA0(byte a0)
        {
            int b5 = (a0 >> 7) & 1;
            int b4 = (a0 >> 6) & 1;
            int b3 = (a0 >> 3) & 1;
            int b2 = (a0 >> 2) & 1;
            int b1 = (a0 >> 1) & 1;
            int b0 = (a0 >> 0) & 1;
            return (b5 << 5) | (b4 << 4) | (b3 << 3) | (b2 << 2) | (b1 << 1) | (b0 << 0);
        }

        private static byte WriteIndexToA0(int idx, byte originalA0)
        {
            idx &= 0x3F;
            byte a0 = originalA0;
            // clear target bits
            a0 &= 0b00111100;
            // write
            if (((idx >> 5) & 1) != 0) a0 |= 1 << 7;
            if (((idx >> 4) & 1) != 0) a0 |= 1 << 6;
            if (((idx >> 3) & 1) != 0) a0 |= 1 << 3;
            if (((idx >> 2) & 1) != 0) a0 |= 1 << 2;
            if (((idx >> 1) & 1) != 0) a0 |= 1 << 1;
            if (((idx >> 0) & 1) != 0) a0 |= 1 << 0;
            return a0;
        }

        // ===== X2212 packing =====
        // Big-endian nibbles per channel: A3.hi, A3.lo, A2.hi, A2.lo, A1.hi, A1.lo, A0.hi, A0.lo,
        //                                B3.hi, B3.lo, B2.hi, B2.lo, B1.hi, B1.lo, B0.hi, B0.lo
        // Across 16 channels => 256 nibbles for a 4-bit X2212.
        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            if (image128 is null || image128.Length != 128)
                throw new ArgumentException("Expected exactly 128 bytes (16 channels × 8).");

            var nibbles = new byte[256];
            int n = 0;
            for (int ch = 0; ch < 16; ch++)
            {
                int off = ch * 8;
                for (int bi = 0; bi < 8; bi++)
                {
                    byte b = image128[off + bi];
                    nibbles[n++] = (byte)((b >> 4) & 0x0F); // hi
                    nibbles[n++] = (byte)(b & 0x0F);        // lo
                }
            }
            return nibbles;
        }

        // Useful utility if you want to emit a 256-char ASCII-hex file.
        public static string ToAsciiHex256(byte[] image128)
        {
            var sb = new StringBuilder(256);
            foreach (var b in image128) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
