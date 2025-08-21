// ToneLock_Final.cs — GE Rangr tones (TX gold + RX derived)
// Drop-in: decode/encode + X2212 helpers + legacy adapters
using System;
using System.Collections.Generic;
using System.IO;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        public const string ToneNameNull = "0";
        public static readonly string[] CgTx = new [] { "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7" };

        private static readonly int[] ScreenToFileBlock =
            new [] { 0, 7,3,1,4,2,5,6,8,15,9,10,12,14,11,13,16 };

        private static (int,int,int) GetTxKey(byte b0, byte b2, byte b3) => ((b0>>4)&1, (b2>>2)&1, b3 & 0x7F);
        private static bool GetTxPresentB3(byte b3) => (b3 & 0x80) != 0;
        private static byte SetTxPresentB3(byte b3, bool present) => present ? (byte)(b3 | 0x80) : (byte)(b3 & 0x7F);

        private static int GetRxIndexA3(byte a3)
        {
            int b5=(a3>>6)&1, b4=(a3>>7)&1, b3=(a3>>0)&1, b2=(a3>>1)&1, b1=(a3>>2)&1, b0=(a3>>3)&1;
            return (b5<<5) | (b4<<4) | (b3<<3) | (b2<<2) | (b1<<1) | (b0<<0);
        }
        private static byte SetRxIndexA3(byte a3, int idx6)
        {
            idx6 &= 0x3F;
            int b5=(idx6>>5)&1, b4=(idx6>>4)&1, b3=(idx6>>3)&1, b2=(idx6>>2)&1, b1=(idx6>>1)&1, b0=(idx6>>0)&1;
            a3 = (byte)((a3 & ~(1<<6)) | (b5<<6));
            a3 = (byte)((a3 & ~(1<<7)) | (b4<<7));
            a3 = (byte)((a3 & ~(1<<0)) | (b3<<0));
            a3 = (byte)((a3 & ~(1<<1)) | (b2<<1));
            a3 = (byte)((a3 & ~(1<<2)) | (b1<<2));
            a3 = (byte)((a3 & ~(1<<3)) | (b0<<3));
            return a3;
        }
        private static int  GetRxBankB3(byte b3) => (b3>>1) & 1;
        private static byte SetRxBankB3(byte b3, int bank01) => (byte)((b3 & ~(1<<1)) | ((bank01 & 1)<<1));

        public static (int byteIndex, int bit) FollowTxBit = (7, 0);
        private static bool GetFollowTx(byte[] img, int off)
        {
            var (bi, bit) = FollowTxBit; byte v = img[off + bi - 4]; return ((v>>bit)&1)==1;
        }
        private static void SetFollowTx(byte[] img, int off, bool on)
        {
            var (bi, bit) = FollowTxBit; ref byte v = ref img[off + bi - 4];
            v = on ? (byte)(v | (1<<bit)) : (byte)(v & ~(1<<bit));
        }

        public static readonly string[] RxBank0 = new string[64];
        public static readonly string[] RxBank1 = new string[64];

        private static readonly Dictionary<(int,int,int), int> TxKeyToIndex = new()
        {
            { (0,0,0x2A), 1 },
            { (0,0,0x37), 1 },
            { (0,1,0x39), 2 },
            { (0,1,0x3A), 3 },
            { (0,0,0x49), 4 },
            { (0,0,0x3A), 5 },
            { (0,1,0x28), 6 },
            { (0,1,0x18), 7 },
            { (0,1,0x47), 8 },
            { (0,0,0x71), 9 },
            { (1,1,0x01), 9 },
            { (0,1,0x66), 10 },
            { (0,0,0x15), 11 },
            { (0,1,0x64), 12 },
            { (0,1,0x53), 13 },
            { (0,1,0x35), 14 },
            { (0,0,0x23), 15 },
            { (0,0,0x5B), 15 },
            { (0,0,0x4B), 16 },
            { (0,1,0x1E), 17 },
            { (1,0,0x50), 18 },
            { (1,1,0x30), 19 },
            { (0,0,0x2F), 20 },
            { (1,1,0x00), 21 },
            { (0,0,0x7F), 22 },
            { (0,0,0x4F), 23 },
            { (0,0,0x7E), 24 },
            { (0,1,0x4B), 25 },
            { (0,0,0x5E), 26 },
            { (0,0,0x3D), 27 },
            { (0,1,0x7D), 28 },
            { (0,1,0x5C), 29 },
            { (0,0,0x1D), 30 },
            { (0,1,0x6C), 31 },
            { (1,0,0x51), 32 },
            { (0,0,0x00), 33 },
        };
        private static readonly Dictionary<int,(int,int,int)> TxIndexToKey = new()
        {
            { 1, (0,0,0x2A) },
            { 2, (0,1,0x39) },
            { 3, (0,1,0x3A) },
            { 4, (0,0,0x49) },
            { 5, (0,0,0x3A) },
            { 6, (0,1,0x28) },
            { 7, (0,1,0x18) },
            { 8, (0,1,0x47) },
            { 9, (0,0,0x71) },
            { 10, (0,1,0x66) },
            { 11, (0,0,0x15) },
            { 12, (0,1,0x64) },
            { 13, (0,1,0x53) },
            { 14, (0,1,0x35) },
            { 15, (0,0,0x23) },
            { 16, (0,0,0x4B) },
            { 17, (0,1,0x1E) },
            { 18, (1,0,0x50) },
            { 19, (1,1,0x30) },
            { 20, (0,0,0x2F) },
            { 21, (1,1,0x00) },
            { 22, (0,0,0x7F) },
            { 23, (0,0,0x4F) },
            { 24, (0,0,0x7E) },
            { 25, (0,1,0x4B) },
            { 26, (0,0,0x5E) },
            { 27, (0,0,0x3D) },
            { 28, (0,1,0x7D) },
            { 29, (0,1,0x5C) },
            { 30, (0,0,0x1D) },
            { 31, (0,1,0x6C) },
            { 32, (1,0,0x51) },
            { 33, (0,0,0x00) },
        };

        static ToneLock()
        {
            for (int i=0;i<64;i++) { RxBank0[i] = "?"; RxBank1[i] = "?"; }
            RxBank0[0] = "0"; RxBank1[0] = "0";
            // seed known derived Rx tones (from your images)
            RxBank1[21] = "131.8";
            RxBank1[63] = "162.2";
            RxBank0[ 3] = "107.2";
            RxBank0[35] = "127.3";
            RxBank0[63] = "114.8";
        }

        // ---------- Public decode ----------
        public static (int? txIndex, string txText) DecodeTx(byte[] image128, int screenCh1to16)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk*8;
            byte b0=image128[off+4], b2=image128[off+6], b3=image128[off+7];
            if (!GetTxPresentB3(b3)) return (0,"0");
            var key = GetTxKey(b0,b2,b3);
            if (TxKeyToIndex.TryGetValue(key, out int idx))
                return (idx, CgTx[idx]);
            return (null, "?");
        }
        public static (int rxIndex, int bank, bool followTx, string rxText) DecodeRx(byte[] image128, int screenCh1to16)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk*8;
            byte a3=image128[off+3], b3=image128[off+7];
            int idx = GetRxIndexA3(a3);
            int bank = GetRxBankB3(b3);
            if (idx==0)
            {
                bool follow = GetFollowTx(image128, off);
                if (follow)
                {
                    var (_, txText) = DecodeTx(image128, screenCh1to16);
                    return (idx, bank, true, txText ?? "0");
                }
                return (idx, bank, false, "0");
            }
            var tbl = bank==0 ? RxBank0 : RxBank1;
            string text = (idx>=0 && idx<64 && tbl[idx] != null) ? tbl[idx] : "?";
            return (idx, bank, false, text);
        }

        // ---------- Public encode ----------
        public static bool TrySetTxTone(byte[] image128, int screenCh1to16, int cgIndex, (int,int,int)? preferredKey=null)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk*8;
            ref byte B0 = ref image128[off+4];
            ref byte B2 = ref image128[off+6];
            ref byte B3 = ref image128[off+7];
            if (cgIndex == 0) { B3 = SetTxPresentB3(B3,false); return true; }
            (int,int,int) key;
            if (preferredKey.HasValue) key = preferredKey.Value;
            else if (!TxIndexToKey.TryGetValue(cgIndex, out key)) return false;
            B3 = SetTxPresentB3(B3,true);
            B3 = (byte)((B3 & 0x80) | (key.Item3 & 0x7F));
            B0 = (byte)((B0 & ~(1<<4)) | ((key.Item1 & 1)<<4));
            B2 = (byte)((B2 & ~(1<<2)) | ((key.Item2 & 1)<<2));
            return true;
        }
        public static void SetRxTone(byte[] image128, int screenCh1to16, int rxIndex0to63, int bank0or1, bool? followTx=null)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk*8;
            ref byte A3 = ref image128[off+3];
            ref byte B3 = ref image128[off+7];
            A3 = SetRxIndexA3(A3, rxIndex0to63);
            B3 = SetRxBankB3(B3, bank0or1);
            if (followTx.HasValue && rxIndex0to63==0) SetFollowTx(image128, off, followTx.Value);
        }

        // ---------- Legacy adapters ----------
        public static string TxToneFromBytes(byte[] image128, int screenCh1to16)
        { var (_, t) = DecodeTx(image128, screenCh1to16); return t ?? ToneNameNull; }
        public static string RxToneFromBytes(byte[] image128, int screenCh1to16)
        { var (_,_,_, t) = DecodeRx(image128, screenCh1to16); return t ?? ToneNameNull; }
        public static string TxToneFromBytes(byte b0, byte b2, byte b3)
        {
            if (!GetTxPresentB3(b3)) return ToneNameNull;
            var key = GetTxKey(b0,b2,b3);
            if (TxKeyToIndex.TryGetValue(key, out int idx)) return CgTx[idx];
            return "?";
        }
        public static string RxToneFromBytes(byte a3, byte b3, string txToneIfFollow=null)
        {
            int idx = GetRxIndexA3(a3);
            int bank = GetRxBankB3(b3);
            if (idx==0)
            {
                bool follow = (b3 & 0x01) != 0;
                if (follow && !string.IsNullOrEmpty(txToneIfFollow)) return txToneIfFollow;
                return ToneNameNull;
            }
            var tbl = bank==0 ? RxBank0 : RxBank1;
            if (idx>=0 && idx<64 && tbl[idx]!=null) return tbl[idx];
            return "?";
        }

        // ---------- I/O helpers ----------
        public static byte[] ReadRgrBinary(string path) => File.ReadAllBytes(path);
        public static byte[] ReadRgrHexDump(string path)
        {
            var hex = File.ReadAllText(path);
            var cleaned = System.Text.RegularExpressions.Regex.Replace(hex, "[^0-9A-Fa-f]", "");
            var bytes = new byte[cleaned.Length/2];
            for (int i=0;i<bytes.Length;i++) bytes[i] = Convert.ToByte(cleaned.Substring(2*i,2),16);
            return bytes;
        }
        public static void WriteRgrBinary(string path, byte[] image128) => File.WriteAllBytes(path, image128);
        public static void WriteRgrHexDump(string path, byte[] image128)
        {
            using var sw = new StreamWriter(path);
            for (int i=0;i<image128.Length;i++)
            { sw.Write(image128[i].ToString("X2")); if ((i%16)==15) sw.WriteLine(); else sw.Write(' '); }
        }
        public static byte[] ToX2212Nibbles(byte[] image128)
        { var n = new byte[256]; int p=0; for (int i=0;i<128;i++) { n[p++]=(byte)((image128[i]>>4)&0xF); n[p++]=(byte)(image128[i]&0xF);} return n; }
        public static byte[] FromX2212Nibbles(byte[] nibbles256)
        { if (nibbles256==null || nibbles256.Length!=256) throw new ArgumentException("Need 256 nibbles");
          var b=new byte[128]; int p=0; for (int i=0;i<128;i++) { b[i]=(byte)((nibbles256[p++]<<4)|(nibbles256[p++]&0xF)); } return b; }
    }
}
