using System;
using System.Collections.Generic;

public static class ToneLock
{
    // Menus remain VALID ONLY (no invalid entries).
    public static readonly string[] ToneMenuTx = ToneIndexing.CanonicalLabels;
    public static readonly string[] ToneMenuRx = ToneIndexing.CanonicalLabels;

    // Some paths use ToneLock.Cg[idx] directly.
    public static readonly string[] Cg = ToneIndexing.CanonicalLabels;

    // ===== Public decode (returns null for INVALID) =====
    public static (string tx, string rx) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        int txIdx = TxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3);
        int rxIdx = RxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3);
        string tx = LabelOrNull(txIdx);    // null => INVALID
        string rx = LabelOrNull(rxIdx);    // null => INVALID
        return (tx, rx);
    }

    public static bool TryGetTxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        label = LabelOrNull(TxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3));
        return true;
    }

    public static bool TryGetRxTone(byte A3, byte A2, byte A1, byte A0,
                                    byte B3, byte B2, byte B1, byte B0,
                                    out string label)
    {
        label = LabelOrNull(RxIndexFromBytes(A0, A1, A2, A3, B0, B1, B2, B3));
        return true;
    }

    // Writes left as no-ops during decode validation
    public static bool TrySetTxTone(ref byte A1, ref byte B1, ref byte A3, string txTone) => true;
    public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string rxTone) => true;

    // ===== Utilities used elsewhere =====
    public static string ToAsciiHex256(byte[] image128)
    {
        var hex = new char[image128.Length * 2];
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

    // ===== Index logic: 0 = "0"; 1..33 = label; otherwise INVALID -> null =====
    private static string LabelOrNull(int idx)
    {
        if (idx == 0) return "0";                    // explicit no-tone
        if (idx < 0 || idx > 33) return null;        // INVALID
        return (idx < Cg.Length) ? Cg[idx] : null;   // valid 1..33
    }

    // TX six-bit index from validated taps (no big-endian for tones)
    private static int TxIndexFromBytes(byte A0, byte A1, byte A2, byte A3,
                                        byte B0, byte B1, byte B2, byte B3)
    {
        // Bits: B0[4] as MSB, B2[2], B3[3], B3[2], B3[1], B3[0] as LSB
        int idx = (((B0 >> 4) & 1) << 5)
                | (((B2 >> 2) & 1) << 4)
                | (((B3 >> 3) & 1) << 3)
                | (((B3 >> 2) & 1) << 2)
                | (((B3 >> 1) & 1) << 1)
                |  ((B3      ) & 1);
        return idx;
    }

    // RX six-bit index from A3 bit window
    private static int RxIndexFromBytes(byte A0, byte A1, byte A2, byte A3,
                                        byte B0, byte B1, byte B2, byte B3)
    {
        // Bits: A3[6] MSB, A3[7], A3[0], A3[1], A3[2], A3[3] LSB
        int idx = (((A3 >> 6) & 1) << 5)
                | (((A3 >> 7) & 1) << 4)
                | (((A3 >> 0) & 1) << 3)
                | (((A3 >> 1) & 1) << 2)
                | (((A3 >> 2) & 1) << 1)
                |  ((A3 >> 3) & 1);
        return idx;
    }
}
