// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Canonical rules:
//   • CTCSS list is frozen; NO "114.1" anywhere.
//   • TX tone label = lookup by EE/EF low nibbles: code = (EEL<<4) | EFL.
//   • We still PRESERVE all four TX low nibbles (E8L, EDL, EEL, EFL) when writing,
//     but only EE/EF determine the label.
//   • RX tones: tuple of three low nibbles (E0L,E6L,E7L) → label (CSV-backed).
//     (Map left empty here; we’ll populate from your CSV next.)
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

        // Menus (UI shows "0" as blank/null; keep these as tone-only).
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index → label (index 0 = "0"). Back-compat alias: Cg
        private static readonly string[] ToneByIndex = BuildToneByIndex();
        public static string[] Cg => ToneByIndex; // <- back-compat for RgrCodec, etc.

        private static string[] BuildToneByIndex()
        {
            var map = new string[CanonicalTonesNoZero.Length + 1];
            map[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++) map[i + 1] = CanonicalTonesNoZero[i];
            return map;
        }

        // Helpers
        private static byte LowNibble(byte b) => (byte)(b & 0x0F);

        // ---------------------------------------------------------------------
        // TRANSMIT — EE/EF low-nibble code table
        //
        // code = ((EE & 0x0F) << 4) | (EF & 0x0F)  // EEL:EFL
        //
        // NOTE: E8L/EDL are preserved when writing but do not affect label decode.
        // ---------------------------------------------------------------------
        public static readonly string[] TransmitFieldSourceNames = { "E8.low", "ED.low", "EE.low", "EF.low" };

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

        private static readonly Dictionary<string, byte> ToneToTxCode = BuildReverseTx();
        private static Dictionary<string, byte> BuildReverseTx()
        {
            var rev = new Dictionary<string, byte>(StringComparer.Ordinal);
            foreach (var kv in TxCodeToTone) rev[kv.Value] = kv.Key;
            return rev;
        }

        public static (byte E8L, byte EDL, byte EEL, byte EFL, byte Code, string Label)
            InspectTransmitNibbles(byte e8, byte ed, byte ee, byte ef)
        {
            byte e8l = LowNibble(e8);
            byte edl = LowNibble(ed);
            byte eel = LowNibble(ee);
            byte efl = LowNibble(ef);
            byte code = (byte)((eel << 4) | efl);
            string label = TxCodeToTone.TryGetValue(code, out var s) ? s : "Err";
            return (e8l, edl, eel, efl, code, label);
        }

        public static string GetTransmitToneLabelFromEEL_EFL(byte ee, byte ef)
        {
            byte code = (byte)((LowNibble(ee) << 4) | LowNibble(ef));
            return TxCodeToTone.TryGetValue(code, out var s) ? s : "Err";
        }

        /// <summary>
        /// Encode label to nibs. EEL/EFL are set to the code for the label.
        /// E8L/EDL are set to 0 here — callers should preserve existing values when writing.
        /// </summary>
        public static bool TryEncodeTransmitNibbles(string label, out byte e8l, out byte edl, out byte eel, out byte efl)
        {
            e8l = 0; edl = 0; eel = 0; efl = 0;
            if (!ToneToTxCode.TryGetValue(label, out var code)) return false;
            eel = (byte)((code >> 4) & 0x0F);
            efl = (byte)(code & 0x0F);
            return true;
        }

        // ---------------------------------------------------------------------
        // RECEIVE — three-low-nibble tuple (E0L, E6L, E7L)
        // (Populate from CSV shortly; left empty so build passes.)
        // ---------------------------------------------------------------------
        private static readonly Dictionary<(byte E0L, byte E6L, byte E7L), string> RxCodeToTone =
            new Dictionary<(byte, byte, byte), string>(capacity: 64);
        private static readonly Dictionary<string, (byte E0L, byte E6L, byte E7L)> ToneToRxCode =
            new Dictionary<string, (byte, byte, byte)>(StringComparer.Ordinal);

        public static (byte E0L, byte E6L, byte E7L, byte Code, string Label)
            InspectReceiveNibbles(byte e0, byte e6, byte e7)
        {
            byte e0l = LowNibble(e0);
            byte e6l = LowNibble(e6);
            byte e7l = LowNibble(e7);
            string label = RxCodeToTone.TryGetValue((e0l, e6l, e7l), out var s) ? s : "Err";
            // Pack a convenience “code” if you like (e.g., (e6l<<4)|e7l), but label is authoritative.
            byte code = (byte)((e6l << 4) | e7l);
            return (e0l, e6l, e7l, code, label);
        }

        public static string GetReceiveToneLabelFromNibbles(byte e0, byte e6, byte e7)
            => RxCodeToTone.TryGetValue((LowNibble(e0), LowNibble(e6), LowNibble(e7)), out var s) ? s : "Err";

        public static bool TryEncodeReceiveNibbles(string label, out byte e0l, out byte e6l, out byte e7l)
        {
            e0l = e6l = e7l = 0;
            if (!ToneToRxCode.TryGetValue(label, out var triple)) return false;
            (e0l, e6l, e7l) = triple;
            return true;
        }

        // Legacy helper you were using for STE:
        public static bool IsSquelchTailEliminationEnabled(byte a3)
            => ((a3 >> 7) & 1) == 1;
    }
}
