// src/ToneLock.cs
// GE Rangr (.RGR) — Tone decoding & labeling + diagnostics helpers
//
// Project standards (per Peter):
// • Do NOT change canonical tone lists without explicit approval.
// • No guessing. Bit windows are spelled out in one place and mirrored in diagnostics.
// • Clear, human-readable names and progressive bit assembly.
//
// Index → label policy:
//   index 0   => "0" (no tone)
//   1..33     => standard 33 CTCSS tones (see list below)
//   otherwise => "Err"
//
// Bit windows (MSB→LSB) — CURRENT (2025-08-30)
//   RX tone index i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
//   TX tone index i5..i0 = [B2.0, B3.1, B2.2, B0.5, B2.4, B2.6]   // NOTE: i2 is B0.5
//
// The TX i2 source (B0.5) is based on comparing DOS listing vs. bytes; it fixes CH1/2
// where index was 16 but should be 20 for 131.8 Hz.
//
// Everything here is deterministic so diagnostic output always matches mapping.

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ------------------------------------------------------------------
        // Canonical tone tables (unchanged)
        // ------------------------------------------------------------------

        private static readonly string[] CanonicalTonesNoZero = new[]
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // Public menus for UI combo boxes (no zero).
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index→label with "0" at index 0. This is the only table used for mapping.
        private static readonly string[] ToneIndexToLabel = BuildToneIndexToLabel();
        public static string[] Cg => ToneIndexToLabel; // readable alias

        private static string[] BuildToneIndexToLabel()
        {
            var table = new string[CanonicalTonesNoZero.Length + 1];
            table[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++)
                table[i + 1] = CanonicalTonesNoZero[i];
            return table;
        }

        // ------------------------------------------------------------------
        // Small helpers
        // ------------------------------------------------------------------

        private static int Bit(byte value, int bitNumber) => (value >> bitNumber) & 0x1;

        private static string LabelFromIndex(int toneIndex)
        {
            if (toneIndex < 0 || toneIndex >= ToneIndexToLabel.Length) return "Err";
            return ToneIndexToLabel[toneIndex];
        }

        // ------------------------------------------------------------------
        // PUBLIC METADATA for diagnostics (keeps logs in sync with window)
        // ------------------------------------------------------------------

        // Names for MSB→LSB (i5..i0)
        public static readonly string[] ReceiveBitSourceNames = new[]
            { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

        public static readonly string[] TransmitBitSourceNames = new[]
            { "B2.0", "B3.1", "B2.2", "B0.5", "B2.4", "B2.6" };

        /// <summary>Return per-bit values (MSB→LSB) and the assembled index for RX.</summary>
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

        /// <summary>Return per-bit values (MSB→LSB) and the assembled index for TX.</summary>
        public static (int[] Values, int Index) InspectTransmitBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int[] v = new int[6];
            v[0] = (B2 >> 0) & 1; // i5
            v[1] = (B3 >> 1) & 1; // i4
            v[2] = (B2 >> 2) & 1; // i3
            v[3] = (B0 >> 5) & 1; // i2  (changed from B3.0)
            v[4] = (B2 >> 4) & 1; // i1
            v[5] = (B2 >> 6) & 1; // i0
            int idx = (v[0] << 5) | (v[1] << 4) | (v[2] << 3) | (v[3] << 2) | (v[4] << 1) | v[5];
            return (v, idx);
        }

        // ==================================================================
        // RX (Receive) — index & label
        // ==================================================================

        /// <summary>
        /// Build the 6-bit Receive Tone Index from A3 using the frozen window:
        /// i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        /// </summary>
        public static int BuildReceiveToneIndex(byte A3)
        {
            // Individual source bits (named explicitly)
            int bitForIndex5_from_A3_6 = Bit(A3, 6); // MSB
            int bitForIndex4_from_A3_7 = Bit(A3, 7);
            int bitForIndex3_from_A3_0 = Bit(A3, 0);
            int bitForIndex2_from_A3_1 = Bit(A3, 1);
            int bitForIndex1_from_A3_2 = Bit(A3, 2);
            int bitForIndex0_from_A3_3 = Bit(A3, 3); // LSB

            // Build index progressively (one integer, bits OR’d in order)
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

        // ==================================================================
        // TX (Transmit) — index & label
        // ==================================================================

        /// <summary>
        /// Build the 6-bit Transmit Tone Index from the channel bytes using the
        /// CURRENT window:
        ///   i5..i0 = [B2.0, B3.1, B2.2, B0.5, B2.4, B2.6]
        /// </summary>
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            // --------- TX WINDOW (MSB→LSB) — EDIT THESE SIX LINES ONLY ----------
            int bitForIndex5_from_B2_0 = (B2 >> 0) & 1; // i5 ← B2.0
            int bitForIndex4_from_B3_1 = (B3 >> 1) & 1; // i4 ← B3.1
            int bitForIndex3_from_B2_2 = (B2 >> 2) & 1; // i3 ← B2.2
            int bitForIndex2_from_B0_5 = (B0 >> 5) & 1; // i2 ← B0.5  (changed)
            int bitForIndex1_from_B2_4 = (B2 >> 4) & 1; // i1 ← B2.4
            int bitForIndex0_from_B2_6 = (B2 >> 6) & 1; // i0 ← B2.6
            // --------------------------------------------------------------------

            int transmitToneIndex = 0;
            transmitToneIndex |= (bitForIndex5_from_B2_0 << 5);
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
