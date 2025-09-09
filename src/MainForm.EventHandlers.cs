using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm
    {
        // HexGrid Event Handlers
        partial void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (hexGrid == null) return;
            
            // Critical bounds checking to prevent crashes
            if (e.RowIndex < 0 || e.RowIndex >= hexGrid.Rows.Count) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= 8) return; // Only 8 hex byte columns
            
            try
            {
                SaveUndoState();
                
                var cell = hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell?.Value == null) return;
                
                string input = cell.Value.ToString() ?? "";
                
                if (IsValidHexByte(input, out byte val))
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

        partial void HexGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // Only format hex columns (0-7), not ASCII column (8)
            if (e.ColumnIndex < 8 && e.Value != null)
            {
                string val = e.Value.ToString() ?? "";
                if (val.Length == 1)
                    e.Value = "0" + val.ToUpper();
                else
                    e.Value = val.ToUpper();
            }
        }

        partial void HexGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (hexGrid == null) return;
            
            ClearRowHighlighting(); // Clear previous highlighting
            
            if (hexGrid.SelectedRows.Count == 1)
            {
                int row = hexGrid.SelectedRows[0].Index;
                _currentChannel = row + 1;
                _lastSelectedRow = row;
                UpdateChannelDisplay();
                
                // Highlight the selected row with color coding
                HighlightCurrentRow(row);
            }
            
            hexGrid.Invalidate();
            
            int selectedCount = hexGrid.SelectedRows.Count;
            if (selectedCount > 1)
            {
                SetStatus($"{selectedCount} rows selected");
            }
            else if (selectedCount == 1)
            {
                SetStatus($"Ch{_currentChannel} selected");
            }
            else
            {
                SetStatus("Ready");
            }
        }

        partial void HexGrid_MouseDown(object? sender, MouseEventArgs e)
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

        partial void HexGrid_KeyDown(object? sender, KeyEventArgs e)
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
                    case Keys.A:
                        // Select all rows
                        if (hexGrid != null)
                        {
                            hexGrid.SelectAll();
                            LogMessage("Selected all rows");
                        }
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Delete:
                        // Clear selected cells
                        ClearSelectedCells();
                        e.Handled = true;
                        break;
                    case Keys.F5:
                        // Refresh display
                        UpdateHexDisplay();
                        LogMessage("Display refreshed");
                        e.Handled = true;
                        break;
                }
            }
        }

        // Helper Methods
        private void ClearSelectedCells()
        {
            if (hexGrid?.SelectedCells == null) return;

            try
            {
                SaveUndoState();
                bool dataChanged = false;

                foreach (DataGridViewCell cell in hexGrid.SelectedCells)
                {
                    if (cell.ColumnIndex < 8) // Only clear hex columns, not ASCII
                    {
                        int offset = cell.RowIndex * 8 + cell.ColumnIndex;
                        if (offset >= 0 && offset < _currentData.Length)
                        {
                            _currentData[offset] = 0x00;
                            dataChanged = true;
                        }
                    }
                }

                if (dataChanged)
                {
                    _dataModified = true;
                    UpdateHexDisplay();
                    LogMessage("Cleared selected cells");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error clearing cells: {ex.Message}");
            }
        }

        private void SelectEntireRow(int rowIndex)
        {
            if (hexGrid?.Rows == null || rowIndex < 0 || rowIndex >= hexGrid.Rows.Count)
                return;

            try
            {
                hexGrid.ClearSelection();
                hexGrid.Rows[rowIndex].Selected = true;
                _currentChannel = rowIndex + 1;
                _lastSelectedRow = rowIndex;
                UpdateChannelDisplay();
            }
            catch (Exception ex)
            {
                LogMessage($"Error selecting row: {ex.Message}");
            }
        }

        // Data validation method (used by DataManagement)
        private bool IsValidHexByte(string input, out byte value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim().ToUpper();
            
            // Remove 0x prefix if present
            if (input.StartsWith("0X"))
                input = input.Substring(2);

            // Must be 1 or 2 hex digits
            if (input.Length == 0 || input.Length > 2)
                return false;

            // Check if all characters are valid hex
            foreach (char c in input)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F')))
                    return false;
            }

            return byte.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out value);
        }
    }
}
