#nullable disable
using System;
using X2212.Tones;

namespace RangrApp.Locked
{
    // Direct array model: six bits -> integer (0..63) -> 64-slot arrays.
    // Follow ignored; RX bank = B3.bit1.
    public static class ToneLock
    {
        public static readonly string[] Cg = ToneIndexing.CanonicalLabels;
        public static string[] ToneMenuTx => Cg;
        public static string[] ToneMenuRx => Cg;

        private static string MapTx(int idx)
        {
            if (idx == 0) return "0";
            var arr = ToneIndexing.TxCodeToTone;
            if (idx >= 0 && idx < arr.Length && arr[idx] != null) return arr[idx];
            return "?";
        }

        private static string MapRx(int bank, int idx)
        {
            if (idx == 0) return "0";
            var arr = (bank == 0) ? ToneIndexing.RxCodeToTone_Bank0 : ToneIndexing.RxCodeToTone_Bank1;
            if (idx >= 0 && idx < arr.Length && arr[idx] != null) return arr[idx];
            return "?";
        }

        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int txIdx = BitExact_Indexer.TxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int rxIdx = BitExact_Indexer.RxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int bank  = (B3 >> 1) & 1; // Follow ignored for now
            return (MapTx(txIdx), MapRx(bank, rxIdx));
        }
    }
}
