// src/ToneLock.cs
// GE Rangr (.RGR) — Tone decoding & labeling (single file, human‑readable)
//
// • This file handles ONLY tone indices (TX/RX) and their display labels.
// • Canonical mapping uses a table where index 0 == "0".
// • 'Follow TX' is ignored.
//
// Windows (current):
//   RX i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
//   TX i5..i0 = [B3.7, B3.4, B0.5, B0.2, B0.1, B0.0]
//
// Namespace note: GlobalUsings.cs includes `global using RangrApp.Locked;`
using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ------------------------------------------------------------------
        // Canonical tone tables (self-contained)
        // ------------------------------------------------------------------
        private static readonly string[] Canon33 = new[] {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // Cg = canonical with zero at index 0
        public static readonly string[] Cg = BuildCg();

        // ComboBox menus (no zero)
        public static readonly string[] ToneMenuTx = Canon33;
        public static readonly string[] ToneMenuRx = Canon33;

        private static string[] BuildCg()
        {
            var cg = new string[Canon33.Length + 1];
            cg[0] = "0";
            for (int i = 0; i < Canon33.Length; i++) cg[i + 1] = Canon33[i];
            return cg;
        }

        private static bool IsCanonicalToneIndex(int toneIndex) =>
            toneIndex == 0 || (toneIndex >= 1 && toneIndex <= 33);

        private static string LabelFromIndex(int toneIndex)
        {
            if (!IsCanonicalToneIndex(toneIndex)) return "Err";
            return Cg[toneIndex];
        }

        // ======================== RX ========================
        public static int BuildReceiveToneIndex(byte A3)
        {
            int idx = 0;
            idx |= ((A3 >> 6) & 1) << 5;  // A3.6 → bit5
            idx |= ((A3 >> 7) & 1) << 4;  // A3.7 → bit4
            idx |= ((A3 >> 0) & 1) << 3;  // A3.0 → bit3
            idx |= ((A3 >> 1) & 1) << 2;  // A3.1 → bit2
            idx |= ((A3 >> 2) & 1) << 1;  // A3.2 → bit1
            idx |= ((A3 >> 3) & 1) << 0;  // A3.3 → bit0
            return idx;
        }
        public static string GetReceiveToneLabel(byte A3) => LabelFromIndex(BuildReceiveToneIndex(A3));
        public static bool IsSquelchTailEliminationEnabled(byte A3) => ((A3 >> 7) & 1) == 1;

        // ======================== TX ========================
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int idx = 0;
            // >>> TX WINDOW (i5..i0 MSB→LSB): edit these six lines if needed <<<
            idx |= ((B3 >> 7) & 1) << 5;  // i5 ← B3.7
            idx |= ((B3 >> 4) & 1) << 4;  // i4 ← B3.4
            idx |= ((B0 >> 5) & 1) << 3;  // i3 ← B0.5
            idx |= ((B0 >> 2) & 1) << 2;  // i2 ← B0.2
            idx |= ((B0 >> 1) & 1) << 1;  // i1 ← B0.1
            idx |= ((B0 >> 0) & 1) << 0;  // i0 ← B0.0
            return idx;
        }
        public static string GetTransmitToneLabel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
            => LabelFromIndex(BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0));

        // ==================== Diagnostics ====================
        public static string DescribeRx(byte A3)
        {
            int b5=(A3>>6)&1,b4=(A3>>7)&1,b3=(A3>>0)&1,b2=(A3>>1)&1,b1=(A3>>2)&1,b0=(A3>>3)&1;
            int idx=BuildReceiveToneIndex(A3);
            return $"RX | src [A3.6 A3.7 A3.0 A3.1 A3.2 A3.3] = [{b5} {b4} {b3} {b2} {b1} {b0}] -> index={idx} label={LabelFromIndex(idx)} STE={((A3>>7)&1)}";
        }
        public static string DescribeTx(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int j5=(B3>>7)&1,j4=(B3>>4)&1,j3=(B0>>5)&1,j2=(B0>>2)&1,j1=(B0>>1)&1,j0=(B0>>0)&1;
            int idx=BuildTransmitToneIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            return $"TX | src [B3.7 B3.4 B0.5 B0.2 B0.1 B0.0] = [{j5} {j4} {j3} {j2} {j1} {j0}] -> index={idx} label={LabelFromIndex(idx)}";
        }
        public static (string TxLabel, string RxLabel, int TxIndex, int RxIndex) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int tx=BuildTransmitToneIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int rx=BuildReceiveToneIndex(A3);
            return (LabelFromIndex(tx), LabelFromIndex(rx), tx, rx);
        }
    }
}
