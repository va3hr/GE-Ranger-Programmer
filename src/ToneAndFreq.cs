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

    // --- Frequency rules (DOS-matching we previously locked) ---
    private static int Hi(byte b) => (b >> 4) & 0xF;
    private static int Lo(byte b) => b & 0xF;

    public static double BaseMHz(byte A0, byte A1)
    {
        // base = ((A0.hi − 1) * 10 + A0.lo) + (A1.hi / 10) + (A1.lo / 100)
        double baseMHz = ((Hi(A0) - 1) * 10 + Lo(A0))
                         + (Hi(A1) / 10.0)
                         + (Lo(A1) / 100.0);
        return baseMHz;
    }

    public static double TxMHz(byte A0, byte A1, byte A2)
    {
        // Rule-based model that matched our gold screen
        // Tx = base(A0,A1) + (A2.low)/100 + (A2.high == 0xA ? 0.015 : 0)
        double tx = BaseMHz(A0, A1)
                    + (Lo(A2) / 100.0)
                    + (Hi(A2) == 0xA ? 0.015 : 0.0);
        return Math.Round(tx, 3);
    }

    public static double RxMHz(byte B0, byte B1, byte B2, double txFallback)
    {
        // Simplex unless split flag: if (B2.hi == 0xE) then Rx = base(B0,B1) + 1.000
        if (Hi(B2) == 0xE)
        {
            return Math.Round(BaseMHz(B0, B1) + 1.000, 3);
        }
        return Math.Round(txFallback, 3);
    }

    // --- TX Tone decoding (dataset-locked bit formula) ---
    // idx5 = {A2.b0, A2.b1, A2.b7, B3.b1, B3.b2} (LSB->MSB)
    private static int GetBit(byte b, int bit) => (b >> bit) & 1;

    private static readonly Dictionary<int, string> TxToneMap = new Dictionary<int, string>
    {
        // From project memory (dataset mapping)
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

        // Unknown index -> "?"
        return "?";
    }
}
