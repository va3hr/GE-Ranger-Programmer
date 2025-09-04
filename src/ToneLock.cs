// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Rules:
//   • Canonical tone list is frozen (no 114.1).
//   • TX uses a canonical 4-nibble pattern per tone: E8L, EDL, EEL, EFL.
//   • Decode key = (EEL<<4)|(EFL). E8L/EDL are validated + written back.
//   • Unknown/unused codes (incl. anything that would imply 114.1) → "Err".
//   • RX kept as-is (A3 bit window) until your RX CSV is final.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical CTCSS table: index 0 is "0" (no tone). No "114.1".
        private static readonly string[] CanonicalTonesNoZero =
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // UI menus (no "0" here — UI shows "0" as blank/null)
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index → label (index 0 = "0")
        private static readonly string[] ToneByIndex = BuildToneByIndex();
        public static string[] Cg => ToneByIndex; // back-compat exposure (receive only)

        private static string[] BuildToneByIndex()
        {
            var map = new string[CanonicalTonesNoZero.Length + 1];
            map[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++) map[i + 1] = CanonicalTonesNoZero[i];
            return map;
        }

        // Helpers
        private static int ExtractBit(byte value, int bitIndex) => (value >> bitIndex) & 1;
        private static string LabelFromIndex(int index) =>
            (index >= 0 && index < ToneByIndex.Length) ? ToneByIndex[index] : "Err";

        // ---------------------------------------------------------------------
        // RECEIVE — (kept as before; we’ll revisit when your RX nibble CSV is final)
        // RxIndexBits [5..0] = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
        // ---------------------------------------------------------------------
        public static readonly string[] ReceiveBitSourceNames = { "A3.6", "A3.7", "A3.0", "A3.1", "A3.2", "A3.3" };

        public static (int[] Bits, int Index) InspectReceiveBits(byte rowA3)
        {
            int[] bits = new int[6];
            bits[0] = ExtractBit(rowA3, 6); // i5
            bits[1] = ExtractBit(rowA3, 7); // i4
            bits[2] = ExtractBit(rowA3, 0); // i3
            bits[3] = ExtractBit(rowA3, 1); // i2
            bits[4] = ExtractBit(rowA3, 2); // i1
            bits[5] = ExtractBit(rowA3, 3); // i0
            int index = (bits[0] << 5) | (bits[1] << 4) | (bits[2] << 3) | (bits[3] << 2) | (bits[4] << 1) | bits[5];
            return (bits, index);
        }

        public static int BuildReceiveToneIndex(byte rowA3) => InspectReceiveBits(rowA3).Index;
        public static string GetReceiveToneLabel(byte rowA3) => LabelFromIndex(BuildReceiveToneIndex(rowA3));
        public static bool IsSquelchTailEliminationEnabled(byte rowA3) => ((rowA3 >> 7) & 1) == 1;

        // ---------------------------------------------------------------------
        // TRANSMIT — full 4-nibble layout (E8L, EDL, EEL, EFL)
        //    EEL:EFL is the decode code; E8L:EDL are preserved/validated + written
        // ---------------------------------------------------------------------
        public static readonly string[] TxNibbleSourceNames = { "B0.low (E8L)", "B1.low (EDL)", "B2.low (EE low)", "B3.low (EF low)" };

        public readonly struct TxQuad
        {
            public readonly byte E8L, EDL, EEL, EFL;
            public TxQuad(byte e8l, byte edl, byte eel, byte efl) { E8L = e8l; EDL = edl; EEL = eel; EFL = efl; }
            public byte Code => (byte)((EEL << 4) | EFL);
        }

        // Label → full nibble quad (from your CSV)
        private static readonly Dictionary<string, TxQuad> TxLabelToQuad = new(StringComparer.Ordinal)
        {
          { "67.0",  new TxQuad(0x6, 0x0, 0x7, 0x1) },
          { "71.9",  new TxQuad(0x6, 0x4, 0xD, 0x3) },
          { "74.4",  new TxQuad(0x6, 0x0, 0x2, 0x3) },
          { "77.0",  new TxQuad(0x6, 0x0, 0x8, 0x4) },
          { "79.7",  new TxQuad(0x6, 0x4, 0xB, 0x5) },
          { "82.5",  new TxQuad(0x6, 0x0, 0x1, 0x5) },
          { "85.4",  new TxQuad(0x6, 0x0, 0x6, 0x6) },
          { "88.5",  new TxQuad(0x6, 0x4, 0xC, 0x7) },
          { "91.5",  new TxQuad(0x6, 0x0, 0x3, 0x7) },
          { "94.8",  new TxQuad(0x6, 0x4, 0x9, 0x8) },
          { "97.4",  new TxQuad(0x6, 0x0, 0x2, 0x8) },
          { "100.0", new TxQuad(0x6, 0x4, 0xC, 0x9) },
          { "103.5", new TxQuad(0x6, 0x4, 0x3, 0x9) },
          { "107.2", new TxQuad(0x6, 0x4, 0xB, 0xA) },
          { "110.9", new TxQuad(0x6, 0x4, 0x3, 0xA) },
          { "114.8", new TxQuad(0x6, 0x4, 0xC, 0xB) },
          { "118.8", new TxQuad(0x0, 0x0, 0x4, 0xB) },
          { "123.0", new TxQuad(0x6, 0x4, 0xD, 0xC) },
          { "127.3", new TxQuad(0x6, 0x0, 0x6, 0xC) },
          { "131.8", new TxQuad(0x7, 0x4, 0xF, 0xD) },
          { "136.5", new TxQuad(0x6, 0x0, 0x9, 0xD) },
          { "141.3", new TxQuad(0x6, 0x4, 0x3, 0xD) },
          { "146.2", new TxQuad(0x6, 0x4, 0xD, 0xE) },
          { "151.4", new TxQuad(0x0, 0x0, 0x7, 0xE) },
          { "156.7", new TxQuad(0x0, 0x0, 0x1, 0xE) },
          { "162.2", new TxQuad(0x6, 0x0, 0xC, 0xF) },
          { "167.9", new TxQuad(0x0, 0x0, 0x7, 0xF) },
          { "173.8", new TxQuad(0x2, 0x0, 0x2, 0xF) },
          { "179.9", new TxQuad(0x7, 0x0, 0xD, 0x0) },
          { "186.2", new TxQuad(0x7, 0x4, 0x8, 0x0) },
          { "192.8", new TxQuad(0x3, 0x0, 0x3, 0x0) },
          { "203.5", new TxQuad(0x6, 0x4, 0xD, 0x1) },
          { "210.7", new TxQuad(0x7, 0x4, 0x8, 0x1) },
        };

        // code (EEL:EFL) → label
        private static readonly Dictionary<byte, string> TxCodeToTone = BuildCodeToTone();
        // code → full quad (fast path for write-back)
        private static readonly Dictionary<byte, TxQuad> TxCodeToQuad = BuildCodeToQuad();
        // label → code (convenience)
        private static readonly Dictionary<string, byte> ToneToTxCode = BuildReverseTx();

        private static Dictionary<byte, string> BuildCodeToTone()
        {
            var m = new Dictionary<byte, string>();
            foreach (var kv in TxLabelToQuad)
                m[kv.Value.Code] = kv.Key;
            return m;
        }
        private static Dictionary<byte, TxQuad> BuildCodeToQuad()
        {
            var m = new Dictionary<byte, TxQuad>();
            foreach (var kv in TxLabelToQuad)
                m[kv.Value.Code] = kv.Value;
            return m;
        }
        private static Dictionary<string, byte> BuildReverseTx()
        {
            var rev = new Dictionary<string, byte>(StringComparer.Ordinal);
            foreach (var kv in TxLabelToQuad)
                rev[kv.Key] = kv.Value.Code;
            return rev;
        }

        public static byte BuildTransmitCodeFromEEEF(byte ee /*B2*/, byte ef /*B3*/)
            => (byte)(((ee & 0x0F) << 4) | (ef & 0x0F));

        public static string GetTransmitToneLabel(byte ee /*B2*/, byte ef /*B3*/)
        {
            byte code = BuildTransmitCodeFromEEEF(ee, ef);
            return TxCodeToTone.TryGetValue(code, out var label) ? label : "Err";
        }

        /// Inspect TX nibble quartet from the four B-bytes (low nibs only).
        /// Returns the nibbles, the code, the label, and whether E8L/EDL match the canonical quad.
        public static (byte E8L, byte EDL, byte EEL, byte EFL, byte Code, string Label, bool HousekeepingOk)
            InspectTransmit(byte b0, byte b1, byte b2, byte b3)
        {
            byte e8l = (byte)(b0 & 0x0F);
            byte edl = (byte)(b1 & 0x0F);
            byte eel = (byte)(b2 & 0x0F);
            byte efl = (byte)(b3 & 0x0F);
            byte code = (byte)((eel << 4) | efl);
            string label = TxCodeToTone.TryGetValue(code, out var s) ? s : "Err";

            bool ok = true;
            if (label != "Err" && TxCodeToQuad.TryGetValue(code, out var canonical))
                ok = (canonical.E8L == e8l) && (canonical.EDL == edl);

            return (e8l, edl, eel, efl, code, label, ok);
        }

        /// Given a label like "131.8", returns the full (E8L, EDL, EEL, EFL) nibble set.
        public static bool TryEncodeTransmitNibbles(string label, out byte e8l, out byte edl, out byte eel, out byte efl)
        {
            e8l = edl = eel = efl = 0;
            if (!TxLabelToQuad.TryGetValue(label, out var q)) return false;
            e8l = q.E8L; edl = q.EDL; eel = q.EEL; efl = q.EFL;
            return true;
        }

        /// Apply a TX label to the four B-bytes (B0..B3), preserving their high nibbles.
        public static bool TryApplyTransmitLabel(ref byte b0, ref byte b1, ref byte b2, ref byte b3, string label)
        {
            if (!TryEncodeTransmitNibbles(label, out byte e8l, out byte edl, out byte eel, out byte efl))
                return false;

            b0 = (byte)((b0 & 0xF0) | e8l);
            b1 = (byte)((b1 & 0xF0) | edl);
            b2 = (byte)((b2 & 0xF0) | eel);
            b3 = (byte)((b3 & 0xF0) | efl);
            return true;
        }
    }
}
