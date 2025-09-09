using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm
    {
        // Data Display Methods
        private void UpdateHexDisplay()
        {
            if (hexGrid?.Rows == null) return;
            
            try
            {
                for (int channel = 0; channel < 16 && channel < hexGrid.Rows.Count; channel++)
                {
                    var row = hexGrid.Rows[channel];
                    if (row?.Cells == null) continue;
                    
                    StringBuilder ascii = new StringBuilder(8);
                    for (int byteIndex = 0; byteIndex < 8 && byteIndex < row.Cells.Count - 1; byteIndex++) // -1 for ASCII column
                    {
                        int offset = channel * 8 + byteIndex;
                        if (offset >= 0 && offset < _currentData.Length)
                        {
                            byte val = _currentData[offset];
                            
                            if (row.Cells[byteIndex] != null)
                            {
                                row.Cells[byteIndex].Value = $"{val:X2}";
                            }
                            
                            char c = (val >= 32 && val <= 126) ? (char)val : '.';
                            ascii.Append(c);
                        }
                    }
                    
                    // Update ASCII column (last column)
                    try
                    {
                        if (row.Cells.Count > 8)
                        {
                            var asciiCell = row.Cells[8]; // ASCII is column 8 (0-7 are hex bytes)
                            if (asciiCell != null)
                                asciiCell.Value = ascii.ToString();
                        }
                    }
                    catch
                    {
                        // Skip ASCII update if it fails
                    }
                }
            }
            catch
            {
                // Silent fail for display updates
            }
        }

        private void UpdateAsciiForRow(int row)
        {
            if (hexGrid?.Rows == null || row < 0 || row >= hexGrid.Rows.Count) return;
            
            try
            {
                var targetRow = hexGrid.Rows[row];
                if (targetRow?.Cells == null || targetRow.Cells.Count <= 8) return;
                
                StringBuilder ascii = new StringBuilder(8);
                for (int col = 0; col < 8; col++)
                {
                    int offset = row * 8 + col;
                    if (offset >= 0 && offset < _currentData.Length)
                    {
                        byte val = _currentData[offset];
                        char c = (val >= 32 && val <= 126) ? (char)val : '.';
                        ascii.Append(c);
                    }
                }
                
                var asciiCell = targetRow.Cells[8]; // ASCII column
                if (asciiCell != null)
                    asciiCell.Value = ascii.ToString();
            }
            catch
            {
                // Silent fail for ASCII updates
            }
        }

        // Undo Functionality
        private void SaveUndoState()
        {
            Array.Copy(_currentData, _undoData, 128);
        }

        private void OnUndo(object? sender, EventArgs e)
        {
            Array.Copy(_undoData, _currentData, 128);
            UpdateHexDisplay();
            _dataModified = true;
            LogMessage("Undo performed");
        }

        // Copy/Paste Operations
        private void OnCopyRow(object? sender, EventArgs e)
        {
            if (hexGrid?.CurrentRow == null) return;

            int sourceRow = hexGrid.CurrentRow.Index;
            for (int i = 0; i < 8; i++)
            {
                _clipboardRow[i] = _currentData[sourceRow * 8 + i];
            }
            LogMessage($"Copied Ch{sourceRow + 1} to clipboard");
        }

        private void OnPasteToSelected(object? sender, EventArgs e)
        {
            if (hexGrid == null) return;
            
            var selectedRows = hexGrid.SelectedRows;
            if (selectedRows.Count == 0) return;

            SaveUndoState();
            
            int count = 0;
            foreach (DataGridViewRow row in selectedRows)
            {
                int targetRow = row.Index;
                for (int i = 0; i < 8; i++)
                {
                    _currentData[targetRow * 8 + i] = _clipboardRow[i];
                }
                count++;
            }
            
            _dataModified = true;
            UpdateHexDisplay();
            LogMessage($"Pasted clipboard to {count} selected rows");
        }

        private void OnClearSelection(object? sender, EventArgs e)
        {
            if (hexGrid == null) return;
            
            hexGrid.ClearSelection();
            if (hexGrid.Rows.Count > 0)
            {
                hexGrid.Rows[0].Selected = true;
                _currentChannel = 1;
                _lastSelectedRow = 0;
                UpdateChannelDisplay();
            }
            LogMessage("Selection cleared");
        }

        private void OnFillAll(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Fill all 16 rows with copied data?", "Confirm Fill All", 
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SaveUndoState();
                
                for (int row = 0; row < 16; row++)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        _currentData[row * 8 + i] = _clipboardRow[i];
                    }
                }
                
                _dataModified = true;
                UpdateHexDisplay();
                LogMessage("Filled all 16 rows with clipboard data");
            }
        }

        // Data Validation and Formatting
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

        private void HighlightCurrentRow(int rowIndex)
        {
            if (hexGrid?.Rows == null || rowIndex < 0 || rowIndex >= hexGrid.Rows.Count)
                return;

            try
            {
                var row = hexGrid.Rows[rowIndex];
                
                // Highlight first 4 bytes in red, second 4 bytes in blue
                for (int col = 0; col < 8 && col < row.Cells.Count; col++)
                {
                    var cell = row.Cells[col];
                    if (cell != null)
                    {
                        if (col < 4)
                        {
                            // First 4 bytes (nibbles 0-3) in red
                            cell.Style.ForeColor = Color.Red;
                        }
                        else
                        {
                            // Second 4 bytes (nibbles 4-7) in blue
                            cell.Style.ForeColor = Color.Blue;
                        }
                    }
                }
            }
            catch
            {
                // Silent fail for highlighting
            }
        }

        private void ClearRowHighlighting()
        {
            if (hexGrid?.Rows == null) return;

            try
            {
                foreach (DataGridViewRow row in hexGrid.Rows)
                {
                    for (int col = 0; col < 8 && col < row.Cells.Count; col++)
                    {
                        var cell = row.Cells[col];
                        if (cell != null)
                        {
                            cell.Style.ForeColor = Color.Lime; // Default color
                        }
                    }
                }
            }
            catch
            {
                // Silent fail for clearing highlights
            }
        }
    }
}
