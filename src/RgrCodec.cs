using System;
using RangrApp.Locked;

namespace X2212
{
    public static class RgrCodec
    {
        public static void DecodeChannel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            byte A7, byte A6, byte A5, byte A4,
            byte B7, byte B6, byte B5, byte B4,
            out string txText, out string rxText)
        {
            Span<byte> row16 = stackalloc byte[16] { A0,A1,A2,A3, B0,B1,B2,B3, A4,A5,A6,A7, B4,B5,B6,B7 };
            var (_, txLabel) = ToneLock.ReadTxFromRow(row16);
            var (_, rxLabel) = ToneLock.ReadRxFromRow(row16);
            txText = txLabel;
            rxText = rxLabel;
        }
    }
}