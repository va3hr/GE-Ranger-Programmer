// TonesBinding_WinForms.cs — simplified, single-writer binding (no debug, no helpers)
// Drop-in overwrite for your existing file. Keeps the same method signature so MainForm compiles
// without changes. Internally it no longer calls TxRx_FillHelper; it decodes *once* here.
//
// Pipeline:
//   screenCh (1..16) -> fileIdx -> A3..B0 bytes -> ToneLock.DecodeChannel(...) -> write grid cells.
//
// Frequency path remains untouched. No ASCII anywhere.

using System.Windows.Forms;
using RangrApp.Locked; // ToneLock

public static class TonesBinding_WinForms
{
    // DOS screen order (1..16) -> 0..15 file block index
    private static readonly int[] ScreenToFile = new int[]
    { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };

    // Signature preserved; debugForceTxCh1 is ignored so no tone gets forced.
    public static void FillRow(DataGridView grid, int rowIndex, byte[] image128, int screenCh1to16,
                               string txColumnName, string rxColumnName, bool debugForceTxCh1 = false)
    {
        int fileIdx = ScreenToFile[screenCh1to16 - 1];
        int off = fileIdx * 8;

        byte A3 = image128[off + 0];
        byte A2 = image128[off + 1];
        byte A1 = image128[off + 2];
        byte A0 = image128[off + 3];
        byte B3 = image128[off + 4];
        byte B2 = image128[off + 5];
        byte B1 = image128[off + 6];
        byte B0 = image128[off + 7];

        var (tx, rx) = ToneLock.DecodeChannel(A3, A2, A1, A0, B3, B2, B1, B0);

        grid.Rows[rowIndex].Cells[txColumnName].Value = tx;
        grid.Rows[rowIndex].Cells[rxColumnName].Value = rx;
    }
}