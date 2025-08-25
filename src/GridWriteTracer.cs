
// GridWriteTracer.cs - attaches guard columns and logs formatting/dirty-state events.
using System;
using System.Linq;
using System.Windows.Forms;

public static class GridWriteTracer
{
    public static void AttachSmart(DataGridView grid, params string[] columnNames)
    {
        if (grid == null || columnNames == null || columnNames.Length == 0) return;
        LogSink.Write($"[{DateTime.Now:HH:mm:ss.fff}] AttachSmart requested for: {string.Join(",", columnNames)}");

        void attachNow()
        {
            try
            {
                foreach (var name in columnNames)
                {
                    var col = grid.Columns[name];
                    if (col == null) { LogSink.Write($"Column not found (yet): {name}"); continue; }
                    var idx = col.Index;

                    DataGridViewColumn replacement;
                    if (col is DataGridViewComboBoxColumn combo)
                    {
                        replacement = new GuardComboBoxColumn
                        {
                            Name = col.Name,
                            HeaderText = col.HeaderText,
                            DataPropertyName = col.DataPropertyName,
                            Width = col.Width,
                            ReadOnly = col.ReadOnly,
                            DisplayIndex = col.DisplayIndex,
                            Visible = col.Visible,
                            AutoSizeMode = col.AutoSizeMode,
                            FlatStyle = combo.FlatStyle
                        };
                        // Copy items/data source if present
                        ((GuardComboBoxColumn)replacement).DisplayMember = combo.DisplayMember;
                        ((GuardComboBoxColumn)replacement).ValueMember = combo.ValueMember;
                        ((GuardComboBoxColumn)replacement).DataSource = combo.DataSource;
                        foreach (var item in combo.Items) ((GuardComboBoxColumn)replacement).Items.Add(item);
                    }
                    else
                    {
                        replacement = new GuardTextBoxColumn
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
                    }

                    grid.Columns.RemoveAt(idx);
                    grid.Columns.Insert(idx, replacement);
                    LogSink.Write($"Replaced column '{name}' with guarded column at index {idx}.");
                }

                // Log key events that often rewrite cell values
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
                grid.CellValueChanged += (s, e) =>
                {
                    if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
                    {
                        var name = grid.Columns[e.ColumnIndex].Name;
                        if (columnNames.Contains(name))
                        {
                            var val = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                            LogSink.Write($"{DateTime.Now:HH:mm:ss.fff} CellValueChanged r{e.RowIndex} c{e.ColumnIndex} ({name}) now='{val ?? "null"}'");
                        }
                    }
                };
                grid.CurrentCellDirtyStateChanged += (s, e) =>
                {
                    var cell = grid.CurrentCell;
                    if (cell != null && columnNames.Contains(grid.Columns[cell.ColumnIndex].Name))
                    {
                        LogSink.Write($"{DateTime.Now:HH:mm:ss.fff} CurrentCellDirtyStateChanged r{cell.RowIndex} c{cell.ColumnIndex} ({grid.Columns[cell.ColumnIndex].Name}) dirty={grid.IsCurrentCellDirty}");
                    }
                };

                LogSink.Write("AttachSmart completed.");
            }
            catch (Exception ex)
            {
                LogSink.Write("AttachSmart error: " + ex);
            }
        }

        // If columns not created yet, attach after handle creation or data binding completes
        if (grid.Columns.Count == 0)
        {
            LogSink.Write("Grid has 0 columns; deferring attach until DataBindingComplete.");
            grid.DataBindingComplete += (s, e) => { LogSink.Write("DataBindingComplete fired. Attaching now."); attachNow(); };
        }
        else
        {
            attachNow();
        }
    }
}
