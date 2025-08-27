
using System;
using System.Collections.Generic;

public static class ToneIndexing
{
    // Canonical GE labels for operator input (dropdowns only — no "?")
    public static readonly string[] CanonicalLabels = new[]
    {
        "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
        "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
        "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
        "203.5","210.7"
    };

    // TX decode = 6-bit code + 3-bit bank (H=B2.7, M=B1.5, L=B2.1)
    // key = (bank<<6) | code. Only proven keys included.
    private static readonly Dictionary<int, string> TxMap = new()
    {
        // Bank 000
        [10]  = "97.4",
        [25]  = "131.8",

        // Bank 100 (H=1, M=0, L=0)
        [74]  = "114.1",
        [75]  = "107.2",
        [89]  = "103.5",
        [90]  = "131.8",
        [93]  = "103.5",

        // Bank 010 (H=0, M=1, L=0)
        [143] = "131.8",
        [156] = "127.3",
        [157] = "110.9",

        // Bank 001 (H=0, M=0, L=1)
        [267] = "156.7",
        [281] = "103.5",

        // Bank 101 (H=1, M=0, L=1)
        [335] = "162.2",
        [344] = "107.2",
        [350] = "114.8",

        // Bank 011 (H=0, M=1, L=1)
        [413] = "162.2",
    };

    public static string? TryDecodeTx(byte A0, byte A1, byte A2, byte A3,
                                      byte B0, byte B1, byte B2, byte B3)
    {
        int code = (((B0 >> 4) & 1) << 5)
                 | (((B2 >> 2) & 1) << 4)
                 | (((B3 >> 3) & 1) << 3)
                 | (((B3 >> 2) & 1) << 2)
                 | (((B3 >> 1) & 1) << 1)
                 |  ((B3      ) & 1);

        int bank = (((B2 >> 7) & 1) << 2)  // H
                 | (((B1 >> 5) & 1) << 1)  // M
                 |  ((B2 >> 1) & 1);       // L

        int key = (bank << 6) | code;
        return TxMap.TryGetValue(key, out var label) ? label : null;
    }

    // RX = six bits from A3 only (no big-endian for tones)
    public static string? TryDecodeRx(byte A0, byte A1, byte A2, byte A3,
                                      byte B0, byte B1, byte B2, byte B3)
    {
        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        // For now: explicit 0 -> "0", others left null (so UI shows "0" via ToneLock).
        return rxIdx == 0 ? "0" : null;
    }
}
