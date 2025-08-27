// X2212 — Tone Indexing (Direct Array Model)
// Your model: pick six bits → build a 6-bit integer → use that integer to index a tone array.
// • No endianness in tone paths
// • Follow bit ignored (for now)
// • RX bank = B3.bit1 handled via separate 64-entry arrays per bank
// • Unknown array slots render "?" (never a wrong label); zero renders "0"

using System;
using System.Globalization;

namespace X2212.Tones
{
    public static class ToneIndexing
    {
        // === Canonical GE Channel Guard tones (exactly as provided) ===
        public static readonly string[] CanonicalLabels = new string[] {
            "0",
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
            "203.5","210.7"
        };

        // === 64-entry code→tone arrays (direct indexing) ===
        // Fill with null for "unknown", or with exact Canonical label strings.
        // Seeded with a few proven pairs; extend as you validate.
        public static readonly string?[] TxCodeToTone = new string?[64];
        public static readonly string?[] RxCodeToTone_Bank0 = new string?[64];
        public static readonly string?[] RxCodeToTone_Bank1 = new string?[64];

        static ToneIndexing()
        {
            // TX seeds (from your TX1_* fixtures, CH15):
            TxCodeToTone[ 1] = "67.0";
            TxCodeToTone[ 9] = "100.0";
            TxCodeToTone[11] = "114.8";
            TxCodeToTone[22] = "85.4";
            TxCodeToTone[48] = "186.2";

            // RX seeds (earlier confirmations):
            RxCodeToTone_Bank0[24] = "210.7";
            RxCodeToTone_Bank0[40] = "67.0";

            // Everything else remains null until confirmed against DOS.
        }

        // === Bit windows (six wires each) ===
        // RX: A3-only — i5..i0 ← [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        public static int DecodeRxIndex(byte A3)
        {
            int i5=(A3>>6)&1, i4=(A3>>7)&1, i3=(A3>>0)&1, i2=(A3>>1)&1, i1=(A3>>2)&1, i0=(A3>>3)&1;
            return (i5<<5)|(i4<<4)|(i3<<3)|(i2<<2)|(i1<<1)|i0;
        }

        // TX: i5..i0 ← [ B0.4 , B2.2 , B3.3 , B3.2 , B3.1 , B3.0 ]
        // Byte order: [A0,A1,A2,A3,B0,B1,B2,B3] = indices 0..7 for reference.
        public static int DecodeTxIndex(byte B0, byte B2, byte B3)
        {
            int i5=(B0>>4)&1, i4=(B2>>2)&1, i3=(B3>>3)&1, i2=(B3>>2)&1, i1=(B3>>1)&1, i0=(B3>>0)&1;
            return (i5<<5)|(i4<<4)|(i3<<3)|(i2<<2)|(i1<<1)|i0;
        }

        // === Label helpers for binding ===
        // Returns "0" for index 0; returns "?" when array slot is null
        public static string LabelForTx(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int idx = DecodeTxIndex(B0,B2,B3);
            if (idx == 0) return "0";
            var s = TxCodeToTone[idx];
            return s ?? "?";
        }

        public static string LabelForRx(byte A3, byte A2, byte B3)
        {
            int idx = DecodeRxIndex(A3);
            if (idx == 0) return "0"; // Follow ignored
            int bank = (B3>>1)&1;
            var arr = (bank==0) ? RxCodeToTone_Bank0 : RxCodeToTone_Bank1;
            var s = arr[idx];
            return s ?? "?";
        }

        // === Optional helpers for learning while you verify ===
        public static void LearnTx(int idx, string label) { if (idx>=0 && idx<64) TxCodeToTone[idx]=label; }
        public static void LearnRx(int bank, int idx, string label)
        {
            if (idx<0 || idx>=64) return;
            if (bank==0) RxCodeToTone_Bank0[idx]=label; else RxCodeToTone_Bank1[idx]=label;
        }
    }
}
