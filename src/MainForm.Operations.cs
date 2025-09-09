using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm
    {
        private void UpdateLptBase()
        {
            if (txtLptBase == null) return;
            
            string text = txtLptBase.Text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (ushort.TryParse(text, NumberStyles.HexNumber, null, out ushort addr))
            {
                _lptBaseAddress = addr;
                txtLptBase.Text = $"0x{_lptBaseAddress:X4}";
                SaveSettings();
                LogMessage($"LPT base address set to 0x{_lptBaseAddress:X4}");
            }
            else
            {
                txtLptBase.Text = $"0x{_lptBaseAddress:X4}";
                LogMessage("Invalid address format - reverted to previous value");
            }
        }

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
                        
                        var hexCell = row.Cells[byteIndex];
                        if (hexCell != null)
                        {
                            hexCell.Value = $"{val:X2}";
                        }
                        
                        char c = (val >= 32 && val <= 126) ? (char)val : '.';
                        ascii.Append(c);
                    }
                    
                    // Safe ASCII cell access
                    if (row.Cells.Count > 8)
                    {
                        var asciiCell = row.Cells[row.Cells.Count - 1];
                        if (asciiCell != null)
                            asciiCell.Value = ascii.ToString();
                    }
                }
            }
            catch
            {
                // Silent fail for hex display updates
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

        // MESSAGE LOGGING
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
                    return;
                }

                // Use normal message colors (not diagnostic test colors)
                txtMessages.BackColor = Color.Black;
                txtMessages.ForeColor = Color.Lime;
                
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
            }
            catch
            {
                // Silent fail for logging
            }
        }

        private void SetStatus(string status)
        {
            if (statusLabel != null && !statusLabel.IsDisposed)
                statusLabel.Text = status;
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

        // Utility methods
        private byte[] ParseHexFile(string content)
        {
            var hexOnly = new StringBuilder();
            foreach (char c in content)
            {
                if ((c >= '0' && c <= '9') || 
                    (c >= 'A' && c <= 'F') || 
                    (c >= 'a' && c <= 'f'))
                {
                    hexOnly.Append(char.ToUpper(c));
                }
            }

            string hexString = hexOnly.ToString();
            if (hexString.Length < 256)
                throw new Exception($"File too short: {hexString.Length/2} bytes (need 128)");

            byte[] data = new byte[128];
            for (int i = 0; i < 128; i++)
            {
                data[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return data;
        }

        private string BytesToHexString(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                sb.Append($"{b:X2}");
            return sb.ToString();
        }

        // Settings management
        private void LoadSettings()
        {
            try
            {
                string iniPath = Path.Combine(Application.StartupPath, "X2212Programmer.ini");
                if (File.Exists(iniPath))
                {
                    string[] lines = File.ReadAllLines(iniPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("LPTBase="))
                        {
                            string value = line.Substring(8);
                            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                value = value.Substring(2);
                            if (ushort.TryParse(value, NumberStyles.HexNumber, null, out ushort addr))
                                _lptBaseAddress = addr;
                        }
                        else if (line.StartsWith("LastFolder="))
                        {
                            string folder = line.Substring(11);
                            if (Directory.Exists(folder))
                                _lastFolderPath = folder;
                        }
                    }
                }
            }
            catch
            {
                // Use defaults if loading fails
            }
        }

        private void SaveSettings()
        {
            try
            {
                string iniPath = Path.Combine(Application.StartupPath, "X2212Programmer.ini");
                string[] lines = { 
                    $"LPTBase=0x{_lptBaseAddress:X4}",
                    $"LastFolder={_lastFolderPath}"
                };
                File.WriteAllLines(iniPath, lines);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
