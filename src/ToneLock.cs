// ToneLock.cs
// Minimal, self-contained tone tables + helpers.
// Keep names stable so MainForm.cs keeps compiling.

using System;
using System.Collections.Generic;
using System.Linq;

public static class ToneLock
{
    // === TX: official GE Rangr 33 tones ===
    // Index 0 means "0" (no tone). Indices 1..33 map to the list below.
    public static readonly string[] TxToneMenuAll = new[]
    {
        "0",
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2",
        "110.9","114.8","118.8","123.0","127.3","131.8","136.5",
        "141.3","146.2","151.4","156.7","162.2","167.9","173.8",
        "179.9","186.2","192.8","203.5","210.7"
    };

    // === RX: empirically derived 0..63 indexable list (0 => "0") ===
    // Order must match the 6‑bit values encoded in the RGR image for RX.
    public static readonly string[] RxToneMenuAll = new[]
    {
        // 0..63
        "0",
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2",
        "110.9","114.8","118.8","123.0","127.3","131.8","136.5",
        "141.3","146.2","151.4","156.7","162.2","167.9","173.8",
        "179.9","186.2","192.8","203.5","210.7",
        // pad remaining with duplicates for safety; if out-of-range, UI shows "?"
        "210.7","210.7","210.7","210.7","210.7","210.7","210.7","210.7",
        "210.7","210.7","210.7","210.7","210.7","210.7","210.7","210.7",
        "210.7","210.7","210.7","210.7","210.7","210.7","210.7","210.7",
        "210.7","210.7","210.7","210.7","210.7","210.7","210.7","210.7"
    };

    // --- Public helpers used by UI ---

    // Returns display string for TX given table index (0 == "0")
    public static string TxIndexToDisplay(int idx)
        => (idx >= 0 && idx < TxToneMenuAll.Length) ? TxToneMenuAll[idx] : "?";

    public static int TxDisplayToIndex(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || s == "0") return 0;
        var i = Array.IndexOf(TxToneMenuAll, s);
        return i >= 0 ? i : -1; // -1 means unknown -> UI shows "?"
    }

    // RX
    public static string RxIndexToDisplay(int idx)
        => (idx >= 0 && idx < RxToneMenuAll.Length) ? RxToneMenuAll[idx] : "?";

    public static int RxDisplayToIndex(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || s == "0") return 0;
        var i = Array.IndexOf(RxToneMenuAll, s);
        return i >= 0 ? i : -1;
    }

    // --- Byte decoding stubs (UI already extracts the 6-bit fields; leave here for compatibility) ---
    public static string TxToneFromBytes(byte[] block)
    {
        // If caller hasn't parsed the 6‑bit field yet, just return "?" to avoid bogus mapping.
        return "?";
    }

    public static string RxToneFromBytes(byte[] block)
    {
        return "?";
    }
}
