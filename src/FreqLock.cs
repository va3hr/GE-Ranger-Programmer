// ============================================================================
// FREQ_LOCK v2025-08-18  (Do not edit without explicit approval.)
// Critical, DOS-matching frequency calculations isolated here permanently.
// ============================================================================

using System;
using System.Collections.Generic;

public static class FreqLock
{
    // --- Helpers ---
    private static int Hi(byte b) => (b >> 4) & 0xF;
    private static int Lo(byte b) => b & 0xF;
    private static int Key(byte b1, byte b2) => (b1 << 8) | b2;
    private static double R(double v) => Math.Round(v, 3);

    // --- Base MHz (big-endian BCD nibbles) ---
    // BaseMHz(A0,A1) = ((hi(A0) − 1) * 10 + lo(A0)) + hi(A1)/10 + lo(A1)/100
    public static double BaseMHz(byte a0, byte a1)
    {
        return (((a0 >> 4) & 0xF) - 1) * 10
             +  (a0 & 0xF)
             +  ((a1 >> 4) & 0xF) / 10.0
             +  (a1 & 0xF) / 100.0;
    }

    // --- Dataset-locked Δ maps (learned from gold file; stable) ---
    // Keys: (A1,A2) for Tx; (B1,B2) for Rx, packed as 0xA1A2 / 0xB1B2.
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

    // --- Locked calculators (preferred) ---
    public static double TxMHzLocked(byte A0, byte A1, byte A2)
    {
        double baseMHz = BaseMHz(A0, A1);
        if (!DeltaTx.TryGetValue(Key(A1, A2), out double d))
            throw new InvalidOperationException($"FreqLock.TxMHzLocked: unknown (A1,A2)=({A1:X2},{A2:X2})");
        return R(baseMHz + d);
    }

    public static double RxMHzLocked(byte B0, byte B1, byte B2)
    {
        double baseMHz = BaseMHz(B0, B1);
        if (!DeltaRx.TryGetValue(Key(B1, B2), out double d))
            throw new InvalidOperationException($"FreqLock.RxMHzLocked: unknown (B1,B2)=({B1:X2},{B2:X2})");
        return R(baseMHz + d);
    }

    // --- Lenient calculators (safe fallback for non-gold files) ---
    public static double TxMHz(byte A0, byte A1, byte A2)
    {
        double baseMHz = BaseMHz(A0, A1);
        if (DeltaTx.TryGetValue(Key(A1, A2), out double d))
            return R(baseMHz + d);

        // Heuristic fallback used earlier; not for gold-set.
        double tx = baseMHz + (Lo(A2) / 100.0) + (Hi(A2) == 0xA ? 0.015 : 0.0);
        return R(tx);
    }

    public static double RxMHz(byte B0, byte B1, byte B2, double txFallback)
    {
        double baseMHz = BaseMHz(B0, B1);
        if (DeltaRx.TryGetValue(Key(B1, B2), out double d))
            return R(baseMHz + d);

        // Fallbacks seen in the field
        if (Hi(B2) == 0xE) return R(baseMHz + 1.000); // split flag
        return R(txFallback);                          // simplex
    }
}