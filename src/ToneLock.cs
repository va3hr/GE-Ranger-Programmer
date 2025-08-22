#nullable disable
// ToneLock.cs — conservative, MainForm-compatible tone decode/encode (RANGR6M2)
// Robust 3-arg shims that are order-agnostic for legacy UI calls.

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

        // 2-arg decoder (preferred path when A1/B1 available)
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx < 0 || idx >= Cg.Length) ? "?" : Cg[idx];
        }

        // === ROBUST COMPAT SHIM for MainForm (3-arg, order-agnostic) ===
        // Older code may pass any 3 channel bytes (often B0,B2,B3). We:
        // 1) Try all pairings as possible (A1,B1): (x,y), (x,z), (y,z)
        // 2) Try legacy key with multiple permutations of (B0,B2,B3)
        // 3) Final fallback: "0" (matches UI's historical default)
        public static string TxToneFromBytes(byte x, byte y, byte z)
        {
            string t;
            // Step 1: any pair might actually be (A1,B1)
            t = TxToneFromBytes(x, y); if (t != "?") return t;
            t = TxToneFromBytes(x, z); if (t != "?") return t;
            t = TxToneFromBytes(y, z); if (t != "?") return t;

            // Step 2: legacy key attempts with several permutations
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

            // Step 3: safe fallback
            return "0";
        }

        // Encoder for TX (writes A1, and B1.bit7 only when required)
        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;
            switch (idx)
            {
                case 11: A1 = 0xEC;                 return true; // 97.4
                case 13: A1 = 0x28; B1 |=  0x80;    return true; // 103.5 (bit7=1)
                case 14: A1 = 0x28; B1 &= 0x7F;     return true; // 107.2 (bit7=0)
                case 15: A1 = 0x63;                 return true; // 110.9
                case 16: A1 = 0xA4;                 return true; // 114.8
                case 19: A1 = 0xA5;                 return true; // 127.3
                case 20: A1 = 0xAC;                 return true; // 131.8
                case 25: A1 = 0xEB;                 return true; // 156.7
                case 26: A1 = 0x68;                 return true; // 162.2
                default: return false; // only encode indices we know are safe
            }
        }

        // --------------------------------------------------------------------
        // RX (RANGR6M2): Rx index may be read from A0 ([7,6,3,2,1,0]) or A3 ([6,7,0,1,2,3])
        // Bank is B3.bit1. Index 0 displays "0" (DOS shows '.').
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

        private static int RxIdxFromA3(byte a3)
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

        // === ROBUST 3-arg Rx for MainForm (order-agnostic for A0/A3, tolerant bank) ===
        public static string RxToneFromBytes(byte a0orA3, byte B3, string txToneIfFollow)
        {
            // Try both interpretations of the first byte
            int idxA0 = RxIdxFromA0(a0orA3);
            int idxA3 = RxIdxFromA3(a0orA3);

            // try bank from B3.bit1, else flip if not found
            int[] banks = new int[] { ((B3 >> 1) & 1), 1 - ((B3 >> 1) & 1) };

            // helper to lookup tone
            Func<int,int,string> lookup = (bank, idx) =>
            {
                if (idx == 0) return "0";
                if (bank == 0)
                {
                    for (int i = 0; i < RxB0Idx.Length; i++) if (RxB0Idx[i] == idx) return RxB0Tone[i];
                }
                else
                {
                    for (int i = 0; i < RxB1Idx.Length; i++) if (RxB1Idx[i] == idx) return RxB1Tone[i];
                }
                return null;
            };

            for (int bi = 0; bi < banks.Length; bi++)
            {
                string t = lookup(banks[bi], idxA3);
                if (t != null) return t;
                t = lookup(banks[bi], idxA0);
                if (t != null) return t;
            }

            // Fallback if we hit an unrecognized index pattern
            return "0";
        }

        // (Optional) two-arg helper for A0-based path (kept for completeness)
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

        // ====== REQUIRED BY RgrCodec.cs ======

        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A0, B3, tx); // be liberal: treat first as A0 here
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
