// ToneLock.cs — minimal, compile-stable tone decode/encode (RANGR6M2)
// Safe for older C# compilers: no tuples, no LINQ, no target-typed 'new'.
using System;
using System.Collections.Generic;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical Channel Guard list. Index 0 => "0".
        public static readonly string[] Cg = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // ---------------- TX (observed RANGR6M2) ----------------
        // Tx is encoded primarily in A1; 0x28 is disambiguated by B1.bit7.
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
            // 0x28 handled below
        };

        private struct TxSpec
        {
            public byte A1;
            public bool HasB1Bit7;
            public bool B1Bit7Value;
        }

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
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 13 : 14;
            int idx;
            if (TxA1ToIdx.TryGetValue(A1, out idx)) return idx;
            return -1;
        }

        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            if (idx < 0 || idx >= Cg.Length) return "?";
            return Cg[idx];
        }

        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;
            TxSpec spec;
            if (!TxIdxToA1.TryGetValue(idx, out spec)) return false;
            A1 = spec.A1;
            if (spec.HasB1Bit7)
            {
                if (spec.B1Bit7Value) B1 |= 0x80;
                else B1 &= 0x7F;
            }
            return true;
        }

        // ---------------- RX (RANGR6M2) ----------------
        // Rx uses a 6-bit index in A0 with bit order [7,6,3,2,1,0]; bank = B3.bit1.
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
            byte a0 = (byte)(originalA0 & 0b00111100); // clear target bits
            if (((idx >> 5) & 1) != 0) a0 |= 1 << 7;
            if (((idx >> 4) & 1) != 0) a0 |= 1 << 6;
            if (((idx >> 3) & 1) != 0) a0 |= 1 << 3;
            if (((idx >> 2) & 1) != 0) a0 |= 1 << 2;
            if (((idx >> 1) & 1) != 0) a0 |= 1 << 1;
            if (((idx >> 0) & 1) != 0) a0 |= 1 << 0;
            return a0;
        }

        // Use nested dictionaries to avoid tuple keys (bank -> (idx -> tone))
        private static readonly Dictionary<int, Dictionary<int, string>> RxMap =
            new Dictionary<int, Dictionary<int, string>>()
        {
            { 0, new Dictionary<int, string>()
                {
                    {  0,"210.7" }, {  3,"103.5" }, {  5,"94.8"  }, {  7,"85.4"  },
                    {  8,"79.7"  }, { 10,"67.0"  }, { 12,"173.8" }, { 13,"162.2" },
                    { 14,"146.2" }, { 15,"131.8" }, { 16,"118.8" }, { 17,"110.9" },
                    { 20,"100.0" }, { 22,"91.5"  }, { 25,"74.4"  }, { 27,"192.8" },
                    { 28,"179.9" }, { 30,"151.4" }, { 31,"136.5" }, { 32,"123.0" },
                    { 40,"82.5"  }, { 42,"71.9"  }, { 45,"167.9" }, { 48,"127.3" },
                    { 49,"203.5" }, { 51,"107.2" }, { 53,"97.4"  }, { 55,"88.5"  },
                    { 57,"77.0"  }, { 59,"114.8" }, { 60,"186.2" }, { 62,"156.7" },
                    { 63,"141.3" }
                }
            },
            { 1, new Dictionary<int, string>()
                {
                    { 12,"107.2" },
                    { 63,"162.2" }
                }
            }
        };

        public static string RxToneFromBytes(byte A0, byte B3)
        {
            int bank = (B3 >> 1) & 1;
            int idx = IndexFromA0(A0);
            if (idx == 0) return "0";
            Dictionary<int,string> inner;
            if (!RxMap.TryGetValue(bank, out inner)) return "?";
            string tone;
            if (inner.TryGetValue(idx, out tone)) return tone;
            return "?";
        }

        public static bool TrySetRxTone(ref byte A0, ref byte B3, string tone)
        {
            if (tone == "0") { A0 = 0x00; return true; }
            int bank = (B3 >> 1) & 1;
            Dictionary<int,string> inner;
            if (!RxMap.TryGetValue(bank, out inner)) return false;
            foreach (var kv in inner)
            {
                if (kv.Value == tone)
                {
                    A0 = WriteIndexToA0(kv.Key, A0);
                    return true;
                }
            }
            return false;
        }

        // -------- Convenience (optional) --------
        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A0, B3);
            return (tx, rx);
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
                    nibbles[n++] = (byte)((b >> 4) & 0x0F);
                    nibbles[n++] = (byte)(b & 0x0F);
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
