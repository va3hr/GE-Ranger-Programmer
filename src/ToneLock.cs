// src/ToneLock.cs
// GE Rangr (.RGR) — Tone decoding/labeling (readable + debug-friendly)
// - Frequencies remain in FreqLock.cs (frozen).
// - CCTS belongs in CctsLock.cs.
// - This file handles ONLY tone indices and labels for RX/TX.
//
// Policy (per project memory):
// • RX index window (i5..i0) = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3].
// • TX index window (i5..i0) = [B3.7, B3.4, B0.5, B0.2, B0.1, B0.0].
// • "Follow TX" bit is ignored entirely.
// • Canonical mapping: index 0 → "0"; indices 1..33 → canonical tones; anything else → "Err".
// • UI should show exact zero as "0"; unknown/out-of-range as "Err".

using System;

namespace Rangr.Tones
{
    public static class ToneLock
    {
        // -----------------------
        // Utilities
        // -----------------------
        private static int ExtractBit(byte value, int bitPosition) => (value >> bitPosition) & 0x1;

        private static bool IsCanonicalToneIndex(int toneIndex) =>
            toneIndex == 0 || (toneIndex >= 1 && toneIndex <= 33);

        private static string ToDisplayLabel(int toneIndex, string[] canonicalLabels)
        {
            // Expect canonicalLabels[0] == "0", [1..33] == 33 canonical tones.
            if (!IsCanonicalToneIndex(toneIndex)) return "Err";
            return canonicalLabels[toneIndex];
        }

        // =====================================================================
        // RX (Receive) — index + label
        // =====================================================================

        /// <summary>
        /// Build the 6-bit Receive Tone Index from A3.
        /// Mapping (MSB→LSB): i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        /// </summary>
        public static int BuildReceiveToneIndex(byte A3)
        {
            int rxToneIndex = 0;

            // Build RX tone index (progressively set bits into one int)
            rxToneIndex |= ((A3 >> 6) & 1) << 5;  // A3.6 → RX bit 5 (MSB)
            rxToneIndex |= ((A3 >> 7) & 1) << 4;  // A3.7 → RX bit 4 (also STE display flag)
            rxToneIndex |= ((A3 >> 0) & 1) << 3;  // A3.0 → RX bit 3
            rxToneIndex |= ((A3 >> 1) & 1) << 2;  // A3.1 → RX bit 2
            rxToneIndex |= ((A3 >> 2) & 1) << 1;  // A3.2 → RX bit 1
            rxToneIndex |= ((A3 >> 3) & 1) << 0;  // A3.3 → RX bit 0 (LSB)

            return rxToneIndex;
        }

        /// <summary>
        /// True if squelch-tail elimination is enabled (A3 bit 7).
        /// (Display-only flag; window still uses A3.7 as RX index bit 4.)
        /// </summary>
        public static bool IsSquelchTailEliminationEnabled(byte A3) => ExtractBit(A3, 7) == 1;

        /// <summary>
        /// Convert RX index (from A3) to a display label via canonical mapping.
        /// </summary>
        public static string GetReceiveToneLabel(byte A3, string[] canonicalLabels)
        {
            int receiveToneIndex = BuildReceiveToneIndex(A3);
            return ToDisplayLabel(receiveToneIndex, canonicalLabels);
        }

        /// <summary>
        /// Diagnostic string for RX: raw source bits and packed index.
        /// </summary>
        public static string GetReceiveToneDebugBits(byte A3)
        {
            int srcA3_6 = (A3 >> 6) & 1;
            int srcA3_7 = (A3 >> 7) & 1; // also STE
            int srcA3_0 = (A3 >> 0) & 1;
            int srcA3_1 = (A3 >> 1) & 1;
            int srcA3_2 = (A3 >> 2) & 1;
            int srcA3_3 = (A3 >> 3) & 1;

            int rxToneIndex = BuildReceiveToneIndex(A3);

            return $"RX bits (i5..i0)=[{srcA3_6}{srcA3_7}{srcA3_0}{srcA3_1}{srcA3_2}{srcA3_3}] → index={rxToneIndex} STE={srcA3_7}";
        }

        // =====================================================================
        // TX (Transmit) — index + label
        // =====================================================================

        /// <summary>
        /// Build the 6-bit Transmit Tone Index from B-bytes.
        /// Mapping (MSB→LSB): i5..i0 = [B3.7, B3.4, B0.5, B0.2, B0.1, B0.0]
        /// (Derived by diffing ZEROALL vs test image with TX tones set.)
        /// </summary>
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,  // kept for signature parity; unused here
            byte B3, byte B2, byte B1, byte B0)
        {
            int txToneIndex = 0;

            // Build TX tone index (progressively set bits into one int)
            txToneIndex |= ((B3 >> 7) & 1) << 5;  // B3.7 → TX bit 5 (MSB)
            txToneIndex |= ((B3 >> 4) & 1) << 4;  // B3.4 → TX bit 4
            txToneIndex |= ((B0 >> 5) & 1) << 3;  // B0.5 → TX bit 3
            txToneIndex |= ((B0 >> 2) & 1) << 2;  // B0.2 → TX bit 2
            txToneIndex |= ((B0 >> 1) & 1) << 1;  // B0.1 → TX bit 1
            txToneIndex |= ((B0 >> 0) & 1) << 0;  // B0.0 → TX bit 0 (LSB)

            return txToneIndex;
        }

        /// <summary>
        /// Convert TX index (from B-bytes) to a display label via canonical mapping.
        /// </summary>
        public static string GetTransmitToneLabel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            string[] canonicalLabels)
        {
            int transmitToneIndex = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            return ToDisplayLabel(transmitToneIndex, canonicalLabels);
        }

        /// <summary>
        /// Diagnostic string for TX: raw source bits and packed index.
        /// </summary>
        public static string GetTransmitToneDebugBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int srcB3_7 = (B3 >> 7) & 1;
            int srcB3_4 = (B3 >> 4) & 1;
            int srcB0_5 = (B0 >> 5) & 1;
            int srcB0_2 = (B0 >> 2) & 1;
            int srcB0_1 = (B0 >> 1) & 1;
            int srcB0_0 = (B0 >> 0) & 1;

            int txToneIndex = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);

            return $"TX bits (i5..i0)=[{srcB3_7}{srcB3_4}{srcB0_5}{srcB0_2}{srcB0_1}{srcB0_0}] → index={txToneIndex}";
        }
    }
}
