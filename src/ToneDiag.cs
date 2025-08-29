using System;
using System.Text;

public static class ToneDiag
{
    // Call this from your existing diagnostic button after A3..B0 and computed tx/rx labels are available
    public static string DumpRow(
        int row,
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0,
        string txLabel, string rxLabel,
        int code, int bank, int rxIdx // keep your existing fields
    )
    {
        // Canonical tone index from label (null if non-canonical or "ERR")
        int? TxIdxFromLbl = ToneIndexing.IndexFromLabel(txLabel);
        int? RxIdxFromLbl = ToneIndexing.IndexFromLabel(rxLabel);

        // Our computed indices
        int txIdxRaw = BitExact_Indexer.TxIndex(A3, A2, A1, A0, B3, B2, B1, B0);
        int rxIdxRaw = BitExact_Indexer.RxIndex(A3, A2, A1, A0, B3, B2, B1, B0);

        // Raw bit windows
        int[] txbits = new int[] {
            (B0 >> 4) & 1,  // B0.4
            (B2 >> 2) & 1,  // B2.2
            (B3 >> 3) & 1,  // B3.3
            (B3 >> 2) & 1,  // B3.2
            (B3 >> 1) & 1,  // B3.1
            (B3 >> 0) & 1   // B3.0
        };
        int[] rxbits = new int[] {
            (A3 >> 6) & 1,  // A3.6
            (A3 >> 7) & 1,  // A3.7
            (A3 >> 0) & 1,  // A3.0
            (A3 >> 1) & 1,  // A3.1
            (A3 >> 2) & 1,  // A3.2
            (A3 >> 3) & 1   // A3.3
        };

        string txbitsStr = $"[{txbits[0]} {txbits[1]} {txbits[2]} {txbits[3]} {txbits[4]} {txbits[5]}]  // [B0.4 B2.2 B3.3 B3.2 B3.1 B3.0]";
        string rxbitsStr = $"[{rxbits[0]} {rxbits[1]} {rxbits[2]} {rxbits[3]} {rxbits[4]} {rxbits[5]}]  // [A3.6 A3.7 A3.0 A3.1 A3.2 A3.3]";

        // Build output line
        return
            $"row {row:00} code={code} bank={bank} " +
            $"TX bits={txbitsStr} → txIdx={txIdxRaw:00} tx={txLabel} (idxFromLbl={(TxIdxFromLbl?.ToString() ?? "NA")})  |  " +
            $"RX bits={rxbitsStr} → rxIdx={rxIdxRaw:00} rx={rxLabel} (idxFromLbl={(RxIdxFromLbl?.ToString() ?? "NA")})  " +
            $"  [{A3:X2} {A2:X2} {A1:X2} {A0:X2}  {B3:X2} {B2:X2} {B1:X2} {B0:X2}]";
    }
}
