using System;

public static class ToneIndexing
{
    // Canonical 33 GE/Channel-Guard tones for operator menus (no "0" here).
    public static readonly string[] CanonicalLabels = new[]
    {
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
        "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
        "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
    };

    // Map 6-bit index to label: 0->"0"; 1..33 -> Canonical; else null (invalid).
    public static string? LabelFromIndex(int idx)
    {
        if (idx == 0) return "0";
        if (idx < 1 || idx > 33) return null;
        return CanonicalLabels[idx - 1];
    }

    // Map label back to index: "0"->0; canonical->1..33; else null.
    public static int? IndexFromLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        if (label == "0") return 0;
        for (int i = 0; i < CanonicalLabels.Length; i++)
            if (CanonicalLabels[i] == label) return i + 1;
        return null;
    }
}
