// src/ToneLock.cs
// GE Rangr (.RGR) — Tone decoding & labeling (single file, human‑readable)
//
// • Frequencies are handled elsewhere (FreqLock.cs, frozen).
// • This file handles ONLY tone indices (TX/RX) and their display labels.
// • Everything you need to debug TX lives in one place below.
//
// Canonical policy (fixed):
//   - We map tone *indices* via a table where index 0 == "0" (no tone).
//   - Indices 1..33 map to the standard 33 CTCSS tones (see Canon33 below).
//   - Any index outside 0..33 → "Err".
//
// Bit windows (current best‑known):
//   RX index bits i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
//   TX index bits i5..i0 = [B3.7, B3.4, B0.5, B0.2, B0.1, B0.0]
//
// >>> IMPORTANT: If TX looks wrong on your DOS screen, you can adjust the
//     six TX source lines in the clearly‑marked block below and rebuild.
//     That block uses the “one integer, progressively set bits” style.
//
// Namespace note: GlobalUsings.cs includes `global using RangrApp.Locked;`
// so this file declares that namespace for drop‑in compatibility.

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ------------------------------------------------------------------
        // Canonical tone tables (self-contained; no external dependency)
        // ------------------------------------------------------------------

        // The 33 standard CTCSS tones (no zero here).
        private static readonly string[] Canon33 = new[] {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // Cg = canonical with leading zero at index 0. This is *the* lookup table.
        public static readonly string[] Cg = BuildCg();

        // Tone menus for your comboboxes (no zero entry).
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
        // Helpers
        // ------------------------------------------------------------------
        private static bool IsCanonicalToneIndex(int toneIndex) =>
            toneIndex == 0 || (toneIndex >= 1 && toneIndex <= 33);

        private static string LabelFromIndex(int toneIndex)
        {
            if (!IsCanonicalToneIndex(toneIndex)) return "Err";
            return Cg[toneIndex]; // Cg[0] == "0"
        }

        // ==================================================================
        // RX (Receive) — index & label
        // ==================================================================

        /// <summary>
        /// Build the 6‑bit Receive Tone Index from A3.
        /// Mapping (MSB→LSB): i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        /// </summary>
        public static int BuildReceiveToneIndex(byte A3)
        {
            int receiveToneIndex = 0;
            receiveToneIndex |= ((A3 >> 6) & 1) << 5;  // A3.6 → bit 5 (MSB)
            receiveToneIndex |= ((A3 >> 7) & 1) << 4;  // A3.7 → bit 4 (also STE display)
            receiveToneIndex |= ((A3 >> 0) & 1) << 3;  // A3.0 → bit 3
            receiveToneIndex |= ((A3 >> 1) & 1) << 2;  // A3.1 → bit 2
            receiveToneIndex |= ((A3 >> 2) & 1) << 1;  // A3.2 → bit 1
            receiveToneIndex |= ((A3 >> 3) & 1) << 0;  // A3.3 → bit 0 (LSB)
            return receiveToneIndex;
        }

        public static string GetReceiveToneLabel(byte A3)
            => LabelFromIndex(BuildReceiveToneIndex(A3));

        public static bool IsSquelchTailEliminationEnabled(byte A3) => ((A3 >> 7) & 1) == 1;

        // ==================================================================
        // TX (Transmit) — index & label
        // ==================================================================

        /// <summary>
        /// Build the 6‑bit Transmit Tone Index from the channel bytes.
        /// *** EDIT THE SIX LINES IN THE MARKED BLOCK BELOW to try a different
        ///     TX window. Keep the “progressively set bits” pattern. ***
        /// </summary>
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int transmitToneIndex = 0;

            // ------------------------------------------------------------------
            // >>> TX INDEX WINDOW — EDIT THIS BLOCK ONLY (i5..i0 MSB→LSB) <<<
            //
            // Current mapping (derived by diffing ZEROALL vs test images):
            // i5..i0 = [B3.7, B3.4, B0.5, B0.2, B0.1, B0.0]
            //
            // To try a different source, change the *left* side only.
            // Example: put A1.3 into bit 2  → replace ((B0 >> 2) & 1) << 2 with ((A1 >> 3) & 1) << 2
            // ------------------------------------------------------------------
            transmitToneIndex |= ((B3 >> 7) & 1) << 5;  // i5 ← B3.7
            transmitToneIndex |= ((B3 >> 4) & 1) << 4;  // i4 ← B3.4
            transmitToneIndex |= ((B0 >> 5) & 1) << 3;  // i3 ← B0.5
            transmitToneIndex |= ((B0 >> 2) & 1) << 2;  // i2 ← B0.2
            transmitToneIndex |= ((B0 >> 1) & 1) << 1;  // i1 ← B0.1
            transmitToneIndex |= ((B0 >> 0) & 1) << 0;  // i0 ← B0.0
            // ------------------------------------------------------------------

            return transmitToneIndex;
        }

        public static string GetTransmitToneLabel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
            => LabelFromIndex(BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0));

        // ==================================================================
        // Friendly diagnostics (optional for your bottom log)
        // ==================================================================
        public static string DescribeRx(byte A3)
        {
            int i5 = (A3 >> 6) & 1;
            int i4 = (A3 >> 7) & 1;
            int i3 = (A3 >> 0) & 1;
            int i2 = (A3 >> 1) & 1;
            int i1 = (A3 >> 2) & 1;
            int i0 = (A3 >> 3) & 1;
            int idx = BuildReceiveToneIndex(A3);
            return $"RX | src [A3.6 A3.7 A3.0 A3.1 A3.2 A3.3] = [{i5} {i4} {i3} {i2} {i1} {i0}] -> index={idx} label={LabelFromIndex(idx)} STE={((A3>>7)&1)}";
        }

        public static string DescribeTx(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int j5 = (B3 >> 7) & 1;
            int j4 = (B3 >> 4) & 1;
            int j3 = (B0 >> 5) & 1;
            int j2 = (B0 >> 2) & 1;
            int j1 = (B0 >> 1) & 1;
            int j0 = (B0 >> 0) & 1;
            int idx = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            return $"TX | src [B3.7 B3.4 B0.5 B0.2 B0.1 B0.0] = [{j5} {j4} {j3} {j2} {j1} {j0}] -> index={idx} label={LabelFromIndex(idx)}";
        }

        /// <summary>
        /// For callers that want both labels and indices at once.
        /// </summary>
        public static (string TxLabel, string RxLabel, int TxIndex, int RxIndex) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int txi = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            int rxi = BuildReceiveToneIndex(A3);
            return (LabelFromIndex(txi), LabelFromIndex(rxi), txi, rxi);
        }
    }
}
