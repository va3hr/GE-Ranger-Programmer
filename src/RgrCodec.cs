// RgrCodec.cs — binary/nibble packing + index builder + CG table (POSITIONS BAKED)
// Bytes from .RGR are treated as *binary*. No ASCII parsing.
// Nibble packing per project rule:
//   • Split each of the 8 channel bytes (A3..A0, B3..B0) into big‑endian nibbles.
//   • Pack those nibbles little‑endian into a 64‑bit word in original byte order.
// Bit positions below are LSB‑first (0..63) on that packed word.

using System;

public static class RgrCodec
{
    // Canonical tone table (index 0 = "0")
    public static readonly string[] Cg = new string[]
    {
        "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
        "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
        "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
        "179.9","186.2","192.8","203.5","210.7"
    };

    // (1)(2) Pack 8 bytes into 64‑bit nibble stream (little‑endian by nibble).
    // Nibble #0 (LSN) = A3.Hi, #1 = A3.Lo, #2 = A2.Hi, ... #15 = B0.Lo.
    public static ulong PackNibbleWord(byte A3, byte A2, byte A1, byte A0,
                                       byte B3, byte B2, byte B1, byte B0)
    {
        ulong w = 0UL; int shift = 0;
        static int Hi(byte b) => (b >> 4) & 0xF;
        static int Lo(byte b) =>  b        & 0xF;
        byte[] bytes = new byte[] { A3, A2, A1, A0, B3, B2, B1, B0 };
        for (int i = 0; i < 8; i++)
        {
            byte b = bytes[i];
            w |= (ulong)Hi(b) << shift; shift += 4;
            w |= (ulong)Lo(b) << shift; shift += 4;
        }
        return w;
    }

    // (3) LSB‑first bit from packed word (0..63)
    public static bool GetBitLSB(ulong w, int bitIndex)
    {
        if ((uint)bitIndex > 63) throw new ArgumentOutOfRangeException(nameof(bitIndex));
        return ((w >> bitIndex) & 1UL) != 0;
    }

    // Build 6‑bit index from explicit positions (bit0..bit5).
    public static int BuildIndex(ulong w, ReadOnlySpan<int> bitPositions)
    {
        if (bitPositions.Length != 6) throw new ArgumentException("Need 6 bit positions");
        int idx = 0;
        for (int i = 0; i < 6; i++)
            if (GetBitLSB(w, bitPositions[i])) idx |= (1 << i);
        return idx;
    }

    // === BAKED proven mappings (LSB‑first positions on packed word) ===
    // TX index bits:   bit0..bit5 <= [B2.2, B3.6, A0.0, A1.3, A2.5, A3.1]
    // RX index bits:   bit0..bit5 <= [B2.3, B3.5, A0.1, A1.2, A2.4, A3.0]
    public static readonly int[] TX_BITS = new int[6] { 46, 34, 28, 23, 9, 5 };
    

// Convenience: compute indices and tone strings directly from 8 bytes.
public static void GetTxRxFromBytes(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out int txIndex, out int rxIndex,
                                    out string txTone, out string rxTone)
{
    ulong w = PackNibbleWord(A3, A2, A1, A0, B3, B2, B1, B0);
    txIndex = BuildIndex(w, TX_BITS);
    rxIndex = BuildIndex(w, RX_BITS);
    if ((uint)txIndex >= (uint)Cg.Length) txIndex = 0;
    if ((uint)rxIndex >= (uint)Cg.Length) rxIndex = 0;
    txTone = Cg[txIndex];
    rxTone = Cg[rxIndex];
}

    public static readonly int[] RX_BITS = new int[6] { 47, 33, 29, 22, 8, 4 };
}