// src/ToneDiag.cs
// Human-readable diagnostic line builder (uses ToneLock’s public metadata)
// Prints the ACTUAL bit sources and values taken from the current bit window.

using System;
using System.Text;
using RangrApp.Locked;

namespace RangrApp
{
    public static class ToneDiag
    {
        private static string Hex2(byte b) => b.ToString("X2");

        public static string FormatRow(
            int rowNumber,
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            int bank)
        {
            // Bytes block
            string bytesBlock =
                $"{Hex2(A3)} {Hex2(A2)} {Hex2(A1)} {Hex2(A0)}  {Hex2(B3)} {Hex2(B2)} {Hex2(B1)} {Hex2(B0)}";

            // RX inspection (names + values + index + label + STE)
            var (rxValues, rxIndex) = ToneLock.InspectReceiveBits(A3);
            string rxNames = string.Join(" ", ToneLock.ReceiveBitSourceNames);
            string rxVals  = string.Join(" ", rxValues);
            string rxLabel = ToneLock.Cg[rxIndex >= 0 && rxIndex < ToneLock.Cg.Length ? rxIndex : 0];
            string steFlag = (((A3 >> 7) & 1) == 1) ? "Y" : "N";

            // TX inspection (names + values + index + label)
            var (txValues, txIndex) = ToneLock.InspectTransmitBits(A3, A2, A1, A0, B3, B2, B1, B0);
            string txNames = string.Join(" ", ToneLock.TransmitBitSourceNames);
            string txVals  = string.Join(" ", txValues);
            string txLabel = ToneLock.Cg[txIndex >= 0 && txIndex < ToneLock.Cg.Length ? txIndex : 0];

            // Final line
            return
                $"row {rowNumber:00} | bytes[A3..B0]={bytesBlock} | " +
                $"TX: sources [{txNames}] values(i5..i0)=[{txVals}] index={txIndex} label={txLabel} | " +
                $"RX: sources [{rxNames}] values(i5..i0)=[{rxVals}] index={rxIndex} label={ToneLock.Cg[rxIndex]} | " +
                $"STE={steFlag} bank={bank}";
        }
    }
}
