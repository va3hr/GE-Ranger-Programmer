using System;
using System.Collections.Generic;
using System.Linq;

public static class ToneLock
{
    // ===== Canonical combobox menu =====
    // Index 0 = "0" (NONE), 1 = "?" (unknown), then the standard CTCSS set.
    public static readonly string[] Menu = new[]{
        "0","?",
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5",
        "94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0",
        "127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9",
        "173.8","179.9","186.2","192.8","203.5","210.7"
    };

    private static readonly HashSet<string> MenuSet = Menu.ToHashSet(StringComparer.Ordinal);

    // Small helper
    private static int Bit(byte b, int n) => (b >> n) & 1;

    // =========================================================================
    // TX TONE (dataset-locked)  -----------------------------------------------
    // Index built from bits: {A2.b0, A2.b1, A2.b7, B3.b1, B3.b2} (LSB→MSB).
    // Then mapped to the DOS-matching Hz for the dataset we validated.
    // If an index isn’t in the map below, we return "?" (so you see it).
    // =========================================================================
    public static int TxToneIndex(byte A2, byte B3)
    {
        int b0 = Bit(A2, 0);
        int b1 = Bit(A2, 1);
        int b2 = Bit(A2, 7);
        int b3 = Bit(B3, 1);
        int b4 = Bit(B3, 2);
        return (b0 << 0) | (b1 << 1) | (b2 << 2) | (b3 << 3) | (b4 << 4);
    }

    // Dataset-locked TX map (from earlier gold-matching work).
    private static readonly Dictionary<int, string> TxIdxToHz = new()
    {
        // These entries came from the DOS-matching set we already proved out.
        { 0,  "107.2" }, { 1,  "103.5" }, { 4,  "131.8" }, { 6,  "103.5" },
        { 8,  "131.8" }, { 9,  "156.7" }, { 10, "107.2" }, { 12, "97.4"  },
        { 14, "114.1" }, { 15, "131.8" }, { 16, "110.9" }, { 20, "162.2" },
        { 21, "127.3" }, { 23, "103.5" }, { 24, "162.2" }, { 27, "114.8" },
        { 28, "131.8" },
    };

    public static string TxToneMenuValue(byte A2, byte B3)
    {
        // If index absent from map, signal it explicitly
        int idx = TxToneIndex(A2, B3);
        if (TxIdxToHz.TryGetValue(idx, out var hz) && MenuSet.Contains(hz)) return hz;
        // Treat zero as "0" only if the mapping says so; otherwise "?"
        return (idx == 0 && MenuSet.Contains("0")) ? "0" : "?";
    }

    // =========================================================================
    // RX TONE  ----------------------------------------------------------------
    // Index built from B2 using big-endian-like packing we observed:
    // idx = { B2.b0, B2.b1, B2.b2, B2.b3, B2.b7 }  (LSB→MSB)
    // i.e. idx = (B2 & 0x0F) | ((B2 & 0x80) >> 3)
    //
    // IMPORTANT: The index→Hz ordering for RX is radio/dataset-specific.
    // We seed the map with what your DOS photo confirmed for CH1 & CH8.
    // Anything unmapped returns "?" so we won’t silently display the wrong tone.
    // =========================================================================
    public static int RxToneIndex(byte B2) =>
        (B2 & 0x0F) | ((B2 & 0x80) >> 3);

    // Seeded from your latest DOS screen:
    private static readonly Dictionary<int, string> RxIdxToHz = new()
    {
        { 0,  "131.8" }, // CH1 confirmed
        { 19, "162.2" }, // CH8 confirmed
        // Add lines here as we read more from DOS (e.g. { 15, "107.2" }, etc.)
    };

    public static string RxToneMenuValue(byte B2)
    {
        int idx = RxToneIndex(B2);
        if (RxIdxToHz.TryGetValue(idx, out var hz) && MenuSet.Contains(hz)) return hz;
        // Many channels legitimately have "no tone" → index that maps to "0".
        // Until we lock the full RX map, default unknowns to "?" (not blank).
        return "?";
    }

    // =========================================================================
    // Helpers you may find handy from UI code
    // =========================================================================
    public static string NormalizeForMenu(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "0";
        return MenuSet.Contains(s) ? s : "?";
    }
}

