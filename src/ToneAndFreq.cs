
using System;
using System.Collections.Generic;
using System.Globalization;

public static class ToneAndFreq
{
    // Dropdown list: "0" first, then standard CTCSS, then "?"
    public static readonly string[] ToneMenu = new string[]
    {
        "0",
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
        "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8",
        "136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","206.5","210.7",
        "?"
    };

    // ---------- Dataset-LOCKED frequency deltas (gold: RANGR6M_cal.csv) ----------
    // Keys are (second byte, third byte) i.e., (A1,A2) for Tx and (B1,B2) for Rx
    internal static readonly Dictionary<(byte b1, byte b2), double> DeltaTx = new()
    {
        { (0x47, 0xA4) , 0.055 },
        { (0x37, 0xEF) , 0.120 },
        { (0x37, 0xAE) , -0.360 },
        { (0x37, 0x6D) , -0.340 },
        { (0x37, 0x2D) , -0.300 },
        { (0x37, 0xEC) , -0.260 },
        { (0x37, 0xAC) , -0.240 },
        { (0x37, 0x68) , -0.220 },
        { (0x37, 0x28) , -0.200 },
        { (0x37, 0xE3) , -0.180 },
        { (0x37, 0x63) , -0.160 },
        { (0x37, 0xE2) , -0.140 },
        { (0x17, 0x2A) , 0.160 },
        { (0x27, 0x28) , 0.160 },
        { (0x27, 0x98) , 0.220 },
        { (0x47, 0xA5) , 0.100 },
    };

    internal static readonly Dictionary<(byte b1, byte b2), double> DeltaRx = new()
    {
        { (0x25, 0x20) , 2.275 },
        { (0x15, 0x6F) , 2.340 },
        { (0x15, 0x2A) , 1.860 },
        { (0x15, 0xEC) , 2.880 },
        { (0x15, 0xA8) , 2.920 },
        { (0x15, 0x6C) , 2.960 },
        { (0x15, 0x28) , 2.980 },
        { (0x15, 0xE3) , 1.000 },
        { (0x15, 0xA7) , 1.020 },
        { (0x15, 0x67) , 1.040 },
        { (0x15, 0xE6) , 1.060 },
        { (0x15, 0x66) , 0.080 },
        { (0x15, 0x22) , 1.180 },
        { (0x25, 0x24) , 1.180 },
        { (0x25, 0x94) , 1.240 },
        { (0x25, 0x25) , 3.320 },
    };

    private static int Hi(byte b) => (b >> 4) & 0xF;
    private static int Lo(byte b) => b & 0xF;

    public static double BaseMHz(byte a0, byte a1)
    {
        // base = ((a0.hi − 1) * 10 + a0.lo) + a1.hi/10 + a1.lo/100
        return ((Hi(a0) - 1) * 10 + Lo(a0)) + (Hi(a1) / 10.0) + (Lo(a1) / 100.0);
    }

    public static double TxMHz(byte A0, byte A1, byte A2)
    {
        double b = BaseMHz(A0, A1);
        DeltaTx.TryGetValue((A1, A2), out double d);
        return Math.Round(b + d, 3);
    }

    public static double RxMHz(byte B0, byte B1, byte B2, double txFallback)
    {
        double b = BaseMHz(B0, B1);
        if (DeltaRx.TryGetValue((B1, B2), out double d))
            return Math.Round(b + d, 3);
        // If pair not present, fall back to simplex
        return Math.Round(txFallback, 3);
    }

    // ---------- TX Tone (dataset bit formula) ----------
    // idx5 = {A2.b0, A2.b1, A2.b7, B3.b1, B3.b2}  (LSB→MSB)
    private static int GetBit(byte b, int bit) => (b >> bit) & 1;

    private static readonly Dictionary<int, string> TxToneMap = new Dictionary<int, string>
    {
        { 0, "0" },
        { 1, "103.5" },
        { 4, "131.8" },
        { 6, "103.5" },
        { 8, "131.8" },
        { 9, "156.7" },
        {10, "107.2" },
        {12, "97.4"  },
        {14, "114.1" },
        {15, "131.8" },
        {16, "110.9" },
        {20, "162.2" },
        {21, "127.3" },
        {23, "103.5" },
        {24, "162.2" },
        {27, "114.8" },
        {28, "131.8" },
    };

    public static string TxToneMenuValue(byte A2, byte B3)
    {
        int idx5 =
            (GetBit(A2, 0) << 0) |
            (GetBit(A2, 1) << 1) |
            (GetBit(A2, 7) << 2) |
            (GetBit(B3, 1) << 3) |
            (GetBit(B3, 2) << 4);

        if (TxToneMap.TryGetValue(idx5, out string hz))
            return hz;

        return "?";
    }
}
