// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Project standards:
//   • Canonical tone list is frozen unless explicitly changed.
//   • Bit windows are explicit, MSB→LSB, with clear names.
//   • Build the index progressively (one integer, bits OR’d in order).
//   • SourceNames + Inspect helpers stay in lockstep for diagnostics.
//   • No per-file overrides or routing tables.
// -----------------------------------------------------------------------------
// Terminology (per channel row of 8 bytes):
//   RowA0 RowA1 RowA2 RowA3   RowB0 RowB1 RowB2 RowB3
//   indexes:      0    1    2     3      4     5     6     7
// -----------------------------------------------------------------------------

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ---------------------------------------------------------------------
        // Canonical CTCSS tone table: index 0 is "0" (no tone).
        // Note: There is NO "114.1" entry by design.
        // ---------------------------------------------------------------------
        private static readonly string[] CanonicalTonesNoZero =
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // Menus used by the UI (no "0" here — UI shows "0" as blank/null)
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index → label map (index 0 = "0")
        private static readonly string[] ToneByIndex = BuildToneByIndex();
        public static string[] Cg => ToneByIndex;

        private static string[] BuildToneByIndex()
        {
            var map = new string[CanonicalTonesNoZero.Length + 1];
            map[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++)
                map[i + 1] = CanonicalTonesNoZero[i];
            return map;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------
        private static int ExtractBit(byte value, int bitIndex) => (value >> bitIndex) & 1;
        private static string LabelFromIndex(int index) =>
            (index >= 0 && index < ToneByIndex.Length) ? ToneByIndex[index] : "Err";

        // Byte positions inside a single 8-byte channel row
        private const int RowA0 = 0, RowA1 = 1, RowA2 = 2, RowA3 = 3;
        private const int RowB0 = 4, RowB1 = 5, RowB2 = 6, RowB3 = 7;

        // ---------------------------------------------------------------------
        // RECEIVE WINDOW (MSB→LSB)
        //
        // RxIndexBits [bit5..bit0] = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        //
        // Notes:
        //   • Bit 7 of A3 is also used by STE (separate helper below).
        //   • UI treats index 0 as “no tone” (blank).
        // ---------------------------------------------------------------------
        public static readonly string[] RxBitWindowSources =
            { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

        public static (int[] Bits, int Index) InspectReceiveBits(byte rowA3)
        {
            int[] b = new int[6];
            b[0] = ExtractBit(rowA3, 6); // i5
            b[1] = ExtractBit(rowA3, 7); // i4
            b[2] = ExtractBit(rowA3, 0); // i3
            b[3] = ExtractBit(rowA3, 1); // i2
            b[4] = ExtractBit(rowA3, 2); // i1
            b[5] = ExtractBit(rowA3, 3); // i0

            int index = (b[0] << 5) | (b[1] << 4) | (b[2] << 3) | (b[3] << 2) | (b[4] << 1) | b[5];
            return (b, index);
        }

        public static int BuildReceiveToneIndex(byte rowA3)
        {
            var (bits, index) = InspectReceiveBits(rowA3);
            return index;
        }

        public static string GetReceiveToneLabel(byte rowA3) => LabelFromIndex(BuildReceiveToneIndex(rowA3));

        public static bool IsSquelchTailEliminationEnabled(byte rowA3)
            => ((rowA3 >> 7) & 1) == 1;

        // ---------------------------------------------------------------------
        // TRANSMIT WINDOW (MSB→LSB)
        //
        // TxIndexBits [bit5..bit0] = [B0.4, B3.1, B0.5, B2.2, B2.4, B2.6]
        //                                  ^^^^^  ^^^^^
        //                                  i3     i2(4’s place, PROVEN)
        //
        // Notes:
        //   • i2 (the 4’s place) is definitively RowB2 bit2 (B2.2) from your
        //     controlled “+4 index” pairs. No routing/overrides involved.
        //   • If you later prove a different i3 for another split/firmware,
        //     change only the third line below and the SourceNames.
        // ---------------------------------------------------------------------
        public static readonly string[] TxBitWindowSources =
            { "B0.4", "B3.1", "B0.5", "B2.2", "B2.4", "B2.6" };

        public static (int[] Bits, int Index) InspectTransmitBits(
            byte rowA3, byte rowA2, byte rowA1, byte rowA0,
            byte rowB3, byte rowB2, byte rowB1, byte rowB0)
        {
            int[] b = new int[6];
            b[0] = ExtractBit(rowB0, 4); // i5 (32’s)
            b[1] = ExtractBit(rowB3, 1); // i4 (16’s)
            b[2] = ExtractBit(rowB0, 5); // i3 (8’s)
            b[3] = ExtractBit(rowB2, 2); // i2 (4’s)  ← PROVEN
            b[4] = ExtractBit(rowB2, 4); // i1 (2’s)
            b[5] = ExtractBit(rowB2, 6); // i0 (1’s)

            int index = (b[0] << 5) | (b[1] << 4) | (b[2] << 3) | (b[3] << 2) | (b[4] << 1) | b[5];
            return (b, index);
        }

        public static int BuildTransmitToneIndex(
            byte rowA3, byte rowA2, byte rowA1, byte rowA0,
            byte rowB3, byte rowB2, byte rowB1, byte rowB0)
        {
            var (bits, index) = InspectTransmitBits(rowA3, rowA2, rowA1, rowA0, rowB3, rowB2, rowB1, rowB0);
            return index;
        }

        public static string GetTransmitToneLabel(
            byte rowA3, byte rowA2, byte rowA1, byte rowA0,
            byte rowB3, byte rowB2, byte rowB1, byte rowB0)
        {
            int txIndex = BuildTransmitToneIndex(rowA3, rowA2, rowA1, rowA0, rowB3, rowB2, rowB1, rowB0);
            return LabelFromIndex(txIndex);
        }
    }
}
