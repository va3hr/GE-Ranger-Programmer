using System;
using System.Globalization;
using System.IO;
using System.Text;

public static class ToneDiag
{
    // Self-contained diagnostic that depends only on ToneIndexing (labels) and raw bytes.
    // No calls into ToneIndexing.TryDecode* — keeps the surface clean.
    public static void WriteDump(string filePath, byte[] logical128)
    {
        File.WriteAllText(filePath, Dump(logical128));
    }

    public static string Dump(byte[] logical128)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ToneDiag: six-bit index model (0..33 valid; >33 => 0)");

        for (int ch = 0; ch < 16; ch++)
        {
            int off = ch * 8;
            if (off + 7 >= logical128.Length) break;

            byte A0 = logical128[off + 0];
            byte A1 = logical128[off + 1];
            byte A2 = logical128[off + 2];
            byte A3 = logical128[off + 3];
            byte B0 = logical128[off + 4];
            byte B1 = logical128[off + 5];
            byte B2 = logical128[off + 6];
            byte B3 = logical128[off + 7];

            int txCode = (((B0 >> 4) & 1) << 5)
                       | (((B2 >> 2) & 1) << 4)
                       | (((B3 >> 3) & 1) << 3)
                       | (((B3 >> 2) & 1) << 2)
                       | (((B3 >> 1) & 1) << 1)
                       |  ((B3      ) & 1);

            int txBank = (((B2 >> 7) & 1) << 2)  // H
                       | (((B1 >> 5) & 1) << 1)  // M
                       |  ((B2 >> 1) & 1);       // L

            int txIdx = txCode; // model: direct 6-bit code as index
            int rxIdx = (((A3 >> 6) & 1) << 5)
                      | (((A3 >> 7) & 1) << 4)
                      | (((A3 >> 0) & 1) << 3)
                      | (((A3 >> 1) & 1) << 2)
                      | (((A3 >> 2) & 1) << 1)
                      |  ((A3 >> 3) & 1);

            string txLabel = LabelFromIdx(txIdx);
            string rxLabel = LabelFromIdx(rxIdx);

            string hex = $"{A0:X2} {A1:X2} {A2:X2} {A3:X2}  {B0:X2} {B1:X2} {B2:X2} {B3:X2}";

            sb.AppendFormat(CultureInfo.InvariantCulture,
                "row {0:D2}  code={1:D2} bank={2}  txIdx={3:D2} tx={4}  rxIdx={5:D2} rx={6}  [{7}]",
                ch+1, txCode, txBank, txIdx, txLabel, rxIdx, rxLabel, hex);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string LabelFromIdx(int idx)
    {
        if (idx <= 0 || idx > 33) return "0";
        if (idx >= ToneIndexing.CanonicalLabels.Length) return "0";
        return ToneIndexing.CanonicalLabels[idx];
    }
}
