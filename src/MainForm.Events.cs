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

        private void HexGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (hexGrid == null) return;
            
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex < 8)
            {
                hexGrid.BeginEdit(false);
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
                bool isCurrentRow = (e.RowIndex == hexGrid.CurrentRow?.Index);
                
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

       // Replace the existing HexGrid_CellEndEdit method in MainForm.Events.cs with this version:

// In MainForm.Events.cs, modify the existing HexGrid_CellEndEdit method to use the channel mapping:
// Find the HexGrid_CellEndEdit method and update it to look like this:

private void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
{
    if (hexGrid == null) return;
    
    // CRITICAL FIX: Proper bounds checking to prevent crashes
    if (e.RowIndex < 0 || e.RowIndex >= hexGrid.Rows.Count) return;
    if (e.ColumnIndex < 0 || e.ColumnIndex >= 8) return; // Only 8 hex columns
    
    try
    {
        SaveUndoState();
        
        var cell = hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (cell?.Value == null) 
        {
            // If cell is null, restore original value
            int channel = e.RowIndex + 1;
            int startAddress = ChannelToAddress[channel];
            int dataOffset = GetDataOffsetForAddress(startAddress);
            int offset = dataOffset + e.ColumnIndex;
            if (offset >= 0 && offset < _currentData.Length)
            {
                cell.Value = $"{_currentData[offset]:X2}";
            }
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
                cell.Value = $"{val:X2}";
                
                // Defer the ASCII update to avoid reentrant calls
                BeginInvoke(new Action(() => UpdateAsciiForRow(e.RowIndex)));
            }
        }
        else
        {
            // Revert to original value
            int channel = e.RowIndex + 1;
            int startAddress = ChannelToAddress[channel];
            int dataOffset = GetDataOffsetForAddress(startAddress);
            int offset = dataOffset + e.ColumnIndex;
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
        // In case of any error, try to restore the original value
        try
        {
            int channel = e.RowIndex + 1;
            int startAddress = ChannelToAddress[channel];
            int dataOffset = GetDataOffsetForAddress(startAddress);
            int offset = dataOffset + e.ColumnIndex;
            if (offset >= 0 && offset < _currentData.Length)
            {
                // Use BeginInvoke to avoid reentrant call
                BeginInvoke(new Action(() =>
                {
                    if (hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex] != null)
                    {
                        hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = $"{_currentData[offset]:X2}";
                    }
                }));
            }
        }
        catch
        {
            // Silent fail on recovery attempt
        }
    }
}

// Also modify the UpdateAsciiForRow method to use the channel mapping:
private void UpdateAsciiForRow(int row)
{
    if (hexGrid?.Rows == null || row < 0 || row >= hexGrid.Rows.Count) return;
    
    try
    {
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
    catch
    {
        // Ignore ASCII update errors
    }
}

// Also replace UpdateAsciiForRow with this version that uses the channel mapping:
private void UpdateAsciiForRowFixed(int row)
{
    if (hexGrid?.Rows == null || row < 0 || row >= hexGrid.Rows.Count) return;
    
    try
    {
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
    catch
    {
        // Ignore ASCII update errors
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

        private void UpdateAsciiForRow(int row)
        {
            if (hexGrid?.Rows == null || row < 0 || row >= hexGrid.Rows.Count) return;
            
            try
            {
                StringBuilder ascii = new StringBuilder(8);
                for (int col = 0; col < 8; col++)
                {
                    byte val = _currentData[row * 8 + col];
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
            catch
            {
                // Ignore ASCII update errors
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

        // DIAGNOSTIC MESSAGE LOGGING
        private void LogMessage(string msg)
        {
            // FIRST: Write to console/debug so we can see what's being called
            Console.WriteLine($"LogMessage called: {msg}");
            
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(LogMessage), msg);
                    return;
                }

                // DIAGNOSTIC: Check if txtMessages exists and where it is
                if (txtMessages == null)
                {
                    Console.WriteLine("ERROR: txtMessages is NULL");
                    this.Text = "ERROR: txtMessages is NULL";
                    return;
                }

                if (txtMessages.IsDisposed)
                {
                    Console.WriteLine("ERROR: txtMessages is DISPOSED");
                    this.Text = "ERROR: txtMessages is DISPOSED";
                    return;
                }

                // DIAGNOSTIC: Log the control's position and size
                Console.WriteLine($"txtMessages Location: {txtMessages.Location}, Size: {txtMessages.Size}");
                Console.WriteLine($"txtMessages Dock: {txtMessages.Dock}, Visible: {txtMessages.Visible}");

                // FORCE VERY VISIBLE COLORS for testing
                txtMessages.BackColor = Color.Blue;   // BLUE background
                txtMessages.ForeColor = Color.Yellow; // YELLOW text
                
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logLine = $"[{timestamp}] {msg}\r\n";
                
                if (txtMessages.Text.Length > 50000)
                {
                    txtMessages.Clear();
                    txtMessages.AppendText("[Log cleared - too long]\r\n");
                }
                
                txtMessages.AppendText(logLine);
                txtMessages.SelectionStart = txtMessages.Text.Length;
                txtMessages.ScrollToCaret();
                txtMessages.Update();
                txtMessages.Refresh();
                txtMessages.BringToFront(); // Force it to front
                Application.DoEvents();

                // Update window title to show we tried to log
                this.Text = $"X2212 - Logged: {msg.Substring(0, Math.Min(20, msg.Length))}...";
                
                Console.WriteLine($"Successfully wrote to txtMessages: {logLine.Trim()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOGGING ERROR: {ex.Message}");
                this.Text = $"X2212 - Log Error: {ex.Message}";
            }
        }

        private void SetStatus(string status)
        {
            if (statusLabel != null && !statusLabel.IsDisposed)
                statusLabel.Text = status;
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



