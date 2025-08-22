// RgrCodec.cs — read/write 128-byte RGR, screen↔file permutation,
// and tone decode via ToneLock using project-memory rules.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace RangrApp.Locked
{
    public static class RgrCodec
    {
        // Screen→File permutation that matches your DOS view (0-based)
        private static readonly int[] ScreenToFile =
            new int[] { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };

        public struct ChannelData
        {
            public byte A3, A2, A1, A0, B3, B2, B1, B0;
        }

        public static byte[] LoadAsciiHex(string path)
        {
            string raw = File.ReadAllText(path);
            StringBuilder hexChars = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (!char.IsWhiteSpace(c)) hexChars.Append(c);
            }
            string hex = hexChars.ToString();
            if (hex.Length < 256) throw new InvalidDataException("Expected at least 256 hex chars.");
            hex = hex.Substring(hex.Length - 256, 256);

            byte[] bytes = new byte[128];
            for (int i = 0; i < 128; i++)
                bytes[i] = byte.Parse(hex.Substring(2 * i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return bytes;
        }

        public static void SaveAsciiHex(string path, byte[] image128)
        {
            File.WriteAllText(path, ToneLock.ToAsciiHex256(image128));
        }

        public static ChannelData GetScreenChannel(byte[] image128, int screenCh1to16)
        {
            int fileIdx = ScreenToFile[screenCh1to16 - 1];
            int off = fileIdx * 8;

            ChannelData cd = new ChannelData();
            cd.A3 = image128[off + 0]; cd.A2 = image128[off + 1];
            cd.A1 = image128[off + 2]; cd.A0 = image128[off + 3];
            cd.B3 = image128[off + 4]; cd.B2 = image128[off + 5];
            cd.B1 = image128[off + 6]; cd.B0 = image128[off + 7];

            // Make those 8 bytes available to ToneLock's legacy shims
            ToneLock.SetLastChannel(cd.A3, cd.A2, cd.A1, cd.A0, cd.B3, cd.B2, cd.B1, cd.B0);
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

            ToneLock.SetLastChannel(v.A3, v.A2, v.A1, v.A0, v.B3, v.B2, v.B1, v.B0);
        }

        public static void DecodeTones(byte[] image128, int screenCh1to16, out string tx, out string rx)
        {
            ChannelData c = GetScreenChannel(image128, screenCh1to16);
            var pair = ToneLock.DecodeChannel(c.A3, c.A2, c.A1, c.A0, c.B3, c.B2, c.B1, c.B0);
            tx = pair.Tx; rx = pair.Rx;
        }

        public static bool TryEncodeTones(byte[] image128, int screenCh1to16, string txTone, string rxTone)
        {
            ChannelData c = GetScreenChannel(image128, screenCh1to16);

            // TX (A1, B1)
            byte A1 = c.A1, B1 = c.B1;
            if (!ToneLock.TrySetTxTone(ref A1, ref B1, txTone)) return false;

            // RX (A3/A2 with bank in B3)
            byte A3 = c.A3, A2 = c.A2, B3 = c.B3;
            if (!ToneLock.TrySetRxTone(ref A3, ref A2, ref B3, rxTone)) return false;

            c.A1 = A1; c.B1 = B1;
            c.A3 = A3; c.A2 = A2; c.B3 = B3;
            SetScreenChannel(image128, screenCh1to16, c);
            return true;
        }

        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            return ToneLock.ToX2212Nibbles(image128);
        }
    }
}
