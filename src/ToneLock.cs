// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Standards:
//   • Canonical tone list is frozen (no 114.1).
//   • Bit windows are explicit, MSB→LSB, with clear names.
//   • Build the index progressively (OR bits into one int).
//   • Provide Inspect helpers + SourceNames for diagnostics.
//   • No per-file overrides or routing tables.
// -----------------------------------------------------------------------------

using System;

namespace GE_Ranger_Programmer
{
    public static class ToneLock
    {
        // Canonical CTCSS table: index 0 is "0" (no tone). No "114.1".
        private static readonly string[] CanonicalTonesNoZero =
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // UI menus (no "0" here — UI shows "0" as blank/null)
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index → label (index 0 = "0")
        private static readonly string[] ToneByIndex = BuildToneByIndex();
        public static string[] Cg => ToneByIndex; // back-compat exposure

        private static string[] BuildToneByIndex()
        {
            var map = new string[CanonicalTonesNoZero.Length + 1];
            map[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++) map[i + 1] = CanonicalTonesNoZero[i];
            return map;
        }

        // Helpers
        private static int ExtractBit(byte value, int bitIndex) => (value >> bitIndex) & 1;
        private static string LabelFromIndex(int index) =>
            (index >= 0 && index < ToneByIndex.Length) ? ToneByIndex[index] : "Err";

        // ---------------------------------------------------------------------
        // RECEIVE WINDOW (MSB→LSB)
        // RxIndexBits [5..0] = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        // ---------------------------------------------------------------------
        public static readonly string[] RxBitWindowSources = { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

        public static (int[] Bits, int Index) InspectReceiveBits(byte rowA3)
        {
            int[] bits = new int[6];
            bits[0] = ExtractBit(rowA3, 6); // i5
            bits[1] = ExtractBit(rowA3, 7); // i4
            bits[2] = ExtractBit(rowA3, 0); // i3
            bits[3] = ExtractBit(rowA3, 1); // i2
            bits[4] = ExtractBit(rowA3, 2); // i1
            bits[5] = ExtractBit(rowA3, 3); // i0

            int index = (bits[0] << 5) | (bits[1] << 4) | (bits[2] << 3) | (bits[3] << 2) | (bits[4] << 1) | bits[5];
            return (bits, index);
        }

        public static int BuildReceiveToneIndex(byte rowA3) =>
            InspectReceiveBits(rowA3).Index;

        public static string GetReceiveToneLabel(byte rowA3) =>
            LabelFromIndex(BuildReceiveToneIndex(rowA3));

        public static bool IsSquelchTailEliminationEnabled(byte rowA3) =>
            ((rowA3 >> 7) & 1) == 1;

        // ---------------------------------------------------------------------
        // TRANSMIT WINDOW (MSB→LSB)
        //
        // TxIndexBits [5..0] = [B0.4, B3.1, B2.2, B0.5, B2.4, B2.6]
        //                                   ^^^^^  ^^^^^
        //                                  i3=8’s  i2=4’s
        //
        // NOTE: Method signatures standardized to (A0,A1,A2,A3,B0,B1,B2,B3)
        //       to match byte naming everywhere else.
        // ---------------------------------------------------------------------
        public static readonly string[] TxBitWindowSources = { "B0.4", "B3.1", "B2.2", "B0.5", "B2.4", "B2.6" };

        public static (int[] Bits, int Index) InspectTransmitBits(
            byte A0, byte A1, byte A2, byte A3,
            byte B0, byte B1, byte B2, byte B3)
        {
            int[] bits = new int[6];
            bits[0] = ExtractBit(B0, 4); // i5 (32’s)
            bits[1] = ExtractBit(B3, 1); // i4 (16’s)
            bits[2] = ExtractBit(B2, 2); // i3 ( 8’s)  <-- RESTORED/LOCKED
            bits[3] = ExtractBit(B0, 5); // i2 ( 4’s)
            bits[4] = ExtractBit(B2, 4); // i1 ( 2’s)
            bits[5] = ExtractBit(B2, 6); // i0 ( 1’s)

            int index = (bits[0] << 5) | (bits[1] << 4) | (bits[2] << 3) | (bits[3] << 2) | (bits[4] << 1) | bits[5];
            return (bits, index);
        }

        public static int BuildTransmitToneIndex(
            byte A0, byte A1, byte A2, byte A3,
            byte B0, byte B1, byte B2, byte B3) =>
            InspectTransmitBits(A0, A1, A2, A3, B0, B1, B2, B3).Index;

        public static string GetTransmitToneLabel(
            byte A0, byte A1, byte A2, byte A3,
            byte B0, byte B1, byte B2, byte B3) =>
            LabelFromIndex(BuildTransmitToneIndex(A0, A1, A2, A3, B0, B1, B2, B3));

        // Back-compat aliases so ToneDiag.cs continues to build unchanged
        public static string[] ReceiveBitSourceNames  => RxBitWindowSources;
        public static string[] TransmitBitSourceNames => TxBitWindowSources;
    }
}

