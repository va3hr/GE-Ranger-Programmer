using System;

public static class RgrCodec
{
    // ---- Tone table is in ToneLock.Cg (already in your repo) ----
    // This class ONLY computes the indices correctly (big-endian, non‑contiguous)
    // and returns the corresponding strings. No UI writes, no side effects.

    // Primary overload used by MainForm: eight channel bytes A3..B0
    public static void DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0,
        out string txText, out string rxText, out int txIndex, out int rxIndex)
    {
        // Build TX index from confirmed big-endian picks (MSB-first bit numbering 7..0)
        txIndex =
            (BigEndian.BitMsb(A3, 1) ? (1 << 5) : 0) |
            (BigEndian.BitMsb(A2, 5) ? (1 << 4) : 0) |
            (BigEndian.BitMsb(A1, 3) ? (1 << 3) : 0) |
            (BigEndian.BitMsb(A0, 0) ? (1 << 2) : 0) |
            (BigEndian.BitMsb(B3, 6) ? (1 << 1) : 0) |
            (BigEndian.BitMsb(B2, 2) ? (1 << 0) : 0);

        // Build RX plain index (no Follow rule applied here per project decision)
        int rxIndexPlain =
            (BigEndian.BitMsb(A3, 0) ? (1 << 5) : 0) |
            (BigEndian.BitMsb(A2, 4) ? (1 << 4) : 0) |
            (BigEndian.BitMsb(A1, 2) ? (1 << 3) : 0) |
            (BigEndian.BitMsb(A0, 1) ? (1 << 2) : 0) |
            (BigEndian.BitMsb(B3, 5) ? (1 << 1) : 0) |
            (BigEndian.BitMsb(B2, 3) ? (1 << 0) : 0);

        rxIndex = rxIndexPlain;

        // Map indices to strings using canonical table.
        // Guard against out-of-range to avoid blanking UI boxes.
        txText = (txIndex >= 0 && txIndex < ToneLock.Cg.Length) ? ToneLock.Cg[txIndex] : "0";
        rxText = (rxIndex >= 0 && rxIndex < ToneLock.Cg.Length) ? ToneLock.Cg[rxIndex] : "0";
    }

    // Convenience tuple-returning overload
    public static (string txText, string rxText, int txIndex, int rxIndex) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        DecodeChannel(A3, A2, A1, A0, B3, B2, B1, B0, out var tx, out var rx, out var tIdx, out var rIdx);
        return (tx, rx, tIdx, rIdx);
    }

    // Array-based overload (expects ch8 ordered A3,A2,A1,A0,B3,B2,B1,B0)
    public static (string txText, string rxText, int txIndex, int rxIndex) DecodeChannel(byte[] ch8)
    {
        if (ch8 == null || ch8.Length < 8) throw new ArgumentException("ch8 must contain 8 bytes ordered A3..B0");
        return DecodeChannel(ch8[0], ch8[1], ch8[2], ch8[3], ch8[4], ch8[5], ch8[6], ch8[7]);
    }

    // Utility kept here because some older code paths referenced it.
    // Converts up to 256 bytes to continuous ASCII-hex (without spaces).
    public static string ToAsciiHex256(ReadOnlySpan<byte> data)
    {
        int n = Math.Min(256, data.Length);
        char[] buf = new char[n * 2];
        int j = 0;
        for (int i = 0; i < n; i++)
        {
            byte b = data[i];
            int hi = (b >> 4) & 0xF;
            int lo = b & 0xF;
            buf[j++] = (char)(hi < 10 ? '0' + hi : 'A' + (hi - 10));
            buf[j++] = (char)(lo < 10 ? '0' + lo : 'A' + (lo - 10));
        }
        return new string(buf);
    }
}
