// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;

namespace GE_Ranger_Programmer
{
    /// <summary>
    /// Tone lookup and formatting.
    /// Rules (per project memory):
    /// - Tx uses the standard GE "gold" list (with "0" meaning no tone) and indexes are 6-bit.
    /// - Rx uses the derived 6-bit map we learned from RXMAP_A/B/C; unknowns display "?".
    /// - Never include "?" in menus. If an unknown comes in, UI shows "?" via NullValue.
    /// </summary>
    public static class ToneLock
    {
        // ---------- Standard TX "gold" table (string form), index 0 = "0" (no tone) ----------
        // NOTE: keep the exact string formatting used by the DOS screen.
        public static readonly string[] TxMenu =
        {
            "0",
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // For UI combo boxes (single list used for both columns).
        public static IReadOnlyList<string> ToneMenuAll => TxMenu;

        // ---------- RX: Derived map (6-bit index -> display string) ----------
        // This table comes from RXMAP_A/B/C sessions. Index 0 = "0" (no tone).
        // Any unused/unknown slot is null (UI shows "?").
        // If you have a fresher dump, we can regenerate; for now this matches the tested map.
        // Length must be 64.
        private static readonly string?[] RxIndexToDisplay =
        {
            // 0 .. 15
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            // 16 .. 31
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
            // 32 .. 47
            "203.5","210.7", null,   null,   null,   null,   null,   null,
            null,   null,   null,   null,   null,   null,   null,   null,
            // 48 .. 63
            null,   null,   null,   null,   null,   null,   null,   null,
            null,   null,   null,   null,   null,   null,   null,   null
        };

        /// <summary>Return a display string for a TX index (0..63). Unknowns -> "?".</summary>
        public static string TxIndexToDisplay(int idx)
        {
            if (idx < 0 || idx >= 64) return "?";
            // TX uses the first 34 entries; others are not valid -> "?"
            return idx < TxMenu.Length ? TxMenu[idx] : "?";
        }

        /// <summary>Return a display string for an RX index (0..63). Unknowns -> "?".</summary>
        public static string RxIndexToDisplay(int idx, out bool isKnown)
        {
            isKnown = false;
            if (idx < 0 || idx >= 64) return "?";
            var s = RxIndexToDisplay[idx];
            if (string.IsNullOrEmpty(s)) return "?";
            isKnown = true;
            return s!;
        }

        /// <summary>Lookup a TX index by display string; returns -1 if not found.</summary>
        public static int TxDisplayToIndex(string? display)
        {
            if (string.IsNullOrWhiteSpace(display)) return -1;
            for (int i = 0; i < TxMenu.Length; i++)
                if (TxMenu[i] == display) return i;
            return -1;
        }
    }
}
