using System;

public static class ToneIndexing
{
    // 0 = no tone. Valid tone indices are 1..33 (inclusive). 114.1 is intentionally excluded.
    public static readonly string[] CanonicalLabels = new[]
    {
        "0",
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2",
        "107.7","110.0","110.9","114.8","118.8","123.0",
        "127.3","131.8","136.5","141.3","146.2","151.4",
        "156.7","162.2","167.9","173.8","179.9","186.2",
        "192.8","203.5","210.7"
    };

    // ===== Compatibility helpers expected by ToneDiag.cs =====
    // 1) Index only
    public static bool TryDecodeTx(byte A3, byte A2, byte A1, byte A0,
                                   byte B3, byte B2, byte B1, byte B0,
                                   out int index)
    {
        index = TxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3);
        return true;
    }

    public static bool TryDecodeRx(byte A3, byte A2, byte A1, byte A0,
                                   byte B3, byte B2, byte B1, byte B0,
                                   out int index)
    {
        index = RxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3);
        return true;
    }

    // 2) Index + bank/code (for detailed diagnostics)
    public static bool TryDecodeTx(byte A3, byte A2, byte A1, byte A0,
                                   byte B3, byte B2, byte B1, byte B0,
                                   out int index, out int bank, out int code)
    {
        // code: 6-bit value used for tone selection
        code = (((B0 >> 4) & 1) << 5)
             | (((B2 >> 2) & 1) << 4)
             | (((B3 >> 3) & 1) << 3)
             | (((B3 >> 2) & 1) << 2)
             | (((B3 >> 1) & 1) << 1)
             |  ((B3      ) & 1);

        // bank: 3-bit group indicator used historically
        bank = (((B2 >> 7) & 1) << 2)  // H
             | (((B1 >> 5) & 1) << 1)  // M
             |  ((B2 >> 1) & 1);       // L

        index = code; // our current model uses the 6-bit code directly as the index
        return true;
    }

    // 3) Label convenience (maps out-of-range to "0")
    public static bool TryDecodeTx(byte A3, byte A2, byte A1, byte A0,
                                   byte B3, byte B2, byte B1, byte B0,
                                   out string label)
    {
        int idx = TxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3);
        label = LabelFromIdx(idx);
        return true;
    }

    public static bool TryDecodeRx(byte A3, byte A2, byte A1, byte A0,
                                   byte B3, byte B2, byte B1, byte B0,
                                   out string label)
    {
        int idx = RxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3);
        label = LabelFromIdx(idx);
        return true;
    }

    // ===== Internal helpers used above =====
    private static string LabelFromIdx(int idx)
    {
        if (idx <= 0 || idx > 33) return "0";         // your rule
        return (idx < CanonicalLabels.Length) ? CanonicalLabels[idx] : "0";
    }

    private static int TxIndexFromBytes(byte A0, byte A1, byte A2, byte A3,
                                        byte B0, byte B1, byte B2, byte B3)
    {
        // Bits: B0[4] MSB, B2[2], B3[3], B3[2], B3[1], B3[0] LSB
        int idx = (((B0 >> 4) & 1) << 5)
                | (((B2 >> 2) & 1) << 4)
                | (((B3 >> 3) & 1) << 3)
                | (((B3 >> 2) & 1) << 2)
                | (((B3 >> 1) & 1) << 1)
                |  ((B3      ) & 1);
        return idx;
    }

    private static int RxIndexFromBytes(byte A0, byte A1, byte A2, byte A3,
                                        byte B0, byte B1, byte B2, byte B3)
    {
        // Bits: A3[6] MSB, A3[7], A3[0], A3[1], A3[2], A3[3] LSB
        int idx = (((A3 >> 6) & 1) << 5)
                | (((A3 >> 7) & 1) << 4)
                | (((A3 >> 0) & 1) << 3)
                | (((A3 >> 1) & 1) << 2)
                | (((A3 >> 2) & 1) << 1)
                |  ((A3 >> 3) & 1);
        return idx;
    }
}
