// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels (TX=EE/EF low nibbles, RX=E0/E6/E7 low nibbles)
// -----------------------------------------------------------------------------
// • Source of truth: ToneCodes_TX_E8_ED_EE_EF.csv (user-supplied).
// • TX decode uses code = (EEL<<4)|(EFL). We also preserve/write the housekeeping
//   low-nibbles (E8L, EDL) exactly as in the table because the radio expects them.
// • RX decode uses the triplet (E0L, E6L, E7L).
// • Unknown/unused codes return "Err".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical CTCSS for UI menus; "0" is shown as blank in UI.
        private static readonly string[] CanonicalNoZero = new[]
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        public static readonly string[] ToneMenuTx = CanonicalNoZero;
        public static readonly string[] ToneMenuRx = CanonicalNoZero;

        // Labels for debug panes (do NOT imply absolute EEPROM addresses).
        public static readonly string[] TxNibbleNames = { "EE.low (TxCodeHi)", "EF.low (TxCodeLo)" };
        public static readonly string[] RxNibbleNames = { "E0.low", "E6.low", "E7.low" };

        // ---------------- TX: code = (EEL<<4)|(EFL) ----------------
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

        // Reverse for writing ALL TX nibs (E8L, EDL, EEL, EFL) exactly as per the radio.
        private static readonly Dictionary<string, (byte E8L, byte EDL, byte EEL, byte EFL)> ToneToTxNibbles =
            new(StringComparer.Ordinal)
        {
            ["67.0"] = (7, 1, 7, 1),
            ["71.9"] = (7, 3, 13, 3),
            ["74.4"] = (7, 3, 2, 3),
            ["77.0"] = (7, 4, 8, 4),
            ["79.7"] = (6, 5, 11, 5),
            ["82.5"] = (6, 5, 1, 5),
            ["85.4"] = (6, 6, 6, 6),
            ["88.5"] = (6, 7, 12, 7),
            ["91.5"] = (6, 7, 3, 7),
            ["94.8"] = (6, 8, 9, 8),
            ["97.4"] = (6, 8, 2, 8),
            ["100.0"] = (6, 9, 12, 9),
            ["103.5"] = (6, 9, 3, 9),
            ["107.2"] = (6,10, 11,10),
            ["110.9"] = (6,10, 3,10),
            ["114.8"] = (6,11, 12,11),
            ["118.8"] = (6,11, 4,11),
            ["123.0"] = (6,12, 13,12),
            ["127.3"] = (6,12, 6,12),
            ["131.8"] = (6,13, 15,13),
            ["136.5"] = (6,13, 9,13),
            ["141.3"] = (6,13, 3,13),
            ["146.2"] = (6,14, 13,14),
            ["151.4"] = (6,14, 7,14),
            ["156.7"] = (6,14, 1,14),
            ["162.2"] = (6,15, 12,15),
            ["167.9"] = (6,15, 7,15),
            ["173.8"] = (6,15, 2,15),
            ["179.9"] = (7, 0, 13, 0),
            ["186.2"] = (7, 4, 8, 0),
            ["192.8"] = (7, 0, 3, 0),
            ["203.5"] = (7, 4, 13, 1),
            ["210.7"] = (7, 4, 8, 1),
        };

        // ---------------- RX: key = (E0L, E6L, E7L) ----------------
        private static readonly Dictionary<(byte E0L, byte E6L, byte E7L), string> RxTripletToTone = new()
        {
            [(byte)7, (byte)1, (byte)1] = "67.0",
            [(byte)7, (byte)3, (byte)3] = "71.9",
            [(byte)7, (byte)3, (byte)3] = "74.4",   // (same triplet appears for 71.9/74.4 in your photo; keep table-driven truth)
            [(byte)7, (byte)4, (byte)4] = "77.0",
            [(byte)6, (byte)5, (byte)5] = "79.7",
            [(byte)6, (byte)5, (byte)5] = "82.5",
            [(byte)6, (byte)6, (byte)6] = "85.4",
            [(byte)6, (byte)7, (byte)7] = "88.5",
            [(byte)6, (byte)7, (byte)7] = "91.5",
            [(byte)6, (byte)8, (byte)8] = "94.8",
            [(byte)6, (byte)8, (byte)8] = "97.4",
            [(byte)6, (byte)9, (byte)9] = "100.0",
            [(byte)6, (byte)9, (byte)9] = "103.5",
            [(byte)6, (byte)10,(byte)10] = "107.2",
            [(byte)6, (byte)10,(byte)10] = "110.9",
            [(byte)6, (byte)11,(byte)11] = "114.8",
            [(byte)6, (byte)11,(byte)11] = "118.8",
            [(byte)6, (byte)12,(byte)12] = "123.0",
            [(byte)6, (byte)12,(byte)12] = "127.3",
            [(byte)6, (byte)13,(byte)13] = "131.8",
            [(byte)6, (byte)13,(byte)13] = "136.5",
            [(byte)6, (byte)13,(byte)13] = "141.3",
            [(byte)6, (byte)14,(byte)14] = "146.2",
            [(byte)6, (byte)14,(byte)14] = "151.4",
            [(byte)6, (byte)14,(byte)14] = "156.7",
            [(byte)6, (byte)15,(byte)15] = "162.2",
            [(byte)6, (byte)15,(byte)15] = "167.9",
            [(byte)6, (byte)15,(byte)15] = "173.8",
            [(byte)7, (byte)0, (byte)0] = "179.9",
            [(byte)7, (byte)4, (byte)0] = "186.2",
            [(byte)7, (byte)0, (byte)0] = "192.8",
            [(byte)7, (byte)4, (byte)1] = "203.5",
            [(byte)7, (byte)4, (byte)1] = "210.7",
        };

        // Reverse for writing RX.
        private static readonly Dictionary<string, (byte E0L, byte E6L, byte E7L)> ToneToRxTriplet =
            new(StringComparer.Ordinal)
        {
            ["67.0"] = (7, 1, 1),
            ["71.9"] = (7, 3, 3),
            ["74.4"] = (7, 3, 3),
            ["77.0"] = (7, 4, 4),
            ["79.7"] = (6, 5, 5),
            ["82.5"] = (6, 5, 5),
            ["85.4"] = (6, 6, 6),
            ["88.5"] = (6, 7, 7),
            ["91.5"] = (6, 7, 7),
            ["94.8"] = (6, 8, 8),
            ["97.4"] = (6, 8, 8),
            ["100.0"] = (6, 9, 9),
            ["103.5"] = (6, 9, 9),
            ["107.2"] = (6,10,10),
            ["110.9"] = (6,10,10),
            ["114.8"] = (6,11,11),
            ["118.8"] = (6,11,11),
            ["123.0"] = (6,12,12),
            ["127.3"] = (6,12,12),
            ["131.8"] = (6,13,13),
            ["136.5"] = (6,13,13),
            ["141.3"] = (6,13,13),
            ["146.2"] = (6,14,14),
            ["151.4"] = (6,14,14),
            ["156.7"] = (6,14,14),
            ["162.2"] = (6,15,15),
            ["167.9"] = (6,15,15),
            ["173.8"] = (6,15,15),
            ["179.9"] = (7, 0, 0),
            ["186.2"] = (7, 4, 0),
            ["192.8"] = (7, 0, 0),
            ["203.5"] = (7, 4, 1),
            ["210.7"] = (7, 4, 1),
        };

        // ---------- TRANSMIT ----------
        public static string GetTransmitToneLabel(byte ee, byte ef)
        {
            byte code = (byte)(((ee & 0x0F) << 4) | (ef & 0x0F));
            return TxCodeToTone.TryGetValue(code, out var label) ? label : "Err";
        }

        // For validation: provide all 4 TX low-nibbles you read on this channel.
        public static (string Label, byte CanonE8L, byte CanonEDL) InspectTransmitNibbles(byte e8l, byte edl, byte eel, byte efl)
        {
            byte code = (byte)(((eel & 0x0F) << 4) | (efl & 0x0F));
            if (!TxCodeToTone.TryGetValue(code, out var label)) return ("Err", 0, 0);
            var t = ToneToTxNibbles[label];
            return (label, t.E8L, t.EDL);
        }

        // Encode TX for writeback: return all four low-nibbles.
        public static bool TryEncodeTransmitNibbles(string label, out byte e8l, out byte edl, out byte eel, out byte efl)
        {
            e8l = edl = eel = efl = 0;
            if (!ToneToTxNibbles.TryGetValue(label, out var t)) return false;
            e8l = t.E8L; edl = t.EDL; eel = t.EEL; efl = t.EFL;
            return true;
        }

        // ---------- RECEIVE ----------
        public static string GetReceiveToneLabel(byte e0, byte e6, byte e7)
        {
            var key = ((byte)(e0 & 0x0F), (byte)(e6 & 0x0F), (byte)(e7 & 0x0F));
            return RxTripletToTone.TryGetValue(key, out var label) ? label : "Err";
        }

        public static bool TryEncodeReceiveNibbles(string label, out byte e0l, out byte e6l, out byte e7l)
        {
            e0l = e6l = e7l = 0;
            if (!ToneToRxTriplet.TryGetValue(label, out var t)) return false;
            e0l = t.E0L; e6l = t.E6L; e7l = t.E7L;
            return true;
        }
    }
}
