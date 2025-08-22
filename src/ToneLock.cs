#nullable disable
// ToneLock.cs — conservative, MainForm-compatible tone decode/encode (RANGR6M2)
// Channel-aware shims so legacy 3-arg UI calls decode correctly.

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

        // ===== Last-channel cache (set by RgrCodec.GetScreenChannel/SetScreenChannel) =====
        private static bool _lastValid;
        private static byte _A3,_A2,_A1,_A0,_B3,_B2,_B1,_B0;

        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A3=A3; _A2=A2; _A1=A1; _A0=A0; _B3=B3; _B2=B2; _B1=B1; _B0=B0;
            _lastValid = true;
        }

        // --------------------------------------------------------------------
        // TX: A1 selects tone, with special-case A1==0x28 using B1.bit7
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

        // Preferred two-arg path
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx < 0 || idx >= Cg.Length) ? "?" : Cg[idx];
        }

        // Channel-aware compat shim for legacy 3-arg UI calls
        public static string TxToneFromBytes(byte x, byte y, byte z)
        {
            if (_lastValid)
            {
                string t = TxToneFromBytes(_A1, _B1);
                if (t != "?") return t;
            }

            // Fallbacks if cache wasn’t set for some reason
            string txy = TxToneFromBytes(x, y); if (txy != "?") return txy;
            string txz = TxToneFromBytes(x, z); if (txz != "?") return txz;
            string tyz = TxToneFromBytes(y, z); if (tyz != "?") return tyz;

            // Legacy key attempts (limited keys observed in RANGR6M2)
            int[] keys = new int[]
            {
                (((x >> 4) & 1) << 9) | (((y >> 2) & 1) << 8) | (z & 0x7F),
                (((y >> 4) & 1) << 9) | (((x >> 2) & 1) << 8) | (z & 0x7F),
                (((x >> 4) & 1) << 9) | (((z >> 2) & 1) << 8) | (y & 0x7F),
                (((z >> 4) & 1) << 9) | (((x >> 2) & 1) << 8) | (y & 0x7F),
                (((y >> 4) & 1) << 9) | (((z >> 2) & 1) << 8) | (x & 0x7F),
                (((z >> 4) & 1) << 9) | (((y >> 2) & 1) << 8) | (x & 0x7F),
            };
            for (int i = 0; i < keys.Length; i++)
            {
                switch (keys[i])
                {
                    case 352: return Cg[19]; // 127.3
                    case 354: return Cg[13]; // 103.5
                    case 864: return Cg[11]; // 97.4
                    case 866: return Cg[26]; // 162.2
                }
            }
            return "0";
        }

        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;

            switch (idx)
            {
                case 11: A1 = 0xEC;                 return true; // 97.4
                case 13: A1 = 0x28; B1 |=  0x80;    return true; // 103.5
                case 14: A1 = 0x28; B1 &= 0x7F;     return true; // 107.2
                case 15: A1 = 0x63;                 return true; // 110.9
                case 16: A1 = 0xA4;                 return true; // 114.8
                case 19: A1 = 0xA5;                 return true; // 127.3
                case 20: A1 = 0xAC;                 return true; // 131.8
                case 25: A1 = 0xEB;                 return true; // 156.7
                case 26: A1 = 0x68;                 return true; // 162.2
                default: return false;
            }
        }

        // --------------------------------------------------------------------
        // RX: A0 carries 6-bit index (bit order [7,6,3,2,1,0]); bank = B3.bit1
        // --------------------------------------------------------------------
        private static int RxIdxFromA0(byte a0)
        {
            int b5 = (a0 >> 7) & 1;
            int b4 = (a0 >> 6) & 1;
            int b3 = (a0 >> 3) & 1;
            int b2 = (a0 >> 2) & 1;
            int b1 = (a0 >> 1) & 1;
            int b0 = (a0 >> 0) & 1;
            return (b5 << 5) | (b4 << 4) | (b3 << 3) | (b2 << 2) | (b1 << 1) | b0;
        }

        // Bank 0 mapping (from RXMAP_A/B/C)
        private static readonly int[]    RxB0Idx   = new int[] { 0,3,5,7,8,10,12,13,14,15,16,17,20,22,25,27,28,30,31,32,40,42,45,48,49,51,53,55,57,59,60,62,63 };
        private static readonly string[] RxB0Tone  = new string[]{ "210.7","103.5","94.8","85.4","79.7","67.0","173.8","162.2","146.2","131.8","118.8","110.9","100.0","91.5","74.4","192.8","179.9","151.4","136.5","123.0","82.5","71.9","167.9","127.3","203.5","107.2","97.4","88.5","77.0","114.8","186.2","156.7","141.3" };
        // Bank 1 minimal (observed in RANGR6M2)
        private static readonly int[]    RxB1Idx   = new int[] { 12, 63 };
        private static readonly string[] RxB1Tone  = new string[]{ "107.2", "162.2" };

        // Channel-aware compat shim for 3-arg UI calls
        public static string RxToneFromBytes(byte a0orA3, byte B3, string txToneIfFollow)
        {
            if (_lastValid)
            {
                int bank = (_B3 >> 1) & 1;
                int idx  = RxIdxFromA0(_A0);
                if (idx == 0) return "0";
                if (bank == 0)
                {
                    for (int i = 0; i < RxB0Idx.Length; i++) if (RxB0Idx[i] == idx) return RxB0Tone[i];
                }
                else
                {
                    for (int i = 0; i < RxB1Idx.Length; i++) if (RxB1Idx[i] == idx) return RxB1Tone[i];
                }
            }

            // Fallback: interpret provided byte as A0 with B3
            int bankTry = (B3 >> 1) & 1;
            int idxTry  = RxIdxFromA0(a0orA3);
            if (idxTry == 0) return "0";
            if (bankTry == 0)
            {
                for (int i = 0; i < RxB0Idx.Length; i++) if (RxB0Idx[i] == idxTry) return RxB0Tone[i];
            }
            else
            {
                for (int i = 0; i < RxB1Idx.Length; i++) if (RxB1Idx[i] == idxTry) return RxB1Tone[i];
            }
            return "0";
        }

        // Two-arg helper (explicit A0/B3)
        public static string RxToneFromBytes(byte A0, byte B3)
        {
            int bank = (B3 >> 1) & 1;
            int idx  = RxIdxFromA0(A0);
            if (idx == 0) return "0";
            if (bank == 0)
            {
                for (int i = 0; i < RxB0Idx.Length; i++) if (RxB0Idx[i] == idx) return RxB0Tone[i];
            }
            else
            {
                for (int i = 0; i < RxB1Idx.Length; i++) if (RxB1Idx[i] == idx) return RxB1Tone[i];
            }
            return "0";
        }

        private static byte WriteIndexToA0(int idx, byte originalA0)
        {
            idx &= 0x3F;
            // Clear target bits [7,6,3,2,1,0] using hex mask 0x3C
            byte a0 = (byte)(originalA0 & 0x3C);
            if (((idx >> 5) & 1) != 0) a0 |= (byte)(1 << 7);
            if (((idx >> 4) & 1) != 0) a0 |= (byte)(1 << 6);
            if (((idx >> 3) & 1) != 0) a0 |= (byte)(1 << 3);
            if (((idx >> 2) & 1) != 0) a0 |= (byte)(1 << 2);
            if (((idx >> 1) & 1) != 0) a0 |= (byte)(1 << 1);
            if (((idx >> 0) & 1) != 0) a0 |= (byte)(1 << 0);
            return a0;
        }

        // ===== Helpers RgrCodec expects =====

        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            SetLastChannel(A3,A2,A1,A0,B3,B2,B1,B0);
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A0, B3);
            return (tx, rx);
        }

        public static bool TrySetRxTone(ref byte A0, ref byte B3, string tone)
        {
            if (tone == "0") { A0 = 0x00; return true; }

            int bank = (B3 >> 1) & 1;
            int idx = -1;
            if (bank == 0)
            {
                for (int i = 0; i < RxB0Idx.Length; i++)
                    if (RxB0Tone[i] == tone) { idx = RxB0Idx[i]; break; }
            }
            else
            {
                for (int i = 0; i < RxB1Idx.Length; i++)
                    if (RxB1Tone[i] == tone) { idx = RxB1Idx[i]; break; }
            }
            if (idx < 0) return false;
            A0 = WriteIndexToA0(idx, A0);
            return true;
        }

        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            if (image128 == null || image128.Length != 128)
                throw new ArgumentException("Expected 128 bytes.");

            byte[] nibbles = new byte[256];
            int n = 0;
            for (int ch = 0; ch < 16; ch++)
            {
                int off = ch * 8;
                for (int i = 0; i < 8; i++)
                {
                    byte b = image128[off + i];
                    nibbles[n++] = (byte)((b >> 4) & 0x0F); // hi
                    nibbles[n++] = (byte)(b & 0x0F);        // lo
                }
            }
            return nibbles;
        }

        public static string ToAsciiHex256(byte[] image128)
        {
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < image128.Length; i++) sb.Append(image128[i].ToString("X2"));
            return sb.ToString();
        }
    }
}
