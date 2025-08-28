using System;
using System.Collections.Generic;

public static class ToneLock
{
    // Operator menus (exactly the 33 canonical tones; no extras)
    public static readonly string[] ToneMenuTx = ToneIndexing.CanonicalLabels;
    public static readonly string[] ToneMenuRx = ToneIndexing.CanonicalLabels;

    // Back-compat: some code expects ToneLock.Cg to be a string[] and uses
    // Cg.Length and Cg[index]. Provide that here.
    public static readonly string[] Cg = ToneIndexing.CanonicalLabels;

    // ---------- Public surface used by MainForm/RgrCodec ----------
    public static (string tx, string rx) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        string tx = TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        string rx = TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        return (tx, rx);
    }

    public static bool TryGetTxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        label = TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        return true;
    }

    public static bool TryGetRxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        label = TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        return true;
    }

    // Kept for compatibility during debug (non-destructive)
    public static bool TrySetTxTone(ref byte A1, ref byte B1, ref byte A3, string txTone) => true;
    public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string rxTone) => true;

    // Utilities RgrCodec expects
    public static string ToAsciiHex256(byte[] image128)
    {
        var hex = new char[image128.Length * 2];
        int p = 0;
        foreach (var b in image128)
        {
            int hi = (b >> 4) & 0xF;
            int lo = b & 0xF;
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

    // ---------------- Internal decode wiring ----------------
    private static string? TryDecodeTx(byte A0, byte A1, byte A2, byte A3,
                                       byte B0, byte B1, byte B2, byte B3)
    {
        // 6-bit TX code (no big-endian conversion here)
        int code = (((B0 >> 4) & 1) << 5)
                 | (((B2 >> 2) & 1) << 4)
                 | (((B3 >> 3) & 1) << 3)
                 | (((B3 >> 2) & 1) << 2)
                 | (((B3 >> 1) & 1) << 1)
                 |  ((B3      ) & 1);

        // 3-bit bank selector H:M:L
        int bank = (((B2 >> 7) & 1) << 2)  // H
                 | (((B1 >> 5) & 1) << 1)  // M
                 |  ((B2 >> 1) & 1);       // L

        int key = (bank << 6) | code;
        return TxMap.TryGetValue(key, out var label) ? label : null;
    }

    private static string? TryDecodeRx(byte A0, byte A1, byte A2, byte A3,
                                       byte B0, byte B1, byte B2, byte B3)
    {
        // 6-bit RX index from A3 (no big-endian conversion for tones)
        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        if (rxIdx == 0) return "0";
        if (rxIdx < 1 || rxIdx > ToneIndexing.CanonicalLabels.Length) return null;
        return ToneIndexing.CanonicalLabels[rxIdx - 1];
    }

    // Shared TX key→label table (checkpoint values; adjust as we verify)
    private static readonly Dictionary<int, string> TxMap = new()
    {
        // Bank 000
        [10]  = "97.4",
        [25]  = "131.8",

        // Bank 100
        [74]  = "114.1",
        [75]  = "107.2",
        [89]  = "103.5",
        [90]  = "131.8",
        [93]  = "103.5",

        // Bank 010
        [143] = "131.8",
        [156] = "127.3",
        [157] = "110.9",

        // Bank 001
        [267] = "156.7",
        [281] = "103.5",

        // Bank 101
        [335] = "162.2",
        [344] = "107.2",
        [350] = "114.8",

        // Bank 011
        [413] = "162.2",
    };
}
