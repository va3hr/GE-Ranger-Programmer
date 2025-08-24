// SPDX-License-Identifier: MIT
// ToneLock.cs — GE Rangr tone decode/encode helpers
// TX and RX paths kept completely separate.
// - TX uses A1/B1 only (A1 primary code; A1==0x28 split by B1.bit7)
// - RX uses A3/B3 only (A3-only 6-bit index; MSB-first mapping)

using System;
using System.IO;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical tone menu =====
        private static readonly string[] Cg = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };
        public static readonly string[] ToneMenuTx = Cg;
        public static readonly string[] ToneMenuRx = Cg;

        // ===== Optional last-channel cache (legacy shim support only) =====
        private static bool _lastValid;
        private static byte _A1, _B1;

        // ===== Diagnostic log (best-effort) =====
        private static bool _logInit;
        private static int _row;
        private static string _logPath;
        private static void EnsureLog()
        {
            if (_logInit) return;
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                _logPath = Path.Combine(docs, "ToneDebug.csv");
                if (!File.Exists(_logPath))
                    File.WriteAllText(_logPath, "row,A3,A2,A1,A0,B3,B2,B1,B0,tx_A1B1,tx_shim,rx_from_A3B3,rx_index,bank,follow" + Environment.NewLine, Encoding.UTF8);
                _logInit = true;
            }
            catch { }
        }
        private static void LogRow(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0, string txDirect, string txShim, string rx, int rxIndex, int bank, int follow)
        {
            if (!_logInit) return;
            try
            {
                _row++;
                string line = string.Format("{0},{1:X2},{2:X2},{3:X2},{4:X2},{5:X2},{6:X2},{7:X2},{8:X2},{9},{10},{11},{12},{13},{14}",
                    _row, A3, A2, A1, A0, B3, B2, B1, B0, txDirect, txShim, rx, rxIndex, bank, follow);
                File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A1 = A1; _B1 = B1;
            _lastValid = true;
        }

        // ============================== TX ==============================
        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            // Mapping derived from RANGR6M_cal.csv
            if (A1 == 0x00) return 0;
            if (A1 == 0xEF) return 20; // 131.8
            if (A1 == 0xEC) return 20; // 131.8
            if (A1 == 0xAE) return 16; // 114.8
            if (A1 == 0x6D) return 13; // 103.5
            if (A1 == 0x2D) return 25; // 156.7
            if (A1 == 0xAC) return 11; // 97.4
            if (A1 == 0x68) return 26; // 162.2
            if (A1 == 0x98) return 26; // 162.2
            if (A1 == 0xA5) return 19; // 127.3
            if (A1 == 0x63) return 16; // 114.8
            if (A1 == 0xE3) return 13; // 103.5
            if (A1 == 0xE2) return 13; // 103.5

            // Special case 0x28 chooses between 107.2 and 110.9 from B1.bit7
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 14 : 15;

            // Fallback: unknown → 0
            return 0;
        }

        public static string TxToneFromBytes(byte A1, byte B1)
        {
            _A1 = A1; _B1 = B1; _lastValid = true;
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx >= 0 && idx < Cg.Length) ? Cg[idx] : "0";
        }
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            if (_lastValid)
            {
                string t = TxToneFromBytes(_A1, _B1);
                if (t != "0") return t;
            }
            string t01 = TxToneFromBytes(p0, p1); if (t01 != "0") return t01;
            string t02 = TxToneFromBytes(p0, p2); if (t02 != "0") return t02;
            string t12 = TxToneFromBytes(p1, p2); if (t12 != "0") return t12;
            return "0";
        }
        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;
            if (idx == 0) { A1 = 0x00; B1 = (byte)(B1 & 0x7F); return true; }

            // encode the subset we know for sure; others left as-is
            switch (idx)
            {
                case 11: A1 = 0xAC; return true; // 97.4
                case 13: A1 = 0x6D; return true; // 103.5
                case 14: A1 = 0x28; B1 = (byte)(B1 | 0x80); return true; // 107.2
                case 15: A1 = 0x28; B1 = (byte)(B1 & 0x7F); return true; // 110.9
                case 16: A1 = 0xAE; return true; // 114.8
                case 19: A1 = 0xA5; return true; // 127.3
                case 20: A1 = 0xA4; return true; // 131.8
                case 25: A1 = 0x2D; return true; // 156.7
                case 26: A1 = 0x68; return true; // 162.2
                default: return false;
            }
        }

        // ============================== RX ==============================
        private static int RxIndexFromA3(byte A3)
        {
            int i5 = (A3 >> 1) & 1; // A3.6 -> lsb1
            int i4 = (A3 >> 0) & 1; // A3.7 -> lsb0
            int i3 = (A3 >> 7) & 1; // A3.0 -> lsb7
            int i2 = (A3 >> 6) & 1; // A3.1 -> lsb6
            int i1 = (A3 >> 5) & 1; // A3.2 -> lsb5
            int i0 = (A3 >> 4) & 1; // A3.3 -> lsb4
            return ((i5<<5)|(i4<<4)|(i3<<3)|(i2<<2)|(i1<<1)|i0) & 0x3F;
        }
        private static void WriteIndexToA3(int idx, ref byte A3)
        {
            idx &= 0x3F;
            byte a3 = (byte)(A3 & 0x0C);
            if (((idx >> 5) & 1) != 0) a3 |= (byte)(1 << 1);
            if (((idx >> 4) & 1) != 0) a3 |= (byte)(1 << 0);
            if (((idx >> 3) & 1) != 0) a3 |= (byte)(1 << 7);
            if (((idx >> 2) & 1) != 0) a3 |= (byte)(1 << 6);
            if (((idx >> 1) & 1) != 0) a3 |= (byte)(1 << 5);
            if (((idx >> 0) & 1) != 0) a3 |= (byte)(1 << 4);
            A3 = a3;
        }
        public static string RxToneFromBytes(byte A3, byte B3, string txDisplay)
        {
            return RxToneCodec.DecodeRxTone(A3, B3, txDisplay);
        }
        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string display)
        {
            byte idx;
            if (!RxToneLock.TryDisplayToIndex(display, out idx)) idx = 0;
            B3 = (byte)(B3 & ~0x01); // follow off
            WriteIndexToA3(idx, ref A3);
            return true;
        }

        // ============================== RgrCodec helpers ==============================
        public static string ToAsciiHex256(byte[] image128)
        {
            if (image128 == null || image128.Length != 128)
                throw new ArgumentException("image128 must be 128 bytes");
            char[] buf = new char[256];
            int k = 0;
            const string hex = "0123456789ABCDEF";
            for (int i = 0; i < 128; i++)
            {
                byte b = image128[i];
                buf[k++] = hex[(b >> 4) & 0xF];
                buf[k++] = hex[b & 0xF];
            }
            return new string(buf);
        }
        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            if (image128 == null || image128.Length != 128)
                throw new ArgumentException("image128 must be 128 bytes");
            var dst = new byte[256];
            int j = 0;
            for (int i = 0; i < 128; i++)
            {
                byte b = image128[i];
                dst[j++] = (byte)((b >> 4) & 0x0F);
                dst[j++] = (byte)(b & 0x0F);
            }
            return dst;
        }

        public struct TonePair
        {
            public string Tx;
            public string Rx;
            public TonePair(string tx, string rx) { Tx = tx; Rx = rx; }
        }

        public static TonePair DecodeChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            EnsureLog();
            SetLastChannel(A3, A2, A1, A0, B3, B2, B1, B0);

            string txDirect = TxToneFromBytes(A1, B1);
            string txShim   = TxToneFromBytes(B0, B2, B3); // legacy path (for comparison only)
            string rx       = RxToneFromBytes(A3, B3, txDirect);

            int rxIdx = RxIndexFromA3(A3);
            int bank  = (B3 >> 1) & 1;
            int follow= B3 & 1;
            LogRow(A3,A2,A1,A0,B3,B2,B1,B0, txDirect, txShim, rx, rxIdx, bank, follow);

            return new TonePair(txDirect, rx);
        }
    }
}
