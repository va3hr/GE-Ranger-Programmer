// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels + TX Bit Router
// -----------------------------------------------------------------------------
// Project standards (Peter):
//   • Canonical tone list is frozen unless explicitly changed by Peter.
//   • Bit windows are explicit, MSB→LSB, with clear names.
//   • Build the index progressively (one integer, bits OR’d in order).
//   • Inspect helpers + SourceNames keep diagnostics in lockstep.
//   • NEW: Per-channel TX bit routing so pooled/banked bits are handled cleanly.
// -----------------------------------------------------------------------------

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ------------------------------
        // Canonical tone labels (index 0 = "0")
        // ------------------------------
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
        // RECEIVE (unchanged; still from A3 window)
        //   RX i5..i0 = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        // ---------------------------------------------------------------------
        public static readonly string[] ReceiveBitSourceNames = new[]
            { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

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

        // ---------------------------------------------------------------------
        // TRANSMIT — Routed bit sources (handles pooled/banked bits)
        //
        // Default per-row mapping (own row):
        //   i5 = B0.4,  i4 = B3.1,  i3 = B2.2,  i2 = B0.5,  i1 = B2.4,  i0 = B2.6
        //
        // For channels that use a banked bit, override only that (ch,bit) entry
        // to point at a different *row* (byte/bit usually stays the same).
        // ---------------------------------------------------------------------

        // Screen→file row mapping (frozen for this project).
        private static readonly int[] ScreenToFileRow = { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };

        // Byte indexes inside a row: A0..A3=0..3, B0..B3=4..7
        private const int A0 = 0, A1 = 1, A2 = 2, A3 = 3, B0 = 4, B1 = 5, B2 = 6, B3 = 7;

        // Bit index positions (human names → array index)
        private const int I5 = 0, I4 = 1, I3 = 2, I2 = 3, I1 = 4, I0 = 5;

        private readonly struct BitRoute
        {
            public BitRoute(int row, int @byte, int bit) { Row = row; Byte = @byte; Bit = bit; }
            public int Row { get; }   // -1 means "use own row"
            public int Byte { get; }  // 0..7  (A0..B3)
            public int Bit { get; }   // 0..7
        }

        // [channel 0..15, bit I5..I0] → route
        private static readonly BitRoute[,] TxRoute = new BitRoute[16, 6];

        static ToneLock()
        {
            // Fill defaults (own row; B0.4, B3.1, B2.2, B0.5, B2.4, B2.6)
            for (int ch = 0; ch < 16; ch++)
            {
                TxRoute[ch, I5] = new BitRoute(-1, B0, 4);
                TxRoute[ch, I4] = new BitRoute(-1, B3, 1);
                TxRoute[ch, I3] = new BitRoute(-1, B2, 2);
                TxRoute[ch, I2] = new BitRoute(-1, B0, 5);
                TxRoute[ch, I1] = new BitRoute(-1, B2, 4);
                TxRoute[ch, I0] = new BitRoute(-1, B2, 6);
            }

            // -------------------------------------------------------------
            // Place any *known* overrides here. Example syntax:
            // Override(ch1:  1, bit: I2, row:  6, @byte: B0, bitNo: 5); // CH1 i2 from row06.B0.5
            // Override(ch1:  9, bit: I4, row: 10, @byte: B3, bitNo: 1); // CH9 i4 from row10.B3.1
            // (Leave empty for now; fill as you prove each pooled bit.)
            // -------------------------------------------------------------
            ApplyTxOverrides();
        }

        private static void Override(int ch1, int bit, int row, int @byte, int bitNo)
        {
            int ch = ch1 - 1;
            if (ch < 0 || ch >= 16) return;
            TxRoute[ch, bit] = new BitRoute(row, @byte, bitNo);
        }

        private static void ApplyTxOverrides()
        {
            // TODO: fill with your proven pooled mappings.
            // Example (commented): Override(1, I2, 6, B0, 5);
        }

        private static int ReadBit(byte[] logical128, int row, int @byte, int bitNo)
            => (logical128[row * 8 + @byte] >> bitNo) & 1;

        public static (int[] Values, int Index) InspectTransmitBitsRouted(byte[] logical128, int screenChannel0)
        {
            int sc = screenChannel0;
            int fallbackRow = ScreenToFileRow[sc];

            int[] v = new int[6];
            // I5..I0
            var r5 = TxRoute[sc, I5]; int row5 = (r5.Row >= 0) ? r5.Row : fallbackRow; v[I5] = ReadBit(logical128, row5, r5.Byte, r5.Bit);
            var r4 = TxRoute[sc, I4]; int row4 = (r4.Row >= 0) ? r4.Row : fallbackRow; v[I4] = ReadBit(logical128, row4, r4.Byte, r4.Bit);
            var r3 = TxRoute[sc, I3]; int row3 = (r3.Row >= 0) ? r3.Row : fallbackRow; v[I3] = ReadBit(logical128, row3, r3.Byte, r3.Bit);
            var r2 = TxRoute[sc, I2]; int row2 = (r2.Row >= 0) ? r2.Row : fallbackRow; v[I2] = ReadBit(logical128, row2, r2.Byte, r2.Bit);
            var r1 = TxRoute[sc, I1]; int row1 = (r1.Row >= 0) ? r1.Row : fallbackRow; v[I1] = ReadBit(logical128, row1, r1.Byte, r1.Bit);
            var r0 = TxRoute[sc, I0]; int row0 = (r0.Row >= 0) ? r0.Row : fallbackRow; v[I0] = ReadBit(logical128, row0, r0.Byte, r0.Bit);

            int idx = (v[I5] << 5) | (v[I4] << 4) | (v[I3] << 3) | (v[I2] << 2) | (v[I1] << 1) | v[I0];
            return (v, idx);
        }

        public static int BuildTransmitToneIndexRouted(byte[] logical128, int screenChannel0)
        {
            var t = InspectTransmitBitsRouted(logical128, screenChannel0);
            return t.Index;
        }

        public static string GetTransmitToneLabelRouted(byte[] logical128, int screenChannel0)
        {
            int idx = BuildTransmitToneIndexRouted(logical128, screenChannel0);
            return LabelFromIndex(idx);
        }

        // ---------------------------------------------------------------------
        // Legacy single-row TX (kept for reference/experiments)
        //   TX i5..i0 = [B0.4, B3.1, B2.2, B0.5, B2.4, B2.6] from the SAME row.
        // ---------------------------------------------------------------------
        public static readonly string[] TransmitBitSourceNames = new[]
            { "B0.4", "B3.1", "B2.2", "B0.5", "B2.4", "B2.6" };

        public static (int[] Values, int Index) InspectTransmitBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int[] v = new int[6];
            v[0] = (B0>>4)&1; // i5 ← B0.4
            v[1] = (B3>>1)&1; // i4 ← B3.1
            v[2] = (B2>>2)&1; // i3 ← B2.2
            v[3] = (B0>>5)&1; // i2 ← B0.5
            v[4] = (B2>>4)&1; // i1 ← B2.4
            v[5] = (B2>>6)&1; // i0 ← B2.6
            int idx = (v[0]<<5)|(v[1]<<4)|(v[2]<<3)|(v[3]<<2)|(v[4]<<1)|v[5];
            return (v, idx);
        }

        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            var t = InspectTransmitBits(A3, A2, A1, A0, B3, B2, B1, B0);
            return t.Index;
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
