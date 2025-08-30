// src/ToneLock.cs
// GE Rangr (.RGR) — Tone decoding/labeling (readable + debug-friendly)
// Frequencies remain in FreqLock.cs (frozen). CCTS belongs in CctLock.cs.
//
// KEY POINT (bug fix):
// Map tone indices using a table where index 0 == "0".
// We provide that table here as ToneLock.Cg. All label getters below
// now use Cg internally (and IGNORE the external array parameter to
// keep call sites working). This fixes the prior off‑by‑one issue that
// displayed 67.0 when index==0 and caused random 'Err' labels.
//
// Windows (confirmed from ZEROALL vs TX_TNS diffs):
//   RX index i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
//   TX index i5..i0 = [B3.7, B3.4, B0.5, B0.2, B0.1, B0.0]
//
// 'Follow TX' is ignored entirely per latest rule.

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ------------------------------------------------------------------
        // Public tone tables expected by callers
        // ------------------------------------------------------------------

        // Canonical 33-tone list (no zero). Keep here for clarity.
        private static readonly string[] Canon33 = new[] {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // Cg = canonical-with-zero at index 0. This is the ONLY array we use to map indices.
        public static readonly string[] Cg = BuildCg();

        // Menus for the UI comboboxes: canonical tones only (no zero).
        public static readonly string[] ToneMenuTx = Canon33;
        public static readonly string[] ToneMenuRx = Canon33;

        private static string[] BuildCg()
        {
            var cg = new string[Canon33.Length + 1];
            cg[0] = "0";
            for (int i = 0; i < Canon33.Length; i++) cg[i + 1] = Canon33[i];
            return cg;
        }

        // ------------------------------------------------------------------
        // Utilities
        // ------------------------------------------------------------------
        private static int ExtractBit(byte value, int bitPosition) => (value >> bitPosition) & 0x1;

        private static bool IsCanonicalToneIndex(int toneIndex) =>
            toneIndex == 0 || (toneIndex >= 1 && toneIndex <= 33);

        private static string ToDisplayLabelUsingCg(int toneIndex)
        {
            if (!IsCanonicalToneIndex(toneIndex)) return "Err";
            return Cg[toneIndex]; // Cg[0] == "0", Cg[1..33] == canonical tones
        }

        // ==================================================================
        // RX (Receive)
        // ==================================================================
        /// <summary>Build the 6‑bit Receive Tone Index from A3.
        /// i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]</summary>
        public static int BuildReceiveToneIndex(byte A3)
        {
            int rxToneIndex = 0;
            rxToneIndex |= ((A3 >> 6) & 1) << 5; // A3.6 → bit5
            rxToneIndex |= ((A3 >> 7) & 1) << 4; // A3.7 → bit4 (also STE display)
            rxToneIndex |= ((A3 >> 0) & 1) << 3; // A3.0 → bit3
            rxToneIndex |= ((A3 >> 1) & 1) << 2; // A3.1 → bit2
            rxToneIndex |= ((A3 >> 2) & 1) << 1; // A3.2 → bit1
            rxToneIndex |= ((A3 >> 3) & 1) << 0; // A3.3 → bit0
            return rxToneIndex;
        }

        public static bool IsSquelchTailEliminationEnabled(byte A3) => ExtractBit(A3, 7) == 1;

        // Label getter (NEW: always uses Cg internally; param kept for compatibility)
        public static string GetReceiveToneLabel(byte A3, string[] _ignoredCanonicalLabels = null!)
            => ToDisplayLabelUsingCg(BuildReceiveToneIndex(A3));

        public static string GetReceiveToneDebugBits(byte A3)
        {
            int b5 = (A3 >> 6) & 1;
            int b4 = (A3 >> 7) & 1;
            int b3 = (A3 >> 0) & 1;
            int b2 = (A3 >> 1) & 1;
            int b1 = (A3 >> 2) & 1;
            int b0 = (A3 >> 3) & 1;
            int idx = BuildReceiveToneIndex(A3);
            return $"RX bits (i5..i0)=[{b5}{b4}{b3}{b2}{b1}{b0}] → index={idx} STE={b4}";
        }

        // ==================================================================
        // TX (Transmit)
        // ==================================================================
        /// <summary>Build the 6‑bit Transmit Tone Index from the B‑bytes.
        /// i5..i0 = [B3.7, B3.4, B0.5, B0.2, B0.1, B0.0]</summary>
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int txToneIndex = 0;
            txToneIndex |= ((B3 >> 7) & 1) << 5; // B3.7 → bit5
            txToneIndex |= ((B3 >> 4) & 1) << 4; // B3.4 → bit4
            txToneIndex |= ((B0 >> 5) & 1) << 3; // B0.5 → bit3
            txToneIndex |= ((B0 >> 2) & 1) << 2; // B0.2 → bit2
            txToneIndex |= ((B0 >> 1) & 1) << 1; // B0.1 → bit1
            txToneIndex |= ((B0 >> 0) & 1) << 0; // B0.0 → bit0
            return txToneIndex;
        }

        // Label getter (NEW: always uses Cg internally; param kept for compatibility)
        public static string GetTransmitToneLabel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            string[] _ignoredCanonicalLabels = null!)
            => ToDisplayLabelUsingCg(BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0));

        public static string GetTransmitToneDebugBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int b5 = (B3 >> 7) & 1;
            int b4 = (B3 >> 4) & 1;
            int b3 = (B0 >> 5) & 1;
            int b2 = (B0 >> 2) & 1;
            int b1 = (B0 >> 1) & 1;
            int b0 = (B0 >> 0) & 1;
            int idx = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            return $"TX bits (i5..i0)=[{b5}{b4}{b3}{b2}{b1}{b0}] → index={idx}";
        }

        // Convenience for existing call sites
        public static (string txLabel, string rxLabel, int txIndex, int rxIndex) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int txIndex = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            int rxIndex = BuildReceiveToneIndex(A3);
            string txLabel = ToDisplayLabelUsingCg(txIndex);
            string rxLabel = ToDisplayLabelUsingCg(rxIndex);
            return (txLabel, rxLabel, txIndex, rxIndex);
        }
    }
}
