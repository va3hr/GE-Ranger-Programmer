// src/ToneLock.cs
// GE Rangr (.RGR) — Tone decoding & labeling
//
// Ground rules (per Peter):
// • Do NOT change the canonical tone list without explicit approval.
// • Do NOT “guess.” The bit windows below are FROZEN until you say otherwise.
// • Use clear names; build bitfields one step at a time with comments.
//
// Index → label policy
//   0 => "0" (no tone)
//   1..33 => the standard 33 CTCSS tones (see list below)
//   otherwise => "Err"
//
// Bit windows (MSB→LSB) — FROZEN
//   RX tone index i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
//   TX tone index i5..i0 = [B2.0, B3.1, B2.2, B3.0, B2.4, B2.6]  // B2/B3 only
//
// Namespace matches GlobalUsings.cs: `global using RangrApp.Locked;`

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ---------------------------------------------------------------------
        // Canonical tone tables (unchanged)
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
        public static string[] Cg => ToneIndexToLabel; // readable alias

        private static string[] BuildToneIndexToLabel()
        {
            var table = new string[CanonicalTonesNoZero.Length + 1];
            table[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++)
                table[i + 1] = CanonicalTonesNoZero[i];
            return table;
        }

        private static int Bit(byte value, int bitNumber) => (value >> bitNumber) & 0x1;
        private static string LabelFromIndex(int toneIndex)
            => (toneIndex >= 0 && toneIndex < ToneIndexToLabel.Length)
                ? ToneIndexToLabel[toneIndex] : "Err";

        // ---------------------------------------------------------------------
        // PUBLIC METADATA for diagnostics (keeps ToneDiag in sync with window)
        // ---------------------------------------------------------------------

        // Names for MSB→LSB (i5..i0)
        public static readonly string[] ReceiveBitSourceNames = new[]
            { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

        public static readonly string[] TransmitBitSourceNames = new[]
            { "B2.0", "B3.1", "B2.2", "B3.0", "B2.4", "B2.6" };

        /// <summary>
        /// Return per-bit values (MSB→LSB) and the assembled index for RX.
        /// </summary>
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

        /// <summary>
        /// Return per-bit values (MSB→LSB) and the assembled index for TX.
        /// </summary>
        public static (int[] Values, int Index) InspectTransmitBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int[] v = new int[6];
            v[0] = Bit(B2, 0); // i5
            v[1] = Bit(B3, 1); // i4
            v[2] = Bit(B2, 2); // i3
            v[3] = Bit(B3, 0); // i2
            v[4] = Bit(B2, 4); // i1
            v[5] = Bit(B2, 6); // i0
            int idx = (v[0] << 5) | (v[1] << 4) | (v[2] << 3) | (v[3] << 2) | (v[4] << 1) | v[5];
            return (v, idx);
        }

        // =====================================================================
        // RX (Receive) — index & label
        // =====================================================================

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

        // =====================================================================
        // TX (Transmit) — index & label  (B2/B3 only)
        // =====================================================================

        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            // --------- TX WINDOW (MSB→LSB) — EDIT THESE SIX LINES ONLY ----------
            int bitForIndex5_from_B2_0 = (B2 >> 0) & 1; // i5 ← B2.0
            int bitForIndex4_from_B3_1 = (B3 >> 1) & 1; // i4 ← B3.1
            int bitForIndex3_from_B2_2 = (B2 >> 2) & 1; // i3 ← B2.2
            int bitForIndex2_from_B3_0 = (B3 >> 0) & 1; // i2 ← B3.0
            int bitForIndex1_from_B2_4 = (B2 >> 4) & 1; // i1 ← B2.4
            int bitForIndex0_from_B2_6 = (B2 >> 6) & 1; // i0 ← B2.6
            // --------------------------------------------------------------------

            int transmitToneIndex = 0;
            transmitToneIndex |= (bitForIndex5_from_B2_0 << 5);
            transmitToneIndex |= (bitForIndex4_from_B3_1 << 4);
            transmitToneIndex |= (bitForIndex3_from_B2_2 << 3);
            transmitToneIndex |= (bitForIndex2_from_B3_0 << 2);
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
