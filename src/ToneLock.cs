
#nullable enable
using System;
using System.Text;
using System.Collections.Generic;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical CTCSS tone list; index 0 = "0"
        public static readonly string[] ToneMenuTx = new[] {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8",
            "136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };
        public static readonly string[] ToneMenuRx = ToneMenuTx;

        public struct Pair { public string Tx; public string Rx; public Pair(string tx,string rx) { Tx=tx; Rx=rx; } }

        // ============ Helpers preserved for RgrCodec ============
        public static string ToAsciiHex256(byte[] image128)
        {
            var sb = new StringBuilder(16*8*3);
            for (int i=0;i<128;i++) { if (i>0) sb.Append(' '); sb.Append(image128[i].ToString("X2")); }
            return sb.ToString();
        }

        public static byte[] ToX2212Nibbles(byte[] image128) => image128; // passthrough (write-path not touched)

        private static bool _lastValid;
        private static byte _A3,_A2,_A1,_A0,_B3,_B2,_B1,_B0;
        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        { _A3=A3; _A2=A2; _A1=A1; _A0=A0; _B3=B3; _B2=B2; _B1=B1; _B0=B0; _lastValid=true; }

        // ============ RX decode (banked, from A0) ============
        private static readonly Dictionary<(byte a1,byte a2), Dictionary<byte,int>> RxBankA0ToIndex
            = new Dictionary<(byte,byte), Dictionary<byte,int>>
            {
            { (0x2C, 0x27), new System.Collections.Generic.Dictionary<byte,int>{ {0xDC, 30}, {0xE3, 14} } },
            { (0x2C, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x76, 9}, {0xDE, 25} } },
            { (0x2D, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x38, 5}, {0x7F, 21} } },
            { (0x2E, 0x17), new System.Collections.Generic.Dictionary<byte,int>{ {0x23, 13}, {0x6C, 29} } },
            { (0x67, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x9D, 27}, {0xC5, 11} } },
            { (0x6C, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x7E, 24}, {0xC7, 8} } },
            { (0x6D, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x2F, 20}, {0xC9, 4} } },
            { (0x98, 0x27), new System.Collections.Generic.Dictionary<byte,int>{ {0x00, 0}, {0x71, 1}, {0x91, 33} } },
            { (0x9C, 0x27), new System.Collections.Generic.Dictionary<byte,int>{ {0x4B, 31}, {0x71, 15} } },
            { (0xA4, 0x47), new System.Collections.Generic.Dictionary<byte,int>{ {0x3A, 1}, {0x40, 17} } },
            { (0xA5, 0x47), new System.Collections.Generic.Dictionary<byte,int>{ {0xCB, 16}, {0xD1, 32} } },
            { (0xAC, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x2E, 23}, {0x37, 7} } },
            { (0xAE, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x49, 3}, {0xD0, 19} } },
            { (0xE6, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x0C, 28}, {0x74, 12} } },
            { (0xE7, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x15, 10}, {0x3D, 26} } },
            { (0xEC, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0xA8, 6}, {0xCF, 22} } },
            { (0xEF, 0x37), new System.Collections.Generic.Dictionary<byte,int>{ {0x80, 18}, {0xBA, 2} } }
            };

        private static string DecodeRx(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0, string txDisplay)
        {
            // Follow handling: if banked index resolves to 0 and follow flag set, mirror TX
            int idx = 0;
            if (RxBankA0ToIndex.TryGetValue((A1,A2), out var map))
            {
                if (!map.TryGetValue(A0, out idx)) idx = 0;
            }
            else idx = 0;

            bool follow = (B3 & 0x01) != 0;
            if (idx==0 && follow) return txDisplay;
            if (idx<0 || idx>=ToneMenuRx.Length) return "0";
            return ToneMenuRx[idx];
        }

        // ============ TX decode (bank-specific via B1,B0) ============
        private static int DecodeTxIndex(byte A1, byte A2, byte B1, byte B0)
        {
            if (A1==0x98 && A2==0x27)
            {
                case (0x94, 0xd3): return 2;
                case (0x94, 0xc7): return 8;
                case (0x90, 0xc9): return 12;
                case (0x90, 0x9d): return 21;
                case (0x90, 0x7e): return 24;
                case (0x94, 0x81): return 33;
                return -1;
            }
            if (A1==0x9C && A2==0x27)
            {
                case (0x90, 0x71): return 1;
                case (0x94, 0x66): return 7;
                case (0x90, 0xc9): return 12;
                case (0x90, 0xcb): return 16;
                case (0x94, 0x80): return 30;
                return -1;
            }
            return -1;
        }

        private static string DecodeTx(byte A1, byte A2, byte B1, byte B0)
        {
            int idx = DecodeTxIndex(A1,A2,B1,B0);
            if (idx>=0 && idx < ToneMenuTx.Length) return ToneMenuTx[idx];
            return "0";
        }

        // Entry point used by RgrCodec
        public static Pair DecodeChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            var tx = DecodeTx(A1,A2,B1,B0);
            var rx = DecodeRx(A3,A2,A1,A0,B3,B2,B1,B0, tx);
            return new Pair(tx, rx);
        }

        // Placeholders to satisfy build; proper encode requires B0 too and RX A0 write rules.
        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone) => false;
        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string tone) => false;
    }
}
