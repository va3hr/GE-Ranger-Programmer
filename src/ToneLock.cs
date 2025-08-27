
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class ToneLock
{
    // Operator menus (valid tone choices only; no "?")
    public static readonly string[] ToneMenuTx = ToneIndexing.CanonicalLabels;
    public static readonly string[] ToneMenuRx = ToneIndexing.CanonicalLabels;

    // ----------------------------------------------------------------
    // Back-compat surface: some files reference ToneLock.Cg.* or the
    // older TryGet*/TrySet* helpers. Provide them here so you can build
    // without re-engineering anything else.
    // ----------------------------------------------------------------
    public static class Cg
    {
        public static readonly string[] MenuTx = ToneIndexing.CanonicalLabels;
        public static readonly string[] MenuRx = ToneIndexing.CanonicalLabels;

        // Dictionaries kept for back-compat. Current decode path does
        // not depend on them, but some code may reference these names.
        public static readonly Dictionary<int, string> TxIndexToTone = new();
        public static readonly Dictionary<int, string> RxIndexToTone_Bank0 = new();
    }

    // Direct decode from the eight channel bytes
    public static (string tx, string rx) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        string tx = TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        string rx = TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        return (tx, rx);
    }

    // -------- Public helpers expected by RgrCodec (stable signatures) --------
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

    // Keep existing write API surface; pass-through for now.
    // (Once indices are finalized, we can implement exact bit-writes.)
    public static bool TrySetTxTone(ref byte A1, ref byte B1, ref byte A3, string txTone)
    {
        // No destructive changes while debugging — leave bytes as-is.
        return true;
    }

    public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string rxTone)
    {
        // No destructive changes while debugging — leave bytes as-is.
        return true;
    }

    // Hex utilities that RgrCodec expects
    public static string ToAsciiHex256(byte[] image128)
    {
        // image128 is 128 bytes. Most tools display 256 ASCII hex chars (2 per byte).
        var hex = new char[image128.Length * 2];
        int p = 0;
        foreach (var b in image128)
        {
            hex[p++] = GetHex((b >> 4) & 0xF);
            hex[p++] = GetHex(b & 0xF);
        }
        return new string(hex);
    }

    private static char GetHex(int v) => (char)(v < 10 ? ('0' + v) : ('A' + (v - 10)));

    public static byte[] ToX2212Nibbles(byte[] image128)
    {
        // Placeholder passthrough while we validate tone paths only.
        var copy = new byte[image128.Length];
        Buffer.BlockCopy(image128, 0, copy, 0, image128.Length);
        return copy;
    }

    // ---------------- Internal decode wiring ----------------
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

        return rxIdx == 0 ? "0" : null;
    }

    // Shared TX key→label table (same content as in ToneIndexing)
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
