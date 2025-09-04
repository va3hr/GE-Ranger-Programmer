using System;
using RangrApp.Locked;

namespace X2212
{
    // Thin wrapper that decodes a channel row using ToneLock's CSV-driven nibble method.
    // Keeps the public signature but no longer relies on legacy bit-window indices.
    public static class RgrCodec
    {
        /// <summary>
        /// Decode a channel from A3..B0 (legacy ordering) and return TX/RX labels.
        /// txIndex/rxIndex are provided for compatibility:
        ///   txIndex = 8-bit TX code (EE<<4|EF), rxIndex = 12-bit RX key (E0<<8|E6<<4|E7).
        /// </summary>
        public static void DecodeChannel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            out string txText, out string rxText, out int txIndex, out int rxIndex)
        {
            Span<byte> row8 = stackalloc byte[8] { A0, A1, A2, A3, B0, B1, B2, B3 };

            var tx = ToneLock.ExtractTxNibbles(row8);
            var rx = ToneLock.ExtractRxNibbles(row8);

            txText = ToneLock.GetTxLabel(tx.ToneCodeHighLow, tx.ToneCodeLowLow);
            rxText = ToneLock.GetRxLabel(rx.E0Low,         rx.E6Low,            rx.E7Low);

            txIndex = tx.ComposeTxCode();        // 0..255
            rxIndex = rx.ComposeRxKey();         // 0..4095 (12-bit)
        }

        /// <summary>
        /// Overload returning a tuple (same semantics as above).
        /// </summary>
        public static (string txText, string rxText, int txIndex, int rxIndex) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            DecodeChannel(A3, A2, A1, A0, B3, B2, B1, B0, out var txText, out var rxText, out var txIndex, out var rxIndex);
            return (txText, rxText, txIndex, rxIndex);
        }
    }
}