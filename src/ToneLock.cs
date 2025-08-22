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

        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            // Special case: A1==0x28 -> B1.bit7 selects 103.5 vs 107.2
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 13 : 14;

            // Map known A1 values to CG indices (from your RANGR6M2 image)
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

        // === COMPAT SHIM for MainForm (3-arg) ===
        // Some older code calls TxToneFromBytes(B0,B2,B3). We first try treating the
        // first two bytes as (A1,B1). If that fails, fall back to the legacy key.
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            // Try interpreting as (A1,B1)
            string t = TxToneFromBytes(p0, p1);
            if (t != "?") return t;

            // Legacy key = (B0.bit4, B2.bit2, B3 & 0x7F)  → limited keys from RANGR6M2
            int key = (((p0 >> 4) & 1) << 9) | (((p1 >> 2) & 1) << 8) | (p2 & 0x7F);
            switch (key)
            {
                case 352: return Cg[19]; // 127.3
                case 354: return Cg[13]; // 103.5
                case 864: return Cg[11]; // 97.4
                case 866: return Cg[26]; // 162.2
                default:  return "?";
            }
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
                case 14: A1 = 0x28; B1 &= (byte)~0x80; return true; // 107.2 (bit7=0)
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

        // (Optional) two-arg helper for A0-based path (kept for completeness)
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

        // A0 path (portable — no binary literals)
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

        // ====== REQUIRED BY RgrCodec.cs ======

        // 1) DecodeChannel — RgrCodec expects this
        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A3, B3, tx);
            return (tx, rx);
        }

        // 2) TrySetRxTone(ref A0, ref B3, string) — RgrCodec expects this
        //    We keep the current bank (from B3.bit1) and set A0 index accordingly.
        public static bool TrySetRxTone(ref byte A0, ref byte B3, string tone)
        {
            if (tone == "0")
            {
                A0 = 0x00;
                return true;
            }

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

            if (idx < 0) return false; // unsupported tone for this bank
            A0 = WriteIndexToA0(idx, A0);
            return true;
        }

        // 3) ToX2212Nibbles — RgrCodec expects this
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

        // Utility — build 256-char ASCII hex from a 128-byte image
        public static string ToAsciiHex256(byte[] image128)
        {
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < image128.Length; i++) sb.Append(image128[i].ToString("X2"));
            return sb.ToString();
        }
    }
}
