// RxToneCodec_AC37.cs
// Bank-specific RX tone codec for (A1=0xAC, A2=0x37).
// Derives RX index (0..32 where 33=210.7 handled separately) from A0 via an affine GF(2) transform.
// Keeps TX and RX strictly separate. Frequency code untouched.
//
// Big-endian reminder: rows are [A3 A2 A1 A0 B3 B2 B1 B0].
// This codec only consults A1,A2,A0 for RX; it never modifies TX bytes.

using System;

namespace X2212
{
    public static class RxToneCodec_AC37
    {
        // M (6x8) and C (6) for y = M·x + C (mod 2), where:
        //   x = A0 bits [b7..b0] (MSB-first),
        //   y = index bits [bit0..bit5] (LSB-first).
        private static readonly byte[,] M = new byte[6,8]
        {
            //  b7 b6 b5 b4 b3 b2 b1 b0
            { 1, 0, 0, 1, 1, 0, 0, 0 }, // bit0
            { 1, 1, 0, 1, 1, 0, 0, 0 }, // bit1
            { 0, 1, 0, 1, 1, 0, 0, 0 }, // bit2
            { 0, 1, 0, 1, 1, 1, 0, 0 }, // bit3
            { 0, 0, 0, 0, 1, 0, 0, 0 }, // bit4
            { 0, 0, 0, 0, 0, 0, 0, 0 }, // bit5 (unused -> always 0 here)
        };
        private static readonly byte[] C = new byte[6] { 0,0,0,0,0,0 };

        // Decode RX index from raw row bytes; returns 0..33 (33 means 210.7), or -1 if not this bank.
        public static int DecodeIndex(byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, byte b1, byte b0)
        {
            // This model is specific to the bank (A1=0xAC, A2=0x37). Refuse otherwise.
            if (a1 != 0xAC || a2 != 0x37)
                return -1;

            // Special case seen in your data: A0==0 maps to index 0 ('.')
            if (a0 == 0x00) return 0;

            // Affine transform y = M·x + C (mod 2)
            int idx = 0;
            for (int bit = 0; bit < 6; bit++)
            {
                int acc = C[bit];
                for (int k = 0; k < 8; k++)
                {
                    int xk = ((a0 >> (7-k)) & 1);
                    acc ^= (M[bit,k] & xk);
                }
                idx |= (acc & 1) << bit;
            }
            return idx; // 1..31 (as observed); index 33 (210.7) uses a different A2.
        }

        // Encode RX index back into (A0) for this bank. Returns true if encoded; false if not this bank or idx out of range.
        public static bool TryEncodeIndex(int idx, byte a3, byte a2, byte a1, ref byte a0, byte b3, byte b2, byte b1, byte b0)
        {
            if (a1 != 0xAC || a2 != 0x37) return false;
            if (idx == 0) { a0 = 0x00; return true; }
            if (idx < 0 || idx > 31) return false; // 33(210.7) not in this bank

            // Solve M·x = y (mod 2) for x (8 bits). Free variables -> 0.
            // Build augmented matrix and do simple Gauss elimination over GF(2).
            byte[,] A = new byte[6,9]; // 8 cols for x, 1 for RHS
            for (int r=0;r<6;r++)
            {
                for (int c=0;c<8;c++) A[r,c] = M[r,c];
                A[r,8] = (byte)(((idx >> r) & 1) ^ C[r]);
            }

            int row = 0;
            for (int col=0; col<8 && row<6; col++)
            {
                int pivot = -1;
                for (int r=row; r<6; r++) if (A[r,col]==1) { pivot=r; break; }
                if (pivot<0) continue;
                if (pivot!=row)
                {
                    for (int c=col;c<9;c++){ byte t=A[row,c]; A[row,c]=A[pivot,c]; A[pivot,c]=t; }
                }
                // eliminate others
                for (int r=0;r<6;r++)
                {
                    if (r==row) continue;
                    if (A[r,col]==1)
                    {
                        for (int c=col;c<9;c++) A[r,c] ^= A[row,c];
                    }
                }
                row++;
            }

            // Back-substitute choosing zeros for any free columns.
            byte[] x = new byte[8];
            for (int r=5;r>=0;r--)
            {
                // find first 1 in row
                int lead = -1;
                for (int c=0;c<8;c++){ if (A[r,c]==1){ lead=c; break; } }
                if (lead<0) continue; // all zero row
                byte acc = A[r,8];
                for (int c=lead+1;c<8;c++) if (A[r,c]==1) acc ^= x[c];
                x[lead] = acc;
            }

            // Pack MSB..LSB into a0
            byte a0new = 0;
            for (int k=0;k<8;k++) a0new |= (byte)(x[k] << (7-k));
            a0 = a0new;
            return true;
        }
    }
}
