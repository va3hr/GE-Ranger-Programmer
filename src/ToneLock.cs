using System;
using System.Collections.Generic;
using System.Linq;

public static class ToneLock
{
    // Operator menus: valid choices only.
    public static readonly string[] ToneMenuTx = ToneIndexing.CanonicalLabels;
    public static readonly string[] ToneMenuRx = ToneIndexing.CanonicalLabels;

    // Back-compat array some files index as ToneLock.Cg[..]
    public static readonly string[] Cg = ToneIndexing.CanonicalLabels;

    private static readonly HashSet<string> _valid = new HashSet<string>(ToneIndexing.CanonicalLabels);

    private static string Normalize(string? label)
    {
        if (string.IsNullOrEmpty(label)) return ")";     // explicit marker for invalid/unknown
        return _valid.Contains(label) ? label : ")";     // never surface a non-canonical label
    }

    // Public surface expected by existing code
    public static (string tx, string rx) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        string tx = Normalize(TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3));
        string rx = Normalize(TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3));
        // treat "0" specially (no tone). If Normalize mapped it to ')', keep ')'
        if (tx == ")" && TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3) == "0") tx = "0";
        if (rx == ")" && TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3) == "0") rx = "0";
        return (tx, rx);
    }

    public static bool TryGetTxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        label = Normalize(TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3));
        return true;
    }

    public static bool TryGetRxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        label = Normalize(TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3));
        return true;
    }

    // Writes are no-ops while we validate decode. (UI editing still uses canonical menus.)
    public static bool TrySetTxTone(ref byte A1, ref byte B1, ref byte A3, string txTone) => true;
    public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string rxTone) => true;

    public static string ToAsciiHex256(byte[] image128)
    {
        char[] hex = new char[image128.Length * 2];
        int p = 0;
        foreach (var b in image128)
        {
            int hi = (b >> 4) & 0xF, lo = b & 0xF;
            hex[p++] = (char)(hi < 10 ? ('0' + hi) : ('A' + (hi - 10)));
            hex[p++] = (char)(lo < 10 ? ('0' + lo) : ('A' + (lo - 10)));
        }
        return new string(hex);
    }

    public static byte[] ToX2212Nibbles(byte[] image128)
    {
        var copy = new byte[image128.Length];
        Buffer.BlockCopy(image128, 0, copy, 0, image128.Length);
        return copy;
    }

    // -------- Internal decode (six-bit code + three-bit bank) --------
    private static string? TryDecodeTx(byte A0, byte A1, byte A2, byte A3,
                                       byte B0, byte B1, byte B2, byte B3)
    {
        int code = (((B0 >> 4) & 1) << 5)
                 | (((B2 >> 2) & 1) << 4)
                 | (((B3 >> 3) & 1) << 3)
                 | (((B3 >> 2) & 1) << 2)
                 | (((B3 >> 1) & 1) << 1)
                 |  ((B3      ) & 1);

        int bank = (((B2 >> 7) & 1) << 2)  // H
                 | (((B1 >> 5) & 1) << 1)  // M
                 |  ((B2 >> 1) & 1);       // L

        int key = (bank << 6) | code;
        return TxMap.TryGetValue(key, out var label) ? label : null;
    }

    private static string? TryDecodeRx(byte A0, byte A1, byte A2, byte A3,
                                       byte B0, byte B1, byte B2, byte B3)
    {
        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        return rxIdx == 0 ? "0" : null; // until RX mapping is finalized
    }

    // -------- TX key→label map (114.1 intentionally excluded) --------
    private static readonly Dictionary<int, string> TxMap = new()
    {
        // bank 000
        [10]  = "131.8",
        [25]  = "131.8",

        // bank 100
        [11 + (4<<6)] = "156.7",  // key 267
        [25 + (4<<6)] = "156.7",  // key 281

        // bank 010
        [28 + (2<<6)] = "127.3",  // key 156
        [29 + (2<<6)] = "110.9",  // key 157 (verify against row 13 if DOS shows 107.7)

        // bank 101
        [15 + (5<<6)] = "162.2",  // key 335
        [24 + (5<<6)] = "107.2",  // key 344
        [30 + (5<<6)] = "114.8",  // key 350

        // bank 001 / 010 entries needed by this file
        [10 + (1<<6)] = "131.8",  // key 74
        [11 + (1<<6)] = "107.2",  // key 75
        [25 + (1<<6)] = "103.5",  // key 89
        [26 + (1<<6)] = "131.8",  // key 90
        [29 + (1<<6)] = "103.5",  // key 93

        // bank 110
        [29 + (6<<6)] = "162.2",  // key 413
    };
}
