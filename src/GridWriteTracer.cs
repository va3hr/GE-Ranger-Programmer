
// GridWriteTracer.cs - attaches GuardTextBoxColumn to chosen columns and logs value overwrites.
using System;
using System.Linq;
using System.Windows.Forms;

public static class GridWriteTracer
{
    public static void Attach(DataGridView grid, params string[] columnNames)
    {
        if (grid == null || columnNames == null || columnNames.Length == 0) return;
        LogSink.Write($"[{DateTime.Now:HH:mm:ss.fff}] Tracer attaching to columns: {string.Join(",", columnNames)}");

        foreach (var name in columnNames)
        {
            var col = grid.Columns[name];
            if (col == null) { LogSink.Write($"Column not found: {name}"); continue; }

            // Only replace text columns; if bound to combo, still useful for display cell.
            var idx = col.Index;
            var gcol = new GuardTextBoxColumn
            {
                Name = col.Name,
                HeaderText = col.HeaderText,
                DataPropertyName = col.DataPropertyName,
                Width = col.Width,
                ReadOnly = col.ReadOnly,
                DisplayIndex = col.DisplayIndex,
                Visible = col.Visible,
                AutoSizeMode = col.AutoSizeMode
            };

            grid.Columns.RemoveAt(idx);
            grid.Columns.Insert(idx, gcol);
            LogSink.Write($"Replaced column '{name}' with GuardTextBoxColumn at index {idx}.");
        }

        // Also log any formatting that rewrites display text
        grid.CellFormatting += (s, e) =>
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                var name = grid.Columns[e.ColumnIndex].Name;
                if (columnNames.Contains(name))
                {
                    LogSink.Write($"{DateTime.Now:HH:mm:ss.fff} CellFormatting r{e.RowIndex} c{e.ColumnIndex} ({name}) value='{e.Value ?? "null"}'");
                }
            }
        };
    }
}
