\
#nullable disable
using System;
using System.Globalization;

public static class ToneDiag
{
    // Returns one line of diagnostics for a channel
    // fileIdx: 0..15 (file order), screenRow: 0..15 (grid row)
    public static string Row(int fileIdx, int screenRow,
        byte A0, byte A1, byte A2, byte A3, byte B0, byte B1, byte B2, byte B3,
        string txShown, string rxShown)
    {
        // 6-bit indices from the agreed windows
        int txIdx = (((B0 >> 4) & 1) << 5)
                  | (((B2 >> 2) & 1) << 4)
                  | (((B3 >> 3) & 1) << 3)
                  | (((B3 >> 2) & 1) << 2)
                  | (((B3 >> 1) & 1) << 1)
                  |  ((B3 >> 0) & 1);

        int rxIdx = (((A3 >> 6) & 1) << 5)
                  | (((A3 >> 7) & 1) << 4)
                  | (((A3 >> 0) & 1) << 3)
                  | (((A3 >> 1) & 1) << 2)
                  | (((A3 >> 2) & 1) << 1)
                  |  ((A3 >> 3) & 1);

        int bank = (B3 >> 1) & 1;

        // Expected labels from arrays (may be null for unknowns)
        string txExpect = X2212.Tones.ToneIndexing.TxCodeToTone[txIdx];
        string rxExpect = (bank == 0) ? X2212.Tones.ToneIndexing.RxCodeToTone_Bank0[rxIdx]
                                      : X2212.Tones.ToneIndexing.RxCodeToTone_Bank1[rxIdx];

        string bytes = $"{A0:X2} {A1:X2} {A2:X2} {A3:X2}  {B0:X2} {B1:X2} {B2:X2} {B3:X2}";
        return $"file[{fileIdx:00}]→row[{screenRow:00}]  idx:TX={txIdx:00} RX={rxIdx:00} bank={bank}  show(TX={txShown??\"<null>\"},RX={rxShown??\"<null>\"}) expect(TX={txExpect??\"<null>\"},RX={rxExpect??\"<null>\"})  bytes={bytes}";
    }
}
