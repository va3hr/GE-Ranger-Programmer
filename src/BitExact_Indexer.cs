// Single-file drop‑in: confirmed 6‑bit windows for RX and TX (no endianness in tone paths)
// Keep your frequency code separate (endianness applies there only).
// Follow‑TX remains DISABLED during debugging; UI shows RX independently.

public static class BitExact_Indexer
{
    // RX index (A3 only): i5..i0 ← [ A3.6 , A3.7 , A3.0 , A3.1 , A3.2 , A3.3 ]
    // This matches prior confirmations (e.g., 210.7 Hz → idx 24).
    public static int RxIndex(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
        int i5 = (A3 >> 6) & 1;
        int i4 = (A3 >> 7) & 1;
        int i3 = (A3 >> 0) & 1;
        int i2 = (A3 >> 1) & 1;
        int i1 = (A3 >> 2) & 1;
        int i0 = (A3 >> 3) & 1;
        return (i5<<5) | (i4<<4) | (i3<<3) | (i2<<2) | (i1<<1) | i0;
    }

    // RX index with follow (kept OFF for now): return raw RX index to keep debugging clear.
    public static int RxIndexWithFollow(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
        // If/when you re‑enable follow: if (RxIndex(...) == 0 && (B3 & 0x01) == 1) → derived from TX.
        return RxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
    }

    // TX index window (validated on your TX1_* fixtures):
    // i5..i0 ← [ B0.4 , B2.2 , B3.3 , B3.2 , B3.1 , B3.0 ]
    // Examples (CH15): 67.0→1, 100.0→9, 114.8→11, 85.4→22, 186.2→48.
    public static int TxIndex(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
        int i5 = (B0 >> 4) & 1;
        int i4 = (B2 >> 2) & 1;
        int i3 = (B3 >> 3) & 1;
        int i2 = (B3 >> 2) & 1;
        int i1 = (B3 >> 1) & 1;
        int i0 = (B3 >> 0) & 1;
        return (i5<<5) | (i4<<4) | (i3<<3) | (i2<<2) | (i1<<1) | i0;
    }
}
