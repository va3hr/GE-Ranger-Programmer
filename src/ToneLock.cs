using System;
using System.Linq;

public static class ToneLock
{
    // Display menu used by both combo boxes. "?" is included for unknowns,
    // but it is NOT part of the raw index mapping.
    public static readonly string[] ToneMenuAll = new[]
    {
        "0", "?", // 0 = NONE, "?" = unknown / out-of-range
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5",
        "94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0",
        "127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9",
        "173.8","179.9","186.2","192.8","203.5","210.7"
    };

    // Raw-index mapping (0..N). NOTE: No "?" here so indices line up.
    private static readonly string[] IndexToTone = new[]
    {
        "0",       // 0 = none
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5",
        "94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0",
        "127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9",
        "173.8","179.9","186.2","192.8","203.5","210.7"
    };

    private static int B(byte b, int bit) => ((b >> bit) & 1);

    /// <summary>
    /// TX tone index (locked pattern we used before):
    /// index = { A2.b0, A2.b1, A2.b7, B3.b1, B3.b2 } with A2.b0 as LSB.
    /// </summary>
    public static string TxToneFromBytes(byte A2, byte B3)
    {
        int idx =
            (B(A2, 0) << 0) |
            (B(A2, 1) << 1) |
            (B(A2, 7) << 2) |
            (B(B3, 1) << 3) |
            (B(B3, 2) << 4);

        return IndexToLabel(idx);
    }

    /// <summary>
    /// RX tone index (provisional locked big-endian window).
    /// We select 5 non-contiguous bits across B1/B2/B3:
    /// index = { B1.b0, B1.b1, B2.b5, B3.b4, B3.b2 } with B1.b0 as LSB.
    /// On unknown datasets this still fails safe to "?".
    /// </summary>
    public static string RxToneFromBytes(byte B1, byte B2, byte B3)
    {
        int idx =
            (B(B1, 0) << 0) |
            (B(B1, 1) << 1) |
            (B(B2, 5) << 2) |
            (B(B3, 4) << 3) |
            (B(B3, 2) << 4);

        return IndexToLabel(idx);
    }

    private static string IndexToLabel(int idx)
    {
        if (idx < 0 || idx >= IndexToTone.Length) return "?";
        return IndexToTone[idx];
    }
}
