
// GuardCells.cs - DataGridView cells/columns that intercept SetValue for TX/RX columns.
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
        LogSink.Write($"{DateTime.Now:HH:mm:ss.fff} TEXT SetValue r{rowIndex} c{this.ColumnIndex} ({colName}) '{prev ?? "null"}' -> '{value ?? "null"}'");
        var st = new StackTrace(1, true);
        LogSink.Write(st.ToString());
        TryTint(rowIndex);
        return base.SetValue(rowIndex, value);
    }

    private void TryTint(int rowIndex)
    {
        try
        {
            var grid = this.DataGridView;
            if (grid != null && rowIndex >= 0 && rowIndex < grid.Rows.Count)
            {
                var cell = grid.Rows[rowIndex].Cells[this.ColumnIndex];
                cell.Style.BackColor = Color.LightPink;
            }
        }
        catch { }
    }
}

public class GuardComboBoxCell : DataGridViewComboBoxCell
{
    protected override bool SetValue(int rowIndex, object value)
    {
        string colName = null;
        try { colName = this.DataGridView?.Columns[this.ColumnIndex]?.Name; } catch { }
        var prev = this.Value;
        LogSink.Write($"{DateTime.Now:HH:mm:ss.fff} COMBO SetValue r{rowIndex} c{this.ColumnIndex} ({colName}) '{prev ?? "null"}' -> '{value ?? "null"}'");
        var st = new StackTrace(1, true);
        LogSink.Write(st.ToString());
        TryTint(rowIndex);
        return base.SetValue(rowIndex, value);
    }

    private void TryTint(int rowIndex)
    {
        try
        {
            var grid = this.DataGridView;
            if (grid != null && rowIndex >= 0 && rowIndex < grid.Rows.Count)
            {
                var cell = grid.Rows[rowIndex].Cells[this.ColumnIndex];
                cell.Style.BackColor = Color.LightPink;
            }
        }
        catch { }
    }
}

public class GuardTextBoxColumn : DataGridViewTextBoxColumn
{
    public GuardTextBoxColumn() { this.CellTemplate = new GuardTextBoxCell(); }
}
public class GuardComboBoxColumn : DataGridViewComboBoxColumn
{
    public GuardComboBoxColumn() { this.CellTemplate = new GuardComboBoxCell(); }
}
