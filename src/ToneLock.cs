#nullable disable
// ToneLock.cs — GE Rangr tone decode/encode (RANGR6M2)
// Traditional C#, minimal changes, DOS-faithful decoding.

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

        // UI menus
        public static readonly string[] ToneMenuTx = Cg;
        public static readonly string[] ToneMenuRx = Cg;

        // ===== Last-channel cache so legacy 3-arg UI calls resolve correctly =====
        private static bool _lastValid;
        private static byte _A3, _A2, _A1, _A0, _B3, _B2, _B1, _B0;

        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A3 = A3; _A2 = A2; _A1 = A1; _A0 = A0; _B3 = B3; _B2 = B2; _B1 = B1; _B0 = B0;
            _lastValid = true;
        }

        // --------------------------------------------------------------------
        // TX: A1 selects the tone (+ special case A1==0x28 uses B1.bit7)
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

        // Legacy 3-arg shim used by MainForm — resolve from cached row first.
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            if (_lastValid)
            {
                string t = TxToneFromBytes(_A1, _B1);
                if (t != "0") return t;
            }
            // Conservative fallbacks if cache wasn’t set.
            string txy = TxToneFromBytes(p0, p1); if (txy != "0") return txy;
            string txz = TxToneFromBytes(p0, p2); if (txz != "0") return txz;
            string tyz = TxToneFromBytes(p1, p2); if (tyz != "0") return tyz;
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
        // RX: 6-bit index = [A3 bit7:6][A2 bit7:4] (big-endian straddle); bank = B3.bit1
        // --------------------------------------------------------------------
        private static int RxIndexFromA3A2(byte a3, byte a2)
{
    // Correct MSB-first mapping using LSB positions (lsb = 7 - msbIndex):
    // i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
    // LSB positions: 1,0,7,6,5,4
    int b5 = (a3 >> 1) & 1; // A3.6 -> lsb1
    int b4 = (a3 >> 0) & 1; // A3.7 -> lsb0
    int b3 = (a3 >> 7) & 1; // A3.0 -> lsb7
    int b2 = (a3 >> 6) & 1; // A3.1 -> lsb6
    int b1 = (a3 >> 5) & 1; // A3.2 -> lsb5
    int b0 = (a3 >> 4) & 1; // A3.3 -> lsb4
    return ((b5<<5)|(b4<<4)|(b3<<3)|(b2<<2)|(b1<<1)|b0) & 0x3F;
}



        // Bank-0 map from RXMAP_A/B/C (sparse); bank-1 sightings seen in RANGR6M2
        private static readonly int[]    RxB0Idx  = new int[] { 0,3,5,7,8,10,12,13,14,15,16,17,20,22,25,27,28,30,31,32,40,42,45,48,49,51,53,55,57,59,60,62,63 };
        private static readonly string[] RxB0Tone = new string[]{ "210.7","103.5","94.8","85.4","79.7","67.0","173.8","162.2","146.2","131.8","118.8","110.9","100.0","91.5","74.4","192.8","179.9","151.4","136.5","123.0","82.5","71.9","167.9","127.3","203.5","107.2","97.4","88.5","77.0","114.8","186.2","156.7","141.3" };
        private static readonly int[]    RxB1Idx  = new int[] { 12, 63 };
        private static readonly string[] RxB1Tone = new string[]{ "107.2", "162.2" };

        private static string RxLookup(int bank, int idx)
        {
            if (idx == 0) return "0";
            if (bank == 0)
            {
                for (int i = 0; i < RxB0Idx.Length; i++)
                    if (RxB0Idx[i] == idx) return RxB0Tone[i];
            }
            else
            {
                for (int i = 0; i < RxB1Idx.Length; i++)
                    if (RxB1Idx[i] == idx) return RxB1Tone[i];
            }
            return "0";
        }

        public static string RxToneFromBytes(byte A3, byte A2, byte B3)
{
    // A2 is ignored for RX decoding; we don't have TX display here, so "TX" will never be chosen.
    return RxToneCodec.DecodeRxTone(A3, B3, txDisplayForFollow: "TX");
}


        // Legacy 3-arg shim used by MainForm — resolve from cached A3/A2/B3
        public static string RxToneFromBytes(byte A3, byte B3, string txDisplay)
{
    return RxToneCodec.DecodeRxTone(A3, B3, txDisplay);
}


        // ===== Optional write helpers (kept minimal) =====

        // Write idx back into A3/A2 (preserve unrelated bits)
        private static void WriteIndexToA3A2(int idx, ref byte A3, ref byte A2)
{
    idx &= 0x3F;
    // Preserve bits other than target positions {1,0,7,6,5,4}
    const byte preserveMask = 0x0C;
    byte newA3 = (byte)(A3 & preserveMask);

    if (((idx >> 5) & 1) != 0) newA3 |= (1<<1); // i5 -> A3.6 (lsb1)
    if (((idx >> 4) & 1) != 0) newA3 |= (1<<0); // i4 -> A3.7 (lsb0)
    if (((idx >> 3) & 1) != 0) newA3 |= (1<<7); // i3 -> A3.0 (lsb7)
    if (((idx >> 2) & 1) != 0) newA3 |= (1<<6); // i2 -> A3.1 (lsb6)
    if (((idx >> 1) & 1) != 0) newA3 |= (1<<5); // i1 -> A3.2 (lsb5)
    if (((idx >> 0) & 1) != 0) newA3 |= (1<<4); // i0 -> A3.3 (lsb4)

    A3 = newA3;
    // A2 remains unchanged.
}
const const byte preserveMask = 0x0C; // equals 0x0C
    byte newA3 = (byte)(A3 & preserveMask);

    if (((idx >> 5) & 1) != 0) newA3 |= (1<<1); // i5 -> A3.6 (lsb1)
    if (((idx >> 4) & 1) != 0) newA3 |= (1<<0); // i4 -> A3.7 (lsb0)
    if (((idx >> 3) & 1) != 0) newA3 |= (1<<7); // i3 -> A3.0 (lsb7)
    if (((idx >> 2) & 1) != 0) newA3 |= (1<<6); // i2 -> A3.1 (lsb6)
    if (((idx >> 1) & 1) != 0) newA3 |= (1<<5); // i1 -> A3.2 (lsb5)
    if (((idx >> 0) & 1) != 0) newA3 |= (1<<4); // i0 -> A3.3 (lsb4)

    A3 = newA3;
    // A2 unchanged.
}

    const byte preserveMask = 0x30; // preserve bits 5 & 4; others (7,6,3,2,1,0) will be rewritten
    byte newA3 = (byte)(A3 & preserveMask);

    if (((idx >> 5) & 1) != 0) newA3 |= (1<<6); // i5 -> A3.6
    if (((idx >> 4) & 1) != 0) newA3 |= (1<<7); // i4 -> A3.7
    if (((idx >> 3) & 1) != 0) newA3 |= (1<<0); // i3 -> A3.0
    if (((idx >> 2) & 1) != 0) newA3 |= (1<<1); // i2 -> A3.1
    if (((idx >> 1) & 1) != 0) newA3 |= (1<<2); // i1 -> A3.2
    if (((idx >> 0) & 1) != 0) newA3 |= (1<<3); // i0 -> A3.3

    A3 = newA3;
    // A2 remains unchanged (no RX index bits live there).
}


        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string tone)
        {
            if (tone == "0")
            {
                // Index 0
                WriteIndexToA3A2(0, ref A3, ref A2);
                return true;
            }

            int bank = (B3 >> 1) & 1;
            int idx  = -1;

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
            WriteIndexToA3A2(idx, ref A3, ref A2);
            return true;
        }

        // Convenience: pack whole 128→256 nibbles for X2212
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

        // Decode a full channel from raw bytes (used by RgrCodec)
        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            SetLastChannel(A3, A2, A1, A0, B3, B2, B1, B0);
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A3, A2, B3);
            return (tx, rx);
        }
    }
}

