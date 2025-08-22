#nullable disable
// ToneLock.cs — faithful GE Rangr tone decode/encode for RANGR6M2
// Traditional C#, no modern features. All tone logic centralized here.

using System;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical CTCSS menu (index 0 == "0") =====
        public static readonly string[] Cg = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // Menus used by MainForm
        public static readonly string[] ToneMenuTx = Cg;
        public static readonly string[] ToneMenuRx = Cg;

        // ===== Cache the last 8 bytes of the channel row so legacy 3-arg calls work =====
        private static bool _lastValid;
        private static byte _A3,_A2,_A1,_A0,_B3,_B2,_B1,_B0;

        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A3=A3; _A2=A2; _A1=A1; _A0=A0; _B3=B3; _B2=B2; _B1=B1; _B0=B0;
            _lastValid = true;
        }

        // --------------------------------------------------------------------
        //  TX decode: A1 chooses the tone, with A1==0x28 disambiguated by B1.bit7
        // --------------------------------------------------------------------
        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 13 : 14; // 103.5 vs 107.2
            switch (A1)
            {
                case 0x29: case 0xAC: case 0xAE: return 20; // 131.8
                case 0xA4: case 0xE3: case 0xE2: return 16; // 114.8
                case 0xA5: return 19;                       // 127.3
                case 0x6D: return 13;                       // 103.5
                case 0xEB: return 25;                       // 156.7
                case 0xEC: return 11;                       // 97.4
                case 0x68: case 0x2A: return 26;            // 162.2
                case 0x98: return 14;                       // 107.2
                case 0x63: return 15;                       // 110.9
                default: return -1;
            }
        }

        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx < 0 || idx >= Cg.Length) ? "0" : Cg[idx];
        }

        // 3-arg shim used by MainForm; resolve via the cached channel first
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            if (_lastValid)
            {
                string t = TxToneFromBytes(_A1, _B1);
                if (t != "0") return t;
            }
            // Fallbacks (pairwise attempts + limited legacy key patterns seen in this image)
            string txy = TxToneFromBytes(p0, p1); if (txy != "0") return txy;
            string txz = TxToneFromBytes(p0, p2); if (txz != "0") return txz;
            string tyz = TxToneFromBytes(p1, p2); if (tyz != "0") return tyz;

            int[] keys = new int[]
            {
                (((p0>>4)&1)<<9) | (((p1>>2)&1)<<8) | (p2 & 0x7F),
                (((p1>>4)&1)<<9) | (((p0>>2)&1)<<8) | (p2 & 0x7F),
                (((p0>>4)&1)<<9) | (((p2>>2)&1)<<8) | (p1 & 0x7F),
                (((p2>>4)&1)<<9) | (((p0>>2)&1)<<8) | (p1 & 0x7F),
                (((p1>>4)&1)<<9) | (((p2>>2)&1)<<8) | (p0 & 0x7F),
                (((p2>>4)&1)<<9) | (((p1>>2)&1)<<8) | (p0 & 0x7F),
