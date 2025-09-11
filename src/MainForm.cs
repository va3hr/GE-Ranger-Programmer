using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        private ushort _lptBaseAddress = 0xA800;
        private byte[] _currentData = new byte[128]; // Little-endian bytes for UI
        private byte[] _undoData = new byte[128];
        private string _lastFilePath = "";
        private string _lastFolderPath = "";
        private int _currentChannel = 1;
        private bool _dataModified = false;
        private byte[] _clipboardRow = new byte[8];
        private int _lastSelectedRow = -1;
        
        // Channel mapping from your text file
        private readonly string[] _channelAddresses = { "E0", "D0", "C0", "B0", "A0", "90", "80", "70", 
                                                       "60", "50", "40", "30", "20", "10", "00", "F0" };
        
        // UI Controls
        private MenuStrip? menuStrip;
        private ToolStripMenuItem? fileMenu;
        private ToolStripMenuItem? deviceMenu;
        private Panel? topPanel;
        private Label? lblLptBase;
        private TextBox? txtLptBase;
        private Label? lblDevice;
        private TextBox? txtDevice;
        private Label? lblChannel;
        private TextBox? txtChannel;
        private DataGridView? hexGrid;
        private TextBox? txtMessages;
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;
        private ToolStripStatusLabel? statusFilePath;

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            InitializeSafety();
            UpdateChannelDisplay();
            SaveUndoState();
        }

        private void InitializeComponent()
        {
            Text = "X2212 Programmer";
            Size = new Size(650, 700);
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
            fileMenu.DropDownItems.Add("Fill All Rows", null, OnFillAll);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            var undoItem = new ToolStripMenuItem("Undo", null, OnUndo);
            undoItem.ShortcutKeys = Keys.Control | Keys.Z;
            fileMenu.DropDownItems.Add(undoItem);
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
            deviceMenu.DropDownItems.Add(new ToolStripSeparator());
            deviceMenu.DropDownItems.Add("Calibrate Timing", null, OnCalibrateTiming);
            
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(deviceMenu);
            Controls.Add(menuStrip);

            // Top Panel
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

            // Hex Grid
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
                MultiSelect = true,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.Black },
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                StandardTab = true
            };

            // Create columns
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

            var asciiCol = new DataGridViewTextBoxColumn
            {
                Name = "ASCII",
                HeaderText = "ASCII",
                Width = 100,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            hexGrid.Columns.Add(asciiCol);

            // Create rows with channel headers
            for (int row = 0; row < 16; row++)
            {
                hexGrid.Rows.Add();
                hexGrid.Rows[row].HeaderCell.Value = $"Ch{row + 1:00} ({_channelAddresses[row]})";
                hexGrid.Rows[row].Height = 20;
            }

            // Context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Copy Row", null, OnCopyRow);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Paste to Selected Rows", null, OnPasteToSelected);
            contextMenu.Items.Add("Paste to All Rows", null, OnFillAll);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Clear Selection", null, OnClearSelection);
            hexGrid.ContextMenuStrip = contextMenu;

            // Wire up events
            this.Load += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    hexGrid.CellEndEdit += HexGrid_CellEndEdit;
                    hexGrid.CellFormatting += HexGrid_CellFormatting;
                    hexGrid.SelectionChanged += HexGrid_SelectionChanged;
                    hexGrid.KeyDown += HexGrid_KeyDown;
                }));
            };
            
            Controls.Add(hexGrid);

            // Message Box
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

            UpdateHexDisplay();
            
            // Initialize messages
            this.Load += InitializeMessages;
            this.Shown += ShowAdditionalMessages;
        }

        private void InitializeMessages(object? sender, EventArgs e)
        {
            TestMessageBox();
            Application.DoEvents();
            Thread.Sleep(100);
            
            LogMessage("=== X2212 Programmer Started ===");
            LogMessage("Message system initialized");
            LogMessage("Ready for operations");
        }

        private void ShowAdditionalMessages(object? sender, EventArgs e)
        {
            LogMessage($"LPT Base Address: 0x{_lptBaseAddress:X4}");
            LogMessage("Use File menu to load .RGR files");
            LogMessage("Use Device menu for X2212 operations");
        }

        private void TestMessageBox()
        {
            if (txtMessages != null)
            {
                txtMessages.BackColor = Color.Black;
                txtMessages.ForeColor = Color.Lime;
                txtMessages.Clear();
                txtMessages.Refresh();
            }
        }

        private void UpdateChannelDisplay()
        {
            if (txtChannel != null)
                txtChannel.Text = $"Ch{_currentChannel}";
        }

        private void InitializeSafety()
        {
            try
            {
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
                hexGrid.CellValueChanged -= HexGrid_CellEndEdit;
                
                for (int displayRow = 0; displayRow < 16 && displayRow < hexGrid.Rows.Count; displayRow++)
                {
                    // Map display row to data row using channel addresses
                    int dataRow = displayRow; // UI rows are in channel order 1-16
                    
                    var row = hexGrid.Rows[displayRow];
                    if (row?.Cells == null) continue;
                    
                    StringBuilder ascii = new StringBuilder(8);
                    for (int byteIndex = 0; byteIndex < 8 && byteIndex < row.Cells.Count; byteIndex++)
                    {
                        int offset = dataRow * 8 + byteIndex;
                        byte val = _currentData[offset];
                        
                        var hexCell = row.Cells[byteIndex];
                        if (hexCell != null)
                        {
                            hexCell.Value = $"{val:X2}";
                        }
                        
                        char c = (val >= 32 && val <= 126) ? (char)val : '.';
                        ascii.Append(c);
                    }
                    
                    if (row.Cells.Count > 8)
                    {
                        var asciiCell = row.Cells[row.Cells.Count - 1];
                        if (asciiCell != null)
                            asciiCell.Value = ascii.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating display: {ex.Message}");
            }
            finally
            {
                if (hexGrid != null)
                {
                    hexGrid.CellValueChanged += HexGrid_CellEndEdit;
                }
            }
        }

        private void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (hexGrid == null || e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= 8) return;
            
            try
            {
                var cell = hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell.Value != null)
                {
                    string value = cell.Value.ToString();
                    if (byte.TryParse(value, NumberStyles.HexNumber, null, out byte newValue))
                    {
                        int dataRow = e.RowIndex; // UI row maps directly to data row
                        int offset = dataRow * 8 + e.ColumnIndex;
                        _currentData[offset] = newValue;
                        _dataModified = true;
                        
                        // Update ASCII display
                        UpdateAsciiDisplay(e.RowIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error processing cell edit: {ex.Message}");
            }
        }

        private void UpdateAsciiDisplay(int rowIndex)
        {
            if (hexGrid?.Rows == null || rowIndex < 0 || rowIndex >= hexGrid.Rows.Count) return;
            
            var row = hexGrid.Rows[rowIndex];
            if (row?.Cells == null) return;
            
            StringBuilder ascii = new StringBuilder(8);
            for (int byteIndex = 0; byteIndex < 8 && byteIndex < row.Cells.Count; byteIndex++)
            {
                int offset = rowIndex * 8 + byteIndex;
                byte val = _currentData[offset];
                char c = (val >= 32 && val <= 126) ? (char)val : '.';
                ascii.Append(c);
            }
            
            if (row.Cells.Count > 8)
            {
                var asciiCell = row.Cells[row.Cells.Count - 1];
                if (asciiCell != null)
                    asciiCell.Value = ascii.ToString();
            }
        }

        private void HexGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (hexGrid == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            
            // Highlight modified cells
            if (e.ColumnIndex < 8)
            {
                int offset = e.RowIndex * 8 + e.ColumnIndex;
                if (_currentData[offset] != _undoData[offset])
                {
                    e.CellStyle.BackColor = Color.LightYellow;
                }
            }
        }

        private void HexGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (hexGrid?.SelectedRows == null || hexGrid.SelectedRows.Count == 0) return;
            
            _currentChannel = hexGrid.SelectedRows[0].Index + 1;
            UpdateChannelDisplay();
        }

        private void HexGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                OnCopyRow(sender, e);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                OnPasteToSelected(sender, e);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                OnUndo(sender, e);
                e.Handled = true;
            }
        }

        // ========== FILE OPERATIONS ========== //

        private void OnFileOpen(object? sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Ranger Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dialog.Title = "Open Ranger File";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ReadHexFile(dialog.FileName);
                }
            }
        }

        private void OnFileSaveAs(object? sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Ranger Files (*.rgr)|*.rgr|All Files (*.*)|*.*";
                dialog.Title = "Save Ranger File";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    WriteHexFile(dialog.FileName);
                }
            }
        }

        private void ReadHexFile(string filePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                
                // Convert from big-endian storage to little-endian display format
                byte[] bigEndianNibbles = X2212Io.ExpandToNibbles(fileData);
                _currentData = X2212Io.CompressNibblesToBytes(bigEndianNibbles);
                
                UpdateHexDisplay();
                SaveUndoState();
                _dataModified = false;
                LogMessage($"File loaded: {filePath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error reading file: {ex.Message}");
            }
        }

        private void WriteHexFile(string filePath)
        {
            try
            {
                // Convert from little-endian display format back to big-endian storage
                byte[] bigEndianNibbles = X2212Io.ExpandToNibbles(_currentData);
                byte[] outputData = X2212Io.CompressNibblesToBytes(bigEndianNibbles);
                
                File.WriteAllBytes(filePath, outputData);
                _dataModified = false;
                LogMessage($"File saved: {filePath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error writing file: {ex.Message}");
            }
        }

        // ========== DEVICE OPERATIONS ========== //

        private void OnDeviceRead(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Reading from X2212 device...");
                _currentData = X2212Io.ReadAllBytes(_lptBaseAddress, LogMessage);
                UpdateHexDisplay();
                SaveUndoState();
                _dataModified = false;
                LogMessage("Read completed successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Error reading from device: {ex.Message}");
            }
        }

        private void OnDeviceWrite(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Writing to X2212 device...");
                X2212Io.ProgramBytes(_lptBaseAddress, _currentData, LogMessage);
                _dataModified = false;
                LogMessage("Write completed successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Error writing to device: {ex.Message}");
            }
        }

        private void OnDeviceVerify(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Verifying X2212 device content...");
                if (X2212Io.VerifyBytes(_lptBaseAddress, _currentData, out int failAddress, LogMessage))
                {
                    LogMessage("Verification successful - all data matches");
                }
                else
                {
                    LogMessage($"Verification failed at address {failAddress:X2}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error verifying device: {ex.Message}");
            }
        }

        private void OnDeviceStore(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Storing RAM to EEPROM...");
                X2212Io.DoStore(_lptBaseAddress, LogMessage);
                LogMessage("Store operation completed");
            }
            catch (Exception ex)
            {
                LogMessage($"Error storing to EEPROM: {ex.Message}");
            }
        }

        private void OnDeviceProbe(object? sender, EventArgs e)
        {
            try
            {
                if (X2212Io.ProbeDevice(_lptBaseAddress, out string diagnosticInfo, LogMessage))
                {
                    LogMessage($"Device probe successful: {diagnosticInfo}");
                }
                else
                {
                    LogMessage($"Device probe failed: {diagnosticInfo}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error probing device: {ex.Message}");
            }
        }

        private void OnCalibrateTiming(object? sender, EventArgs e)
        {
            try
            {
                X2212Io.TimingCalibrationTest(_lptBaseAddress, LogMessage);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during timing calibration: {ex.Message}");
            }
        }

        // ========== EDIT OPERATIONS ========== //

        private void OnCopyRow(object? sender, EventArgs e)
        {
            if (hexGrid?.CurrentRow == null) return;
            
            int rowIndex = hexGrid.CurrentRow.Index;
            if (rowIndex < 0 || rowIndex >= 16) return;
            
            // Copy the row data
            Array.Copy(_currentData, rowIndex * 8, _clipboardRow, 0, 8);
            LogMessage($"Copied row {rowIndex + 1} to clipboard");
        }

        private void OnPasteToSelected(object? sender, EventArgs e)
        {
            if (hexGrid?.SelectedRows == null || hexGrid.SelectedRows.Count == 0) return;
            
            foreach (DataGridViewRow selectedRow in hexGrid.SelectedRows)
            {
                int rowIndex = selectedRow.Index;
                if (rowIndex < 0 || rowIndex >= 16) continue;
                
                // Paste clipboard data to selected row
                Array.Copy(_clipboardRow, 0, _currentData, rowIndex * 8, 8);
                _dataModified = true;
            }
            
            UpdateHexDisplay();
            LogMessage($"Pasted to {hexGrid.SelectedRows.Count} row(s)");
        }

        private void OnFillAll(object? sender, EventArgs e)
        {
            if (hexGrid?.Rows == null) return;
            
            for (int row = 0; row < 16; row++)
            {
                Array.Copy(_clipboardRow, 0, _currentData, row * 8, 8);
            }
            
            _dataModified = true;
            UpdateHexDisplay();
            LogMessage("Pasted to all rows");
        }

        private void OnClearSelection(object? sender, EventArgs e)
        {
            if (hexGrid?.SelectedRows == null) return;
            
            foreach (DataGridViewRow selectedRow in hexGrid.SelectedRows)
            {
                int rowIndex = selectedRow.Index;
                if (rowIndex < 0 || rowIndex >= 16) continue;
                
                // Clear the row data
                for (int i = 0; i < 8; i++)
                {
                    _currentData[rowIndex * 8 + i] = 0;
                }
                _dataModified = true;
            }
            
            UpdateHexDisplay();
            LogMessage($"Cleared {hexGrid.SelectedRows.Count} row(s)");
        }

        private void OnUndo(object? sender, EventArgs e)
        {
            byte[] temp = _currentData;
            _currentData = _undoData;
            _undoData = temp;
            _dataModified = false;
            UpdateHexDisplay();
            LogMessage("Undo completed");
        }

        private void SaveUndoState()
        {
            Array.Copy(_currentData, _undoData, _currentData.Length);
        }

        private void OnExit(object? sender, EventArgs e)
        {
            if (_dataModified)
            {
                var result = MessageBox.Show("Save changes before exiting?", "Unsaved Changes", 
                                           MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    OnFileSaveAs(sender, e);
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }
            
            Application.Exit();
        }

        // ========== UTILITY METHODS ========== //

        private void LogMessage(string message)
        {
            if (txtMessages != null)
            {
                txtMessages.AppendText($"{DateTime.Now:HH:mm:ss}: {message}\r\n");
                txtMessages.ScrollToCaret();
            }
        }

        private void LoadSettings()
        {
            // Load settings from registry or config file
            try
            {
                // Implementation for loading settings
            }
            catch
            {
                // Silent fail on settings load
            }
        }

        private void SaveSettings()
        {
            // Save settings to registry or config file
            try
            {
                // Implementation for saving settings
            }
            catch
            {
                // Silent fail on settings save
            }
        }
    }
}
