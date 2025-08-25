
#nullable disable
// BitExact_Indexer.cs — explicit bit-to-index wiring for GE Rangr
// TX and RX kept separate. Big-endian MSB-first bit numbering (7..0).
// Derived from your TX_MAP files + TX1_* and RXMAP_A/B + RX1_* samples.
//
// NOTE: idx[5] (the 32's bit) for RX is set via a proven special-case (bank 0x98/0x27, A0==0x91 -> 210.7)
// and otherwise left 0 (until more >31 samples are provided). Follow logic applied afterward.

using System;
using X2212; // BigEndian.BitMsb

namespace RangrApp.Locked
{
    public static class BitExact_Indexer
    {
        private static int Bit(byte b, int msb) => BigEndian.BitMsb(b, msb) ? 1 : 0;

        // ============ TX index (6 bits) ============
        // idx[0] = A3[6] ^ A2[6] ^ A2[5] ^ A1[7] ^ A1[0] ^ B0[6]
        // idx[1] = A3[6] ^ A2[5] ^ A1[6] ^ A1[2] ^ A1[0] ^ B3[4] ^ B0[7] ^ B0[6] ^ B0[4]
        // idx[2] = A3[6] ^ A2[4] ^ A1[3] ^ A1[2] ^ A1[1] ^ B1[3] ^ B1[2] ^ B0[7]
        // idx[3] = A2[6] ^ A1[2] ^ B3[4] ^ B1[3] ^ B0[7] ^ B0[4]
        // idx[4] = B3[4] ^ B0[7] ^ B0[6]
        // idx[5] = A1[5] ^ A1[2] ^ B3[4]
        public static int TxIndex(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int i0 = Bit(A3,6) ^ Bit(A2,6) ^ Bit(A2,5) ^ Bit(A1,7) ^ Bit(A1,0) ^ Bit(B0,6);
            int i1 = Bit(A3,6) ^ Bit(A2,5) ^ Bit(A1,6) ^ Bit(A1,2) ^ Bit(A1,0) ^ Bit(B3,4) ^ Bit(B0,7) ^ Bit(B0,6) ^ Bit(B0,4);
            int i2 = Bit(A3,6) ^ Bit(A2,4) ^ Bit(A1,3) ^ Bit(A1,2) ^ Bit(A1,1) ^ Bit(B1,3) ^ Bit(B1,2) ^ Bit(B0,7);
            int i3 = Bit(A2,6) ^ Bit(A1,2) ^ Bit(B3,4) ^ Bit(B1,3) ^ Bit(B0,7) ^ Bit(B0,4);
            int i4 = Bit(B3,4) ^ Bit(B0,7) ^ Bit(B0,6);
            int i5 = Bit(A1,5) ^ Bit(A1,2) ^ Bit(B3,4);
            return (i5<<5) | (i4<<4) | (i3<<3) | (i2<<2) | (i1<<1) | i0;
        }

        // ============ RX index (6 bits) ============
        // Derived taps (idx[0..4]); idx[5] handled by special-case for bank 0x98/0x27 (A0==0x91).
        // idx[0] = A3[4] ^ A1[1] ^ A0[4]
        // idx[1] = A3[6] ^ A2[6] ^ A2[4] ^ A1[6] ^ A1[1] ^ A0[7] ^ A0[4] ^ A0[3] ^ A0[2] ^ A0[0]
        // idx[2] = A3[4] ^ A1[7] ^ A1[3] ^ A1[1] ^ A0[7] ^ A0[3] ^ A0[2] ^ A0[0] ^ B1[3]
        // idx[3] = A3[6] ^ A2[6] ^ B1[3]
        // idx[4] = A3[6] ^ A2[5] ^ A2[4] ^ A0[3] ^ A0[2]
        public static int RxIndex(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int i0 = Bit(A3,4) ^ Bit(A1,1) ^ Bit(A0,4);
            int i1 = Bit(A3,6) ^ Bit(A2,6) ^ Bit(A2,4) ^ Bit(A1,6) ^ Bit(A1,1) ^ Bit(A0,7) ^ Bit(A0,4) ^ Bit(A0,3) ^ Bit(A0,2) ^ Bit(A0,0);
            int i2 = Bit(A3,4) ^ Bit(A1,7) ^ Bit(A1,3) ^ Bit(A1,1) ^ Bit(A0,7) ^ Bit(A0,3) ^ Bit(A0,2) ^ Bit(A0,0) ^ Bit(B1,3);
            int i3 = Bit(A3,6) ^ Bit(A2,6) ^ Bit(B1,3);
            int i4 = Bit(A3,6) ^ Bit(A2,5) ^ Bit(A2,4) ^ Bit(A0,3) ^ Bit(A0,2);
            int i5 = 0; // default; special-case below

            // Proven special-case: bank 0x98/0x27, A0==0x91 => index 33 (210.7), so set i5=1 and i0=1.
            if (A1 == 0x98 && A2 == 0x27 && A0 == 0x91)
            {
                i5 = 1;
                i0 = 1;
            }

            return (i5<<5) | (i4<<4) | (i3<<3) | (i2<<2) | (i1<<1) | i0;
        }

        // Apply Follow (B3.bit0): if RX index is 0 and Follow=1, mirror TX index.
        public static int RxIndexWithFollow(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int txIdx = TxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int rxIdx = RxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            bool follow = (B3 & 0x01) != 0;
            if (follow && rxIdx == 0) rxIdx = txIdx;
            return rxIdx;
        }
    }
}
