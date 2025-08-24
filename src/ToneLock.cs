// SPDX-License-Identifier: MIT
// ToneLock.cs — GE Rangr tone decode/encode helpers.
// IMPORTANT: TX and RX logic are completely SEPARATE here.
// - TX uses A1/B1 only
// - RX uses A3/B3 only (A3-only 6‑bit index, MSB‑first mapping)

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical CTCSS menu (index 0 == "0") =====
        // NOTE: Exposed via ToneMenuTx/ToneMenuRx for the UI.
        private static readonly string[] Cg = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // Public menus for MainForm
        public static readonly string[] ToneMenuTx = Cg;
        public static readonly string[] ToneMenuRx = Cg;

        // ===== Optional last-channel cache (legacy shim support only) =====
        private static bool _lastValid;
        private static byte _A1, _B1;

        // MainForm may call this after reading a channel; harmless if unused.
        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A1 = A1; _B1 = B1;
            _lastValid = true;
        }

        // ====================================================================
        // TX  (A1 carries index; special-case A1==0x28 uses B1.bit7)
        // ====================================================================

        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            if (A1 == 0x00) return 0; // no tone

            // Split code 0x28 on B1.bit7: 103.5 vs 107.2
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 13 : 14;

            // Known single-byte codes seen in your images/files
            switch (A1)
            {
                case 0xEC: return 11; // 97.4
                case 0x6D: return 13; // 103.5 (alt)
                case 0x98: return 14; // 107.2 (alt)
                case 0x63: return 15; // 110.9
                case 0xA4: // 114.8
                case 0xE3:
                case 0xE2: return 16;
                case 0xA5: return 19; // 127.3
                case 0x29: // 131.8
                case 0xAC:
                case 0xAE: return 20;
                case 0xEB: return 25; // 156.7
                case 0x68: // 162.2
                case 0x2A: return 26;
                default:   return -1; // unknown/unsupported code
            }
        }

        // Decode TX display from A1/B1
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx >= 0 && idx < Cg.Length) ? Cg[idx] : "0";
        }

        // Legacy shim used by existing call sites: tries cache then all pairs
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

        // Encode TX selection back to A1/B1 (RX untouched)
        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;

            if (idx == 0) { A1 = 0x00; B1 = (byte)(B1 & 0x7F); return true; }

            switch (idx)
            {
                case 11: A1 = 0xEC;                     return true; // 97.4
                case 13: A1 = 0x28; B1 = (byte)(B1 | 0x80); return true; // 103.5
                case 14: A1 = 0x28; B1 = (byte)(B1 & 0x7F); return true; // 107.2
                case 15: A1 = 0x63;                     return true; // 110.9
                case 16: A1 = 0xA4;                     return true; // 114.8
                case 19: A1 = 0xA5;                     return true; // 127.3
                case 20: A1 = 0x29;                     return true; // 131.8
                case 25: A1 = 0xEB;                     return true; // 156.7
                case 26: A1 = 0x68;                     return true; // 162.2
                default: return false;
            }
        }

        // ====================================================================
        // RX  (A3-only 6-bit index with MSB-first numbering; bank in B3.bit1)
        // ====================================================================

        // MSB-first spec: i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
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

        // Write RX index back into A3; A2 is not used for RX
        private static void WriteIndexToA3(int idx, ref byte A3)
        {
            idx &= 0x3F;
            // Clear positions {1,0,7,6,5,4}, preserve others (keep 3..2 via mask)
            byte a3 = (byte)(A3 & 0x0C);
            if (((idx >> 5) & 1) != 0) a3 |= (byte)(1 << 1); // i5 -> lsb1
            if (((idx >> 4) & 1) != 0) a3 |= (byte)(1 << 0); // i4 -> lsb0
            if (((idx >> 3) & 1) != 0) a3 |= (byte)(1 << 7); // i3 -> lsb7
            if (((idx >> 2) & 1) != 0) a3 |= (byte)(1 << 6); // i2 -> lsb6
            if (((idx >> 1) & 1) != 0) a3 |= (byte)(1 << 5); // i1 -> lsb5
            if (((idx >> 0) & 1) != 0) a3 |= (byte)(1 << 4); // i0 -> lsb4
            A3 = a3;
        }

        // RX display via RxToneCodec (keeps maps centralized)
        public static string RxToneFromBytes(byte A3, byte B3, string txDisplay)
        {
            return RxToneCodec.DecodeRxTone(A3, B3, txDisplay);
        }

        // Encode RX selection into A3/B3. No "follow TX" in current UI, so clear bit0.
        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string display)
        {
            byte idx;
            if (!RxToneLock.TryDisplayToIndex(display, out idx)) idx = 0;
            B3 = (byte)(B3 & ~0x01); // follow off (no UI for follow today)
            WriteIndexToA3(idx, ref A3);
            // A2 untouched.
            return true;
        }

        // ====================================================================
        // Utility APIs expected by RgrCodec/MainForm
        // ====================================================================

        // Public tone menus (already exposed above)
        // public static readonly string[] ToneMenuTx = Cg;
        // public static readonly string[] ToneMenuRx = Cg;

        // Convert 128 bytes to 256-char ASCII hex (uppercase, no spaces).
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

        public struct TonePair
        {
            public string Tx;
            public string Rx;
            public TonePair(string tx, string rx) { Tx = tx; Rx = rx; }
        }

        // Decode both TX and RX tones for a channel from raw bytes (A3..B0)
        public static TonePair DecodeChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            string tx = TxToneFromBytes(A1, B1);
            string rx = RxToneFromBytes(A3, B3, tx);
            return new TonePair(tx, rx);
        }
    }
}
