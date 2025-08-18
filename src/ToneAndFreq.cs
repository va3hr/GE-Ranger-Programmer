using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;

public static class ToneAndFreq
{
    private static int Hi(byte b) => (b >> 4) & 0xF;
    private static int Lo(byte b) => b & 0xF;

    public static double BaseMHz(byte a0, byte a1)
    {
        return ((Hi(a0) - 1) * 10 + Lo(a0)) + (Hi(a1) / 10.0) + (Lo(a1) / 100.0);
    }

    // === Dataset-locked delta maps derived from RANGR6M_cal.csv ===
    private static readonly System.Collections.Generic.Dictionary<int,double> DeltaTx = new()
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

    private static readonly System.Collections.Generic.Dictionary<int,double> DeltaRx = new()
    {
        [0x2520] = 2.275,
        [0x156F] = 2.340,
        [0x152A] = 1.860,
        [0x15EC] = 2.880,
        [0x15A8] = 2.920,
        [0x156C] = 2.960,
        [0x1528] = 2.980,
        [0x15E3] = 1.0,
        [0x15A7] = 1.020,
        [0x1567] = 1.040,
        [0x15E6] = 1.060,
        [0x1566] = 0.080,
        [0x1522] = 1.180,
        [0x2524] = 1.180,
        [0x2594] = 1.240,
        [0x2525] = 3.320,
    };

    private static int Key(byte b1, byte b2) => (b1 << 8) | b2;

    public static double TxMHz(byte A0, byte A1, byte A2)
    {
        double baseMHz = BaseMHz(A0, A1);
        if (DeltaTx.TryGetValue(Key(A1, A2), out double d))
            return Math.Round(baseMHz + d, 3);

        // Fallback (older heuristic) for unknown pairs
        double tx = baseMHz + (Lo(A2) / 100.0) + (Hi(A2) == 0xA ? 0.015 : 0.0);
        return Math.Round(tx, 3);
    }

    public static double RxMHz(byte B0, byte B1, byte B2, double txFallback)
    {
        double baseMHz = ((Hi(B0) - 1) * 10 + Lo(B0)) + (Hi(B1) / 10.0) + (Lo(B1) / 100.0);
        if (DeltaRx.TryGetValue(Key(B1, B2), out double d))
            return Math.Round(baseMHz + d, 3);

        // Fallbacks: simple split flag previously observed; else simplex
        if (Hi(B2) == 0xE) return Math.Round(baseMHz + 1.000, 3);
        return Math.Round(txFallback, 3);
    }

    // Tone menu unchanged here; UI uses a predefined list.
    public static readonly string[] ToneMenu = new string[]
    {
        "0",
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
        "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8",
        "136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","206.5","210.7",
        "?"
    };
}