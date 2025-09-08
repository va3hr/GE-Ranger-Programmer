using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        private ushort _lptBaseAddress = 0xA800;
        private byte[] _currentData = new byte[128];
        private string _lastFilePath = "";
        
        // UI Controls
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem deviceMenu;
        private Label lblLptBase;
        private TextBox txtLptBase;
        private DataGridView hexGrid;
        private TextBox txtMessages;
        private Panel topPanel;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            InitializeSafety();
        }

        private void InitializeComponent()
        {
            Text = "X2212 Programmer";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            // Menu Strip
            menuStrip = new MenuStrip();
            
            // File Menu
            fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open .RGR...", null, OnFileOpen);
            fileMenu.DropDownItems.Add("Save As .RGR...", null, OnFileSaveAs);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());
            
            // Device Menu
            deviceMenu = new ToolStripMenuItem("Device");
            deviceMenu.DropDownItems.Add("Read from X2212", null, OnDeviceRead);
            deviceMenu.DropDownItems.Add("Write to X2212", null, OnDeviceWrite);
            deviceMenu.DropDownItems.Add("Verify", null, OnDeviceVerify);
            deviceMenu.DropDownItems.Add(new ToolStripSeparator());
            deviceMenu.DropDownItems.Add("Store to EEPROM", null, OnDeviceStore);
            deviceMenu.DropDownItems.Add("Probe Device", null, OnDeviceProbe);
            
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(deviceMenu);
            Controls.Add(menuStrip);

            // Top Panel with LPT Base Address
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            lblLptBase = new Label
            {
                Text = "LPT Base:",
                Location = new Point(10, 12),
                AutoSize = true
            };

            txtLptBase = new TextBox
            {
                Text = $"0x{_lptBaseAddress:X4}",
                Location = new Point(80, 10),
                Width = 80
            };
            txtLptBase.Leave += (s, e) => UpdateLptBase();
            txtLptBase.KeyPress += (s, e) => 
            {
                if (e.KeyChar == (char)Keys.Return)
                {
                    UpdateLptBase();
                    e.Handled = true;
                }
            };

            topPanel.Controls.Add(lblLptBase);
            topPanel.Controls.Add(txtLptBase);
            Controls.Add(topPanel);

            // Hex Grid
            hexGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 300,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersWidth = 60,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ScrollBars = ScrollBars.None,
                Font = new Font("Consolas", 9)
            };

            // Create columns for hex display (Address + 16 bytes)
            hexGrid.Columns.Clear();
            for (int i = 0; i < 16; i++)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = $"col{i:X}",
                    HeaderText = $"{i:X}",
                    Width = 30,
                    MaxInputLength = 2,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                hexGrid.Columns.Add(col);
            }

            // ASCII column
            var asciiCol = new DataGridViewTextBoxColumn
            {
                Name = "ASCII",
                HeaderText = "ASCII",
                Width = 140,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            hexGrid.Columns.Add(asciiCol);

            // Create rows (8 rows for 128 bytes)
            for (int row = 0; row < 8; row++)
            {
                hexGrid.Rows.Add();
                hexGrid.Rows[row].HeaderCell.Value = $"{row * 16:X2}";
            }

            hexGrid.CellEndEdit += HexGrid_CellEndEdit;
            hexGrid.CellFormatting += HexGrid_CellFormatting;
            Controls.Add(hexGrid);

            // Message Box
            txtMessages = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8),
                BackColor = Color.Black,
                ForeColor = Color.Lime
            };
            Controls.Add(txtMessages);

            // Status Strip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusStrip.Items.Add(statusLabel);
            Controls.Add(statusStrip);

            // Initialize with empty data
            UpdateHexDisplay();
        }

        private void InitializeSafety()
        {
            try
            {
                // Set parallel port to safe idle state
                X2212Io.SetIdle(_lptBaseAddress);
                LogMessage("Port initialized - safe idle state");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not initialize port: {ex.Message}");
            }
        }

        private void UpdateLptBase()
        {
            string text = txtLptBase.Text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (ushort.TryParse(text, NumberStyles.HexNumber, null, out ushort addr))
            {
                _lptBaseAddress = addr;
                txtLptBase.Text = $"0x{_lptBaseAddress:X4}";
                SaveSettings();
                LogMessage($"LPT base set to 0x{_lptBaseAddress:X4}");
            }
            else
            {
                txtLptBase.Text = $"0x{_lptBaseAddress:X4}";
                LogMessage("Invalid address format");
            }
        }

        private void UpdateHexDisplay()
        {
            for (int row = 0; row < 8; row++)
            {
                StringBuilder ascii = new StringBuilder(16);
                for (int col = 0; col < 16; col++)
                {
                    int offset = row * 16 + col;
                    byte val = _currentData[offset];
                    hexGrid.Rows[row].Cells[col].Value = $"{val:X2}";
                    
                    // Build ASCII representation
                    char c = (val >= 32 && val <= 126) ? (char)val : '.';
                    ascii.Append(c);
                }
                hexGrid.Rows[row].Cells["ASCII"].Value = ascii.ToString();
            }
        }

        private void HexGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 16) return; // ASCII column
            
            var cell = hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string input = cell.Value?.ToString() ?? "";
            
            if (byte.TryParse(input, NumberStyles.HexNumber, null, out byte val))
            {
                int offset = e.RowIndex * 16 + e.ColumnIndex;
                _currentData[offset] = val;
                cell.Value = $"{val:X2}";
                
                // Update ASCII column
                UpdateAsciiForRow(e.RowIndex);
            }
            else
            {
                // Revert to original value
                int offset = e.RowIndex * 16 + e.ColumnIndex;
                cell.Value = $"{_currentData[offset]:X2}";
            }
        }

        private void HexGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 16 && e.Value != null)
            {
                // Ensure hex values are always uppercase and 2 digits
                string val = e.Value.ToString();
                if (val.Length == 1)
                    e.Value = "0" + val.ToUpper();
                else
                    e.Value = val.ToUpper();
            }
        }

        private void UpdateAsciiForRow(int row)
        {
            StringBuilder ascii = new StringBuilder(16);
            for (int col = 0; col < 16; col++)
            {
                byte val = _currentData[row * 16 + col];
                char c = (val >= 32 && val <= 126) ? (char)val : '.';
                ascii.Append(c);
            }
            hexGrid.Rows[row].Cells["ASCII"].Value = ascii.ToString();
        }

        // File Operations
        private void OnFileOpen(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open .RGR File";
                dlg.Filter = "RGR Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dlg.InitialDirectory = Path.GetDirectoryName(_lastFilePath) ?? 
                                       Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string content = File.ReadAllText(dlg.FileName);
                        _currentData = ParseHexFile(content);
                        UpdateHexDisplay();
                        _lastFilePath = dlg.FileName;
                        LogMessage($"Loaded: {Path.GetFileName(dlg.FileName)}");
                        statusLabel.Text = Path.GetFileName(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnFileSaveAs(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save .RGR File";
                dlg.Filter = "RGR Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dlg.InitialDirectory = Path.GetDirectoryName(_lastFilePath) ?? 
                                       Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string hexContent = BytesToHexString(_currentData);
                        File.WriteAllText(dlg.FileName, hexContent);
                        _lastFilePath = dlg.FileName;
                        LogMessage($"Saved: {Path.GetFileName(dlg.FileName)}");
                        statusLabel.Text = Path.GetFileName(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Device Operations
        private void OnDeviceRead(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Reading from X2212...");
                var nibbles = X2212Io.ReadAllNibbles(_lptBaseAddress, LogMessage);
                _currentData = X2212Io.CompressNibblesToBytes(nibbles);
                UpdateHexDisplay();
                LogMessage("Read complete - 128 bytes");
                statusLabel.Text = "Read from device";
            }
            catch (Exception ex)
            {
                LogMessage($"Read failed: {ex.Message}");
                MessageBox.Show($"Read failed: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceWrite(object sender, EventArgs e)
        {
            if (MessageBox.Show("Write current data to X2212?", "Confirm Write", 
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                LogMessage("Writing to X2212...");
                var nibbles = X2212Io.ExpandToNibbles(_currentData);
                X2212Io.ProgramNibbles(_lptBaseAddress, nibbles, LogMessage);
                LogMessage("Write complete");
                statusLabel.Text = "Written to device";
            }
            catch (Exception ex)
            {
                LogMessage($"Write failed: {ex.Message}");
                MessageBox.Show($"Write failed: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceVerify(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Verifying...");
                var nibbles = X2212Io.ExpandToNibbles(_currentData);
                bool ok = X2212Io.VerifyNibbles(_lptBaseAddress, nibbles, out int failIndex, LogMessage);
                
                if (ok)
                {
                    LogMessage("Verify OK - all 256 nibbles match");
                    statusLabel.Text = "Verify OK";
                }
                else
                {
                    LogMessage($"Verify FAILED at nibble {failIndex}");
                    statusLabel.Text = $"Verify failed at {failIndex}";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Verify failed: {ex.Message}");
                MessageBox.Show($"Verify failed: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceStore(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Sending STORE command...");
                X2212Io.DoStore(_lptBaseAddress, LogMessage);
                LogMessage("STORE complete");
            }
            catch (Exception ex)
            {
                LogMessage($"STORE failed: {ex.Message}");
            }
        }

        private void OnDeviceProbe(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Probing for X2212...");
                bool found = X2212Io.ProbeDevice(_lptBaseAddress, out string reason, LogMessage);
                
                if (found)
                {
                    LogMessage($"Device found: {reason}");
                    statusLabel.Text = "X2212 detected";
                }
                else
                {
                    LogMessage($"Device not found: {reason}");
                    statusLabel.Text = "X2212 not detected";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Probe failed: {ex.Message}");
            }
        }

        // Utilities
        private byte[] ParseHexFile(string content)
        {
            // Remove all whitespace and non-hex characters
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

            string hex = hexOnly.ToString();
            if (hex.Length < 256)
                throw new Exception($"File too short: {hex.Length/2} bytes (need 128)");

            byte[] data = new byte[128];
            for (int i = 0; i < 128; i++)
            {
                data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
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

        private void LogMessage(string msg)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtMessages.AppendText($"[{timestamp}] {msg}\r\n");
            txtMessages.SelectionStart = txtMessages.Text.Length;
            txtMessages.ScrollToCaret();
        }

        // Settings
        private void LoadSettings()
        {
            try
            {
                string savedBase = Properties.Settings.Default.LPTBase;
                if (!string.IsNullOrEmpty(savedBase))
                {
                    if (savedBase.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        savedBase = savedBase.Substring(2);
                    if (ushort.TryParse(savedBase, NumberStyles.HexNumber, null, out ushort addr))
                        _lptBaseAddress = addr;
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                Properties.Settings.Default.LPTBase = $"0x{_lptBaseAddress:X4}";
                Properties.Settings.Default.Save();
            }
            catch { }
        }
    }
}
