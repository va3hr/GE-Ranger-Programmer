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
    public static (string? tx, string? rx) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        // TX: true 6-bit index assembled from scattered bits (no bank mixing)
        int txIdx = BitExact_Indexer.TxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
        string? tx = ToneIndexing.LabelFromIndex(txIdx);

        // RX: 6-bit index from A3 per locked bit order; Follow-TX on idx==0 if B3.bit0==1
        int rxIdxRaw = BitExact_Indexer.RxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
        string? rx;
        bool followTx = (B3 & 0x01) != 0;
        if (rxIdxRaw == 0)
            rx = followTx ? tx : "0";
        else
            rx = ToneIndexing.LabelFromIndex(rxIdxRaw);

        return (tx, rx);
    }

    public static bool TryGetTxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        label = ToneIndexing.LabelFromIndex(BitExact_Indexer.TxIndex(A3, A2, A1, A0, B3, B2, B1, B0)) ?? "Err";
        return true;
    }

    public static bool TryGetRxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        {
        int rxIdxRaw = BitExact_Indexer.RxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
        bool followTx = (B3 & 0x01) != 0;
        if (rxIdxRaw == 0)
            label = followTx ? ToneIndexing.LabelFromIndex(BitExact_Indexer.TxIndex(A3, A2, A1, A0, B3, B2, B1, B0)) ?? "Err" : "0";
        else
            label = ToneIndexing.LabelFromIndex(rxIdxRaw) ?? "Err";
    }
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
    int idx = BitExact_Indexer.TxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
    return ToneIndexing.LabelFromIndex(idx);
}

    private static string? TryDecodeRx(byte A0, byte A1, byte A2, byte A3,
                                   byte B0, byte B1, byte B2, byte B3)
{
    int rxIdxRaw = BitExact_Indexer.RxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
    if (rxIdxRaw == 0)
    {
        bool followTx = (B3 & 0x01) != 0;
        if (followTx)
        {
            int txIdx = BitExact_Indexer.TxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
            return ToneIndexing.LabelFromIndex(txIdx);
        }
        return "0";
    }
    return ToneIndexing.LabelFromIndex(rxIdxRaw);
}

    // Shared TX key→label table (checkpoint values; adjust as we verify)
    private static readonly Dictionary<int, string> TxMap = new()
    {
        // Bank 000
        [10]  = "97.4",
        [25]  = "131.8",

        // Bank 100
        [74] = "131.8",
        [75]  = "107.2",
        [89]  = "103.5",
        [90]  = "131.8",
        [93]  = "103.5",

        // Bank 010
        [143] = "114.1",
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
