using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm
    {
        // HexGrid Event Handlers
        private void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (hexGrid == null) return;
            
            // Critical bounds checking to prevent crashes
            if (e.RowIndex < 0 || e.RowIndex >= hexGrid.Rows.Count) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= 8) return;
            
            try
            {
                SaveUndoState();
                
                var cell = hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell?.Value == null) return;
                
                string input = cell.Value.ToString() ?? "";
                
                if (byte.TryParse(input, NumberStyles.HexNumber, null, out byte val))
                {
                    int offset = e.RowIndex * 8 + e.ColumnIndex;
                    if (offset >= 0 && offset < _currentData.Length && _currentData[offset] != val)
                    {
                        _currentData[offset] = val;
                        _dataModified = true;
                        cell.Value = $"{val:X2}";
                        
                        UpdateAsciiForRow(e.RowIndex);
                        LogMessage($"Modified Ch{e.RowIndex + 1} byte {e.ColumnIndex}: {val:X2}");
                    }
                }
                else
                {
                    // Revert to original value
                    int offset = e.RowIndex * 8 + e.ColumnIndex;
                    if (offset >= 0 && offset < _currentData.Length)
                    {
                        cell.Value = $"{_currentData[offset]:X2}";
                        LogMessage("Invalid hex value - reverted");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Cell edit error: {ex.Message}");
            }
        }

        private void HexGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 8 && e.Value != null)
            {
                string val = e.Value.ToString() ?? "";
                if (val.Length == 1)
                    e.Value = "0" + val.ToUpper();
                else
                    e.Value = val.ToUpper();
            }
        }

        private void HexGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (hexGrid == null) return;
            
            if (hexGrid.SelectedRows.Count == 1)
            {
                int row = hexGrid.SelectedRows[0].Index;
                _currentChannel = row + 1;
                _lastSelectedRow = row;
                UpdateChannelDisplay();
            }
            
            hexGrid.Invalidate();
            
            int selectedCount = hexGrid.SelectedRows.Count;
            if (selectedCount > 1)
            {
                SetStatus($"{selectedCount} rows selected");
            }
            else if (selectedCount == 1)
            {
                SetStatus("Ready");
            }
        }

        private void HexGrid_MouseDown(object? sender, MouseEventArgs e)
        {
            if (hexGrid == null) return;
            
            var hitTest = hexGrid.HitTest(e.X, e.Y);
            if (hitTest.RowIndex >= 0)
            {
                if (Control.ModifierKeys == Keys.Shift && _lastSelectedRow >= 0)
                {
                    // Shift-click: select range
                    int start = Math.Min(_lastSelectedRow, hitTest.RowIndex);
                    int end = Math.Max(_lastSelectedRow, hitTest.RowIndex);
                    
                    hexGrid.ClearSelection();
                    for (int i = start; i <= end; i++)
                    {
                        hexGrid.Rows[i].Selected = true;
                    }
                    
                    LogMessage($"Selected rows {start + 1} to {end + 1} (Ch{start + 1}-Ch{end + 1})");
                    return;
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    // Ctrl-click: toggle individual row
                    hexGrid.Rows[hitTest.RowIndex].Selected = !hexGrid.Rows[hitTest.RowIndex].Selected;
                    LogMessage($"Toggled row {hitTest.RowIndex + 1} (Ch{hitTest.RowIndex + 1})");
                    return;
                }
                else
                {
                    // Normal click: single selection
                    _lastSelectedRow = hitTest.RowIndex;
                    _currentChannel = hitTest.RowIndex + 1;
                    UpdateChannelDisplay();
                }
            }
        }

        private void HexGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.C:
                        OnCopyRow(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.V:
                        OnPasteToSelected(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.Z:
                        OnUndo(sender, e);
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
