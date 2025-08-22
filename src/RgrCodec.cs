// RgrCodec.cs
// Namespace intentionally omitted to match project default compile (top-level types)
// If you have a namespace, place this file inside it consistently with the rest of the project.

using System;
using System.Collections.Generic;
using System.Globalization;

public static class RgrCodec
{
    // --- Nested type expected by MainForm.cs ---
    // Keep it very light; UI binds to these fields/properties.
    public class Channel
    {
        public int Index { get; set; }                 // 1..16 for display
        public double TxMHz { get; set; }
        public double RxMHz { get; set; }
        public string TxTone { get; set; } = "0";      // display string, "0" means no tone
        public string RxTone { get; set; } = "0";      // display string, "0" means no tone
        public int Cct { get; set; }
        public bool Ste { get; set; }
        public string Hex { get; set; } = "";          // raw 8-byte cell as spaced hex
        // Optional: raw bytes in big-endian nibble order
        public byte[]? Raw { get; set; }
    }

    // --- Helpers to format / parse ---
    public static string BytesToHex(ReadOnlySpan<byte> bytes)
    {
        char[] c = new char[bytes.Length * 3 - 1];
        int p = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            c[p++] = GetHexChar(b >> 4);
            c[p++] = GetHexChar(b & 0xF);
            if (i != bytes.Length - 1) c[p++] = ' ';
        }
        return new string(c);
    }

    private static char GetHexChar(int v) => (char)(v < 10 ? '0' + v : 'A' + (v - 10));

    // --- No-op stubs kept for compatibility with existing calls in MainForm.cs ---
    // If MainForm calls into these to extract pieces, keep signatures stable.

    // Return the 8 bytes (64 bits) that belong to channel 'ch' (0-based) from a 128-byte image.
    public static byte[] ReadChannelBlock(byte[] image, int chZeroBased)
    {
        // Safety
        if (image == null) throw new ArgumentNullException(nameof(image));
        if (image.Length < 8 * 16) throw new ArgumentException("RGR image must be at least 128 bytes.");
        if (chZeroBased < 0 || chZeroBased >= 16) throw new ArgumentOutOfRangeException(nameof(chZeroBased));

        // Layout is 16 channels × 8 bytes each, contiguous; caller already accounts for endianness.
        // If your format differs, adjust here.
        var block = new byte[8];
        Buffer.BlockCopy(image, chZeroBased * 8, block, 0, 8);
        return block;
    }

    // Placeholder decode that only fills Hex; the UI will use ToneLock/FreqLock/CctLock to populate other fields.
    public static Channel MakeChannelFromBlock(int chOneBased, byte[] block)
    {
        return new Channel
        {
            Index = chOneBased,
            Hex = BytesToHex(block),
            Raw = (byte[])block.Clone()
        };
    }
}
