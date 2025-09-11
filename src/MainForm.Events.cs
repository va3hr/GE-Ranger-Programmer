using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        // Event handlers
        private void HexGrid_MouseDown(object? sender, MouseEventArgs e)
        {
            if (hexGrid == null) return;
            
            // CRITICAL FIX: Force commit any pending edits BEFORE processing mouse events
            if (hexGrid.IsCurrentCellInEditMode)
            {
                try
                {
                    hexGrid.EndEdit();
                    Application.DoEvents(); // Allow edit to complete
                }
                catch (Exception ex)
                {
                    LogMessage($"Edit commit error in MouseDown: {ex.Message}");
                    return; // Don't proceed if edit failed
                }
            }
            
            var hitTest = hexGrid.HitTest(e.X, e.Y);
            if (hitTest.RowIndex >= 0)
            {
                if (Control.ModifierKeys == Keys.Shift && _lastSelectedRow >= 0)
                {
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
                    hexGrid.Rows[hitTest.RowIndex].Selected = !hexGrid.Rows[hitTest.RowIndex].Selected;
                    LogMessage($"Toggled row {hitTest.RowIndex + 1} (Ch{hitTest.RowIndex + 1})");
                    return;
                }
                else
                {
                    _lastSelectedRow = hitTest.RowIndex;
                    _currentChannel = hitTest.RowIndex + 1;
                    UpdateChannelDisplay();
                }
            }
        }

        private void HexGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (hexGrid == null) return;
            
            // CRITICAL FIX: Force commit any pending edits BEFORE changing selection
            if (hexGrid.IsCurrentCellInEditMode)
            {
                try
                {
                    hexGrid.EndEdit();
                    // Give the edit time to complete
                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    LogMessage($"Edit commit error in SelectionChanged: {ex.Message}");
                    // Continue with selection change even if edit failed
                }
            }
            
            if (hexGrid.SelectedRows.Count == 1)
            {
                var selectedRow = hexGrid.SelectedRows[0];
                if (selectedRow != null)
                {
                    int row = selectedRow.Index;
                    _currentChannel = row + 1;
                    _lastSelectedRow = row;
                    UpdateChannelDisplay();
                }
            }
            
            // MOVED: Invalidate after all processing is complete
            try
            {
                hexGrid.Invalidate();
            }
            catch
            {
                // Ignore invalidate errors
            }
            
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

        private void HexGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (hexGrid == null) return;
            
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex < 8)
            {
                try
                {
                    hexGrid.BeginEdit(false);
                }
                catch (Exception ex)
                {
                    LogMessage($"BeginEdit error: {ex.Message}");
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

        private void HexGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (hexGrid == null) return;
            
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex < 8)
            {
                bool isCurrentRow = (hexGrid.CurrentRow != null && e.RowIndex == hexGrid.CurrentRow.Index);
                
                if (isCurrentRow)
                {
                    e.Graphics.FillRectangle(Brushes.White, e.CellBounds);
                    
                    Color textColor = e.ColumnIndex < 4 ? Color.Red : Color.Blue;
                    
                    if (e.Value != null)
                    {
                        using (var brush = new SolidBrush(textColor))
                        {
                            var textRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, 
                                                        e.CellBounds.Width - 4, e.CellBounds.Height - 4);
                            e.Graphics.DrawString(e.Value.ToString() ?? "", e.CellStyle.Font, brush, textRect,
                                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                        }
                    }
                    
                    e.Graphics.DrawRectangle(Pens.DarkGray, e.CellBounds);
                    e.Handled = true;
                }
            }
        }

        private void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (hexGrid == null) return;
            
            // CRITICAL FIX: Proper bounds checking to prevent crashes
            if (e.RowIndex < 0 || e.RowIndex >= hexGrid.Rows.Count) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= 8) return; // Only 8 hex columns
            
            try
            {
                SaveUndoState();
                
                var row = hexGrid.Rows[e.RowIndex];
                if (row == null || row.IsNewRow) return;
                
                var cell = row.Cells[e.ColumnIndex];
                if (cell?.Value == null) 
                {
                    // If cell is null, restore original value
                    RestoreCellValue(e.RowIndex, e.ColumnIndex);
                    return;
                }
                
                string input = cell.Value.ToString() ?? "";
                
                // Parse the hex value
                if (byte.TryParse(input, NumberStyles.HexNumber, null, out byte val))
                {
                    // CRITICAL: Use the channel mapping to find the correct data offset
                    int channel = e.RowIndex + 1;  // Channel 1-16
                    int startAddress = ChannelToAddress[channel];
                    
                    // Calculate the offset in our 128-byte array using the mapping function
                    int dataOffset = GetDataOffsetForAddress(startAddress);
                    int finalOffset = dataOffset + e.ColumnIndex;
                    
                    // Ensure we stay within bounds
                    if (finalOffset >= 128) finalOffset = finalOffset % 128;
                    
                    if (finalOffset >= 0 && finalOffset < _currentData.Length)
                    {
                        if (_currentData[finalOffset] != val)
                        {
                            _currentData[finalOffset] = val;
                            _dataModified = true;
                            LogMessage($"Modified Ch{channel} byte {e.ColumnIndex} (address {(startAddress + e.ColumnIndex):X2}): {val:X2}");
                        }
                        
                        // FIXED: Update the cell value directly (no BeginInvoke)
                        cell.Value = $"{val:X2}";
                        
                        // FIXED: Update ASCII directly (no BeginInvoke)
                        UpdateAsciiForRow(e.RowIndex);
                    }
                }
                else
                {
                    // Revert to original value
                    RestoreCellValue(e.RowIndex, e.ColumnIndex);
                    LogMessage("Invalid hex value - reverted");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Cell edit error: {ex.Message}");
                // In case of any error, try to restore the original value
                try
                {
                    RestoreCellValue(e.RowIndex, e.ColumnIndex);
                }
                catch
                {
                    // Silent fail on recovery attempt
                }
            }
        }

        private void HexGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (hexGrid == null) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= 8) return;
            
            string input = e.FormattedValue?.ToString() ?? "";
            
            // Allow empty values (will be handled in CellEndEdit)
            if (string.IsNullOrEmpty(input)) return;
            
            // Validate hex input
            if (!byte.TryParse(input, NumberStyles.HexNumber, null, out _))
            {
                e.Cancel = true;
                LogMessage("Invalid hex value - must be 00-FF");
                
                // Show error message to user
                MessageBox.Show("Please enter a valid hex value (00-FF)", "Invalid Input", 
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // FIXED: Helper method to restore cell value (removed BeginInvoke)
        private void RestoreCellValue(int rowIndex, int columnIndex)
        {
            if (hexGrid == null) return;
            if (rowIndex < 0 || rowIndex >= hexGrid.Rows.Count) return;
            if (columnIndex < 0 || columnIndex >= 8) return;
            
            var row = hexGrid.Rows[rowIndex];
            if (row == null || row.IsNewRow) return;
            
            try
            {
                int channel = rowIndex + 1;
                int startAddress = ChannelToAddress[channel];
                int dataOffset = GetDataOffsetForAddress(startAddress);
                int offset = dataOffset + columnIndex;
                
                if (offset >= 0 && offset < _currentData.Length)
                {
                    // FIXED: Direct update without BeginInvoke
                    var cell = hexGrid.Rows[rowIndex].Cells[columnIndex];
                    if (cell != null)
                    {
                        cell.Value = $"{_currentData[offset]:X2}";
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error restoring cell value: {ex.Message}");
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

        // FIXED: UpdateAsciiForRow (removed try-catch that was hiding errors)
        private void UpdateAsciiForRow(int row)
        {
            if (hexGrid?.Rows == null || row < 0 || row >= hexGrid.Rows.Count) return;
            
            int channel = row + 1;  // Channel 1-16
            int startAddress = ChannelToAddress[channel];
            int dataOffset = GetDataOffsetForAddress(startAddress);
            
            StringBuilder ascii = new StringBuilder(8);
            for (int col = 0; col < 8; col++)
            {
                int finalOffset = dataOffset + col;
                if (finalOffset >= 128) finalOffset = finalOffset % 128;
                
                byte val = _currentData[finalOffset];
                char c = (val >= 32 && val <= 126) ? (char)val : '.';
                ascii.Append(c);
            }
            
            var targetRow = hexGrid.Rows[row];
            if (targetRow?.Cells != null && targetRow.Cells.Count > 8)
            {
                var asciiCell = targetRow.Cells[targetRow.Cells.Count - 1];
                if (asciiCell != null)
                    asciiCell.Value = ascii.ToString();
            }
        }

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

        // IMPROVED: Message logging with better error handling
        private void LogMessage(string msg)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(LogMessage), msg);
                    return;
                }

                if (txtMessages == null || txtMessages.IsDisposed)
                {
                    // Fallback to console if txtMessages is not available
                    Console.WriteLine($"[LOG] {msg}");
                    return;
                }

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logLine = $"[{timestamp}] {msg}\r\n";
                
                // Clear log if it gets too long
                if (txtMessages.Text.Length > 50000)
                {
                    txtMessages.Clear();
                    txtMessages.AppendText("[Log cleared - too long]\r\n");
                }
                
                txtMessages.AppendText(logLine);
                txtMessages.SelectionStart = txtMessages.Text.Length;
                txtMessages.ScrollToCaret();
            }
            catch (Exception ex)
            {
                // Fallback to console if logging fails
                Console.WriteLine($"[LOG ERROR] {ex.Message}: {msg}");
            }
        }

        private void SetStatus(string status)
        {
            try
            {
                if (statusLabel != null && !statusLabel.IsDisposed)
                    statusLabel.Text = status;
            }
            catch
            {
                // Ignore status update errors
            }
        }

        // Edit menu operations
        private void OnExit(object? sender, EventArgs e)
        {
            if (_dataModified)
            {
                DialogResult result = MessageBox.Show(
                    "Data has been modified. Do you want to save before exiting?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        OnFileSaveAs(sender, e);
                        if (_dataModified) return;
                        break;
                    case DialogResult.Cancel:
                        return;
                    case DialogResult.No:
                        break;
                }
            }
            Close();
        }

        private void OnCopyRow(object? sender, EventArgs e)
        {
            if (hexGrid?.CurrentRow == null) return;

            int sourceRow = hexGrid.CurrentRow.Index;
            int channel = sourceRow + 1;
            int startAddress = ChannelToAddress[channel];
            int dataOffset = GetDataOffsetForAddress(startAddress);
            
            for (int i = 0; i < 8; i++)
            {
                int finalOffset = dataOffset + i;
                if (finalOffset >= 128) finalOffset = finalOffset % 128;
                _clipboardRow[i] = _currentData[finalOffset];
            }
            LogMessage($"Copied Ch{channel} to clipboard");
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
                if (row == null) continue;
                
                int targetRow = row.Index;
                int channel = targetRow + 1;
                int startAddress = ChannelToAddress[channel];
                int dataOffset = GetDataOffsetForAddress(startAddress);
                
                for (int i = 0; i < 8; i++)
                {
                    int finalOffset = dataOffset + i;
                    if (finalOffset >= 128) finalOffset = finalOffset % 128;
                    _currentData[finalOffset] = _clipboardRow[i];
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
            
            // Force end edit before clearing selection
            if (hexGrid.IsCurrentCellInEditMode)
            {
                try
                {
                    hexGrid.EndEdit();
                }
                catch
                {
                    // Ignore edit errors when clearing selection
                }
            }
            
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
                    int channel = row + 1;
                    int startAddress = ChannelToAddress[channel];
                    int dataOffset = GetDataOffsetForAddress(startAddress);
                    
                    for (int i = 0; i < 8; i++)
                    {
                        int finalOffset = dataOffset + i;
                        if (finalOffset >= 128) finalOffset = finalOffset % 128;
                        _currentData[finalOffset] = _clipboardRow[i];
                    }
                }
                
                _dataModified = true;
                UpdateHexDisplay();
                LogMessage("Filled all 16 rows with clipboard data");
            }
        }

        private bool CheckForUnsavedChanges()
        {
            if (_dataModified)
            {
                DialogResult result = MessageBox.Show(
                    "Data has been modified. Do you want to save before loading a new file?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        OnFileSaveAs(this, EventArgs.Empty);
                        return !_dataModified;
                    case DialogResult.Cancel:
                        return false;
                    case DialogResult.No:
                        return true;
                }
            }
            return true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dataModified)
            {
                DialogResult result = MessageBox.Show(
                    "Data has been modified. Do you want to save before exiting?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        OnFileSaveAs(this, EventArgs.Empty);
                        if (_dataModified)
                        {
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                    case DialogResult.No:
                        break;
                }
            }

            SaveSettings();
            base.OnFormClosing(e);
        }
    }
}
