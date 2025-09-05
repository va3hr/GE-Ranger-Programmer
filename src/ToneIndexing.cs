using System;
namespace RangrApp.Locked
{
public static class ToneIndexing
{
    // Public: some calling code expects this symbol to exist.
    public static readonly string[] CanonicalLabels = new[] {
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
        "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
        "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
    };

    /// <summary>Map 0..63 index to label. 0 → "0"; 1..33 → canonical; others → null (UI should show "Err").</summary>
    public static string? LabelFromIndex(int idx)
    {
        if (idx == 0) return "0";
        if (idx >= 1 && idx <= 33) return CanonicalLabels[idx - 1];
        return null;
    }

    /// <summary>Map label to index. "0" → 0; exact canonical → 1..33; otherwise null (→ "Err").</summary>
    public static int? IndexFromLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var s = label.Trim();
        if (s == "0") return 0;

        for (int i = 0; i < CanonicalLabels.Length; i++)
            if (s == CanonicalLabels[i]) return i + 1;

        return null;
    }
}
}

