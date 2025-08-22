
// Auto-generated 2025-08-22T05:47:22.157761 — RgrCodec.cs
using System;
using System.Collections.Generic;
using System.IO;
using RangrApp.Locked;

namespace RangrApp.Codec {
    public static class RgrCodec {
        public const int ChannelCount = 16;
        public const int BlockSize = 8;
        private static readonly int[] ScreenToBlock = new int[] { 6,2,0,3,1,4,5,7,14,8,9,11,13,10,12,15 };

        public static List<(string Tx, string Rx)> DecodeTones(string path) {
            var img = File.ReadAllBytes(path);
            var list = new List<(string,string)>(ChannelCount);
            for (int ch=0; ch<ChannelCount; ch++) {
                var blk = GetBlock(img, ch);
                byte A3=blk[3], B0=blk[4], B2=blk[6], B3=blk[7];
                string tx = ToneLock.TxToneFromBytes(B0,B2,B3);
                string rx = ToneLock.RxToneFromBytes(A3,B3, tx);
                list.Add((tx,rx));
            }
            return list;
        }

        private static Span<byte> GetBlock(byte[] img, int screenCh) {
            int off = ScreenToBlock[screenCh]*BlockSize;
            return new Span<byte>(img, off, BlockSize);
        }
    }
}
