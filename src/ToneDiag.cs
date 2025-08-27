
using System.Globalization;

public static class ToneDiag
{
    public static string Row(int fileIdx, int screenRow,
        byte A0, byte A1, byte A2, byte A3, byte B0, byte B1, byte B2, byte B3,
        string txShown, string rxShown)
    {
        int txCode = (((B0 >> 4) & 1) << 5)
                   | (((B2 >> 2) & 1) << 4)
                   | (((B3 >> 3) & 1) << 3)
                   | (((B3 >> 2) & 1) << 2)
                   | (((B3 >> 1) & 1) << 1)
                   |  ((B3      ) & 1);

        int bank = (((B2 >> 7) & 1) << 2)  // H
                 | (((B1 >> 5) & 1) << 1)  // M
                 |  ((B2 >> 1) & 1);       // L

        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        string txExpect = ToneIndexing.TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "<null>";
        string rxExpect = ToneIndexing.TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "<null>";

        string bytes = string.Format(CultureInfo.InvariantCulture,
            "{0:X2} {1:X2} {2:X2} {3:X2}  {4:X2} {5:X2} {6:X2} {7:X2}",
            A0, A1, A2, A3, B0, B1, B2, B3);

        return $"file[{fileIdx:00}]->row[{screenRow:00}]  TXcode={txCode:00} bank={bank} RXidx={rxIdx:00}  " +
               $"show(TX={(txShown ?? "<null>")},RX={(rxShown ?? "<null>")}) " +
               $"map(TX={txExpect},RX={rxExpect})  bytes={bytes}";
    }
}
