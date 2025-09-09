
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm
    {
        // File Operations
        partial void OnFileOpen(object? sender, EventArgs e)
        {
            if (_dataModified)
            {
                DialogResult result = MessageBox.Show(
                    "Data has been modified. Do you want to save before opening a new file?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        OnFileSaveAs(sender, e);
                        if (_dataModified) // Save was cancelled
                            return;
                        break;
                    case DialogResult.Cancel:
                        return; // Don't open
                    case DialogResult.No:
                        break; // Open without saving
                }
            }

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open .RGR File";
                dlg.Filter = "RGR Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dlg.InitialDirectory = Path.GetDirectoryName(_lastFilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        LoadRgrFile(dlg.FileName);
                        _lastFilePath = dlg.FileName;
                        _dataModified = false;
                        LogMessage($"Loaded file: {dlg.FileName}");
                        LogMessage($"File path: {Path.GetFullPath(dlg.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error loading file: {ex.Message}");
                        MessageBox.Show($"Error loading file: {ex.Message}", "File Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        partial void OnFileSaveAs(object? sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save .RGR File";
                dlg.Filter = "RGR Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dlg.InitialDirectory = Path.GetDirectoryName(_lastFilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (!string.IsNullOrEmpty(_lastFilePath))
                {
                    dlg.FileName = Path.GetFileName(_lastFilePath);
                }

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SaveRgrFile(dlg.FileName);
                        _lastFilePath = dlg.FileName;
                        _dataModified = false;
                        LogMessage($"Saved file: {dlg.FileName}");
                        LogMessage($"File path: {Path.GetFullPath(dlg.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error saving file: {ex.Message}");
                        MessageBox.Show($"Error saving file: {ex.Message}", "File Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Settings Management
        partial void LoadSettings()
        {
            string iniPath = Path.Combine(Application.StartupPath, "X2212Programmer.ini");
            
            try
            {
                if (File.Exists(iniPath))
                {
                    string[] lines = File.ReadAllLines(iniPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("LPTBase=", StringComparison.OrdinalIgnoreCase))
                        {
                            string valueStr = line.Substring(8);
                            if (ushort.TryParse(valueStr, System.Globalization.NumberStyles.HexNumber, null, out ushort value))
                            {
                                _lptBaseAddress = value;
                                if (txtLptBase != null)
                                {
                                    txtLptBase.Text = $"0x{_lptBaseAddress:X4}";
                                }
                                LogMessage($"Loaded LPT Base from INI: 0x{_lptBaseAddress:X4}");
                            }
                        }
                        else if (line.StartsWith("LastFile=", StringComparison.OrdinalIgnoreCase))
                        {
                            _lastFilePath = line.Substring(9);
                        }
                    }
                }
                else
                {
                    LogMessage($"INI file not found, using default LPT Base: 0x{_lptBaseAddress:X4}");
                    SaveSettings(); // Create default INI file
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            string iniPath = Path.Combine(Application.StartupPath, "X2212Programmer.ini");
            
            try
            {
                var lines = new[]
                {
                    $"LPTBase={_lptBaseAddress:X4}",
                    $"LastFile={_lastFilePath}"
                };
                
                File.WriteAllLines(iniPath, lines);
                LogMessage($"Settings saved to: {iniPath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error saving settings: {ex.Message}");
            }
        }

        partial void OnLptBaseChanged(object? sender, EventArgs e)
        {
            if (txtLptBase?.Text == null) return;

            try
            {
                string text = txtLptBase.Text.Trim();
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(2);
                }

                if (ushort.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out ushort value))
                {
                    _lptBaseAddress = value;
                    SaveSettings(); // Auto-save to INI
                    LogMessage($"LPT Base Address changed to: 0x{_lptBaseAddress:X4}");
                }
                else
                {
                    // Revert to previous valid value
                    txtLptBase.Text = $"0x{_lptBaseAddress:X4}";
                    LogMessage("Invalid LPT Base Address format");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating LPT Base: {ex.Message}");
                txtLptBase.Text = $"0x{_lptBaseAddress:X4}";
            }
        }

        // Logging
        partial void LogMessage(string message)
        {
            if (txtMessages == null) return;

            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}";
                
                if (txtMessages.InvokeRequired)
                {
                    txtMessages.Invoke(new Action(() =>
                    {
                        txtMessages.AppendText(logEntry + Environment.NewLine);
                        txtMessages.ScrollToCaret();
                    }));
                }
                else
                {
                    txtMessages.AppendText(logEntry + Environment.NewLine);
                    txtMessages.ScrollToCaret();
                }
            }
            catch
            {
                // Silent fail for logging
            }
        }

        // File Format Handlers
        private void LoadRgrFile(string fileName)
        {
            byte[] fileData = File.ReadAllBytes(fileName);
            
            if (fileData.Length != 128)
            {
                throw new InvalidDataException($"Invalid file size: {fileData.Length} bytes (expected 128)");
            }

            SaveUndoState();
            Array.Copy(fileData, _currentData, 128);
            UpdateHexDisplay();
            
            LogMessage($"Loaded {fileData.Length} bytes from {Path.GetFileName(fileName)}");
        }

        private void SaveRgrFile(string fileName)
        {
            File.WriteAllBytes(fileName, _currentData);
            LogMessage($"Saved {_currentData.Length} bytes to {Path.GetFileName(fileName)}");
        }

        // Form Closing Handler
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
                        if (_dataModified) // Save was cancelled
                        {
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                    case DialogResult.No:
                        break; // Exit without saving
                }
            }

            SaveSettings();
            base.OnFormClosing(e);
        }
    }
}
