// RgrCodec.cs — read/write 128-byte RGR blocks, apply screen↔file permutation,
// and use ToneLock to decode/encode tones (RANGR6M2 personality).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RangrApp.Locked
{
    public static class RgrCodec
    {
        // Screen↔file channel permutation for RANGR6M2 (1-based in docs → 0-based here)
        private static readonly int[] ScreenToFile = { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };
        private static readonly int[] FileToScreen = Enumerable.Range(0, 16).Select(i => Array.IndexOf(ScreenToFile, i)).ToArray();

        public static byte[] LoadAsciiHex(string path)
        {
            var hex = new string(File.ReadAllText(path).Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (hex.Length < 256) throw new InvalidDataException("Expected 256 hex chars.");
            hex = hex.Substring(hex.Length - 256, 256);
            var bytes = new byte[128];
            for (int i = 0; i < 128; i++)
                bytes[i] = byte.Parse(hex.AsSpan(2 * i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return bytes;
        }

        public static void SaveAsciiHex(string path, byte[] image128)
        {
            File.WriteAllText(path, ToneLock.ToAsciiHex256(image128));
        }

        public static (byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0) GetScreenChannel(byte[] image128, int screenCh1to16)
        {
            int fileIdx = ScreenToFile[screenCh1to16 - 1];
            int off = fileIdx * 8;
            return (image128[off + 0], image128[off + 1], image128[off + 2], image128[off + 3],
                    image128[off + 4], image128[off + 5], image128[off + 6], image128[off + 7]);
        }

        public static void SetScreenChannel(byte[] image128, int screenCh1to16, (byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0) v)
        {
            int fileIdx = ScreenToFile[screenCh1to16 - 1];
            int off = fileIdx * 8;
            image128[off + 0] = v.A3; image128[off + 1] = v.A2; image128[off + 2] = v.A1; image128[off + 3] = v.A0;
            image128[off + 4] = v.B3; image128[off + 5] = v.B2; image128[off + 6] = v.B1; image128[off + 7] = v.B0;
        }

        // ===== High-level helpers =====

        public static (string Tx, string Rx) DecodeTones(byte[] image128, int screenCh1to16)
        {
            var c = GetScreenChannel(image128, screenCh1to16);
            return ToneLock.DecodeChannel(c.A3, c.A2, c.A1, c.A0, c.B3, c.B2, c.B1, c.B0);
        }

        public static bool TryEncodeTones(byte[] image128, int screenCh1to16, string txTone, string rxTone)
        {
            var c = GetScreenChannel(image128, screenCh1to16);

            // TX
            byte A1 = c.A1, B1 = c.B1;
            if (!ToneLock.TrySetTxTone(ref A1, ref B1, txTone)) return false;

            // RX (bank preserved from existing B3)
            byte A0 = c.A0, B3 = c.B3;
            if (!ToneLock.TrySetRxTone(ref A0, ref B3, rxTone)) return false;

            // write back, preserving untouched bytes
            SetScreenChannel(image128, screenCh1to16, (c.A3, c.A2, A1, A0, B3, c.B2, B1, c.B0));
            return true;
        }
    }
}

}
