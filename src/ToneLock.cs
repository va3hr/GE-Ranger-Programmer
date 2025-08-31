// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Project standards (Peter):
//   • No silent changes to constants (tone tables are canonical and unchanged).
//   • Bit windows are spelled out explicitly, MSB→LSB, with clear names.
//   • Build the index progressively (one integer, bits OR’d in order).
//   • Provide Inspect* helpers and SourceNames arrays so diagnostics always match.
//   • Comments must state exactly where to edit if a mapping changes.
// -----------------------------------------------------------------------------

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ---------------------------------------------------------------------
        // Canonical tone tables (UNCHANGED — DO NOT MODIFY WITHOUT EXPLICIT OK)
        // Index 0 = "0" (no tone); 1..33 map to standard CTCSS tones.
        // ---------------------------------------------------------------------

        private static readonly string[] CanonicalTonesNoZero = new[]
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        private static readonly string[] ToneIndexToLabel = BuildToneIndexToLabel();
        public static string[] Cg => ToneIndexToLabel; // alias used by diagnostics

        private static string[] BuildToneIndexToLabel()
        {
            var table = new string[CanonicalTonesNoZero.Length + 1];
            table[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++)
                table[i + 1] = CanonicalTonesNoZero[i];
            return table;
        }

        // ---------------------------------------------------------------------
        // Small helpers
        // ---------------------------------------------------------------------

        private static int Bit(byte value, int bitNumber) => (value >> bitNumber) & 0x1;

        private static string LabelFromIndex(int toneIndex)
        {
            if (toneIndex < 0 || toneIndex >= ToneIndexToLabel.Length) return "Err";
            return ToneIndexToLabel[toneIndex];
        }

        // ---------------------------------------------------------------------
        // Bit windows (MSB→LSB) — CURRENT
        //   RX i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        //   TX i5..i0 = [B0.4, B3.1, B2.2, B0.5, B2.4, B2.6]
        //                ^^^^^  ^^^^^  ^^^^^  ^^^^^  ^^^^^  ^^^^^
        //                i5     i4     i3     i2     i1     i0
        //
        // NOTE: Per Peter’s directive, ONLY i5 and i1 were remapped here:
        //   • i5 (32’s) = B0.4   (REMAPPED)
        //   • i1 (2’s)  = B2.4   (REMAPPED)
        //   • i4,i3,i2,i0 remain exactly as before.
        // ---------------------------------------------------------------------

        public static readonly string[] ReceiveBitSourceNames = new[]
            { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

        public static readonly string[] TransmitBitSourceNames = new[]
            { "B0.4", "B3.1", "B2.2", "B0.5", "B2.4", "B2.6" };

        // ---------------------------------------------------------------------
        // Inspect helpers (read bits and compute the index — used by diagnostics)
        // ---------------------------------------------------------------------

        public static (int[] Values, int Index) InspectReceiveBits(byte A3)
        {
            int[] v = new int[6];
            v[0] = Bit(A3, 6); // i5
            v[1] = Bit(A3, 7); // i4
            v[2] = Bit(A3, 0); // i3
            v[3] = Bit(A3, 1); // i2
            v[4] = Bit(A3, 2); // i1
            v[5] = Bit(A3, 3); // i0
            int idx = (v[0] << 5) | (v[1] << 4) | (v[2] << 3) | (v[3] << 2) | (v[4] << 1) | v[5];
            return (v, idx);
        }

        public static (int[] Values, int Index) InspectTransmitBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int[] v = new int[6];
            v[0] = (B0 >> 4) & 1; // i5 ← B0.4   (REMAPPED)
            v[1] = (B3 >> 1) & 1; // i4 ← B3.1
            v[2] = (B2 >> 2) & 1; // i3 ← B2.2
            v[3] = (B0 >> 5) & 1; // i2 ← B0.5
            v[4] = (B2 >> 4) & 1; // i1 ← B2.4   (REMAPPED)
            v[5] = (B2 >> 6) & 1; // i0 ← B2.6
            int idx = (v[0] << 5) | (v[1] << 4) | (v[2] << 3) | (v[3] << 2) | (v[4] << 1) | v[5];
            return (v, idx);
        }

        // ---------------------------------------------------------------------
        // RX (Receive) — index & label
        // ---------------------------------------------------------------------

        /// <summary>
        /// Build the 6‑bit Receive Tone Index (MSB→LSB = A3.6, A3.7, A3.0, A3.1, A3.2, A3.3).
        /// </summary>
        public static int BuildReceiveToneIndex(byte A3)
        {
            int bitForIndex5_from_A3_6 = Bit(A3, 6); // MSB
            int bitForIndex4_from_A3_7 = Bit(A3, 7);
            int bitForIndex3_from_A3_0 = Bit(A3, 0);
            int bitForIndex2_from_A3_1 = Bit(A3, 1);
            int bitForIndex1_from_A3_2 = Bit(A3, 2);
            int bitForIndex0_from_A3_3 = Bit(A3, 3); // LSB

            int receiveToneIndex = 0;
            receiveToneIndex |= (bitForIndex5_from_A3_6 << 5);
            receiveToneIndex |= (bitForIndex4_from_A3_7 << 4);
            receiveToneIndex |= (bitForIndex3_from_A3_0 << 3);
            receiveToneIndex |= (bitForIndex2_from_A3_1 << 2);
            receiveToneIndex |= (bitForIndex1_from_A3_2 << 1);
            receiveToneIndex |= (bitForIndex0_from_A3_3 << 0);

            return receiveToneIndex;
        }

        public static string GetReceiveToneLabel(byte A3)
        {
            int rxIndex = BuildReceiveToneIndex(A3);
            return LabelFromIndex(rxIndex);
        }

        public static bool IsSquelchTailEliminationEnabled(byte A3)
        {
            // STE flag is A3.7
            return ((A3 >> 7) & 1) == 1;
        }

        // ---------------------------------------------------------------------
        // TX (Transmit) — index & label
        // ---------------------------------------------------------------------

        /// <summary>
        /// Build the 6‑bit Transmit Tone Index from channel bytes using the CURRENT window.
        /// TX i5..i0 = [B0.4, B3.1, B2.2, B0.5, B2.4, B2.6]
        /// ONLY i5 and i1 are remapped; all others are unchanged.
        /// </summary>
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            // --------- TX WINDOW (MSB→LSB) — EDIT THESE SIX LINES ONLY ----------
            int bitForIndex5_from_B0_4 = (B0 >> 4) & 1; // i5 ← B0.4   (REMAPPED)
            int bitForIndex4_from_B3_1 = (B3 >> 1) & 1; // i4 ← B3.1
            int bitForIndex3_from_B2_2 = (B2 >> 2) & 1; // i3 ← B2.2
            int bitForIndex2_from_B0_5 = (B0 >> 5) & 1; // i2 ← B0.5
            int bitForIndex1_from_B2_4 = (B2 >> 4) & 1; // i1 ← B2.4   (REMAPPED)
            int bitForIndex0_from_B2_6 = (B2 >> 6) & 1; // i0 ← B2.6
            // -------------------------------------------------------------------

            int transmitToneIndex = 0;
            transmitToneIndex |= (bitForIndex5_from_B0_4 << 5);
            transmitToneIndex |= (bitForIndex4_from_B3_1 << 4);
            transmitToneIndex |= (bitForIndex3_from_B2_2 << 3);
            transmitToneIndex |= (bitForIndex2_from_B0_5 << 2);
            transmitToneIndex |= (bitForIndex1_from_B2_4 << 1);
            transmitToneIndex |= (bitForIndex0_from_B2_6 << 0);

            return transmitToneIndex;
        }

        public static string GetTransmitToneLabel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int txIndex = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            return LabelFromIndex(txIndex);
        }
    }
}
