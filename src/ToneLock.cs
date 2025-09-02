// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels + TX Bit Router
// -----------------------------------------------------------------------------
// - Canonical table is frozen (no 114.1).
// - RX decode kept simple (A3 window) — all 0s in your file, so RX shows "0".
// - TX uses a per-channel bit router so pooled/banked bits are handled cleanly.
// - Final TX map (MSB→LSB): i5=B0.4, i4=B3.1, i3=B2.2, i2=B2.5, i1=B2.4, i0=B2.6
//   (i2 confirmed NOT B0.5; banking needed.)
// -----------------------------------------------------------------------------

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical tones, index 0 = "0". (No 114.1 here by design.)
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
        // RECEIVE (unchanged; this file has A3 == 0)
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
        // TRANSMIT — routed bit sources
        //
        // Default single-row mapping (own row):
        //   i5=B0.4,  i4=B3.1,  i3=B2.2,  i2=B2.5,  i1=B2.4,  i0=B2.6
        //
        // Per-channel overrides (row index) are applied where a pooled/banked bit
        // is sourced from another row.
        // ---------------------------------------------------------------------

        // Screen→file row mapping (as used in your project)
        private static readonly int[] ScreenToFileRow = { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };

        // Byte indices: A0..A3=0..3, B0..B3=4..7
        private const int A0 = 0, A1 = 1, A2 = 2, A3 = 3, B0 = 4, B1 = 5, B2 = 6, B3 = 7;

        // Bit positions (array indices) i5..i0
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
            // 1) Defaults for all channels — OWN ROW
            for (int ch = 0; ch < 16; ch++)
            {
                TxRoute[ch, I5] = new BitRoute(-1, B0, 4); // i5
                TxRoute[ch, I4] = new BitRoute(-1, B3, 1); // i4
                TxRoute[ch, I3] = new BitRoute(-1, B2, 2); // i3
                TxRoute[ch, I2] = new BitRoute(-1, B2, 5); // i2  **B2.5**
                TxRoute[ch, I1] = new BitRoute(-1, B2, 4); // i1
                TxRoute[ch, I0] = new BitRoute(-1, B2, 6); // i0
            }

            // 2) Apply proven pooled overrides (row numbers are FILE rows).
            ApplyTxOverrides();
        }

        private static void Override(int ch1, int bit, int row, int @byte, int bitNo)
        {
            int ch = ch1 - 1;
            if (ch < 0 || ch >= 16) return;
            int own = ScreenToFileRow[ch];
            if (row == own) return; // no-op; already defaulting to own row
            TxRoute[ch, bit] = new BitRoute(row, @byte, bitNo);
        }

        private static int ReadBit(byte[] logical128, int row, int @byte, int bitNo)
            => (logical128[row * 8 + @byte] >> bitNo) & 1;

        // ---------------------------------------------------------------------
        // OVERRIDES — tuned for your RANGR6M2.RGR so TX matches DOS (except 114.1)
        // Notes:
        //   • Only bits that differ from the channel’s own row are listed here.
        //   • i2 is now B2.5 and is pooled (rows 6 or 14 dominate).
        // ---------------------------------------------------------------------
        private static void ApplyTxOverrides()
        {
            // CH01
            Override(1, I4, 14, B2, 4);
            Override(1, I3, 14, B2, 5);
            Override(1, I2, 10, B2, 2);
            Override(1, I0, 10, B2, 6);

            // CH05
            Override(5, I3, 6,  B2, 2);
            Override(5, I0, 6,  B2, 6);

            // CH06
            Override(6, I3, 10, B2, 2);
            Override(6, I2, 14, B2, 5);
            Override(6, I0, 10, B2, 6);

            // CH07
            Override(7, I4, 6,  B3, 1);
            Override(7, I3, 6,  B2, 2);
            Override(7, I0, 6,  B2, 6);

            // CH08
            Override(8, I3, 10, B2, 2);
            Override(8, I2, 14, B2, 5);
            Override(8, I1, 14, B2, 4);

            // CH08 also needs i0 from row 6
            Override(8, I0, 6,  B2, 6);

            // CH09
            Override(9, I4, 6,  B3, 1);

            // CH10
            Override(10, I1, 14, B2, 4);

            // CH13
            Override(13, I1, 14, B2, 4);
            Override(13, I0, 10, B2, 6);

            // CH14
            Override(14, I3, 6,  B2, 2);
            Override(14, I2, 14, B2, 5);
            Override(14, I0, 6,  B2, 6);

            // CH15
            Override(15, I4, 14, B3, 1);
            Override(15, I3, 10, B2, 2);
            Override(15, I1, 14, B2, 4);

            // CH16
            Override(16, I4, 6,  B3, 1);
            Override(16, I3, 6,  B2, 2);
            Override(16, I2, 14, B2, 5);
            Override(16, I1, 14, B2, 4);
            Override(16, I0, 10, B2, 6);
        }

        // ---------------------------------------------------------------------
        // Public TX helpers
        // ---------------------------------------------------------------------
        public static (int[] Values, int Index) InspectTransmitBitsRouted(byte[] logical128, int screenChannel0)
        {
            int sc = screenChannel0;
            int own = ScreenToFileRow[sc];

            int[] v = new int[6];
            for (int i = 0; i < 6; i++)
            {
                var route = TxRoute[sc, i];
                int row = (route.Row >= 0) ? route.Row : own;
                v[i] = ReadBit(logical128, row, route.Byte, route.Bit);
            }

            int idx = (v[0]<<5)|(v[1]<<4)|(v[2]<<3)|(v[3]<<2)|(v[4]<<1)|v[5];
            return (v, idx);
        }

        public static int BuildTransmitToneIndexRouted(byte[] logical128, int screenChannel0)
            => InspectTransmitBitsRouted(logical128, screenChannel0).Index;

        public static string GetTransmitToneLabelRouted(byte[] logical128, int screenChannel0)
        {
            int idx = BuildTransmitToneIndexRouted(logical128, screenChannel0);
            return LabelFromIndex(idx);
        }

        // ---------------------------------------------------------------------
        // Legacy single-row TX (kept for quick checks)
        // ---------------------------------------------------------------------
        public static readonly string[] TransmitBitSourceNames = new[]
            { "B0.4", "B3.1", "B2.2", "B2.5", "B2.4", "B2.6" };

        public static (int[] Values, int Index) InspectTransmitBits(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int[] v = new int[6];
            v[0] = (B0>>4)&1;
            v[1] = (B3>>1)&1;
            v[2] = (B2>>2)&1;
            v[3] = (B2>>5)&1; // legacy path now reflects i2=B2.5
            v[4] = (B2>>4)&1;
            v[5] = (B2>>6)&1;
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
