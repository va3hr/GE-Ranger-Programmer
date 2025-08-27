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

    // TX index: i5..i0 ← [ B0.4 , B2.2 , B3.3 , B3.2 , B3.1 , B3.0 ]
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
