using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        // File Operations
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
                        if (statusFilePath != null) statusFilePath.Text = _lastFilePath;
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
                        if (statusFilePath != null) statusFilePath.Text = _lastFilePath;
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

        // Device Operations
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

        // NEW: Timing Calibration
        private void OnCalibrateTiming(object? sender, EventArgs e)
        {
            if (MessageBox.Show("This will test various timing values to find optimal settings.\n" +
                               "The process may take a minute. Continue?", 
                               "Calibrate Timing", 
                               MessageBoxButtons.YesNo, 
                               MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                LogMessage("=== Starting timing calibration ===");
                SetStatus("Calibrating...");
                
                // Save current timing values in case we need to restore them
                int originalSetup = X2212Io.SetupTime_us;
                int originalPulse = X2212Io.PulseWidth_us;
                int originalHold = X2212Io.HoldTime_us;
                
                // Run calibration
                X2212Io.TimingCalibrationTest(_lptBaseAddress, LogMessage);
                
                // Find minimum working timing for optimal settings
                int minTiming = X2212Io.FindMinimumWorkingTiming(_lptBaseAddress, LogMessage);
                
                if (minTiming > 0)
                {
                    // Apply safe timing (2x minimum for reliability)
                    int safeTiming = minTiming * 2;
                    X2212Io.ApplyTimingSettings(safeTiming, safeTiming, safeTiming / 2, LogMessage);
                    
                    LogMessage($"Applied safe timing: {X2212Io.GetCurrentTiming()}");
                    
                    // Save to INI file
                    SaveSettings();
                    
                    LogMessage("Calibration complete. Settings saved to INI file.");
                    SetStatus("Calibration complete");
                    
                    MessageBox.Show($"Calibration successful!\n\n" +
                                  $"Minimum working: {minTiming}µs\n" +
                                  $"Applied safe timing: {safeTiming}µs\n\n" +
                                  $"Settings saved to INI file.",
                                  "Calibration Complete",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                }
                else
                {
                    // Restore original values if calibration failed
                    X2212Io.ApplyTimingSettings(originalSetup, originalPulse, originalHold, LogMessage);
                    
                    LogMessage("Calibration failed - no working timing found. Original settings restored.");
                    SetStatus("Calibration failed");
                    
                    MessageBox.Show("Calibration failed to find working timing values.\n" +
                                  "Original settings have been restored.\n\n" +
                                  "Please check your hardware connection.",
                                  "Calibration Failed",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Calibration error: {ex.Message}");
                MessageBox.Show($"Calibration failed: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        // Settings management - UPDATED to include timing values
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
                        // Load timing values
                        else if (line.StartsWith("SetupTime="))
                        {
                            if (int.TryParse(line.Substring(10), out int value))
                                X2212Io.SetupTime_us = value;
                        }
                        else if (line.StartsWith("PulseWidth="))
                        {
                            if (int.TryParse(line.Substring(11), out int value))
                                X2212Io.PulseWidth_us = value;
                        }
                        else if (line.StartsWith("HoldTime="))
                        {
                            if (int.TryParse(line.Substring(9), out int value))
                                X2212Io.HoldTime_us = value;
                        }
                        else if (line.StartsWith("StoreTime="))
                        {
                            if (int.TryParse(line.Substring(10), out int value))
                                X2212Io.StoreTime_ms = value;
                        }
                    }
                    
                    LogMessage($"Loaded timing: {X2212Io.GetCurrentTiming()}");
                }
                else
                {
                    LogMessage($"Using default timing: {X2212Io.GetCurrentTiming()}");
                }
            }
            catch
            {
                // Use defaults if loading fails
                LogMessage("Failed to load settings, using defaults");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string iniPath = Path.Combine(Application.StartupPath, "X2212Programmer.ini");
                string[] lines = { 
                    $"LPTBase=0x{_lptBaseAddress:X4}",
                    $"LastFolder={_lastFolderPath}",
                    $"SetupTime={X2212Io.SetupTime_us}",
                    $"PulseWidth={X2212Io.PulseWidth_us}",
                    $"HoldTime={X2212Io.HoldTime_us}",
                    $"StoreTime={X2212Io.StoreTime_ms}"
                };
                File.WriteAllLines(iniPath, lines);
                LogMessage($"Settings saved including timing: {X2212Io.GetCurrentTiming()}");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
