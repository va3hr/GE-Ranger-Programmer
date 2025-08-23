// SPDX-License-Identifier: MIT
// GE Rangr Programmer — RX tone menu + parsing helpers (kept separate from TX).
// Index space is 6-bit (0..63). Index 0 means "no tone" and renders as "0".
// This file does NOT touch any EEPROM bit packing; it’s just display<->index helpers.

#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RangrApp.Locked
{
    /// <summary>
    /// RX tone lookup & parsing. Keep separate from TxToneLock.
    /// </summary>
    public static class RxToneLock
    {
        /// <summary>
        /// Canonical display strings for the supported tones.
        /// Position is the logical RX menu index; 0 is "0" (no tone).
        /// NOTE: Menu mirrors TX for UI consistency; EEPROM encoding index may differ.
        /// </summary>
        public static readonly string[] MenuAll = new[]
        {
            "0",
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
            "203.5","210.7"
        };

        private static readonly Dictionary<string, byte> s_displayToIndex =
            BuildDisplayToIndex(MenuAll);

        /// <summary>
        /// Convert a free-form display string to an RX index (0..63).
        /// Accepts "0", ".", "", null as no-tone; returns false only for a non-empty unknown.
        /// </summary>
        public static bool TryDisplayToIndex(string? display, out byte index)
        {
            if (string.IsNullOrWhiteSpace(display) || display == "0" || display == ".")
            {
                index = 0;
                return true;
            }

            var key = Normalize(display);
            if (s_displayToIndex.TryGetValue(key, out index))
                return true;

            index = 0;
            return false;
        }

        /// <summary>
        /// Get the canonical display string for an RX index.
        /// Unknown indexes return "?" (never add "?" to menus).
        /// </summary>
        public static string IndexToDisplay(byte index)
        {
            if (index < MenuAll.Length) return MenuAll[index];
            return "?";
        }

        // -------- internals --------

        private static string Normalize(string s)
        {
            s = s.Trim().Replace(',', '.');
            if (s == "0" || s == ".") return "0";
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                var rounded = Math.Round(v, 1, MidpointRounding.AwayFromZero);
                return rounded.ToString("0.0", CultureInfo.InvariantCulture);
            }
            return s;
        }

        private static Dictionary<string, byte> BuildDisplayToIndex(string[] menu)
        {
            var dict = new Dictionary<string, byte>(StringComparer.Ordinal);
            for (byte i = 0; i < menu.Length; i++)
            {
                var d = menu[i];
                dict[Normalize(d)] = i;

                if (d.EndsWith(".0", StringComparison.Ordinal))
                {
                    dict[d.AsSpan(0, d.Length - 2).ToString()] = i;
                }
            }
            return dict;
        }
    }
}
