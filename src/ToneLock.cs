using System;
using System.Collections.Generic;

public static class ToneLock
{
    // Menus: exactly the 33 canonical tones (no "0" entry).
    public static readonly string[] ToneMenuTx = ToneIndexing.CanonicalLabels;
    public static readonly string[] ToneMenuRx = ToneIndexing.CanonicalLabels;

    // Back-compat shim to keep older references compiling.
    public static class Cg
    {
        public static readonly string[] MenuTx = ToneIndexing.CanonicalLabels;
        public static readonly string[] MenuRx = ToneIndexing.CanonicalLabels;
        public static readonly Dictionary<int, string> TxIndexToTone = new(); // not used in decode path
        public static readonly Dictionary<int, string> RxIndexToTone_Bank0 = new(); // not used in decode path
    }

    // Primary decode entry. Returns "0" for index 0, canonical label for 1..33, or null -> ERR.
    public static (string? tx, string? rx) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        string? tx = TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3);
        string? rx = TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3);
        return (tx, rx);
    }

    // Historical surface
    public static bool TryGetTxTone(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0, out string? label)
    { label = TryDecodeTx(A0,A1,A2,A3,B0,B1,B2,B3); return true; }
    public static bool TryGetRxTone(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0, out string? label)
    { label = TryDecodeRx(A0,A1,A2,A3,B0,B1,B2,B3); return true; }

    public static bool TrySetTxTone(ref byte A1, ref byte B1, ref byte A3, string txTone) => true; // no-ops for now
    public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string rxTone) => true;

    // Hex helpers retained
    public static string ToAsciiHex256(byte[] image128)
    {
        var hex = new char[image128.Length * 2];
        int p = 0;
        foreach (var b in image128)
        {
            int hi = (b >> 4) & 0xF, lo = b & 0xF;
       	    hex[p++] = (char)(hi < 10 ? '0' + hi : 'A' + (hi - 10));
            hex[p++] = (char)(lo < 10 ? '0' + lo : 'A' + (lo - 10));
        }
        return new string(hex);
    }
    public static byte[] ToX2212Nibbles(byte[] image128)
    {
        var copy = new byte[image128.Length];
        Buffer.BlockCopy(image128, 0, copy, 0, image128.Length);
        return copy;
    }

    // ---------- Decode core (no big-endian for tones) ----------
    private static string? TryDecodeTx(byte A0, byte A1, byte A2, byte A3,
                                       byte B0, byte B1, byte B2, byte B3)
    {
        // 6-bit code window (LSB..MSB order preserved per prior discussion)
        int code = (((B0 >> 4) & 1) << 5)
                 | (((B2 >> 2) & 1) << 4)
                 | (((B3 >> 3) & 1) << 3)
                 | (((B3 >> 2) & 1) << 2)
                 | (((B3 >> 1) & 1) << 1)
                 |  ((B3      ) & 1);

        // 3-bit bank select
        int bank = (((B2 >> 7) & 1) << 2)  // H
                 | (((B1 >> 5) & 1) << 1)  // M
                 |  ((B2 >> 1) & 1);       // L

        if (code == 0) return "0"; // explicit "no tone"

        int key = (bank << 6) | code;
        return TxMap.TryGetValue(key, out var label) ? label : null; // null -> ERR
    }

    private static string? TryDecodeRx(byte A0, byte A1, byte A2, byte A3,
                                       byte B0, byte B1, byte B2, byte B3)
    {
        int idx = (((A3 >> 6) & 1) << 5)
                | (((A3 >> 7) & 1) << 4)
                | (((A3 >> 0) & 1) << 3)
                | (((A3 >> 1) & 1) << 2)
                | (((A3 >> 2) & 1) << 1)
                |  ((A3 >> 3) & 1);

        return ToneIndexing.LabelFromIndex(idx); // "0" for 0, null for invalid 34..63
    }

    // Fill this dictionary using your TX1_*.RGR probes: key=(bank<<6)|code, value=canonical label
    private static readonly Dictionary<int, string> TxMap = new()
    {
        // Seeded examples; expand/adjust to cover all 33 canonical tones
        [(1 << 6) | 29] = "103.5",
        [(1 << 6) | 25] = "103.5",
        [(1 << 6) | 30] = "110.9",
        [(1 << 6) | 28] = "127.3",
        [(0 << 6) |  0] = "0",      // explicit no-tone (already handled by code==0, but harmless)
        [(0 << 6) | 27] = "141.3",
        [(0 << 6) | 23] = "167.9",
        [(7 << 6) | 22] = "151.4",
        [(6 << 6) | 27] = "173.8",
        [(3 << 6) | 25] = "162.2",
    };
}
