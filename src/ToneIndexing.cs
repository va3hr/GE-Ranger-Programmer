#nullable disable
using System;

namespace X2212.Tones
{
    public static class ToneIndexing
    {
        // Canonical GE Channel Guard labels (menu; index 0 == "0")
        public static readonly string[] CanonicalLabels = new string[] {
            "0",
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
            "203.5","210.7"
        };

        // Direct 64-slot arrays; unknowns left null => UI shows "?"
        public static readonly string[] TxCodeToTone = new string[64];
        public static readonly string[] RxCodeToTone_Bank0 = new string[64];
        public static readonly string[] RxCodeToTone_Bank1 = new string[64];

        static ToneIndexing()
        {
            // Seeds learned from your TX1_* fixtures (universal, byte-true)
            TxCodeToTone[ 1] = "67.0";
            TxCodeToTone[ 9] = "100.0";
            TxCodeToTone[11] = "114.8";
            TxCodeToTone[22] = "85.4";
            TxCodeToTone[48] = "186.2";

            // Seeds learned from your RX1_* fixtures (bank 0)
            RxCodeToTone_Bank0[16] = "186.2";
            RxCodeToTone_Bank0[24] = "210.7";
            RxCodeToTone_Bank0[38] = "85.4";
            RxCodeToTone_Bank0[39] = "151.4"; // normalized from 151.6 to canonical 151.4
            RxCodeToTone_Bank0[57] = "100.0";
        }
    }
}
