
// TonesBinding_WinForms.cs - utility to set DataGridView cells.
using System.Windows.Forms;

public static class TonesBinding_WinForms
{
    // Set TX/RX cells on a row.
    // txColumnName/rxColumnName: the column names for your grid (e.g., "TxTone", "RxTone").
    // If debugForceTxCh1 is true, TX of screen channel 1 is forced to "100.0" to prove the callsite.
    public static void FillRow(DataGridView grid, int rowIndex, byte[] image128, int screenCh1to16,
                               string txColumnName, string rxColumnName, bool debugForceTxCh1 = false)
    {
        var tones = debugForceTxCh1
            ? TxRx_FillHelper.GetDisplayTones_DebugTxCh1_1000(image128, screenCh1to16)
            : TxRx_FillHelper.GetDisplayTones(image128, screenCh1to16);

        grid.Rows[rowIndex].Cells[txColumnName].Value = tones.Tx;
        grid.Rows[rowIndex].Cells[rxColumnName].Value = tones.Rx;
    }
}
