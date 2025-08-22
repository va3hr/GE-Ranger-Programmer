// Auto-generated 2025-08-22T03:10:39.221373.
// RgrCodec.cs — 16 channels × 8-byte blocks, nibble-big-endian stored bytes.
// Frequency decode/encode remains in FreqLock.cs (frozen).
using System;
using System.Collections.Generic;
using System.IO;
using RangrApp.Locked;

namespace RangrApp.Codec {
    public static class RgrCodec {
        public const int ChannelCount = 16;
        public const int BlockSize = 8;
        public const int ImageSize = ChannelCount * BlockSize; // 128 bytes

        // Storage permutation used across the project (screen CH01..CH16 → file block index, 0-based):
        // [1-based: 7,3,1,4,2,5,6,8,15,9,10,12,14,11,13,16]
        private static readonly int[] ScreenToBlock = new int[] { 6,2,0,3,1,4,5,7,14,8,9,11,13,10,12,15 };
        private static readonly int[] BlockToScreen = BuildInverse(ScreenToBlock);

        private static int[] BuildInverse(int[] s2b) {
            var inv = new int[s2b.Length];
            for (int screen=0; screen<s2b.Length; screen++) inv[s2b[screen]] = screen;
            return inv;
        }

        public static byte[] LoadImage(string path) => File.ReadAllBytes(path);
        public static void SaveImage(string path, byte[] image) => File.WriteAllBytes(path, image);

        public static Span<byte> GetChannelBlock(byte[] image, int screenChannel) {
            if (image==null || image.Length < ImageSize) throw new ArgumentException("Bad RGR image length");
            if (screenChannel < 0 || screenChannel >= ChannelCount) throw new ArgumentOutOfRangeException(nameof(screenChannel));
            int blk = ScreenToBlock[screenChannel];
            int off = blk * BlockSize;
            return new Span<byte>(image, off, BlockSize);
        }

        // ---- Decode helpers (tones only; frequencies handled elsewhere) ----
        public static string DecodeTxTone(byte[] image, int screenChannel) {
            var blk = GetChannelBlock(image, screenChannel);
            byte b0 = blk[4], b2 = blk[6], b3 = blk[7];
            return ToneLock.TxToneFromBytes(b0,b2,b3);
        }

        public static string DecodeRxTone(byte[] image, int screenChannel, string txToneIfFollow = "0") {
            var blk = GetChannelBlock(image, screenChannel);
            byte a3 = blk[3], b3 = blk[7];
            return ToneLock.RxToneFromBytes(a3,b3,txToneIfFollow);
        }

        // ---- Encode helpers (strict; no snapping) ----
        public static bool TryEncodeTxTone(byte[] image, int screenChannel, string name) {
            var blk = GetChannelBlock(image, screenChannel);
            byte b0 = blk[4], b2 = blk[6], b3 = blk[7];
            bool ok = ToneLock.TryEncodeTxTone(name, ref b0, ref b2, ref b3);
            if (!ok) return false;
            blk[4]=b0; blk[6]=b2; blk[7]=b3;
            return true;
        }

        public static bool TryEncodeRxTone(byte[] image, int screenChannel, string name, bool follow=false) {
            var blk = GetChannelBlock(image, screenChannel);
            byte a3 = blk[3], b3 = blk[7];
            bool ok = ToneLock.TryEncodeRxTone(name, ref a3, ref b3);
            if (!ok) return false;
            b3 = ToneLock.RxFollowIntoB3(b3, follow);
            blk[3]=a3; blk[7]=b3;
            return true;
        }

        // Utilities for callers that need raw indices/bits (debug/diagnostic only)
        public static int GetRawRxIndex(byte[] image, int screenChannel) {
            var blk = GetChannelBlock(image, screenChannel);
            return ToneLock.RxIndexFromA3(blk[3]);
        }
        public static int GetRawRxBank(byte[] image, int screenChannel) {
            var blk = GetChannelBlock(image, screenChannel);
            return ToneLock.RxBankFromB3(blk[7]);
        }
        public static bool GetRawRxFollow(byte[] image, int screenChannel) {
            var blk = GetChannelBlock(image, screenChannel);
            return ToneLock.RxFollowBit(blk[7]);
        }
    }
}
