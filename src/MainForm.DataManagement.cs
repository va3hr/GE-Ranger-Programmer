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
                    for (int byteIndex = 0; byteIndex < 8 && byteIndex < row.Cells.Count; byteIndex++)
                    {
                        int offset = channel * 8 + byteIndex;
                        byte val = _currentData[offset];
                        
                        if (row.Cells[byteIndex] != null)
                        {
                            row.Cells[byteIndex].Value = $"{val:X2}";
                        }
                        
                        char c = (val >= 32 && val <= 126) ? (char)val : '.';
                        ascii.Append(c);
                    }
                    
                    // Update ASCII column
                    try
                    {
                        if (row.Cells.Count > 8)
                        {
                            var asciiCell = row.Cells[row.Cells.Count - 1];
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
                
                var asciiCell = targetRow.Cells[targetRow.Cells.Count - 1];
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
    }
}
