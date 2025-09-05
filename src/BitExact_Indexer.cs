// BitExact_Indexer — TX index from B0 only; RX unchanged
namespace RangrApp.Locked
{
public static class BitExact_Indexer
{
    // RX index (per your A3 window): i5..i0 ← [ A3.6 , A3.7 , A3.0 , A3.1 , A3.2 , A3.3 ]
    public static int RxIndex(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
int BitWindow = 0;
BitWindow |= ((A3 >> 6) & 1) << 5;  // i5 into bit 5
BitWindow |= ((A3 >> 7) & 1) << 4;  // i4 into bit 4
BitWindow |= ((A3 >> 0) & 1) << 3;  // i3 into bit 3
BitWindow |= ((A3 >> 1) & 1) << 2;  // i2 into bit 2
BitWindow |= ((A3 >> 2) & 1) << 1;  // i1 into bit 1
BitWindow |= ((A3 >> 3) & 1) << 0;  // i0 into bit 0
return BitWindow;




        
    //    int i5 = (A3 >> 6) & 1;
    //    int i4 = (A3 >> 7) & 1;
   //     int i3 = (A3 >> 0) & 1;
    //    int i2 = (A3 >> 1) & 1;
   //     int i1 = (A3 >> 2) & 1;
    //    int i0 = (A3 >> 3) & 1;
    //    return (i5<<5) | (i4<<4) | (i3<<3) | (i2<<2) | (i1<<1) | i0;
    }

    // TX index: i5..i0 ← [ B0.7 , B0.6 , B0.5 , B0.4 , B0.3 , B0.2 ]  (upper-six bits only)
    // This respects ZEROALL (all zeros in B0) and keeps tones fully canonical.
    public static int TxIndex(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
        int i5 = (B0 >> 7) & 1;
        int i4 = (B0 >> 6) & 1;
        int i3 = (B0 >> 5) & 1;
        int i2 = (B0 >> 4) & 1;
        int i1 = (B0 >> 3) & 1;
        int i0 = (B0 >> 2) & 1;
        return (i5<<5) | (i4<<4) | (i3<<3) | (i2<<2) | (i1<<1) | i0;
    }
}
}

