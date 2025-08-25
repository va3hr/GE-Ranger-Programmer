
// TonesBinding_WinForms.cs - convenience binder for DataGridView
using System.Windows.Forms;

public static class TonesBinding_WinForms
{
    // txColumnName/rxColumnName: your actual column names, e.g., "TxTone" and "RxTone"
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
