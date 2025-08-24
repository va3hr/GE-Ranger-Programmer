// SPDX-License-Identifier: MIT
// ToneLock.cs — GE Rangr tone decode/encode (TX and RX kept strictly separate).

#nullable enable
using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical CTCSS menu (index 0 == "0") =====
        // (Kept here so ToneLock can render display text without touching Tx/Rx codec classes.)
        private static readonly string[] Cg = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // ===== Optional last-channel cache (legacy UI shim support only) =====
        // Does NOT alter the bytes MainForm reads/writes. Purely informational.
        private static bool _lastValid;
        private static byte _A1, _B1;

        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A1 = A1; _B1 = B1;
            _lastValid = true;
        }

        // ====================================================================
        // TX  (A1 carries index; special-case A1==0x28 uses B1.bit7)
        // ====================================================================

        // Decode TX index from A1/B1
        private static int TxIndexFromA1B1(byte A1, byte B1)
        {
            if (A1 == 0x00) return 0;                 // no tone
            if (A1 == 0x28) return ((B1 & 0x80) != 0) ? 13 : 14; // 103.5 vs 107.2

            return A1 switch
            {
                0x29 or 0xAC or 0xAE => 20,   // 131.8
                0xA4 or 0xE3 or 0xE2 => 16,   // 114.8
                0xA5                   => 19, // 127.3
                0x6D                   => 13, // 103.5
                0xEB                   => 25, // 156.7
                0xEC                   => 11, // 97.4
                0x68 or 0x2A           => 26, // 162.2
                0x98                   => 14, // 107.2
                0x63                   => 15, // 110.9
                _                      => -1
            };
        }

        // Canonical TX display from A1/B1
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            int idx = TxIndexFromA1B1(A1, B1);
            return (idx >= 0 && idx < Cg.Length) ? Cg[idx] : "0";
        }

        // Legacy 3-arg shim used by some UIs — do NOT change call sites.
        // Tries cache first (if previously set), then tries all pairs (order matters).
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

        // Encode TX selection back to A1/B1 (separate from RX)
        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            // Map exact display to menu index
            int idx = Array.IndexOf(Cg, tone);
            if (idx < 0) return false;

            // Default: clear bit7, A1=0 for index 0
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
                default: return false; // unknown TX write encoding for this entry
            }
        }

        // ====================================================================
        // RX  (A3-only 6-bit index with MSB-first numbering; bank in B3.bit1)
        // ====================================================================

        // Form RX index from A3 (MSB-first mapping: i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3])
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

        // Write RX index back into A3 (preserve all other bits). A2 is not used.
        private static void WriteIndexToA3(int idx, ref byte A3)
        {
            idx &= 0x3F;
            // Clear the six target positions {1,0,7,6,5,4} then OR as needed.
            byte a3 = (byte)(A3 & 0x0C); // preserve bits 3..2
            if (((idx >> 5) & 1) != 0) a3 |= (byte)(1 << 1); // i5 -> lsb1
            if (((idx >> 4) & 1) != 0) a3 |= (byte)(1 << 0); // i4 -> lsb0
            if (((idx >> 3) & 1) != 0) a3 |= (byte)(1 << 7); // i3 -> lsb7
            if (((idx >> 2) & 1) != 0) a3 |= (byte)(1 << 6); // i2 -> lsb6
            if (((idx >> 1) & 1) != 0) a3 |= (byte)(1 << 5); // i1 -> lsb5
            if (((idx >> 0) & 1) != 0) a3 |= (byte)(1 << 4); // i0 -> lsb4
            A3 = a3;
        }

        // Canonical RX display from A3/B3 (delegates to RxToneCodec to keep maps separate)
        public static string RxToneFromBytes(byte A3, byte B3, string txDisplay)
        {
            return RxToneCodec.DecodeRxTone(A3, B3, txDisplay);
        }

        // Encode RX selection back to A3/B3. Bank/follow policy: keep current bank (B3.bit1) and clear follow.
        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string display)
        {
            // Normalize "0"/"." to index 0; otherwise map to index (bank decision stays as-is).
            byte idx;
            if (!RxToneLock.TryDisplayToIndex(display, out idx)) idx = 0;

            // Clear follow for explicit tones, per current UI (no "follow TX" option yet).
            B3 = (byte)(B3 & ~0x01);

            WriteIndexToA3(idx, ref A3);
            // A2 is not part of RX index — leave untouched.
            return true;
        }

        // ====================================================================
        // Utilities used by RgrCodec for X2212 nibble packing (no tone logic)
        // ====================================================================
        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            if (image128 == null) throw new ArgumentNullException(nameof(image128));
            var dst = new byte[image128.Length * 2];
            int j = 0;
            for (int i = 0; i < image128.Length; i++)
            {
                byte b = image128[i];
                dst[j++] = (byte)((b >> 4) & 0x0F); // Hi nibble
                dst[j++] = (byte)(b & 0x0F);        // Lo nibble
            }
            return dst;
        }
    }
}
