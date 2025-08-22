// RgrCodec.cs — channel block helpers. Namespace RangrApp.Codec
using System;

namespace RangrApp.Codec
{
    public static class RgrCodec
    {
        public const int Channels = 16;
        public const int BlockSize = 8;

        // Screen CH → file block index (1-based screen to 0-based file index)
        private static readonly int[] ScreenToFile = { 7,3,1,4,2,5,6,8,15,9,10,12,14,11,13,16 };

        public static int ScreenToFileIndex(int screenCh0Based)
        {
            if (screenCh0Based < 0 || screenCh0Based >= Channels) throw new ArgumentOutOfRangeException(nameof(screenCh0Based));
            return ScreenToFile[screenCh0Based]-1;
        }

        public static Span<byte> GetChannelBlock(byte[] image, int screenChannel1Based)
        {
            int fileIndex = ScreenToFileIndex(screenChannel1Based-1);
            int offset = fileIndex * BlockSize;
            return new Span<byte>(image, offset, BlockSize);
        }
    }
}
