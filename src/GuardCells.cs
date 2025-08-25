
// GuardCells.cs - DataGridView cell/column that intercepts value writes to TX/RX columns.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

public class GuardTextBoxCell : DataGridViewTextBoxCell
{
    protected override bool SetValue(int rowIndex, object value)
    {
        string colName = null;
        try { colName = this.DataGridView?.Columns[this.ColumnIndex]?.Name; } catch { }
        var prev = this.Value;
        string line1 = $"{DateTime.Now:HH:mm:ss.fff} SetValue r{rowIndex} c{this.ColumnIndex} ({colName}) '{prev ?? "null"}' -> '{value ?? "null"}'";
        LogSink.Write(line1);

        // Capture a short stack (skip 1 frame for this method)
        var st = new StackTrace(1, true);
        LogSink.Write(st.ToString());

        // Visual marker so you can see when a cell was written.
        try
        {
            var grid = this.DataGridView;
            if (grid != null && rowIndex >= 0 && rowIndex < grid.Rows.Count)
            {
                var cell = grid.Rows[rowIndex].Cells[this.ColumnIndex];
                cell.Style.BackColor = Color.LightPink;
            }
        }
        catch { /* non-fatal */ }

        return base.SetValue(rowIndex, value);
    }
}

public class GuardTextBoxColumn : DataGridViewTextBoxColumn
{
    public GuardTextBoxColumn()
    {
        this.CellTemplate = new GuardTextBoxCell();
    }
}
