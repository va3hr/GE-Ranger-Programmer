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

        // === COMPAT SHIM for MainForm (3-arg) ===
        // Some older code calls TxToneFromBytes(B0,B2,B3). We first try treating the
        // first two bytes as (A1,B1). If that fails, fall back to the legacy key.
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            string t = TxToneFromBytes(p0, p1);
            if (t != "?") return t;

            // Legacy key = (B0.bit4, B2.bit2, B3 & 0x7F)
            int key = (((p0 >> 4) & 1) << 9) | (((p1 >> 2) & 1) << 8) | (p2 & 0x7F);
            // Minimal keys observed in your RANGR6M2 image:
            int[] keys   = new int[] { 352, 354, 864, 866 };
            int[] keyIdx = new int[] {  19,  13,  11,  26 };
            for (int i = 0; i < keys.Length; i++)
                if (keys[i] == key) return Cg[keyIdx[i]];

            return "?";
        }

        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;
            for (int i = 0; i < TxIdxList.Length; i++)
            {
                if (TxIdxList[i] == idx)
                {
                    A1 = TxIdxA1[i];
                    sbyte b = TxIdxB1Bit7[i];
                    if (b == 1) B1 |= 0x80;
                    else if (b == 0) B1 &= 0x7F;
                    return true;
                }
            }
            return false; // we only encode indices we know are safe
        }

        // --------------------------------------------------------------------
        // RX (RANGR6M2): Rx index from A3 straddle [6,7,0,1,2,3]; bank = B3.bit1
        // Index 0 displays "0" (DOS shows a lone '.').
        // --------------------------------------------------------------------
        private static int RxIndexFromA3(byte a3)
        {
            int b5 = (a3 >> 6) & 1;
            int b4 = (a3 >> 7) & 1;
            int b3 = (a3 >> 0) & 1;
            int b2 = (a3 >> 1) & 1;
            int b1 = (a3 >> 2) & 1;
            int b0 = (a3 >> 3) & 1;
            return (b5 << 5) | (b4 << 4) | (b3 << 3) | (b2 << 2) | (b1 << 1) | b0;
        }

        // Bank 0 mapping (from RXMAP_A/B/C)
        private static readonly int[]    RxB0Idx   = new int[] { 0,3,5,7,8,10,12,13,14,15,16,17,20,22,25,27,28,30,31,32,40,42,45,48,49,51,53,55,57,59,60,62,63 };
        private static readonly string[] RxB0Tone  = new string[]{ "210.7","103.5","94.8","85.4","79.7","67.0","173.8","162.2","146.2","131.8","118.8","110.9","100.0","91.5","74.4","192.8","179.9","151.4","136.5","123.0","82.5","71.9","167.9","127.3","203.5","107.2","97.4","88.5","77.0","114.8","186.2","156.7","141.3" };
        // Bank 1 minimal (observed in RANGR6M2)
        private static readonly int[]    RxB1Idx   = new int[] { 12, 63 };
        private static readonly string[] RxB1Tone  = new string[]{ "107.2", "162.2" };

        // MainForm uses this 3-arg version
        public static string RxToneFromBytes(byte A3, byte B3, string txToneIfFollow)
        {
            int bank = (B3 >> 1) & 1;
            int idx  = RxIndexFromA3(A3);
            if (idx == 0) return "0";
            if (bank == 0)
            {
                for (int i = 0; i < RxB0Idx.Length; i++) if (RxB0Idx[i] == idx) return RxB0Tone[i];
            }
            else
            {
                for (int i = 0; i < RxB1Idx.Length; i++) if (RxB1Idx[i] == idx) return RxB1Tone[i];
            }
            return "?";
        }

        // (Optional) two-arg helper for A0-based path
        public static string RxToneFromBytes(byte A0, byte B3)
        {
            int bank = (B3 >> 1) & 1;
            int idx  = IndexFromA0(A0);
            if (idx == 0) return "0";
            if (bank == 0)
            {
                for (int i = 0; i < RxB0Idx.Length; i++) if (RxB0Idx[i] == idx) return RxB0Tone[i];
            }
            else
            {
                for (int i = 0; i < RxB1Idx.Length; i++) if (RxB1Idx[i] == idx) return RxB1Tone[i];
            }
            return "?";
        }

        // A0 path (kept for completeness) — portable (no binary literals)
        private static int IndexFromA0(byte a0)
        {
            int b5 = (a0 >> 7) & 1;
            int b4 = (a0 >> 6) & 1;
            int b3 = (a0 >> 3) & 1;
            int b2 = (a0 >> 2) & 1;
            int b1 = (a0 >> 1) & 1;
            int b0 = (a0 >> 0) & 1;
            return (b5 << 5) | (b4 << 4) | (b3 << 3) | (b2 << 2) | (b1 << 1) | b0;
        }

        private static byte WriteIndexToA0(int idx, byte originalA0)
        {
            idx &= 0x3F;
            // Clear target bits [7,6,3,2,1,0] using hex mask 0x3C (== 0b00111100)
            byte a0 = (byte)(originalA0 & 0x3C);
            if (((idx >> 5) & 1) != 0) a0 |= (byte)(1 << 7);
            if (((idx >> 4) & 1) != 0) a0 |= (byte)(1 << 6);
            if (((idx >> 3) & 1) != 0) a0 |= (byte)(1 << 3);
            if (((idx >> 2) & 1) != 0) a0 |= (byte)(1 << 2);
            if (((idx >> 1) & 1) != 0) a0 |= (byte)(1 << 1);
            if (((idx >> 0) & 1) != 0) a0 |= (byte)(1 << 0);
            return a0;
        }

        // Utility — build 256-char ASCII hex from a 128-byte image
        public static string ToAsciiHex256(byte[] image128)
        {
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < image128.Length; i++) sb.Append(image128[i].ToString("X2"));
            return sb.ToString();
        }
    }
}
