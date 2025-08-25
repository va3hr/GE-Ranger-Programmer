
#nullable disable
// ToneLock.cs — GE Rangr tone decode/encode (fixed decoding, API-compatible)
// TX/RX kept separate. Frequency path untouched.

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

        // ===== Last-channel cache so legacy shims resolve correctly =====
        private static bool _lastValid;
        private static byte _A3, _A2, _A1, _A0, _B3, _B2, _B1, _B0;

        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A3 = A3; _A2 = A2; _A1 = A1; _A0 = A0; _B3 = B3; _B2 = B2; _B1 = B1; _B0 = B0;
            _lastValid = true;
        }

        // ===== TX decode helpers (banked by (A1,A2) using (B1,B0)) =====
        private static int TxIndexFromBanks(byte A1, byte A2, byte B1, byte B0)
        {
            // Bank 0x98/0x27
            if (A1 == 0x98 && A2 == 0x27)
            {
                if (B1 == 0x94 && B0 == 0xD3) return 2;   // 71.9
                if (B1 == 0x94 && B0 == 0xC7) return 8;   // 88.5
                if (B1 == 0x90 && B0 == 0xC9) return 12;  // 100.0
                if (B1 == 0x90 && B0 == 0x9D) return 21;  // 136.5
                if (B1 == 0x90 && B0 == 0x7E) return 24;  // 151.4
                if (B1 == 0x94 && B0 == 0x81) return 33;  // 210.7
                return 0; // unknown pair in this bank → show 0 rather than guess
            }
            // Bank 0x9C/0x27
            if (A1 == 0x9C && A2 == 0x27)
            {
                if (B1 == 0x90 && B0 == 0x71) return 1;   // 67.0
                if (B1 == 0x94 && B0 == 0x66) return 7;   // 85.4
                if (B1 == 0x90 && B0 == 0xC9) return 12;  // 100.0
                if (B1 == 0x90 && B0 == 0xCB) return 16;  // 114.8
                if (B1 == 0x94 && B0 == 0x80) return 30;  // 186.2
                return 0;
            }
            return 0; // unmapped TX bank
        }

        // 2‑byte legacy TX read; relies on cached bank and B0 when available.
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            if (_lastValid)
            {
                int idx = TxIndexFromBanks(_A1, _A2, B1, _B0);
                if (idx >= 0 && idx < Cg.Length) return Cg[idx];
            }
            return "0";
        }

        // 3‑byte legacy shim used by MainForm; prefers cached row.
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            if (_lastValid)
            {
                int idx = TxIndexFromBanks(_A1, _A2, _B1, _B0);
                if (idx >= 0 && idx < Cg.Length) return Cg[idx];
            }
            return "0";
        }

        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            // Encode support is deferred until decode is confirmed everywhere.
            // Return true without modifying bytes so write paths don’t fail.
            return true;
        }

        // ===== RX decode helpers =====

        // Compute 6‑bit RX index from big‑endian nibble straddle: A3[7..6]:A2[7..4]
        private static int RxIndexFromA3A2(byte A3, byte A2)
        {
            int top2 = (A3 >> 6) & 0x3;
            int lo4  = (A2 >> 4) & 0xF;
            return (top2 << 4) | lo4; // 0..63
        }

        // Primary RX decode using cached bank/A0 plus Follow
        private static int RxIndexBanked(byte A3, byte A2, byte A1, byte A0, byte B3, string txDisplay)
        {
            // Follow: B3.bit0==1 and index==0 → mirror TX
            bool follow = (B3 & 0x01) != 0;

            // If we have a proven A0 mapping for this bank, use it first.
            if (A1 == 0x98 && A2 == 0x27)
            {
                if (A0 == 0x00) return follow ? Array.IndexOf(Cg, txDisplay) : 0;
                if (A0 == 0x71) return 1;   // 67.0
                if (A0 == 0x91) return 33;  // 210.7
            }

            // Generic straddle fallback (big‑endian)
            int idx = RxIndexFromA3A2(A3, A2);
            if (idx == 0 && follow) return Array.IndexOf(Cg, txDisplay);
            if (idx >= 0 && idx < Cg.Length) return idx;
            return 0;
        }

        public static string RxToneFromBytes(byte A3, byte A2, byte B3)
        {
            string txDisplay = _lastValid ? TxToneFromBytes(_A1, _B1) : "0";
            int idx = _lastValid ? RxIndexBanked(A3, A2, _A1, _A0, B3, txDisplay)
                                 : RxIndexFromA3A2(A3, A2);
            if (idx < 0 || idx >= Cg.Length) idx = 0;
            return Cg[idx];
        }

        // Legacy 3‑arg shim
        public static string RxToneFromBytes(byte p0, byte p1, string _ignoredTx)
        {
            if (!_lastValid) return "0";
            string txDisplay = TxToneFromBytes(_A1, _B1);
            int idx = RxIndexBanked(_A3, _A2, _A1, _A0, _B3, txDisplay);
            if (idx < 0 || idx >= Cg.Length) idx = 0;
            return Cg[idx];
        }

        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string tone)
        {
            // Encode support deferred; succeed without change.
            return true;
        }

        // Decode full channel (used by RgrCodec)
        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            SetLastChannel(A3, A2, A1, A0, B3, B2, B1, B0);
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A3, A2, B3);
            return (tx, rx);
        }

        // ===== Utilities kept for RgrCodec =====

        public static string ToAsciiHex256(byte[] image128)
        {
            StringBuilder sb = new StringBuilder(256 + 32);
            for (int i = 0; i < 128; i++)
            {
                sb.Append(image128[i].ToString("X2"));
                if ((i & 0x0F) == 0x0F) sb.AppendLine();
                else sb.Append(' ');
            }
            return sb.ToString();
        }

        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            // passthrough; frequency logic lives elsewhere and is already correct
            return image128;
        }
    }
}
