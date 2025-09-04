// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels (TX EE/EF + RX E0/E6/E7)
// -----------------------------------------------------------------------------
// • TX decode: code = (EEL<<4) | EFL  (EE/EF low nibbles).
// • RX decode: key  = (E0L<<8) | (E6L<<4) | E7L  (three low nibbles).
// • Full writeback nibble patterns exposed for TX (E8L, EDL, EEL, EFL) and
//   RX (E0L, E6L, E7L).
// • Canonical tone list has no 114.1. Unknown codes => "Err".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical CTCSS list (index 0 is "0" = no tone)
        private static readonly string[] CanonicalTonesNoZero =
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // UI menus (no "0"; UI shows blank/null for "0")
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index → label (index 0 = "0")  — kept for RgrCodec back-compat
        private static readonly string[] ToneByIndex = BuildToneByIndex();
        public static string[] Cg => ToneByIndex;

        private static string[] BuildToneByIndex()
        {
            var map = new string[CanonicalTonesNoZero.Length + 1];
            map[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++) map[i + 1] = CanonicalTonesNoZero[i];
            return map;
        }

        // Useful names for diag panes
        public static readonly string[] TransmitFieldSourceNames = { "EE.low", "EF.low" };
        public static readonly string[] ReceiveFieldSourceNames  = { "E0.low", "E6.low", "E7.low" };

        // Squelch Tail Elimination bit (unchanged)
        public static bool IsSquelchTailEliminationEnabled(byte rowA3) => ((rowA3 >> 7) & 1) == 1;

        // ---------------------------------------------------------------------
        // TRANSMIT — EE/EF low-nibble code table + full nibble pattern
        // ---------------------------------------------------------------------

        // Code (EEL:EFL) → label (multiple codes may map to same label)
        private static readonly Dictionary<byte, string> TxCodeToTone = new()
        {
            { 0x71, "67.0"  }, { 0xD3, "71.9"  }, { 0x20, "74.4"  }, { 0x84, "77.0"  },
            { 0xB5, "79.7"  }, { 0x15, "82.5"  }, { 0x66, "85.4"  }, { 0xC7, "88.5"  },
            { 0x37, "91.5"  }, { 0x98, "94.8"  }, { 0x38, "97.4"  }, { 0xC9, "100.0" },
            { 0x39, "103.5" }, { 0xBA, "107.2" }, { 0x3A, "110.9" }, { 0xCB, "114.8" },
            { 0x4B, "118.8" }, { 0xDC, "123.0" }, { 0x6C, "127.3" }, { 0xFD, "131.8" },
            { 0x9D, "136.5" }, { 0x3D, "141.3" }, { 0xDE, "146.2" }, { 0x7E, "151.4" },
            { 0x1E, "156.7" }, { 0xCF, "162.2" }, { 0x7F, "167.9" }, { 0x2F, "173.8" },
            { 0xD0, "179.9" }, { 0x80, "186.2" }, { 0x30, "192.8" }, { 0xD1, "203.5" },
            { 0x81, "210.7" }
        };

        // Chosen canonical code per label (for writing EE/EF)
        private static readonly Dictionary<string, byte> ToneToTxCode = new(StringComparer.Ordinal)
        {
            { "67.0", 0x71 }, { "71.9", 0xD3 }, { "74.4", 0x20 }, { "77.0", 0x84 },
            { "79.7", 0xB5 }, { "82.5", 0x15 }, { "85.4", 0x66 }, { "88.5", 0xC7 },
            { "91.5", 0x37 }, { "94.8", 0x98 }, { "97.4", 0x38 }, { "100.0", 0xC9 },
            { "103.5", 0x39 }, { "107.2", 0xBA }, { "110.9", 0x3A }, { "114.8", 0xCB },
            { "118.8", 0x4B }, { "123.0", 0xDC }, { "127.3", 0x6C }, { "131.8", 0xFD },
            { "136.5", 0x9D }, { "141.3", 0x3D }, { "146.2", 0xDE }, { "151.4", 0x7E },
            { "156.7", 0x1E }, { "162.2", 0xCF }, { "167.9", 0x7F }, { "173.8", 0x2F },
            { "179.9", 0xD0 }, { "186.2", 0x80 }, { "192.8", 0x30 }, { "203.5", 0xD1 },
            { "210.7", 0x81 }
        };

        // Full writeback nibble pattern per label (E8L, EDL, EEL, EFL)
        private static readonly Dictionary<string, (byte E8L, byte EDL, byte EEL, byte EFL)> ToneToTxNibbles
            = new(StringComparer.Ordinal)
        {
            { "67.0",  (7, 4, 1, 1) },  { "71.9",  (6,13,13, 3) }, { "74.4",  (6, 0, 2, 0) },
            { "77.0",  (8, 4, 8, 4) },  { "79.7",  (6, 4,11, 5) }, { "82.5",  (6, 0, 1, 5) },
            { "85.4",  (6, 6, 6, 6) },  { "88.5",  (6, 1,12, 7) }, { "91.5",  (6, 0, 3, 7) },
            { "94.8",  (9, 8, 9, 8) },  { "97.4",  (6, 0, 3, 8) }, { "100.0", (12, 9,12, 9) },
            { "103.5", (6, 0, 3, 9) },  { "107.2", (11,10,11,10) },{ "110.9", (6, 0, 3,10) },
            { "114.8", (12,11,12,11) }, { "118.8", (6, 4, 4,11) }, { "123.0", (13,12,13,12) },
            { "127.3", (6,12, 6,12) },  { "131.8", (15,13,15,13) },{ "136.5", (6, 0, 9,13) },
            { "141.3", (6,13, 3,13) },  { "146.2", (13,14,13,14) },{ "151.4", (6,14, 7,14) },
            { "156.7", (6, 0, 1,14) },  { "162.2", (12,15,12,15) },{ "167.9", (6,15, 7,15) },
            { "173.8", (6,15, 2,15) },  { "179.9", (7, 0,13, 0) }, { "186.2", (7, 4, 8, 0) },
            { "192.8", (7, 0, 1, 0) },  { "203.5", (7, 0,13, 1) }, { "210.7", (7, 4, 8, 1) }
        };

        public static byte BuildTransmitCodeFromEEEF(byte ee, byte ef) =>
            (byte)(((ee & 0x0F) << 4) | (ef & 0x0F));

        public static (byte EEL, byte EFL, byte Code, string Label) InspectTransmit(byte ee, byte ef)
        {
            byte eel  = (byte)(ee & 0x0F);
            byte efl  = (byte)(ef & 0x0F);
            byte code = (byte)((eel << 4) | efl);
            string label = TxCodeToTone.TryGetValue(code, out var s) ? s : "Err";
            return (eel, efl, code, label);
        }

        public static string GetTransmitToneLabel(byte ee, byte ef)
        {
            var code = BuildTransmitCodeFromEEEF(ee, ef);
            return TxCodeToTone.TryGetValue(code, out var label) ? label : "Err";
        }

        /// Full TX nibble pattern (E8L, EDL, EEL, EFL) for the given label.
        public static bool TryEncodeTransmitNibbles(string label, out byte e8l, out byte edl, out byte eel, out byte efl)
        {
            e8l = edl = eel = efl = 0;
            if (!ToneToTxNibbles.TryGetValue(label, out var p)) return false;
            e8l = p.E8L; edl = p.EDL; eel = p.EEL; efl = p.EFL;
            return true;
        }

        /// EE/EF low-nibbles only.
        public static bool TryEncodeTransmitEEEF(string label, out byte eel, out byte efl)
        {
            eel = efl = 0;
            if (!ToneToTxCode.TryGetValue(label, out var code)) return false;
            eel = (byte)((code >> 4) & 0x0F);
            efl = (byte)(code & 0x0F);
            return true;
        }

        // ---------------------------------------------------------------------
        // RECEIVE — 3-nibble table (E0L, E6L, E7L)
        // ---------------------------------------------------------------------

        // Triplet key → label
        private static readonly Dictionary<ushort, string> RxTripToTone = new()
        {
            { 0x000, "0"     }, { 0x803, "67.0"  }, { 0x80C, "71.9"  }, { 0x20E, "74.4"  },
            { 0x111, "77.0"  }, { 0xA13, "79.7"  }, { 0x415, "82.5"  }, { 0xC17, "85.4"  },
            { 0x61C, "88.5"  }, { 0xB20, "91.5"  }, { 0xF22, "94.8"  }, { 0xA25, "97.4"  },
            { 0xC27, "100.0" }, { 0xB29, "103.5" }, { 0x02C, "107.2" }, { 0x12E, "110.9" },
            { 0xD31, "114.8" }, { 0x233, "118.8" }, { 0xA35, "123.0" }, { 0xC37, "127.3" },
            { 0xA39, "131.8" }, { 0xA3D, "136.5" }, { 0xC3F, "141.3" }, { 0xB41, "146.2" },
            { 0xB44, "151.4" }, { 0xC46, "156.7" }, { 0xB49, "162.2" }, { 0xC4B, "167.9" },
            { 0xB50, "173.8" }, { 0xB52, "179.9" }, { 0xA55, "186.2" }, { 0xA57, "192.8" },
            { 0xA5B, "203.5" }, { 0xA60, "210.7" }
        };

        // Label → triplet nibbles (E0L, E6L, E7L)
        private static readonly Dictionary<string, (byte E0L, byte E6L, byte E7L)> ToneToRxTrip
            = new(StringComparer.Ordinal)
        {
            { "0",      (0, 0, 0) },   { "67.0",  (8, 0, 3) },   { "71.9",  (8, 0,12) },
            { "74.4",   (2, 0,14) },   { "77.0",  (1, 1, 1) },   { "79.7",  (10,1, 3) },
            { "82.5",   (4, 1, 5) },   { "85.4",  (12,1, 7) },   { "88.5",  (6, 1,12) },
            { "91.5",   (11,2, 0) },   { "94.8",  (15,2, 2) },   { "97.4",  (10,2, 5) },
            { "100.0",  (12,2, 7) },   { "103.5", (11,2, 9) },   { "107.2", (0, 2,12) },
            { "110.9",  (1, 2,14) },   { "114.8", (13,3, 1) },   { "118.8", (2, 3, 3) },
            { "123.0",  (10,3, 5) },   { "127.3", (12,3, 7) },   { "131.8", (10,3, 9) },
            { "136.5",  (10,3,13) },   { "141.3", (12,3,15) },   { "146.2", (11,4, 1) },
            { "151.4",  (11,4, 4) },   { "156.7", (12,4, 6) },   { "162.2", (11,4, 9) },
            { "167.9",  (12,4,11) },   { "173.8", (11,5, 0) },   { "179.9", (11,5, 2) },
            { "186.2",  (10,5, 5) },   { "192.8", (10,5, 7) },   { "203.5", (10,5,11) },
            { "210.7",  (10,6, 0) }
        };

        private static ushort BuildReceiveKey(byte e0, byte e6, byte e7) =>
            (ushort)(((e0 & 0x0F) << 8) | ((e6 & 0x0F) << 4) | (e7 & 0x0F));

        public static (byte E0L, byte E6L, byte E7L, ushort Key, string Label) InspectReceive(byte e0, byte e6, byte e7)
        {
            byte e0l = (byte)(e0 & 0x0F);
            byte e6l = (byte)(e6 & 0x0F);
            byte e7l = (byte)(e7 & 0x0F);
            ushort key = BuildReceiveKey(e0, e6, e7);
            string label = RxTripToTone.TryGetValue(key, out var s) ? s : "Err";
            return (e0l, e6l, e7l, key, label);
        }

        public static string GetReceiveToneLabel(byte e0, byte e6, byte e7)
        {
            ushort key = BuildReceiveKey(e0, e6, e7);
            return RxTripToTone.TryGetValue(key, out var label) ? label : "Err";
        }

        public static bool TryEncodeReceiveNibbles(string label, out byte e0l, out byte e6l, out byte e7l)
        {
            e0l = e6l = e7l = 0;
            if (!ToneToRxTrip.TryGetValue(label, out var p)) return false;
            e0l = p.E0L; e6l = p.E6L; e7l = p.E7L;
            return true;
        }
    }
}
