using System;
using System.Collections.Generic;
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
        private void LogMessage(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), msg);
                return;
            }

            // Ensure we're writing to the BLACK MESSAGE BOX
            if (txtMessages != null)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logLine = $"[{timestamp}] {msg}\r\n";
                
                txtMessages.AppendText(logLine);
                txtMessages.SelectionStart = txtMessages.Text.Length;
                txtMessages.ScrollToCaret();
                txtMessages.Update(); // Force immediate display
                txtMessages.Refresh();
            }
        }

        // Settings using INI file approach
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
                    }
                }
            }
            catch
            {
                // Use default if loading fails
            }
        }

        private void SaveSettings()
        {
            try
            {
                string iniPath = Path.Combine(Application.StartupPath, "X2212Programmer.ini");
                string[] lines = { $"LPTBase=0x{_lptBaseAddress:X4}" };
                File.WriteAllLines(iniPath, lines);
            }
            catch
            {
                // Ignore save errors
            }
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
}private ushort _lptBaseAddress = 0xA800;
        private byte[] _currentData = new byte[128]; // 16 channels × 8 bytes each
        private string _lastFilePath = "";
        private int _currentChannel = 1; // Current channel (1-16)
        private bool _dataModified = false; // Track if data has been changed
        private byte[] _clipboardRow = new byte[8]; // For copying rows
        private int _lastSelectedRow = -1; // For shift-click selection
        
        // UI Controls
        private MenuStrip menuStrip = null!;
        private ToolStripMenuItem fileMenu = null!;
        private ToolStripMenuItem deviceMenu = null!;
        private Panel topPanel = null!;
        private Label lblLptBase = null!;
        private TextBox txtLptBase = null!;
        private Label lblDevice = null!;
        private TextBox txtDevice = null!;
        private Label lblChannel = null!;
        private TextBox txtChannel = null!;
        private DataGridView hexGrid = null!;
        private TextBox txtMessages = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel statusLabel = null!;
        private ToolStripStatusLabel statusFilePath = null!;

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            InitializeSafety();
            UpdateChannelDisplay();
        }

        private void InitializeComponent()
        {
            Text = "X2212 Programmer";
            Size = new Size(650, 700); // Reduced width
            StartPosition = FormStartPosition.CenterScreen;

            // Menu Strip
            menuStrip = new MenuStrip();
            
            // File Menu
            fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open .RGR...", null, OnFileOpen);
            fileMenu.DropDownItems.Add("Save As .RGR...", null, OnFileSaveAs);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Copy Row", null, OnCopyRow);
            fileMenu.DropDownItems.Add("Paste to Selected", null, OnPasteToSelected);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, OnExit);
            
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

            // Top Panel with controls
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10)
            };

            // LPT Base Address
            lblLptBase = new Label
            {
                Text = "LPT Base:",
                Location = new Point(10, 15),
                AutoSize = true
            };

            txtLptBase = new TextBox
            {
                Text = $"0x{_lptBaseAddress:X4}",
                Location = new Point(80, 12),
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

            // Device Type
            lblDevice = new Label
            {
                Text = "Device:",
                Location = new Point(200, 15),
                AutoSize = true
            };

            txtDevice = new TextBox
            {
                Text = "X2212",
                Location = new Point(250, 12),
                Width = 60,
                ReadOnly = true,
                BackColor = SystemColors.Control
            };

            // Channel Display
            lblChannel = new Label
            {
                Text = "Channel:",
                Location = new Point(350, 15),
                AutoSize = true
            };

            txtChannel = new TextBox
            {
                Text = "Ch1",
                Location = new Point(405, 12),
                Width = 60,
                ReadOnly = true,
                BackColor = SystemColors.Control
            };

            topPanel.Controls.Add(lblLptBase);
            topPanel.Controls.Add(txtLptBase);
            topPanel.Controls.Add(lblDevice);
            topPanel.Controls.Add(txtDevice);
            topPanel.Controls.Add(lblChannel);
            topPanel.Controls.Add(txtChannel);
            Controls.Add(topPanel);

            // Hex Grid - 16 channels × 8 bytes each
            hexGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 400,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersWidth = 60,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true, // Enable multi-selection
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.Black }
            };

            // Create columns for hex display (8 bytes)
            hexGrid.Columns.Clear();
            for (int i = 0; i < 8; i++)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = $"byte{i}",
                    HeaderText = $"{i:X}",
                    Width = 35,
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
                Width = 100,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            hexGrid.Columns.Add(asciiCol);

            // Create 16 rows - CORRECT ORDER: Ch1=E0 to Ch16=F0
            string[] channelAddresses = { "E0", "D0", "C0", "B0", "A0", "90", "80", "70", 
                                          "60", "50", "40", "30", "20", "10", "00", "F0" };
            
            for (int row = 0; row < 16; row++)
            {
                hexGrid.Rows.Add();
                hexGrid.Rows[row].HeaderCell.Value = channelAddresses[row];
                hexGrid.Rows[row].Height = 20;
            }

            // Add right-click context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Copy Row", null, OnCopyRow);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Paste to Selected Rows", null, OnPasteToSelected);
            contextMenu.Items.Add("Paste to All Rows", null, OnFillAll);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Clear Selection", null, OnClearSelection);
            hexGrid.ContextMenuStrip = contextMenu;

            hexGrid.CellEndEdit += HexGrid_CellEndEdit;
            hexGrid.CellFormatting += HexGrid_CellFormatting;
            hexGrid.SelectionChanged += HexGrid_SelectionChanged;
            hexGrid.CellDoubleClick += HexGrid_CellDoubleClick;
            hexGrid.KeyDown += HexGrid_KeyDown;
            hexGrid.CellPainting += HexGrid_CellPainting;
            hexGrid.MouseDown += HexGrid_MouseDown; // Add mouse handling for shift-click
            Controls.Add(hexGrid);

            // Message Box - Force immediate display
            txtMessages = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.Lime
            };
            Controls.Add(txtMessages);

            // Status Strip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusFilePath = new ToolStripStatusLabel("No file loaded")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(statusFilePath);
            Controls.Add(statusStrip);

            // Initialize display 
            UpdateHexDisplay();
            
            // FORCE messages to appear immediately
            this.Load += (s, e) => {
                txtMessages.Text = "Message window initialized...\r\n";
                txtMessages.AppendText("Application started successfully\r\n");
                txtMessages.AppendText("Ready for device operations\r\n");
                txtMessages.Refresh();
            };
        }

        private string GetChannelAddress(int channel)
        {
            // CORRECT mapping: Ch1=E0, Ch2=D0, ..., Ch15=00, Ch16=F0
            string[] addresses = { "E0", "D0", "C0", "B0", "A0", "90", "80", "70", 
                                   "60", "50", "40", "30", "20", "10", "00", "F0" };
            return (channel >= 1 && channel <= 16) ? addresses[channel - 1] : "E0";
        }

        private int GetChannelFromRowIndex(int rowIndex)
        {
            // Row 0 = Ch1, Row 1 = Ch2, ..., Row 15 = Ch16
            return rowIndex + 1;
        }

        private void HexGrid_MouseDown(object sender, MouseEventArgs e)
        {
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
                    return; // Don't let normal selection handling occur
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    // Ctrl-click: toggle individual row
                    hexGrid.Rows[hitTest.RowIndex].Selected = !hexGrid.Rows[hitTest.RowIndex].Selected;
                    LogMessage($"Toggled row {hitTest.RowIndex + 1} (Ch{hitTest.RowIndex + 1})");
                    return; // Don't let normal selection handling occur
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

        private void HexGrid_SelectionChanged(object sender, EventArgs e)
        {
            // Update current channel if single selection
            if (hexGrid.SelectedRows.Count == 1)
            {
                int row = hexGrid.SelectedRows[0].Index;
                _currentChannel = row + 1;
                _lastSelectedRow = row;
                UpdateChannelDisplay();
            }
            
            hexGrid.Invalidate(); // Force repaint for colors
            
            // Update status bar with selection count
            int selectedCount = hexGrid.SelectedRows.Count;
            if (selectedCount > 1)
            {
                statusLabel.Text = $"{selectedCount} rows selected";
            }
            else if (selectedCount == 1)
            {
                statusLabel.Text = "Ready";
            }
        }

        private void HexGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex < 8)
            {
                hexGrid.BeginEdit(false);
            }
        }

        private void HexGrid_KeyDown(object sender, KeyEventArgs e)
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
                }
            }
        }

        private void HexGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // FOREGROUND ONLY coloring - no background changes
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex < 8)
            {
                bool isCurrentRow = (e.RowIndex == hexGrid.CurrentRow?.Index);
                
                if (isCurrentRow)
                {
                    // Paint white background
                    e.Graphics.FillRectangle(Brushes.White, e.CellBounds);
                    
                    // Determine text color: Red for first 4 bytes (Tx), Blue for second 4 bytes (Rx)
                    Color textColor = e.ColumnIndex < 4 ? Color.Red : Color.Blue;
                    
                    // Draw text in appropriate color
                    if (e.Value != null)
                    {
                        using (var brush = new SolidBrush(textColor))
                        {
                            var textRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, 
                                                        e.CellBounds.Width - 4, e.CellBounds.Height - 4);
                            e.Graphics.DrawString(e.Value.ToString(), e.CellStyle.Font, brush, textRect,
                                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                        }
                    }
                    
                    // Draw border
                    e.Graphics.DrawRectangle(Pens.DarkGray, e.CellBounds);
                    e.Handled = true;
                }
            }
        }

        private void UpdateChannelDisplay()
        {
            txtChannel.Text = $"Ch{_currentChannel}";
        }

        private void InitializeSafety()
        {
            try
            {
                // Set parallel port to safe idle state
                X2212Io.SetIdle(_lptBaseAddress);
                LogMessage("Driver initialized - port set to safe idle state");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Driver not found or could not initialize port: {ex.Message}");
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
            for (int channel = 0; channel < 16; channel++)
            {
                StringBuilder ascii = new StringBuilder(8);
                for (int byteIndex = 0; byteIndex < 8; byteIndex++)
                {
                    int offset = channel * 8 + byteIndex;
                    byte val = _currentData[offset];
                    hexGrid.Rows[channel].Cells[byteIndex].Value = $"{val:X2}";
                    
                    // Build ASCII representation
                    char c = (val >= 32 && val <= 126) ? (char)val : '.';
                    ascii.Append(c);
                }
                hexGrid.Rows[channel].Cells["ASCII"].Value = ascii.ToString();
            }
        }

        private void HexGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 8) return; // ASCII column
            
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
                    
                    // Update ASCII column for this row
                    UpdateAsciiForRow(e.RowIndex);
                    LogMessage($"Modified Ch{e.RowIndex + 1} byte {e.ColumnIndex}: {val:X2}");
                }
            }
            else
            {
                // Revert to original value
                int offset = e.RowIndex * 8 + e.ColumnIndex;
                cell.Value = $"{_currentData[offset]:X2}";
                LogMessage("Invalid hex value - reverted");
            }
        }

        private void HexGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 8 && e.Value != null)
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
            StringBuilder ascii = new StringBuilder(8);
            for (int col = 0; col < 8; col++)
            {
                byte val = _currentData[row * 8 + col];
                char c = (val >= 32 && val <= 126) ? (char)val : '.';
                ascii.Append(c);
            }
            hexGrid.Rows[row].Cells["ASCII"].Value = ascii.ToString();
        }

        // File Operations - Fixed nullability
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
                        if (_dataModified) // Save was cancelled
                            return;
                        break;
                    case DialogResult.Cancel:
                        return; // Don't exit
                    case DialogResult.No:
                        break; // Exit without saving
                }
            }
            Close();
        }

        private void OnCopyRow(object? sender, EventArgs e)
        {
            if (hexGrid.CurrentRow == null) return;

            int sourceRow = hexGrid.CurrentRow.Index;
            for (int i = 0; i < 8; i++)
            {
                _clipboardRow[i] = _currentData[sourceRow * 8 + i];
            }
            LogMessage($"Copied Ch{sourceRow + 1} to clipboard");
        }

        private void OnPasteToSelected(object? sender, EventArgs e)
        {
            var selectedRows = hexGrid.SelectedRows;
            if (selectedRows.Count == 0) return;

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

        private void OnFileOpen(object? sender, EventArgs e)
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
                        _dataModified = false; // Reset modified flag after loading
                        UpdateHexDisplay();
                        _lastFilePath = dlg.FileName;
                        LogMessage($"Loaded file: {Path.GetFileName(dlg.FileName)}");
                        statusLabel.Text = "File loaded";
                        statusFilePath.Text = _lastFilePath;
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
                dlg.InitialDirectory = Path.GetDirectoryName(_lastFilePath) ?? 
                                       Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string hexContent = BytesToHexString(_currentData);
                        File.WriteAllText(dlg.FileName, hexContent);
                        _lastFilePath = dlg.FileName;
                        _dataModified = false; // Reset modified flag after saving
                        LogMessage($"Saved file: {Path.GetFileName(dlg.FileName)}");
                        statusLabel.Text = "File saved";
                        statusFilePath.Text = _lastFilePath;
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

        // Device Operations - Fixed nullability (all use object? sender)
        private void OnDeviceRead(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Reading from X2212 device...");
                var nibbles = X2212Io.ReadAllNibbles(_lptBaseAddress, LogMessage);
                _currentData = X2212Io.CompressNibblesToBytes(nibbles);
                _dataModified = false; // Reset modified flag after reading from device
                UpdateHexDisplay();
                LogMessage("Read operation completed - 128 bytes received");
                statusLabel.Text = "Read from device";
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
                statusLabel.Text = "Written to device";
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
                    statusLabel.Text = "Verify OK";
                }
                else
                {
                    LogMessage($"Verify operation failed at nibble {failIndex}");
                    statusLabel.Text = $"Verify failed at {failIndex}";
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
                statusLabel.Text = "Stored to EEPROM";
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
                    statusLabel.Text = "X2212 detected";
                }
                else
                {
                    LogMessage($"Device probe failed: {reason}");
                    statusLabel.Text = "X2212 not detected";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Device probe error: {ex.Message}");
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

        private void LogMessage(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), msg);
                return;
            }

            // Ensure we're writing to the BLACK MESSAGE BOX
            if (txtMessages != null)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logLine = $"[{timestamp}] {msg}\r\n";
                
                txtMessages.AppendText(logLine);
                txtMessages.SelectionStart = txtMessages.Text.Length;
                txtMessages.ScrollToCaret();
                txtMessages.Update(); // Force immediate display
                txtMessages.Refresh();
            }
        }

        // Settings using INI file approach
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
                    }
                }
            }
            catch
            {
                // Use default if loading fails
            }
        }

        private void SaveSettings()
        {
            try
            {
                string iniPath = Path.Combine(Application.StartupPath, "X2212Programmer.ini");
                string[] lines = { $"LPTBase=0x{_lptBaseAddress:X4}" };
                File.WriteAllLines(iniPath, lines);
            }
            catch
            {
                // Ignore save errors
            }
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
