// src/ToneDiag.cs
// Human-readable diagnostic line builder.
// This file intentionally defines ToneDiag in BOTH namespaces so calls like
// `ToneDiag.FormatRow(...)` work regardless of whether MainForm is in
// `RangrApp` or `RangrApp.Locked`. No other behavior is changed.

using System;

// -----------------------------------------------------------------------------
// Primary implementation in RangrApp (uses ToneLock, which is in RangrApp.Locked)
// -----------------------------------------------------------------------------
namespace RangrApp
{
    using RangrApp.Locked;

    public static class ToneDiag
    {
        private static string Hex2(byte b) => b.ToString("X2");

        /// <summary>
        /// Build one readable diagnostic line for the bottom log.
        /// Echoes bytes, TX/RX source names (MSB→LSB), per-bit values,
        /// computed indices/labels, STE, and bank.
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

// -----------------------------------------------------------------------------
// Bridge in RangrApp.Locked (for callers compiled inside that namespace)
// -----------------------------------------------------------------------------
namespace RangrApp.Locked
{
    public static class ToneDiag
    {
        /// <summary>
        /// Forwards to RangrApp.ToneDiag so MainForm can call ToneDiag.FormatRow(...)
        /// regardless of its namespace. No behavior change.
        /// </summary>
        public static string FormatRow(
            int rowNumber,
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            int bank)
            => RangrApp.ToneDiag.FormatRow(rowNumber, A3, A2, A1, A0, B3, B2, B1, B0, bank);
    }
}
