// Auto-generated ToneLock.cs
using System;
using System.Collections.Generic;

namespace RangrApp.Locked
{
    /// <summary>
    /// Central place for tone menus and conversions.
    /// Tx and Rx both use 6-bit indices (0..63). Index 0 means "no tone" and must display "0".
    /// Unknown / out-of-range indices display "?".
    /// IMPORTANT: The "?" value is never included in any drop-down; it's display-only.
    /// </summary>
    public static class ToneLock
    {
        public const string None = "0";
        public const string Unknown = "?";

        // Standard GE Channel Guard CTCSS set (manual table) — 33 tones.
        // We add index 0 => "0" (no tone). Indices 1..33 map to the tones below.
        private static readonly string[] _toneValues = new[]
        {
            // 1..33 (index 0 is reserved for None)
            "67.0", "71.9", "74.4", "77.0", "79.7", "82.5",
            "85.4", "88.5", "91.5", "94.8", "97.4", "100.0",
            "103.5","107.2","110.9","114.8","118.8","123.0",
            "127.3","131.8","136.5","141.3","146.2","151.4",
            "156.7","162.2","167.9","173.8","179.9","186.2",
            "192.8","203.5","210.7"
        };

        /// <summary>Menu for UI drop-downs. Does not include the unknown marker.</summary>
        public static IReadOnlyList<string> TxToneMenuAll => _menu;
        public static IReadOnlyList<string> RxToneMenuAll => _menu;

        private static readonly List<string> _menu = BuildMenu();

        private static List<string> BuildMenu()
        {
            var list = new List<string>(1 + _toneValues.Length);
            list.Add(None);
            list.AddRange(_toneValues);
            return list;
        }

        // -------- Display helpers (Tx) --------
        public static string TxIndexToDisplay(int index) => IndexToDisplay(index);

        public static int DisplayToTxIndex(string display) => DisplayToIndex(display);

        // -------- Display helpers (Rx) --------
        public static string RxIndexToDisplay(int index) => IndexToDisplay(index);

        // Backward-compatible overload used by older MainForm code; bank is ignored by design.
        public static string RxIndexToDisplay(int index, int /*bank*/ _)
            => RxIndexToDisplay(index);

        public static int DisplayToRxIndex(string display) => DisplayToIndex(display);

        // -------- Internals --------
        private static string IndexToDisplay(int index)
        {
            if (index <= 0) return None;
            if (index <= _toneValues.Length) return _toneValues[index - 1];
            // For out-of-range 6-bit values (34..63) show unknown.
            return Unknown;
        }

        private static int DisplayToIndex(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return 0;
            var d = display.Trim();
            if (d == None) return 0;
            // We never put "?" in the menu, but if it slips in, treat as 0 to avoid writing garbage.
            if (d == Unknown) return 0;
            for (int i = 0; i < _toneValues.Length; i++)
            {
                if (string.Equals(_toneValues[i], d, StringComparison.Ordinal))
                    return i + 1; // map back to 1..33
            }
            return 0;
        }

        public static bool IsValidMenuValue(string? display)
        {
            if (string.IsNullOrWhiteSpace(display)) return true; // treat blank as none
            var d = display.Trim();
            if (d == None) return true;
            // Unknown is display-only; not valid for user selection
            if (d == Unknown) return false;
            for (int i = 0; i < _toneValues.Length; i++)
                if (_toneValues[i] == d) return true;
            return false;
        }
    }
}
