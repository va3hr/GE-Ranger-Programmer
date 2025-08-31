// src/ToneLock.cs
// GE Rangr (.RGR) — Tone decoding & labeling
//
// Ground rules (per Peter):
// • Do NOT change the canonical tone list without explicit approval.
// • Do NOT “guess” silently. Any window changes are confined to SIX lines below.
// • Use clear names; build bitfields one step at a time with comments.
//
// Index → label policy
//   0 => "0" (no tone)
//   1..33 => the standard 33 CTCSS tones (see list below)
//   otherwise => "Err"
//
// Bit windows (MSB→LSB)
//   RX tone index i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]  (unchanged)
//   TX tone index i5..i0 = [B2.0, B3.1, B2.2, B3.0, B2.4, B2.6]  (**B2/B3 only**)
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

        // Public menus for UI combo boxes (no zero).
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index→label with "0" at index 0. This is the only table used for mapping.
        private static readonly string[] ToneIndexToLabel = BuildToneIndexToLabel();

        // Some callers previously used ToneLock.Cg; keep a readable alias.
        public static string[] Cg => ToneIndexToLabel;

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
            if (toneIndex < 0 || toneIndex >= ToneIndexToLabel.Length)
                return "Err";
            return ToneIndexToLabel[toneIndex];
        }

        // =====================================================================
        // RX (Receive) — index & label
        // =====================================================================

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
            return Bit(A3, 7) == 1;
        }

        // =====================================================================
        // TX (Transmit) — index & label  (B2/B3 only)
        // =====================================================================

        /// <summary>
        /// Build the 6-bit Transmit Tone Index from the channel bytes using the
        /// B2/B3-only window:
        ///   i5..i0 = [B2.0, B3.1, B2.2, B3.0, B2.4, B2.6]
        /// These six “source lines” are the only place to change the mapping.
        /// </summary>
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            // --------- TX WINDOW (MSB→LSB) — EDIT THESE SIX LINES ONLY ----------
            int bitForIndex5_from_B2_0 = Bit(B2, 0); // i5 (MSB) ← B2.0
            int bitForIndex4_from_B3_1 = Bit(B3, 1); // i4       ← B3.1
            int bitForIndex3_from_B2_2 = Bit(B2, 2); // i3       ← B2.2
            int bitForIndex2_from_B3_0 = Bit(B3, 0); // i2       ← B3.0
            int bitForIndex1_from_B2_4 = Bit(B2, 4); // i1       ← B2.4
            int bitForIndex0_from_B2_6 = Bit(B2, 6); // i0 (LSB) ← B2.6
            // --------------------------------------------------------------------

            // Build index progressively (one integer, bits OR’d in order)
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

        // =====================================================================
        // Diagnostics (for the bottom log) — explicit, no cryptic names
        // =====================================================================

        public static string DescribeReceive(byte A3)
        {
            // Echo the exact source bits in MSB→LSB order and the result.
            int bitForIndex5_from_A3_6 = Bit(A3, 6);
            int bitForIndex4_from_A3_7 = Bit(A3, 7);
            int bitForIndex3_from_A3_0 = Bit(A3, 0);
            int bitForIndex2_from_A3_1 = Bit(A3, 1);
            int bitForIndex1_from_A3_2 = Bit(A3, 2);
            int bitForIndex0_from_A3_3 = Bit(A3, 3);

            int rxIndex = BuildReceiveToneIndex(A3);
            string rxLabel = LabelFromIndex(rxIndex);
            string steFlag = (bitForIndex4_from_A3_7 == 1) ? "Y" : "N";

            return $"RX bits MSB→LSB: [A3.6={bitForIndex5_from_A3_6}  A3.7={bitForIndex4_from_A3_7}  A3.0={bitForIndex3_from_A3_0}  A3.1={bitForIndex2_from_A3_1}  A3.2={bitForIndex1_from_A3_2}  A3.3={bitForIndex0_from_A3_3}] | index={rxIndex} | label={rxLabel} | STE={steFlag}";
        }

        public static string DescribeTransmit(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            // Echo the exact source bits in MSB→LSB order and the result.
            int bitForIndex5_from_B2_0 = Bit(B2, 0);
            int bitForIndex4_from_B3_1 = Bit(B3, 1);
            int bitForIndex3_from_B2_2 = Bit(B2, 2);
            int bitForIndex2_from_B3_0 = Bit(B3, 0);
            int bitForIndex1_from_B2_4 = Bit(B2, 4);
            int bitForIndex0_from_B2_6 = Bit(B2, 6);

            int txIndex = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            string txLabel = LabelFromIndex(txIndex);

            return $"TX bits MSB→LSB: [B2.0={bitForIndex5_from_B2_0}  B3.1={bitForIndex4_from_B3_1}  B2.2={bitForIndex3_from_B2_2}  B3.0={bitForIndex2_from_B3_0}  B2.4={bitForIndex1_from_B2_4}  B2.6={bitForIndex0_from_B2_6}] | index={txIndex} | label={txLabel}";
        }

        /// <summary>
        /// Convenience helper that returns both labels and indices at once.
        /// </summary>
        public static (string TxLabel, string RxLabel, int TxIndex, int RxIndex) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int txIndex = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            int rxIndex = BuildReceiveToneIndex(A3);
            string txLabel = LabelFromIndex(txIndex);
            string rxLabel = LabelFromIndex(rxIndex);
            return (txLabel, rxLabel, txIndex, rxIndex);
        }
    }
}
