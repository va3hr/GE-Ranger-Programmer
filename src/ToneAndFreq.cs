using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

public static class ToneAndFreq
{
    internal static readonly System.Collections.Generic.Dictionary<int,double> DeltaTxByA1A2 = new()
    {
    };

    internal static readonly System.Collections.Generic.Dictionary<int,double> DeltaRxByB1B2 = new()
    {
    };

    internal static readonly System.Collections.Generic.Dictionary<int,double> TxToneHzByIndex = new()
    {
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

    public static readonly string[] ToneMenu = new string[] {
        "0 (NONE)",
        "67.0",
        "69.3",
        "71.9",
        "74.4",
        "77.0",
        "79.7",
        "82.5",
        "85.4",
        "88.5",
        "91.5",
        "94.8",
        "97.4",
        "100.0",
        "103.5",
        "107.2",
        "110.9",
        "114.8",
        "118.8",
        "123.0",
        "127.3",
        "131.8",
        "136.5",
        "141.3",
        "146.2",
        "151.4",
        "156.7",
        "162.2",
        "167.9",
        "173.8",
        "179.9",
        "186.2",
        "192.8",
        "203.5",
        "210.7",
        "?"
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

    public static string TxToneMenuValue(byte a2, byte b3)
    {
        int idx = ComputeTxToneIndex(a2, b3);
        if (idx == 0) return "0 (NONE)";
        if (TxToneHzByIndex.TryGetValue(idx, out double hz))
            return hz.ToString("0.0", CultureInfo.InvariantCulture);
        return "?";
    }
}
