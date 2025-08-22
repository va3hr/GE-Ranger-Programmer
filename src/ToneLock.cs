// Auto-generated 2025-08-22T03:10:39.220598 from project mapping files.
using System;
using System.Collections.Generic;

namespace RangrApp.Locked {
    public static class ToneLock {
        // Tone menus (UI). "0" = no tone. No "?" in menus.
        public static readonly string[] ToneMenuTx = new [] {
            "0",
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8","97.4","100.0",
            "103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };
        public static readonly string[] ToneMenuRx = ToneMenuTx;
        public static readonly string[] ToneMenuAll = ToneMenuTx;

        public const string ToneNameNull = "0";
        public static string ToneNameNullProp => ToneNameNull;

        // ---- Derived Rx map (idx,bank) → tone ----
        public static readonly string[] RxBank0 = new string[64];
        public static readonly string[] RxBank1 = new string[64];

        // ---- Tx key → CG index (1..33) ----
        private static readonly Dictionary<(int,int,int), int> TxKeyToIndex = new() {
            [(0,1,0x3A)] = 1,
            [(0,0,0x3A)] = 2,
            [(0,1,0x39)] = 3,
            [(0,0,0x49)] = 4,
            [(0,1,0x28)] = 5,
            [(0,1,0x18)] = 6,
            [(0,0,0x37)] = 7,
            [(0,1,0x47)] = 8,
            [(0,1,0x66)] = 9,
            [(0,0,0x15)] = 10,
            [(0,1,0x35)] = 11,
            [(0,1,0x64)] = 12,
            [(0,0,0x23)] = 13,
            [(0,1,0x53)] = 14,
            [(0,0,0x71)] = 15,
            [(0,0,0x4B)] = 16,
            [(1,1,0x30)] = 17,
            [(1,1,0x00)] = 18,
            [(1,0,0x50)] = 19,
            [(0,0,0x2F)] = 20,
            [(0,0,0x7F)] = 21,
            [(0,0,0x4F)] = 22,
            [(0,1,0x1E)] = 23,
            [(0,0,0x7E)] = 24,
            [(0,0,0x5E)] = 25,
            [(0,0,0x3D)] = 26,
            [(0,0,0x1D)] = 27,
            [(0,1,0x7D)] = 28,
            [(0,1,0x6C)] = 29,
            [(0,1,0x5C)] = 30,
            [(0,1,0x4B)] = 31,
            [(1,0,0x51)] = 32,
        };

        // Optional inverse: CG index → key
        private static readonly (int b0b4,int b2b2,int code)[] TxIndexToKey = BuildInverseTx();

        static ToneLock() {
            // seed Rx banks
            RxBank0[0] = "0"; RxBank1[0] = "0";
            RxBank0[3] = "173.8";
            RxBank0[10] = "94.8";
            RxBank0[11] = "162.2";
            RxBank0[17] = "82.5";
            RxBank0[24] = "210.7";
            RxBank0[34] = "100.0";
            RxBank0[38] = "91.5";
            RxBank0[40] = "110.9";
            RxBank0[45] = "192.8";
            RxBank0[51] = "186.2";
            RxBank0[55] = "156.7";
            RxBank0[56] = "203.5";
            RxBank0[60] = "107.2";
            RxBank0[61] = "114.8";
            RxBank0[63] = "141.3";
            RxBank1[1] = "79.7";
            RxBank1[5] = "67.0";
            RxBank1[7] = "146.2";
            RxBank1[12] = "103.5";
            RxBank1[14] = "85.4";
            RxBank1[15] = "131.8";
            RxBank1[16] = "123.0";
            RxBank1[21] = "71.9";
            RxBank1[27] = "167.9";
            RxBank1[32] = "118.8";
            RxBank1[35] = "179.9";
            RxBank1[39] = "151.4";
            RxBank1[41] = "74.4";
            RxBank1[47] = "136.5";
            RxBank1[48] = "127.3";
            RxBank1[57] = "77.0";
            RxBank1[58] = "97.4";
            RxBank1[62] = "88.5";
        }

        private static (int,int,int)[] BuildInverseTx() {
            var arr = new (int,int,int)[34]; // 0..33 (0 unused)
            foreach (var kv in TxKeyToIndex) arr[kv.Value] = kv.Key;
            return arr;
        }

        // ---- Rx helpers (packed 6-bit window + flags) ----
        public static int RxIndexFromA3(byte a3) {
            int b5=(a3>>6)&1, b4=(a3>>7)&1, b3=(a3>>0)&1, b2=(a3>>1)&1, b1=(a3>>2)&1, b0=(a3>>3)&1;
            return (b5<<5)|(b4<<4)|(b3<<3)|(b2<<2)|(b1<<1)|(b0<<0);
        }
        public static byte RxIndexIntoA3(byte a3, int idx) {
            idx &= 0x3F; // 6 bits
            int b5=(idx>>5)&1, b4=(idx>>4)&1, b3=(idx>>3)&1, b2=(idx>>2)&1, b1=(idx>>1)&1, b0=(idx>>0)&1;
            a3 = (byte)((a3 & ~(1<<6)) | (b5<<6));
            a3 = (byte)((a3 & ~(1<<7)) | (b4<<7));
            a3 = (byte)((a3 & ~(1<<0)) | (b3<<0));
            a3 = (byte)((a3 & ~(1<<1)) | (b2<<1));
            a3 = (byte)((a3 & ~(1<<2)) | (b1<<2));
            a3 = (byte)((a3 & ~(1<<3)) | (b0<<3));
            return a3;
        }
        public static int RxBankFromB3(byte b3) => (b3>>1)&1;
        public static byte RxBankIntoB3(byte b3, int bank) {
            b3 = (byte)((b3 & ~(1<<1)) | ((bank&1)<<1));
            return b3;
        }
        public static bool RxFollowBit(byte b3) => (b3 & 0x01) != 0;
        public static byte RxFollowIntoB3(byte b3, bool follow) => follow ? (byte)(b3|1) : (byte)(b3 & ~1);

        // ---- Strict decoders (no snapping) ----
        public static string TxToneFromBytes(byte b0, byte b2, byte b3) {
            bool present = (b3 & 0x80) != 0;
            if (!present) return "0";
            int code = b3 & 0x7F;
            var key = ((b0>>4)&1, (b2>>2)&1, code);
            if (TxKeyToIndex.TryGetValue(key, out int idx)) return ToneMenuTx[idx];
            return "?";
        }
        public static string RxToneFromBytes(byte a3, byte b3, string txToneIfFollow = "0") {
            int idx = RxIndexFromA3(a3);
            int bank = RxBankFromB3(b3);
            if (idx==0) {
                if (RxFollowBit(b3)) return string.IsNullOrEmpty(txToneIfFollow) ? "0" : txToneIfFollow;
                return "0";
            }
            var tbl = bank==0 ? RxBank0 : RxBank1;
            var s = (idx>=0 && idx<tbl.Length) ? tbl[idx] : null;
            return string.IsNullOrEmpty(s) ? "?" : s;
        }

        // ---- Encoders (strict; only exact matches) ----
        public static bool TryEncodeTxTone(string name, ref byte b0, ref byte b2, ref byte b3) {
            if (string.IsNullOrWhiteSpace(name) || name=="." || name=="0") {
                b3 = (byte)(b3 & 0x7F); // clear present
                return true;
            }
            int idx = Array.IndexOf(ToneMenuTx, name);
            if (idx <= 0) return false; // unknown tone name
            var key = TxIndexToKey[idx];
            // set present bit and code
            b3 = (byte)((b3 & 0x80) | 0x80 | (key.code & 0x7F));
            // set B0.bit4 and B2.bit2
            if (key.b0b4==1) b0 = (byte)(b0 | (1<<4)); else b0 = (byte)(b0 & ~(1<<4));
            if (key.b2b2==1) b2 = (byte)(b2 | (1<<2)); else b2 = (byte)(b2 & ~(1<<2));
            return true;
        }

        public static bool TryEncodeRxTone(string name, ref byte a3, ref byte b3) {
            if (string.IsNullOrWhiteSpace(name) || name=="." || name=="0") {
                a3 = RxIndexIntoA3(a3, 0);
                // keep follow bit as-is; caller controls it
                return true;
            }
            // find bank/index for this tone
            int idx0 = Array.IndexOf(RxBank0, name);
            if (idx0 > 0) { a3 = RxIndexIntoA3(a3, idx0); b3 = RxBankIntoB3(b3, 0); return true; }
            int idx1 = Array.IndexOf(RxBank1, name);
            if (idx1 > 0) { a3 = RxIndexIntoA3(a3, idx1); b3 = RxBankIntoB3(b3, 1); return true; }
            return false; // unknown name
        }
    }
}
