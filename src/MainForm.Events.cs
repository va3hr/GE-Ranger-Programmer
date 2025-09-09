// MainForm.Events.cs - All Event Handlers
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm
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

        private void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (hexGrid == null) return;
            if (e.ColumnIndex >= 8) return;
            
            SaveUndoState();
            
            var cell = hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string input = cell.Value?.ToString() ?? "";
            
            if (byte.TryParse(input, NumberStyles.HexNumber, null, out byte val))
            {
                int offset = e.RowIndex * 8 + e.ColumnIndex;
                if (_currentData[offset] != val)
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
                int offset = e.RowIndex * 8 + e.ColumnIndex;
                cell.Value = $"{_currentData[offset]:X2}";
                LogMessage("Invalid hex value - reverted");
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

        // File Operations Menu Events
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

        private void OnUndo(object? sender, EventArgs e)
        {
            Array.Copy(_undoData, _currentData, 128);
            UpdateHexDisplay();
            _dataModified = true;
            LogMessage("Undo performed");
        }

        private void OnFileOpen(object? sender, EventArgs e)
        {
            if (!CheckForUnsavedChanges()) return;
                
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open .RGR File";
                dlg.Filter = "RGR Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dlg.InitialDirectory = !string.IsNullOrEmpty(_lastFolderPath) ? _lastFolderPath :
                                       Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string content = File.ReadAllText(dlg.FileName);
                        _currentData = ParseHexFile(content);
                        _dataModified = false;
                        SaveUndoState();
                        UpdateHexDisplay();
                        _lastFilePath = dlg.FileName;
                        
                        string? folderPath = Path.GetDirectoryName(dlg.FileName);
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            _lastFolderPath = folderPath;
                            SaveSettings();
                        }
                        
                        LogMessage($"Loaded file: {Path.GetFileName(dlg.FileName)}");
                        SetStatus("File loaded");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error loading file: {ex.Message}");
                        MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnFileSaveAs(object? sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save .RGR File";
                dlg.Filter = "RGR Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dlg.InitialDirectory = !string.IsNullOrEmpty(_lastFolderPath) ? _lastFolderPath :
                                       Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string hexContent = BytesToHexString(_currentData);
                        File.WriteAllText(dlg.FileName, hexContent);
                        _lastFilePath = dlg.FileName;
                        
                        string? folderPath = Path.GetDirectoryName(dlg.FileName);
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            _lastFolderPath = folderPath;
                            SaveSettings();
                        }
                        
                        _dataModified = false;
                        LogMessage($"Saved file: {Path.GetFileName(dlg.FileName)}");
                        SetStatus("File saved");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error saving file: {ex.Message}");
                        MessageBox.Show($"Error saving file: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Device Operations Menu Events
        private void OnDeviceRead(object? sender, EventArgs e)
        {
            if (!CheckForUnsavedChanges()) return;
                
            try
            {
                LogMessage("Reading from X2212 device...");
                var nibbles = X2212Io.ReadAllNibbles(_lptBaseAddress, LogMessage);
                _currentData = X2212Io.CompressNibblesToBytes(nibbles);
                _dataModified = false;
                SaveUndoState();
                UpdateHexDisplay();
                LogMessage("Read operation completed - 128 bytes received");
                SetStatus("Read from device");
            }
            catch (Exception ex)
            {
                LogMessage($"Read operation failed: {ex.Message}");
                MessageBox.Show($"Read failed: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceWrite(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Write current data to X2212 device?", "Confirm Write", 
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                LogMessage("Writing to X2212 device...");
                var nibbles = X2212Io.ExpandToNibbles(_currentData);
                X2212Io.ProgramNibbles(_lptBaseAddress, nibbles, LogMessage);
                LogMessage("Write operation completed");
                SetStatus("Written to device");
            }
            catch (Exception ex)
            {
                LogMessage($"Write operation failed: {ex.Message}");
                MessageBox.Show($"Write failed: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceVerify(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Verifying device data...");
                var nibbles = X2212Io.ExpandToNibbles(_currentData);
                bool ok = X2212Io.VerifyNibbles(_lptBaseAddress, nibbles, out int failIndex, LogMessage);
                
                if (ok)
                {
                    LogMessage("Verify operation completed - all 256 nibbles match");
                    SetStatus("Verify OK");
                }
                else
                {
                    LogMessage($"Verify operation failed at nibble {failIndex}");
                    SetStatus($"Verify failed at {failIndex}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Verify operation failed: {ex.Message}");
                MessageBox.Show($"Verify failed: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceStore(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Sending STORE command to save RAM to EEPROM...");
                X2212Io.DoStore(_lptBaseAddress, LogMessage);
                LogMessage("STORE operation completed");
                SetStatus("Stored to EEPROM");
            }
            catch (Exception ex)
            {
                LogMessage($"STORE operation failed: {ex.Message}");
            }
        }

        private void OnDeviceProbe(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Probing for X2212 device...");
                bool found = X2212Io.ProbeDevice(_lptBaseAddress, out string reason, LogMessage);
                
                if (found)
                {
                    LogMessage($"Device probe successful: {reason}");
                    SetStatus("X2212 detected");
                }
                else
                {
                    LogMessage($"Device probe failed: {reason}");
                    SetStatus("X2212 not detected");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Device probe error: {ex.Message}");
            }
        }
    }
}
