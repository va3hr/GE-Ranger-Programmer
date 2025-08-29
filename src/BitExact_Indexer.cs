// BitExact_Indexer — six-bit windows for RX/TX (no endianness for tones)
public static class BitExact_Indexer
{
    // RX index (A3 only): i5..i0 ← [ A3.6 , A3.7 , A3.0 , A3.1 , A3.2 , A3.3 ]
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

    // TX index (revised): the same six physical bits as before, but reversed bit-order into the index.
    // i5..i0 ← [ B3.0 , B3.1 , B3.2 , B3.3 , B2.2 , B0.4 ]
    // Rationale: For RANGR6M2.RGR, code=10 (001010b) becomes index=20 (010100b) → 131.8 (correct);
    //            code=15 (001111b) becomes index=60 (111100b) → >33 → Err (matches DOS 114.1 non‑canonical).
    public static int TxIndex(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
        int i5 = (B3 >> 0) & 1;
        int i4 = (B3 >> 1) & 1;
        int i3 = (B3 >> 2) & 1;
        int i2 = (B3 >> 3) & 1;
        int i1 = (B2 >> 2) & 1;
        int i0 = (B0 >> 4) & 1;
        return (i5<<5) | (i4<<4) | (i3<<3) | (i2<<2) | (i1<<1) | i0;
    }
}
