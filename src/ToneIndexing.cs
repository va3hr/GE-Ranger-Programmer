namespace RangrApp;

public static class ToneIndexing
{
    // Canonical GE labels for the drop-downs (operator input). No "?" here.
    public static readonly string[] CanonicalLabels = new[]
    {
        "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
        "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
        "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
        "203.5","210.7"
    };

    // ---- TX decode: 6-bit code + 3-bit bank (L = B2.1, M = B1.5, H = B2.7)
    // Key = (bank<<6) | code. Below are the keys observed in your RANGR6M2 file
    // and their GE tone labels. Unknown keys simply won't be in this dictionary.
    private static readonly Dictionary<int,string> TxMap = new()
    {
        // code=10  bank L,M,H = 0,0,0
        [10]  = "97.4",
        // code=25  bank L,M,H = 0,0,0
        [25]  = "131.8",
        // code=10  bank 1,0,0
        [74]  = "114.1",
        // code=11  bank 1,0,0
        [75]  = "107.2",
        // code=25  bank 1,0,0
        [89]  = "103.5",
        // code=26  bank 1,0,0
        [90]  = "131.8",
        // code=29  bank 1,0,0
        [93]  = "103.5",
        // code=15  bank 0,1,0
        [143] = "131.8",
        // code=28  bank 0,1,0
        [156] = "127.3",
        // code=29  bank 0,1,0
        [157] = "110.9",
        // code=11  bank 0,0,1
        [267] = "156.7",
        // code=25  bank 0,0,1
        [281] = "103.5",
        // code=15  bank 1,0,1
        [335] = "162.2",
        // code=24  bank 1,0,1
        [344] = "107.2",
        // code=30  bank 1,0,1
        [350] = "114.8",
        // code=29  bank 0,1,1
        [413] = "162.2",
    };

    public static string? TryDecodeTx(byte A0, byte A1, byte A2, byte A3, byte B0, byte B1, byte B2, byte B3)
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

    // ---- RX decode: the 6 bits we’ve been using (no big-endian)
    public static string? TryDecodeRx(byte A0, byte A1, byte A2, byte A3, byte B0, byte B1, byte B2, byte B3)
    {
        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        // If you want “0” for unknowns, return "0"; otherwise null to leave blank.
        // For now we’ll only map the RX index 0 → "0" (no tone) and leave others
        // blank until we finish the RX table.
        return rxIdx == 0 ? "0" : null;
    }
}
