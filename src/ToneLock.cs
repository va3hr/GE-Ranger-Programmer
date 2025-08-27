#nullable disable
using System;
using X2212.Tones;

namespace RangrApp.Locked
{
    // UI-facing entry points kept the same. MainForm calls DecodeChannel(...) and uses ToneMenuTx/Rx.
    public static class ToneLock
    {
        public static readonly string[] Cg = ToneIndexing.CanonicalLabels;
        public static string[] ToneMenuTx => Cg;
        public static string[] ToneMenuRx => Cg;

        private static string MapTxCode(int idx)
        {
            if (idx == 0) return "0";
            var arr = ToneIndexing.TxCodeToTone;
            if (idx >= 0 && idx < arr.Length && arr[idx] != null) return arr[idx];
            return "?";
        }

        private static string MapRxCode(int bank, int idx)
        {
            if (idx == 0) return "0";
            var arr = (bank == 0) ? ToneIndexing.RxCodeToTone_Bank0 : ToneIndexing.RxCodeToTone_Bank1;
            if (idx >= 0 && idx < arr.Length && arr[idx] != null) return arr[idx];
            return "?";
        }

        // The one API MainForm uses.
        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int txIdx = BitExact_Indexer.TxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int rxIdx = BitExact_Indexer.RxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int bank  = (B3 >> 1) & 1;       // Follow ignored
            return (MapTxCode(txIdx), MapRxCode(bank, rxIdx));
        }
    }
}
