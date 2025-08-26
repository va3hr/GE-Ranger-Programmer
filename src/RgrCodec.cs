// RgrCodec.cs — binary/nibble packing + index builder + CG table
// Bytes from .RGR are treated as *binary*. No ASCII parsing anywhere here.
//
// Pipeline this file guarantees:
//   1) Split each of the 8 channel bytes (A3..A0, B3..B0) into big‑endian nibbles.
//   2) Pack those nibbles little‑endian into a single 64‑bit word in original byte order.
//   3) Build 6‑bit indices from explicit bit positions (LSB‑first, 0..63).
//   4) Map indices to the canonical CG table (index 0 = "0").
//
// This file does *not* touch the UI. Call it from MainForm.
// If you already have the six confirmed positions, set TX_BITS/RX_BITS below.

using System;

namespace X2212
{
    public static class RgrCodec
    {
        // Canonical tone table (index 0 is "0")
        public static readonly string[] Cg = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // (1)(2) Pack 8 bytes into 64-bit nibble stream as specified.
        // Nibble #0 (LSN) = A3.Hi, #1 = A3.Lo, #2 = A2.Hi, ... #15 = B0.Lo.
        public static ulong PackNibbleWord(byte A3, byte A2, byte A1, byte A0,
                                           byte B3, byte B2, byte B1, byte B0)
        {
            ulong w = 0UL;
            int shift = 0;

            // Local inline helpers (big-endian nibble split)
            static int Hi(byte b) => (b >> 4) & 0xF;
            static int Lo(byte b) => b & 0xF;

            byte[] bytes = new byte[] { A3, A2, A1, A0, B3, B2, B1, B0 };
            for (int i = 0; i < 8; i++)
            {
                byte b = bytes[i];
                w |= (ulong)Hi(b) << shift; shift += 4;
                w |= (ulong)Lo(b) << shift; shift += 4;
            }
            return w;
        }

        // (3) Extract LSB-first bit from packed nibble word (0..63)
        public static bool GetBitLSB(ulong w, int bitIndex)
        {
            if ((uint)bitIndex > 63) throw new ArgumentOutOfRangeException(nameof(bitIndex));
            return ((w >> bitIndex) & 1UL) != 0;
        }

        // Build 6-bit index from explicit bit positions (LSB-first)
        public static int BuildIndex(ulong w, ReadOnlySpan<int> bitPositions)
        {
            if (bitPositions.Length != 6) throw new ArgumentException("Need 6 bit positions");
            int idx = 0;
            for (int i = 0; i < 6; i++)
            {
                if (GetBitLSB(w, bitPositions[i])) idx |= (1 << i);
            }
            return idx;
        }

        // (4) Optional: provide the confirmed 6 positions for each side here (LSB-first 0..63).
        // Replace these placeholders with your empirically-validated mapping.
        public static readonly int[] TX_BITS = new int[6] { 2, 9, 21, 26, 38, 41 }; // TODO: replace with your proven positions
        public static readonly int[] RX_BITS = new int[6] { 3, 8, 20, 27, 36, 43 }; // TODO: replace with your proven positions
    }
}