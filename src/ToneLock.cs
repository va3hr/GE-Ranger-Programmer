namespace GE_Ranger_Programmer;

/// <summary>
/// Static helpers for tone menus and conversions between display text and index values.
/// Rx and Tx use different index spaces in the binary, but the UI shares the same display list.
/// The mapping from binary->index should be handled inside RgrCodec during decode/encode.
/// </summary>
public static class ToneLock
{
    // UI list for both Tx and Rx selectors
    public static readonly string[] ToneMenuAll = new string[] {
        "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}", "{x}"
    };

    // Utility: convert UI display string to index (0..N-1) or -1 if not found.
    public static int DisplayToIndex(string? display)
    {
        if (string.IsNullOrWhiteSpace(display)) return 0; // treat blank as "0"
        var idx = Array.IndexOf(ToneMenuAll, display.Trim());
        return idx < 0 ? -1 : idx;
    }

    // Utility: convert index to display, returns "?" when out of range.
    public static string IndexToDisplay(int index)
    {
        if (index < 0 || index >= ToneMenuAll.Length) return "?";
        return ToneMenuAll[index];
    }

    // Shims kept for older call-sites that were looking for specific names:
    public static string RxIndexToDisplay(int index) => IndexToDisplay(index);
    public static string TxIndexToDisplay(int index) => IndexToDisplay(index);
    public static int   RxDisplayToIndex(string? display) => DisplayToIndex(display);
    public static int   TxDisplayToIndex(string? display) => DisplayToIndex(display);
}
