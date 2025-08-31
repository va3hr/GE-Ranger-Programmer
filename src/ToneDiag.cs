// src/ToneDiag.cs
// Human-readable diagnostic line builder.
// Namespace matches ToneLock (RangrApp.Locked) so MainForm can call
// RangrApp.Locked.ToneDiag.FormatRow(...) directly.
//
// Prints the ACTUAL bit sources and values taken from the current windows
// defined in ToneLock (via public metadata + Inspect helpers).

using System;

namespace RangrApp.Locked
{
    public static class ToneDiag
    {
        private static string Hex2(byte b) => b.ToString("X2");

        /// <summary>
        /// Build one readable diagnostic line for the bottom log.
        /// It echoes the bytes, the TX/RX bit source names (MSB→LSB), their values,
        /// and the computed indices/labels, plus STE and bank.
        /// </summary>
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
            string rxLabel = (rxIndex >= 0 && rxIndex < ToneLock.Cg.Length) ? ToneLock.Cg[rxIndex] : "Err";
            string steFlag = (((A3 >> 7) & 1) == 1) ? "Y" : "N";

            // TX inspection (names + values + index + label)
            var (txValues, txIndex) = ToneLock.InspectTransmitBits(A3, A2, A1, A0, B3, B2, B1, B0);
            string txNames = string.Join(" ", ToneLock.TransmitBitSourceNames);
            string txVals  = string.Join(" ", txValues);
            string txLabel = (txIndex >= 0 && txIndex < ToneLock.Cg.Length) ? ToneLock.Cg[txIndex] : "Err";

            // Final line
            return
                $"row {rowNumber:00} | bytes[A3..B0]={bytesBlock} | " +
                $"TX: sources [{txNames}] values(i5..i0)=[{txVals}] index={txIndex} label={txLabel} | " +
                $"RX: sources [{rxNames}] values(i5..i0)=[{rxVals}] index={rxIndex} label={rxLabel} | " +
                $"STE={steFlag} bank={bank}";
        }
    }
}
