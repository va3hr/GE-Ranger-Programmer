using System;
using System.Globalization;

public static class ToneIndexing
{
    // Canonical 33 tones (indices 1..33). Index 0 is handled specially as "0".
    private static readonly string[] Canonical = new[] {
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
        "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
        "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
    };

    /// <summary>
    /// Map an index to a label. Returns "0" for 0, one of the canonical labels for 1..33, else null (-> Err in UI).
    /// </summary>
    public static string? LabelFromIndex(int idx)
    {
        if (idx == 0) return "0";
        if (idx >= 1 && idx <= 33) return Canonical[idx - 1];
        return null; // caller must render "Err"
    }

    /// <summary>
    /// Map a label to an index. Returns 0 for "0".
    /// Returns 1..33 for an exact canonical tone label.
    /// Returns null for anything else (-> Err).
    /// </summary>
    public static int? IndexFromLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var s = label.Trim();

        // Enforce exact canonical strings. Accept a single leading/trailing space only after Trim.
        if (s == "0") return 0;

        // No rounding. No "114.1". Only the fixed set is valid.
        for (int i = 0; i < Canonical.Length; i++)
        {
            if (s == Canonical[i]) return i + 1;
        }
        return null; // caller must render "Err"
    }
}
