using System;
using System.Globalization;
using System.Text;

public static class ToneDiag
{
    // ===== Core helpers =====
    private static string LabelFromIdx(int idx)
    {
        if (idx <= 0 || idx > 33) return "0"; // project rule
        if (idx >= ToneIndexing.CanonicalLabels.Length) return "0";
        return ToneIndexing.CanonicalLabels[idx];
    }

    private static int TxIndex(byte A0, byte A1, byte A2, byte A3,
                               byte B0, byte B1, byte B2, byte B3,
                               out int bank, out int code)
    {
        code = (((B0 >> 4) & 1) << 5)
             | (((B2 >> 2) & 1) << 4)
             | (((B3 >> 3) & 1) << 3)
             | (((B3 >> 2) & 1) << 2)
             | (((B3 >> 1) & 1) << 1)
             |  ((B3      ) & 1);

        bank = (((B2 >> 7) & 1) << 2)  // H
             | (((B1 >> 5) & 1) << 1)  // M
             |  ((B2 >> 1) & 1);       // L

        return code; // model: direct 6-bit code as index
    }

    private static int RxIndex(byte A0, byte A1, byte A2, byte A3,
                               byte B0, byte B1, byte B2, byte B3)
    {
        int idx = (((A3 >> 6) & 1) << 5)
                | (((A3 >> 7) & 1) << 4)
                | (((A3 >> 0) & 1) << 3)
                | (((A3 >> 1) & 1) << 2)
                | (((A3 >> 2) & 1) << 1)
                |  ((A3 >> 3) & 1);
        return idx;
    }

    private static string FormatRow(int row1Based,
                                    byte A0, byte A1, byte A2, byte A3,
                                    byte B0, byte B1, byte B2, byte B3)
    {
        int bank, code;
        int txIdx = TxIndex(A0, A1, A2, A3, B0, B1, B2, B3, out bank, out code);
        int rxIdx = RxIndex(A0, A1, A2, A3, B0, B1, B2, B3);
        string tx = LabelFromIdx(txIdx);
        string rx = LabelFromIdx(rxIdx);
        string hex = string.Format(CultureInfo.InvariantCulture,
            "{0:X2} {1:X2} {2:X2} {3:X2}  {4:X2} {5:X2} {6:X2} {7:X2}",
            A0, A1, A2, A3, B0, B1, B2, B3);
        return string.Format(CultureInfo.InvariantCulture,
            "row {0:D2}  code={1:D2} bank={2}  txIdx={3:D2} tx={4}  rxIdx={5:D2} rx={6}  [{7}]",
            row1Based, code, bank, txIdx, tx, rxIdx, rx, hex);
    }

    private static byte ToByte(object o)
    {
        if (o is byte b) return b;
        if (o is sbyte sb) return unchecked((byte)sb);
        if (o is int i) return unchecked((byte)i);
        if (o is uint ui) return unchecked((byte)ui);
        if (o is short s) return unchecked((byte)s);
        if (o is ushort us) return unchecked((byte)us);
        if (o is long l) return unchecked((byte)l);
        if (o is ulong ul) return unchecked((byte)ul);
        if (o is string str)
        {
            str = str.Trim();
            if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (byte.TryParse(str.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bx)) return bx;
            }
            if (byte.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bd)) return bd;
        }
        return 0;
    }

    // ===== Public APIs =====

    // FILE order (A0..B3) explicitly
    public static string RowFileOrder(int row1Based,
                                      byte A0, byte A1, byte A2, byte A3,
                                      byte B0, byte B1, byte B2, byte B3)
        => FormatRow(row1Based, A0, A1, A2, A3, B0, B1, B2, B3);

    // SCREEN order (A3..A0 B3..B0) explicitly
    public static string Row(int row1Based,
                             byte A3, byte A2, byte A1, byte A0,
                             byte B3, byte B2, byte B1, byte B0)
        => FormatRow(row1Based, A0, A1, A2, A3, B0, B1, B2, B3);

    // Back-compat: forgiving overload with 12 args. We use the first eight bytes; extras ignored.
    public static string Row(int row1Based,
                             object A3, object A2, object A1, object A0,
                             object B3, object B2, object B1, object B0,
                             object _x, object _y, object _z)
        => FormatRow(row1Based,
                     ToByte(A0), ToByte(A1), ToByte(A2), ToByte(A3),
                     ToByte(B0), ToByte(B1), ToByte(B2), ToByte(B3));

    // Convenience: compute from the full logical-128 image (file order)
    public static string Row(int row1Based, byte[] logical128)
    {
        if (logical128 == null || logical128.Length < 128) return $"row {row1Based:D2}  (insufficient data)";
        int ch = Math.Clamp(row1Based - 1, 0, 15);
        int off = ch * 8;
        byte A0 = logical128[off + 0];
        byte A1 = logical128[off + 1];
        byte A2 = logical128[off + 2];
        byte A3 = logical128[off + 3];
        byte B0 = logical128[off + 4];
        byte B1 = logical128[off + 5];
        byte B2 = logical128[off + 6];
        byte B3 = logical128[off + 7];
        return FormatRow(row1Based, A0, A1, A2, A3, B0, B1, B2, B3);
    }

    // Full dump for logging or saving
    public static string Dump(byte[] logical128)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ToneDiag: six-bit index model (0..33 valid; >33 => 0)");
        for (int r = 1; r <= 16; r++)
            sb.AppendLine(Row(r, logical128));
        return sb.ToString();
    }
}
