
// RgrCodec.cs — read/write 128-byte RGR & tone decode wired directly to ToneLock.DecodeChannel
// IMPORTANT: Frequencies are NOT changed. TX/RX kept separate. No cache dependency.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace RangrApp.Locked
{
    public struct ChannelData
    {
        public byte A3, A2, A1, A0, B3, B2, B1, B0;
        public string TxTone, RxTone; // resolved via ToneLock.Cg[index]
    }

    public static class RgrCodec
    {
        // Screen→File permutation that matches your DOS view (0-based)
        private static readonly int[] ScreenToFile =
            new int[] { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };

        // ===== I/O ============================================================
        public static byte[] LoadAsciiHex(string path)
        {
            var raw = File.ReadAllBytes(path);
            // Accept either 128-byte binary or 256 ASCII-hex characters (whitespace ok)
            if (raw.Length == 128) return raw;

            string s = File.ReadAllText(path);
            var sb = new StringBuilder(256);
            foreach (char c in s)
            {
                if ((c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F'))
                    sb.Append(c);
            }
            if (sb.Length != 256)
                throw new InvalidDataException($""Expected 128 bytes or 256 ASCII-hex chars; got {raw.Length} bytes / {sb.Length} hex chars."");

            var image = new byte[128];
            for (int i = 0; i < 128; i++)
            {
                image[i] = byte.Parse(sb.ToString(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return image;
        }

        public static void SaveAsciiHex(string path, byte[] image128)
        {
            File.WriteAllText(path, ToneLock.ToAsciiHex256(image128));
        }

        // ===== Channel access (screen order 1..16) ============================
        public static ChannelData GetScreenChannel(byte[] image128, int screenCh1to16)
        {
            int fileIdx = ScreenToFile[screenCh1to16 - 1];
            int off = fileIdx * 8;

            ChannelData cd = new ChannelData
            {
                A3 = image128[off + 0],
                A2 = image128[off + 1],
                A1 = image128[off + 2],
                A0 = image128[off + 3],
                B3 = image128[off + 4],
                B2 = image128[off + 5],
                B1 = image128[off + 6],
                B0 = image128[off + 7]
            };

            // **Direct decode** — no cache dependency, passes all 8 bytes
            var (tx, rx) = ToneLock.DecodeChannel(cd.A3, cd.A2, cd.A1, cd.A0, cd.B3, cd.B2, cd.B1, cd.B0);
            cd.TxTone = tx;
            cd.RxTone = rx;
            return cd;
        }

        public static void SetScreenChannel(byte[] image128, int screenCh1to16, ChannelData v)
        {
            int fileIdx = ScreenToFile[screenCh1to16 - 1];
            int off = fileIdx * 8;
            image128[off + 0] = v.A3; image128[off + 1] = v.A2;
            image128[off + 2] = v.A1; image128[off + 3] = v.A0;
            image128[off + 4] = v.B3; image128[off + 5] = v.B2;
            image128[off + 6] = v.B1; image128[off + 7] = v.B0;
        }

        // Optional: set tones by name (encode path is currently a no-op in ToneLock on purpose)
        public static bool TrySetTones(byte[] image128, int screenCh1to16, string txTone, string rxTone)
        {
            int fileIdx = ScreenToFile[screenCh1to16 - 1];
            int off = fileIdx * 8;

            byte A3 = image128[off + 0], A2 = image128[off + 1], A1 = image128[off + 2], A0 = image128[off + 3];
            byte B3 = image128[off + 4], B2 = image128[off + 5], B1 = image128[off + 6], B0 = image128[off + 7];

            bool ok1 = ToneLock.TrySetTxTone(ref A1, ref B1, txTone);
            bool ok2 = ToneLock.TrySetRxTone(ref A3, ref A2, ref B3, rxTone);
            if (!(ok1 && ok2)) return false;

            image128[off + 0] = A3; image128[off + 1] = A2;
            image128[off + 2] = A1; image128[off + 3] = A0;
            image128[off + 4] = B3; image128[off + 5] = B2;
            image128[off + 6] = B1; image128[off + 7] = B0;
            return true;
        }

        // For callers that expect this passthrough
        public static byte[] ToX2212Nibbles(byte[] image128) => ToneLock.ToX2212Nibbles(image128);
    }
}
