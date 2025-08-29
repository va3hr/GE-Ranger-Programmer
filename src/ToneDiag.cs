using System;

public static class ToneDiag
{
    // Compact row summary that MainForm appends to the log.
    // NOTE: signature preserved to match MainForm.cs call site.
    public static string Row(
        int fileIdx, int screenRow,
        byte A0, byte A1, byte A2, byte A3,
        byte B0, byte B1, byte B2, byte B3,
        string? txLabel, string? rxLabel)
    {
        // Reproduce existing "code" and "bank" so logs remain comparable
        int code = (((B0 >> 4) & 1) << 5)
                 | (((B2 >> 2) & 1) << 4)
                 | (((B3 >> 3) & 1) << 3)
                 | (((B3 >> 2) & 1) << 2)
                 | (((B3 >> 1) & 1) << 1)
                 |  ((B3      ) & 1);

        int bank = (((B2 >> 7) & 1) << 2)
                 | (((B1 >> 5) & 1) << 1)
                 |  ((B2 >> 1) & 1);

        // Raw RX index from A3 (locked bit order)
        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        // Our indexers (for verification)
        int txIdxRaw = BitExact_Indexer.TxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
        int rxIdxRaw = BitExact_Indexer.RxIndex(A3, A2, A1, A0, B3, B2, B1, B0);

        // Index implied by labels (canonical 33 only)
        int? txIdxFromLbl = ToneIndexing.IndexFromLabel(txLabel ?? "");
        int? rxIdxFromLbl = ToneIndexing.IndexFromLabel(rxLabel ?? "");

        // Bit windows shown explicitly
        string txBits = $"[{((B0>>4)&1)} {((B2>>2)&1)} {((B3>>3)&1)} {((B3>>2)&1)} {((B3>>1)&1)} {((B3>>0)&1)}]"; // [B0.4 B2.2 B3.3 B3.2 B3.1 B3.0]
        string rxBits = $"[{((A3>>6)&1)} {((A3>>7)&1)} {((A3>>0)&1)} {((A3>>1)&1)} {((A3>>2)&1)} {((A3>>3)&1)}]"; // [A3.6 A3.7 A3.0 A3.1 A3.2 A3.3]

        string aHex = $"{A0:X2} {A1:X2} {A2:X2} {A3:X2}";
        string bHex = $"{B0:X2} {B1:X2} {B2:X2} {B3:X2}";

        string txShow = txLabel ?? "ERR";
        string rxShow = rxLabel ?? "ERR";

        return $"row {screenRow:D2} code={code:D2} bank={bank} " +
               $"txBits={txBits} txIdx={txIdxRaw:00} tx={txShow} (idxFromLbl={(txIdxFromLbl?.ToString() ?? "NA")})  " +
               $"rxBits={rxBits} rxIdx={rxIdxRaw:00} rx={rxShow} (idxFromLbl={(rxIdxFromLbl?.ToString() ?? "NA")})  " +
               $"[{aHex}  {bHex}]";
    }
}
