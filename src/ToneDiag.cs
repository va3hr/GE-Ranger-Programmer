// src/ToneDiag.cs
// Human-readable diagnostic line builder (exact, no hard-coded windows in MainForm).
// Namespace matches ToneLock: RangrApp.Locked.
// Uses the current TX/RX window mapping explicitly here to avoid namespace drift.

using System;

namespace RangrApp.Locked
{
    public static class ToneDiag
    {
        private static string Hex2(byte b) => b.ToString("X2");

        // Current TX window (MSB→LSB): i5..i0 = [B2.0, B3.1, B2.2, B3.0, B2.4, B2.6]
        private static readonly string[] TxSourceNames = new[] { "B2.0","B3.1","B2.2","B3.0","B2.4","B2.6" };
        // RX window (MSB→LSB): i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        private static readonly string[] RxSourceNames = new[] { "A3.6","A3.7","A3.0","A3.1","A3.2","A3.3" };

        public static string FormatRow(
            int rowNumber,
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            int bank)
        {
            string bytesBlock =
                $"{Hex2(A3)} {Hex2(A2)} {Hex2(A1)} {Hex2(A0)}  {Hex2(B3)} {Hex2(B2)} {Hex2(B1)} {Hex2(B0)}";

            // RX bits and index
            int[] rxBits = new int[6];
            rxBits[0] = (A3 >> 6) & 1; // A3.6 -> i5
            rxBits[1] = (A3 >> 7) & 1; // A3.7 -> i4
            rxBits[2] = (A3 >> 0) & 1; // A3.0 -> i3
            rxBits[3] = (A3 >> 1) & 1; // A3.1 -> i2
            rxBits[4] = (A3 >> 2) & 1; // A3.2 -> i1
            rxBits[5] = (A3 >> 3) & 1; // A3.3 -> i0
            int rxIndex = (rxBits[0]<<5)|(rxBits[1]<<4)|(rxBits[2]<<3)|(rxBits[3]<<2)|(rxBits[4]<<1)|rxBits[5];
            string rxLabel = (rxIndex >= 0 && rxIndex < ToneLock.Cg.Length) ? ToneLock.Cg[rxIndex] : "Err";
            string steFlag = (((A3 >> 7) & 1) == 1) ? "Y" : "N";

            // TX bits and index (B2/B3 only window)
            int[] txBits = new int[6];
            txBits[0] = (B2 >> 0) & 1; // B2.0 -> i5
            txBits[1] = (B3 >> 1) & 1; // B3.1 -> i4
            txBits[2] = (B2 >> 2) & 1; // B2.2 -> i3
            txBits[3] = (B3 >> 0) & 1; // B3.0 -> i2
            txBits[4] = (B2 >> 4) & 1; // B2.4 -> i1
            txBits[5] = (B2 >> 6) & 1; // B2.6 -> i0
            int txIndex = (txBits[0]<<5)|(txBits[1]<<4)|(txBits[2]<<3)|(txBits[3]<<2)|(txBits[4]<<1)|txBits[5];
            string txLabel = (txIndex >= 0 && txIndex < ToneLock.Cg.Length) ? ToneLock.Cg[txIndex] : "Err";

            string txNames = string.Join(" ", TxSourceNames);
            string txVals  = string.Join(" ", txBits);
            string rxNames = string.Join(" ", RxSourceNames);
            string rxVals  = string.Join(" ", rxBits);

            return
                $"row {rowNumber:00} | bytes[A3..B0]={bytesBlock} | " +
                $"TX: sources [{txNames}] values(i5..i0)=[{txVals}] index={txIndex} label={txLabel} | " +
                $"RX: sources [{rxNames}] values(i5..i0)=[{rxVals}] index={rxIndex} label={rxLabel} | " +
                $"STE={steFlag} bank={bank}";
        }
    }
}
