// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Project standards (Peter):
//   • Canonical tone list is frozen unless explicitly changed by Peter.
//   • Bit windows are explicit, MSB→LSB, with clear names.
//   • Build the index progressively (one integer, bits OR’d in order).
//   • Inspect helpers + SourceNames keep diagnostics in lockstep.
// -----------------------------------------------------------------------------

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical tone table (UNCHANGED). Index 0 = "0".
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
        public static string[] Cg => ToneIndexToLabel;

        private static string[] BuildToneIndexToLabel()
        {
            var t = new string[CanonicalTonesNoZero.Length + 1];
            t[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++) t[i + 1] = CanonicalTonesNoZero[i];
            return t;
        }

        private static int Bit(byte v, int n) => (v >> n) & 1;
        private static string LabelFromIndex(int idx) => (idx >= 0 && idx < ToneIndexToLabel.Length) ? ToneIndexToLabel[idx] : "Err";

        // ---------------------------------------------------------------------
        // CURRENT WINDOWS (MSB→LSB)
        //   RX i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        //   TX i5..i0 = [B0.4, B2.5, B2.2, B0.5, B2.4, B2.6]
        //                ^^^^^  ^^^^^  ^^^^^  ^^^^^  ^^^^^  ^^^^^
        //                i5     i4     i3     i2     i1     i0
        //
        // Changes from your last revision:
        //   • i5 (32’s) stays B0.4 (remapped earlier).
        //   • i1 (2’s)  stays B2.4 (remapped earlier).
        //   • i4 (16’s) is now B2.5 (was B3.1).  // <-- new proof from TX1_1365
        // ---------------------------------------------------------------------

        public static readonly string[] ReceiveBitSourceNames = new[]
            { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

        public static readonly string[] TransmitBitSourceNames = new[]
            { "B0.4", "B2.5", "B2.2", "B0.5", "B2.4", "B2.6" };

        public static (int[] Values, int Index) InspectReceiveBits(byte A3)
        {
            int[] v = new int[6];
            v[0] = Bit(A3, 6);
            v[1] = Bit(A3, 7);
            v[2] = Bit(A3, 0);
            v[3] = Bit(A3, 1);
            v[4] = Bit(A3, 2);
            v[5] = Bit(A3, 3);
            int idx = (v[0]<<5)|(v[1]<<4)|(v[2]<<3)|(v[3]<<2)|(v[4]<<1)|v[5];
            return (v, idx);
        }

        public static (int[] Values, int Index) InspectTransmitBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int[] v = new int[6];
            v[0] = (B0>>4)&1; // i5 ← B0.4
            v[1] = (B2>>5)&1; // i4 ← B2.5   (UPDATED)
            v[2] = (B2>>2)&1; // i3 ← B2.2
            v[3] = (B0>>5)&1; // i2 ← B0.5
            v[4] = (B2>>4)&1; // i1 ← B2.4
            v[5] = (B2>>6)&1; // i0 ← B2.6
            int idx = (v[0]<<5)|(v[1]<<4)|(v[2]<<3)|(v[3]<<2)|(v[4]<<1)|v[5];
            return (v, idx);
        }

        public static int BuildReceiveToneIndex(byte A3)
        {
            int i5 = Bit(A3, 6), i4 = Bit(A3, 7), i3 = Bit(A3, 0),
                i2 = Bit(A3, 1), i1 = Bit(A3, 2), i0 = Bit(A3, 3);
            int idx = 0;
            idx |= (i5<<5); idx |= (i4<<4); idx |= (i3<<3);
            idx |= (i2<<2); idx |= (i1<<1); idx |= (i0<<0);
            return idx;
        }

        public static string GetReceiveToneLabel(byte A3) => LabelFromIndex(BuildReceiveToneIndex(A3));

        public static bool IsSquelchTailEliminationEnabled(byte A3) => ((A3>>7)&1)==1;

        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            // EDIT THESE SIX LINES ONLY (MSB→LSB)
            int i5 = (B0>>4)&1; // B0.4
            int i4 = (B2>>5)&1; // B2.5   (UPDATED)
            int i3 = (B2>>2)&1; // B2.2
            int i2 = (B0>>5)&1; // B0.5
            int i1 = (B2>>4)&1; // B2.4
            int i0 = (B2>>6)&1; // B2.6

            int idx = 0;
            idx |= (i5<<5);
            idx |= (i4<<4);
            idx |= (i3<<3);
            idx |= (i2<<2);
            idx |= (i1<<1);
            idx |= (i0<<0);
            return idx;
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
