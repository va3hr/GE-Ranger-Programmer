// SPDX-License-Identifier: MIT
// GE Rangr Programmer — RX tone menu + parsing helpers (namespace version).

#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RangrApp.Locked;

public static class RxToneLock
{
    /// Canonical display strings for RX tones. Index 0 == "0" (no tone).
    public static readonly string[] MenuAll = new[]
    {
        "0",
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
        "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
        "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
        "203.5","210.7"
    };

    // Legacy-friendly alias to reduce code churn
    public static IReadOnlyList<string> ToneMenu => MenuAll;

    private static readonly Dictionary<string, byte> s_displayToIndex =
        BuildDisplayToIndex(MenuAll);

    /// Convert a display string to an RX index (0..63). "0" or "." => 0.
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

    /// Get canonical display text for an RX index. Unknown => "?".
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
