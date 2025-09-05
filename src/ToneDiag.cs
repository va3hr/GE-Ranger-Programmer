// -----------------------------------------------------------------------------
// ToneDiag.cs — human‑readable diagnostics for TX/RX tone mapping (locks to ToneLock)
// -----------------------------------------------------------------------------

using System;

namespace GE_Ranger_Programmer
{
    public static class ToneDiag
    {
        private static string H(byte b) => b.ToString("X2");

        public static string FormatRow(
            int rowNumber,
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            int bank)
        {
            string bytesBlock = $"{H(A3)} {H(A2)} {H(A1)} {H(A0)}  {H(B3)} {H(B2)} {H(B1)} {H(B0)}";

            var (rxValsArr, rxIdx) = ToneLock.InspectReceiveBits(A3);
            var (txValsArr, txIdx) = ToneLock.InspectTransmitBits(A3, A2, A1, A0, B3, B2, B1, B0);

            string rxNames = string.Join(" ", ToneLock.ReceiveBitSourceNames);
            string txNames = string.Join(" ", ToneLock.TransmitBitSourceNames);

            string rxVals = string.Join(" ", Array.ConvertAll(rxValsArr, v => v.ToString()));
            string txVals = string.Join(" ", Array.ConvertAll(txValsArr, v => v.ToString()));

            string rxLabel = (rxIdx >= 0 && rxIdx < ToneLock.Cg.Length) ? ToneLock.Cg[rxIdx] : "Err";
            string txLabel = (txIdx >= 0 && txIdx < ToneLock.Cg.Length) ? ToneLock.Cg[txIdx] : "Err";
            string ste = (((A3 >> 7) & 1) == 1) ? "Y" : "N";

            return
                $"row {rowNumber:00} | bytes[A3..B0]={bytesBlock} | " +
                $"TX: sources [{txNames}] values(i5..i0)=[{txVals}] index={txIdx} label={txLabel} | " +
                $"RX: sources [{rxNames}] values(i5..i0)=[{rxVals}] index={rxIdx} label={rxLabel} | " +
                $"STE={ste} bank={bank}";
        }
    }
}

