using System;
using System.Globalization;
using System.Collections.Generic;

public static class ToneAndFreq
{
    internal static readonly System.Collections.Generic.Dictionary<int,double> DeltaTxByA1A2 = new()
    {
        [0x47A4] = 0.055,
        [0x37EF] = 0.120,
        [0x37AE] = -0.360,
        [0x376D] = -0.340,
        [0x372D] = -0.300,
        [0x37EC] = -0.260,
        [0x37AC] = -0.240,
        [0x3768] = -0.220,
        [0x3728] = -0.200,
        [0x37E3] = -0.180,
        [0x3763] = -0.160,
        [0x37E2] = -0.140,
        [0x172A] = 0.160,
        [0x2728] = 0.160,
        [0x2798] = 0.220,
        [0x47A5] = 0.100,
    };

    internal static readonly System.Collections.Generic.Dictionary<int,double> DeltaRxByB1B2 = new()
    {
        [0x2520] = 2.275,
        [0x156F] = 2.340,
        [0x152A] = 1.860,
        [0x15EC] = 2.880,
        [0x15A8] = 2.920,
        [0x156C] = 2.960,
        [0x1528] = 2.980,
        [0x15E3] = 1.000,
        [0x15A7] = 1.020,
        [0x1567] = 1.040,
        [0x15E6] = 1.060,
        [0x1566] = 0.080,
        [0x1522] = 1.180,
        [0x2524] = 1.180,
        [0x2594] = 1.240,
        [0x2525] = 3.320,
    };

    internal static readonly System.Collections.Generic.Dictionary<int,double> TxToneHzByIndex = new()
    {
        [0] = 107.2,
        [1] = 103.5,
        [4] = 131.8,
        [6] = 103.5,
        [8] = 131.8,
        [9] = 156.7,
        [10] = 107.2,
        [12] = 97.4,
        [14] = 114.1,
        [15] = 131.8,
        [16] = 110.9,
        [20] = 162.2,
        [21] = 127.3,
        [23] = 103.5,
        [24] = 162.2,
        [27] = 114.8,
        [28] = 131.8,
    };

    private static int Hi(byte b) => (b >> 4) & 0xF;
    private static int Lo(byte b) => b & 0xF;

    public static double BaseMHz(byte a0, byte a1)
    {
        double base10 = ((Hi(a0) - 1) * 10) + Lo(a0);
        double frac   = (Hi(a1) / 10.0) + (Lo(a1) / 100.0);
        return Math.Round(base10 + frac, 3);
    }

    public static double TxMHz(byte a0, byte a1, byte a2)
    {
        double baseMHz = BaseMHz(a0, a1);
        int key = (a1 << 8) | a2;
        if (DeltaTxByA1A2.TryGetValue(key, out double d))
            return Math.Round(baseMHz + d, 3);

        // Fallback heuristic only if necessary (shouldn't be for gold file)
        double add = (Lo(a2) / 100.0) + (Hi(a2) == 0xA ? 0.015 : 0.0);
        return Math.Round(baseMHz + add, 3);
    }

    public static double RxMHz(byte b0, byte b1, byte b2, double txMHz)
    {
        int key = (b1 << 8) | b2;
        if (DeltaRxByB1B2.TryGetValue(key, out double d))
            return Math.Round(BaseMHz(b0, b1) + d, 3);

        if (Hi(b2) == 0xE) return Math.Round(BaseMHz(b0,b1) + 1.000, 3);
        return Math.Round(txMHz, 3);
    }

    // ---- TX tone (dataset-locked) ----
    // Bits used (LSB=bit0): A2.b0, A2.b1, A2.b7, B3.b1, B3.b2
    public static int ComputeTxToneIndex(byte a2, byte b3)
    {
        int bit0 = (a2 & 0x01) >> 0;
        int bit1 = (a2 & 0x02) >> 1;
        int bit2 = (a2 & 0x80) >> 7;
        int bit3 = (b3 & 0x02) >> 1;
        int bit4 = (b3 & 0x04) >> 2;
        return (bit0 << 0) | (bit1 << 1) | (bit2 << 2) | (bit3 << 3) | (bit4 << 4);
    }

    public static string TxToneDisplay(byte a2, byte b3)
    {
        int idx = ComputeTxToneIndex(a2, b3);
        if (idx == 0) return string.Empty; // NONE shown as blank per spec
        if (TxToneHzByIndex.TryGetValue(idx, out double hz))
            return hz.ToString("0.0", CultureInfo.InvariantCulture);
        return "?";
    }
}
