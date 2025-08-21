// RgrCodec.cs — read/write 128-byte RGR blocks, apply screen↔file permutation,
// and use ToneLock to decode/encode tones (RANGR6M2 personality).

using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RangrApp.Locked
{
    public static class RgrCodec
    {
        // Screen→File permutation for RANGR6M2 (0-based)
        // (1:7,2:3,3:1,4:4,5:2,6:5,7:6,8:8,9:15,10:9,11:10,12:12,13:14,14:11,15:13,16:16)
        private static readonly int[] ScreenToFile = { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };

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

        public static void
