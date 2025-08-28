using System;

public static class ToneDiag
{
    // Compact row summary that MainForm appends to the log.
    public static string Row(
        int fileIdx, int screenRow,
        byte A0, byte A1, byte A2, byte A3,
        byte B0, byte B1, byte B2, byte B3,
        string? txLabel, string? rxLabel)
    {
        int code = (((B0 >> 4) & 1) << 5)
                 | (((B2 >> 2) & 1) << 4)
                 | (((B3 >> 3) & 1) << 3)
                 | (((B3 >> 2) & 1) << 2)
                 | (((B3 >> 1) & 1) << 1)
                 |  ((B3      ) & 1);

        int bank = (((B2 >> 7) & 1) << 2)
                 | (((B1 >> 5) & 1) << 1)
                 |  ((B2 >> 1) & 1);

        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        string hex = $"{A0:X2} {A1:X2} {A2:X2} {A3:X2}  {B0:X2} {B1:X2} {B2:X2} {B3:X2}";
        string txShow = txLabel ?? "ERR";
        string rxShow = rxLabel ?? "ERR";
        return $"row {screenRow:D2} code={code:D2} bank={bank} rxIdx={rxIdx:D2} tx={txShow} rx={rxShow}  [{hex}]";
    }
}
