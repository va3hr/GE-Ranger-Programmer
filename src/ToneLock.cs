#nullable disable
// ToneLock.cs — conservative, MainForm-compatible tone decode/encode (RANGR6M2)
// All tone logic stays here. No modern C# features to keep GitHub builds happy.

using System;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical CG list (index 0 == "0") =====
        public static readonly string[] Cg = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // Menus expected by MainForm
        public static readonly string[] ToneMenuTx = Cg;
        public static readonly string[] ToneMenuRx = Cg;

        // --------------------------------------------------------------------
        // TX (RANGR6M2): primary encoding is A1; 0x28 disambiguated by B1.bit7
        // --------------------------------------------------------------------

        // A1 -> CG index list (except A1==0x28 which depends on B1.bit7)
        private static readonly byte[] TxA1Vals = new byte[]
        {
            0x29,0xAC,0xAE, // 131.8
            0xA4,0xE3,0xE2, // 114.8
            0xA5,           // 127.3
            0x6D,           // 103.5
            0xEB,           // 156.7
            0xEC,           // 97.4
            0x68,0x2A,      // 162.2
            0x98,           // 107.2
            0x63            // 110.9
        };
        private static readonly int[]  TxA1Idxs = new int[]
        {
            20,20,20,
            16,16,16,
            19,
            13,
            25,
            11,
            26,26,
            14,
            15
        };

        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 13 : 14; // 103.5 vs 107.2
            for (int i = 0; i < TxA1Vals.Length; i++)
                if (TxA1Vals[i] == A1) return TxA1Idxs[i];
            return -1;
        }

        // Reverse map (indices present in your RANGR6M2)
        private static readonly int[]  TxIdxList   = new int[] { 11, 13, 14, 15, 16, 19, 20, 25, 26 };
        private static readonly byte[] TxIdxA1     = new byte[] { 0xEC,0x28,0x28,0x63,0xA4,0xA5,0xAC,0xEB,0x68 };
        // -1 = ignore B1.bit7; 1 = set; 0 = clear
        private static readonly sbyte[] TxIdxB1Bit7 = new sbyte[] { -1,    1,    0,   -1,   -1,   -1,   -1,   -1,   -1 };

        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx < 0 || idx >= Cg.Length) ? "?" : Cg[idx];
        }

        // === COMPAT
