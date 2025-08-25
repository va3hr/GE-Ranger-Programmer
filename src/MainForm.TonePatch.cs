
// MainForm.TonePatch.cs — drop-in partial class to write TX/RX tones to the UI.
// Safe: no frequency changes; uses existing ToneLock big-endian decoding.
// Assumes your form class is 'public partial class MainForm : Form' and you have a DataGridView.

using System.Windows.Forms;

public partial class MainForm : Form
{
    // Toggle this to visually prove the callsite is feeding the UI (CH1 TX shows "100.0").
    private const bool FORCE_TX_CH1 = true; // set to false after you confirm the UI path

    // Overload that uses your grid field directly (rename if your grid is not 'gridChannels').
    public void PatchApplyTones(byte[] image128)
    {
        // Change these to your exact column names if different:
        const string TxCol = "TxTone"; // e.g., "TxTone" or "Tx Tone"
        const string RxCol = "RxTone"; // e.g., "RxTone" or "Rx Tone"

        for (int row = 0; row < 16; row++)
        {
            int screenCh = row + 1;
            // If your grid has fewer/more than 16 rows, guard accordingly
            if (row >= gridChannels.Rows.Count) break;

            TonesBinding_WinForms.FillRow(gridChannels, row, image128, screenCh,
                                          TxCol, RxCol,
                                          debugForceTxCh1: (FORCE_TX_CH1 && screenCh == 1));
        }
    }

    // Variant if you prefer to pass the grid explicitly and choose column names at the callsite.
    public void PatchApplyTones(DataGridView grid, byte[] image128, string txColumnName, string rxColumnName)
    {
        for (int row = 0; row < grid.Rows.Count && row < 16; row++)
        {
            int screenCh = row + 1;
            TonesBinding_WinForms.FillRow(grid, row, image128, screenCh,
                                          txColumnName, rxColumnName,
                                          debugForceTxCh1: (FORCE_TX_CH1 && screenCh == 1));
        }
    }
}
