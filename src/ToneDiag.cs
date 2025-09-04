// -----------------------------------------------------------------------------
// ToneDiag.cs — clean diagnostics for TX/RX tone mapping (uses ToneLock)
// -----------------------------------------------------------------------------

using System;

namespace RangrApp.Locked
{
    public static class ToneDiag
    {
        private static string H(byte b) => b.ToString("X2");

        public static string FormatRow(
            int rowNumber,
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            // second half of 16B channel row (A4..A7,B4..B7)
            byte A7, byte A6, byte A5, byte A4,
            byte B7, byte B6, byte B5, byte B4)
        {
            Span<byte> row16 = stackalloc byte[16] { A0,A1,A2,A3, B0,B1,B2,B3, A4,A5,A6,A7, B4,B5,B6,B7 };

            string rawHex = ToneLock.FormatRowHex(row16);
            var tx = ToneLock.ExtractTxNibbles(row16);
            var rx = ToneLock.ExtractRxNibbles(row16);
            string txLabel = ToneLock.GetTxLabel(tx.ToneCodeHighLow, tx.ToneCodeLowLow);
            string rxLabel = ToneLock.GetRxLabel(rx.E0Low, rx.E6Low, rx.E7Low);
            string ste = ToneLock.SteDisplayFromFLow(tx.ToneCodeLowLow); // or from RX 'F' low nibble if you prefer

            return
                $"row {rowNumber:00} | bytes[A0..A3 B0..B3 | A4..A7 B4..B7]={rawHex} | " +
                $"TX[E8L={tx.HousekeepingNibble0Low:X1}, EDL={tx.HousekeepingNibble1Low:X1}, EEL={tx.ToneCodeHighLow:X1}, EFL={tx.ToneCodeLowLow:X1}] " +
                $"RX[E0L={rx.E0Low:X1}, E6L={rx.E6Low:X1}, E7L={rx.E7Low:X1}] " +
                $"Tx:{txLabel} Rx:{rxLabel} STE:{ste}";
        }
    }
}