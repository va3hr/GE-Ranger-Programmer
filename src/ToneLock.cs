// NOTE: Keep this class in the global namespace so calls like ToneLock.X work
// from anywhere without a namespace qualifier.
public static class ToneLock
{
    // UI convenience: some existing code binds to ToneLock.ToneMenuAll.
    // Index 0 is the literal string "0" meaning NO TONE.
    public static readonly string[] ToneMenuAll = new[]
    {
        "0",
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
        "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
        "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
        "203.5","210.7"
    };

    // Keep separate lists for Tx and Rx to preserve independent index spaces.
    public static readonly string[] TxToneMenuAll = ToneMenuAll;
    public static readonly string[] RxToneMenuAll = ToneMenuAll;

    // Some code may still reference this constant; make it explicit.
    public const string ToneMenuNull = "0";

    // ----- Helpers: index -> display string -----
    public static string TxIndexToDisplay(int index)
        => (index >= 0 && index < TxToneMenuAll.Length) ? TxToneMenuAll[index] : "?";

    public static string RxIndexToDisplay(int index)
        => (index >= 0 && index < RxToneMenuAll.Length) ? RxToneMenuAll[index] : "?";

    // ----- Helpers: display string -> index -----
    // Returns 0 for "0" (no tone), -1 for unknown (caller should display "?").
    public static int DisplayToTxIndex(string? display)
    {
        var s = NormalizeDisplay(display);
        if (s == ToneMenuNull) return 0;
        int idx = System.Array.IndexOf(TxToneMenuAll, s);
        return idx >= 0 ? idx : -1;
    }

    public static int DisplayToRxIndex(string? display)
    {
        var s = NormalizeDisplay(display);
        if (s == ToneMenuNull) return 0;
        int idx = System.Array.IndexOf(RxToneMenuAll, s);
        return idx >= 0 ? idx : -1;
    }

    // Small utility: trim and treat a solitary "." as "0" (historical DOS display artifact).
    private static string NormalizeDisplay(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return ToneMenuNull;
        s = s.Trim();
        if (s == ".") return ToneMenuNull;
        return s;
    }
}
