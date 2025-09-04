// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Rules:
//   • Canonical tone list is frozen (no 114.1).
//   • TX tone is a lookup from the low nibbles of EE/EF: code = (EEL<<4) | EFL.
//   • Unknown/unused codes (incl. anything that would imply 114.1) → "Err".
//   • RX kept as-is for now (bit window on A3); we’ll revisit.
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
        public static string[] Cg => ToneByIndex; // back-compat exposure

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
        // RECEIVE — (kept as before; we’ll revisit)
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

        public static int BuildReceiveToneIndex(byte rowA3) =>
            InspectReceiveBits(rowA3).Index;

        public static string GetReceiveToneLabel(byte rowA3) =>
            LabelFromIndex(BuildReceiveToneIndex(rowA3));

        public static bool IsSquelchTailEliminationEnabled(byte rowA3) =>
            ((rowA3 >> 7) & 1) == 1;

        // ---------------------------------------------------------------------
        // TRANSMIT — EE/EF low-nibble code table (final)
        //
        // code = ((EE & 0x0F) << 4) | (EF & 0x0F)  // EEL:EFL
        //
        // Notes:
        // • E8L/EDL nibbles are housekeeping; they don't affect the label.
        // • No 114.1 exists; unknown codes => "Err".
        // ---------------------------------------------------------------------
        public static readonly string[] TransmitFieldSourceNames = { "EE.low", "EF.low" };

        // For UI/debugging the EEPROM slice (GE style: E0..E7 / E8..EF)
        public static readonly string[] EepromTransmitSourceNames = { "E8L", "EDL", "EEL", "EFL" };

        private static readonly Dictionary<byte, string> TxCodeToTone = new()
        {
            { 0x71, "67.0"  }, { 0xD3, "71.9"  }, { 0x23, "74.4"  }, { 0x84, "77.0"  },
            { 0xB5, "79.7"  }, { 0x15, "82.5"  }, { 0x66, "85.4"  }, { 0xC7, "88.5"  },
            { 0x37, "91.5"  }, { 0x98, "94.8"  }, { 0x28, "97.4"  }, { 0xC9, "100.0" },
            { 0x39, "103.5" }, { 0xBA, "107.2" }, { 0x3A, "110.9" }, { 0xCB, "114.8" },
            { 0x4B, "118.8" }, { 0xDC, "123.0" }, { 0x6C, "127.3" }, { 0xFD, "131.8" },
            { 0x9D, "136.5" }, { 0x3D, "141.3" }, { 0xDE, "146.2" }, { 0x7E, "151.4" },
            { 0x1E, "156.7" }, { 0xCF, "162.2" }, { 0x7F, "167.9" }, { 0x2F, "173.8" },
            { 0xD0, "179.9" }, { 0x80, "186.2" }, { 0x30, "192.8" }, { 0xD1, "203.5" },
            { 0x81, "210.7" }
        };

        // Reverse map for writing: label → code (only canonical labels included)
        private static readonly Dictionary<string, byte> ToneToTxCode = BuildReverseTx();

        private static Dictionary<string, byte> BuildReverseTx()
        {
            var rev = new Dictionary<string, byte>(StringComparer.Ordinal);
            foreach (var kv in TxCodeToTone)
                rev[kv.Value] = kv.Key;
            return rev;
        }

        public static byte BuildTransmitCodeFromEEEF(byte ee, byte ef)
            => (byte)(((ee & 0x0F) << 4) | (ef & 0x0F));

        public static (byte EEL, byte EFL, byte Code, string Label) InspectTransmit(byte ee, byte ef)
        {
            byte eel = (byte)(ee & 0x0F);
            byte efl = (byte)(ef & 0x0F);
            byte code = (byte)((eel << 4) | efl);
            string label = TxCodeToTone.TryGetValue(code, out var s) ? s : "Err";
            return (eel, efl, code, label);
        }

        public static string GetTransmitToneLabel(byte ee, byte ef)
        {
            var code = BuildTransmitCodeFromEEEF(ee, ef);
            return TxCodeToTone.TryGetValue(code, out var label) ? label : "Err";
        }

        /// <summary>
        /// Given a 16-byte EEPROM channel block (E0..EF), returns TX nibbles and label.
        /// </summary>
        public static (byte E8L, byte EDL, byte EEL, byte EFL, byte Code, string Label)
            InspectTransmitFromBlock(byte[] block16)
        {
            if (block16 == null || block16.Length < 16) throw new ArgumentException("Need 16-byte block E0..EF");
            byte e8l = (byte)(block16[0x08] & 0x0F);
            byte edl = (byte)(block16[0x0D] & 0x0F);
            byte eel = (byte)(block16[0x0E] & 0x0F);
            byte efl = (byte)(block16[0x0F] & 0x0F);
            byte code = (byte)((eel << 4) | efl);
            string label = TxCodeToTone.TryGetValue(code, out var s) ? s : "Err";
            return (e8l, edl, eel, efl, code, label);
        }

        /// <summary>
        /// Convenience: get TX label directly from a 16-byte EEPROM block (E0..EF).
        /// </summary>
        public static string GetTransmitToneLabelFromBlock(byte[] block16)
        {
            var (_, _, _, _, _, label) = InspectTransmitFromBlock(block16);
            return label;
        }

        /// <summary>
        /// Given a label like "131.8", returns (EEL, EFL) nibbles to write into EE/EF low nibbles.
        /// Returns false if the label isn't in the canonical set.
        /// </summary>
        public static bool TryEncodeTransmitNibbles(string label, out byte eel, out byte efl)
        {
            eel = efl = 0;
            if (!ToneToTxCode.TryGetValue(label, out var code)) return false;
            eel = (byte)((code >> 4) & 0x0F);
            efl = (byte)(code & 0x0F);
            return true;
        }

        // ---------------------------------------------------------------------
        // UI nibble helpers (for a GE-style "H|L" view with low nibble on the right)
        // ---------------------------------------------------------------------
        public static string NibblesHL(byte b) => $"{(b >> 4) & 0xF:X}|{b & 0xF:X}";
        public static byte LowNibble(byte b) => (byte)(b & 0x0F);
        public static byte HighNibble(byte b) => (byte)((b >> 4) & 0x0F);
    }
}
