// ToneLock.cs — conservative, MainForm-compatible tone decode/encode (RANGR6M2)
// Namespace matches your repo. All tone logic stays here.

using System;
using System.Collections.Generic;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Channel Guard menu (index 0 == "0") =====
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
        // TX (RANGR6M2): observed is A1-driven; 0x28 disambiguated by B1.bit7
        // --------------------------------------------------------------------
        private static readonly Dictionary<byte, int> TxA1ToIdx = new Dictionary<byte, int>()
        {
            { 0x29, 20 }, // 131.8
            { 0xAC, 20 }, // 131.8
            { 0xAE, 20 }, // 131.8
            { 0xA4, 16 }, // 114.8
            { 0xE3, 16 }, // 114.8
            { 0xE2, 16 }, // 114.8
            { 0xA5, 19 }, // 127.3
            { 0x6D, 13 }, // 103.5
            { 0xEB, 25 }, // 156.7
            { 0xEC, 11 }, // 97.4
            { 0x68, 26 }, // 162.2
            { 0x2A, 26 }, // 162.2
            { 0x98, 14 }, // 107.2
            { 0x63, 15 }  // 110.9
            // 0x28 is handled in TxIndexFromA1B1 using B1.bit7
        };

        private struct TxSpec
        {
            public byte A1;
            public bool HasB1Bit7;
            public bool B1Bit7Value;
        }

        // Minimal reverse set (tones present in your RANGR6M2 image)
        private static readonly Dictionary<int, TxSpec> TxIdxToA1 = new Dictionary<int, TxSpec>()
        {
            { 11, new TxSpec { A1 = 0xEC, HasB1Bit7 = false, B1Bit7Value = false } }, // 97.4
            { 13, new TxSpec { A1 = 0x28, HasB1Bit7 = true,  B1Bit7Value = true  } }, // 103.5
            { 14, new TxSpec { A1 = 0x28, HasB1Bit7 = true,  B1Bit7Value = false } }, // 107.2
            { 15, new TxSpec { A1 = 0x63, HasB1Bit7 = false, B1Bit7Value = false } }, // 110.9
            { 16, new TxSpec { A1 = 0xA4, HasB1Bit7 = false, B1Bit7Value = false } }, // 114.8
            { 19, new TxSpec { A1 = 0xA5, HasB1Bit7 = false, B1Bit7Value = false } }, // 127.3
            { 20, new TxSpec { A1 = 0xAC, HasB1Bit7 = false, B1Bit7Value = false } }, // 131.8
            { 25, new TxSpec { A1 = 0xEB, HasB1Bit7 = false, B1Bit7Value = false } }, // 156.7
            { 26, new TxSpec { A1 = 0x68, HasB1Bit7 = false, B1Bit7Value = false } }, // 162.2
        };

        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 13 : 14; // B1.bit7 picks 103.5 vs 107.2
            int idx;
            if (TxA1ToIdx.TryGetValue(A1, out idx)) return idx;
            return -1;
        }

        // Decoder used elsewhere in code (2-arg form)
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx < 0 || idx >= Cg.Length) ? "?" : Cg[idx];
        }

        // === COMPAT SHIM for MainForm ===
        // Old code calls TxToneFromBytes(b0,b2,b3). We try A1/B1 first (if those
        // were actually passed), otherwise fall back to a small key map.
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            // Try interpreting as (A1,B1)
            string t = TxToneFromBytes(p0, p1);
            if (t != "?") return t;

            // Fallback: older scheme key = (B0.bit4, B2.bit2, B3&0x7F)
            int key = (((p0 >> 4) & 1) << 9) | (((p1 >> 2) & 1) << 8) | (p2 & 0x7F);
            int idx;
            if (TxKeyToIdx.TryGetValue(key, out idx) && idx >= 0 && idx < Cg.Length)
                return Cg[idx];

            return "?";
        }

        // Minimal key map (from your RANGR6M2 image). This covers some cases
        // if older code really passes (B0,B2,B3). A1/B1 path above is preferred.
        private static readonly Dictionary<int, int> TxKeyToIdx = new Dictionary<int, int>()
        {
            { 352, 19 }, // 127.3
            { 354, 13 }, // 103.5
            { 864, 11 }, // 97.4
            { 866, 26 }  // 162.2
        };

        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;
            TxSpec spec;
            if (!TxIdxToA1.TryGetValue(idx, out spec

