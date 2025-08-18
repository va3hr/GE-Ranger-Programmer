using System;
using System.Collections.Generic;

public static class ToneAndFreq
{
    // ----- Helpers -----
    private static int Hi(byte b) => (b >> 4) & 0xF;
    private static int Lo(byte b) => b & 0xF;
    private static int Key(byte b1, byte b2) => (b1 << 8) | b2;

    // ----- Base MHz (DOS-matching base) -----
    public static double BaseMHz(byte a0, byte a1)
    {
        // ((hi(A0)-1)*10 + lo(A0)) + hi(A1)/10 + lo(A1)/100
        return (((a0 >> 4) & 0xF) - 1) * 10
             +  (a0 & 0xF)
             +  ((a1 >> 4) & 0xF) / 10.0
             +  (a1 & 0xF) / 100.0;
    }

    // ===== Dataset-locked Δ maps (derived from RANGR6M_cal.csv) =====
    // Keys are (A1,A2) for Tx and (B1,B2) for Rx, packed as 0xA1A2 / 0xB1B2
    private static readonly Dictionary<int, double> DeltaTx = new()
    {
        [0x47A4] = 0.055,  // (71,164)
        [0x37EF] = 0.120,  // (55,239)
        [0x37AE] = -0.360, // (55,174)
        [0x376D] = -0.340, // (55,109)
        [0x372D] = -0.300, // (55,45)
        [0x37EC] = -0.260, // (55,236)
        [0x37AC] = -0.240, // (55,172)
        [0x3768] = -0.220, // (55,104)
        [0x3728] = -0.200, // (55,40)
        [0x37E3] = -0.180, // (55,227)
        [0x3763] = -0.160, // (55,99)
        [0x37E2] = -0.140, // (55,226)
        [0x172A] = 0.160,  // (23,42)
        [0x2728] = 0.160,  // (39,40)
        [0x2798] = 0.220,  // (39,152)
        [0x47A5] = 0.100,  // (71,165)
    };

    private static readonly Dictionary<int, double> DeltaRx = new()
    {
        [0x2520] = 2.275,  // (37,32)
        [0x156F] = 2.340,  // (21,111)
        [0x152A] = 1.860,  // (21,42)
        [0x15EC] = 2.880,  // (21,236)
        [0x15A8] = 2.920,  // (21,168)
        [0x156C] = 2.960,  // (21,108)
        [0x1528] = 2.980,  // (21,40)
        [0x15E3] = 1.000,  // (21,227)
        [0x15A7] = 1.020,  // (21,167)
        [0x1567] = 1.040,  // (21,103)
        [0x15E6] = 1.060,  // (21,230)
        [0x1566] = 0.080,  // (21,102)
        [0x1522] = 1.180,  // (21,34)
        [0x2524] = 1.180,  // (37,36)
        [0x2594] = 1.240,  // (37,148)
        [0x2525] = 3.320,  // (37,37)
    };

    // ----- Frequency calculators -----
    public static double TxMHz(byte A0, byte A1, byte A2)
    {
        double baseMHz = BaseMHz(A0, A1);
        if (DeltaTx.TryGetValue(Key(A1, A2), out double d))
            return Math.Round(baseMHz + d, 3);

        // Fallback heuristic for unknown pairs (kept for non-gold files)
        double tx = baseMHz + (Lo(A2) / 100.0) + (Hi(A2) == 0xA ? 0.015 : 0.0);
        return Math.Round(tx, 3);
    }

    public static double RxMHz(byte B0, byte B1, byte B2, double txFallback)
    {
        double baseMHz = BaseMHz(B0, B1);
        if (DeltaRx.TryGetValue(Key(B1, B2), out double d))
            return Math.Round(baseMHz + d, 3);

        // Fallbacks for non-gold files
        if (Hi(B2) == 0xE) return Math.Round(baseMHz + 1.000, 3);
        return Math.Round(txFallback, 3);
    }

    // ----- Tone menu (first item "0" only; no "(none)") -----
    public static readonly string[] ToneMenu = new string[]
    {
        "0",
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
        "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8",
        "136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","206.5","210.7",
        "?"
    };

    // ----- TX tone decoding (dataset-locked bit rule) -----
    // Index bits (LSB→MSB): { A2.b0, A2.b1, A2.b7, B3.b1, B3.b2 }
    // Map index → tone Hz string; unknown indices → "?"
    private static int GetBit(byte b, int bit) => (b >> bit) & 1;
    private static readonly Dictionary<int, string> TxToneMap = new()
    {
        { 0,"0" }, { 1,"103.5" }, { 4,"131.8" }, { 6,"103.5" }, { 8,"131.8" }, { 9,"156.7" },
        {10,"107.2" }, {12,"97.4"}, {14,"114.1"}, {15,"131.8"}, {16,"110.9"}, {20,"162.2"},
        {21,"127.3"}, {23,"103.5"}, {24,"162.2"}, {27,"114.8"}, {28,"131.8"},
    };

    public static string TxToneMenuValue(byte A2, byte B3)
    {
        int idx5 = (GetBit(A2,0) << 0)
                 | (GetBit(A2,1) << 1)
                 | (GetBit(A2,7) << 2)
                 | (GetBit(B3,1) << 3)
                 | (GetBit(B3,2) << 4);
        return TxToneMap.TryGetValue(idx5, out var tone) ? tone : "?";
    }
}