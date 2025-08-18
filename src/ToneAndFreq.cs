using System;

public static class ToneAndFreq
{
    // Dataset-locked Δ maps (derived from RANGR6M_cal.csv)
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

        // Fallback (Model B) if key not found
        double add = (Lo(a2) / 100.0) + (Hi(a2) == 0xA ? 0.015 : 0.0);
        return Math.Round(baseMHz + add, 3);
    }

    public static double RxBase(byte b0, byte b1) => BaseMHz(b0, b1);

    public static double RxMHz(byte b0, byte b1, byte b2, double txMHz)
    {
        // Dataset-locked Δ keyed by (B1,B2); if missing, rule-based fallback
        int key = (b1 << 8) | b2;
        if (DeltaRxByB1B2.TryGetValue(key, out double d))
            return Math.Round(RxBase(b0, b1) + d, 3);

        // Rule-based fallback: simplex unless split flag (B2.hi == 0xE)
        if (Hi(b2) == 0xE) return Math.Round(RxBase(b0,b1) + 1.000, 3);
        return Math.Round(txMHz, 3);
    }
}
