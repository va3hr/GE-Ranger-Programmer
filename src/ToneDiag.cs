// Clean ToneDiag using ToneLock.Read* helpers
using System;

namespace RangrApp.Locked
{
    public static class ToneDiag
    {
        public static string FormatRow(
            int rowNumber,
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            byte A7, byte A6, byte A5, byte A4,
            byte B7, byte B6, byte B5, byte B4)
        {
            Span<byte> row16 = stackalloc byte[16] { A0,A1,A2,A3, B0,B1,B2,B3, A4,A5,A6,A7, B4,B5,B6,B7 };
            var (tx, txLabel) = ToneLock.ReadTxFromRow(row16);
            var (rx, rxLabel) = ToneLock.ReadRxFromRow(row16);
            string rawHex = ToneLock.FormatRowHex(row16);
            return $"row {rowNumber:00} | bytes[A0..A3 B0..B3 | A4..A7 B4..B7]={rawHex} | " +
                   $"TX[E8L={tx.E8Low:X1}, EDL={tx.EDLow:X1}, EEL={tx.EELow:X1}, EFL={tx.EFLow:X1}] " +
                   $"RX[E0L={rx.E0Low:X1}, E6L={rx.E6Low:X1}, E7L={rx.E7Low:X1}] " +
                   $"Tx:{txLabel} Rx:{rxLabel}";
        }
    }
}