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
        private byte[] _currentData = new byte[128];
        private byte[] _undoData = new byte[128];
        private string _lastFilePath = "";
        private string _lastFolderPath = "";
        private int _currentChannel = 1;
        private bool _dataModified = false;
        private byte[] _clipboardRow = new byte[8];
        private int _lastSelectedRow = -1;
        
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

        // CRITICAL: Channel to Address mapping 
        // This maps channel numbers (1-16) to their corresponding hex addresses in the EEPROM
        private readonly Dictionary<int, int> ChannelToAddress = new Dictionary<int, int>
        {
            { 1, 0xE0 },  // Channel 1 starts at address E0
            { 2, 0xD0 },  // Channel 2 starts at address D0
            { 3, 0xC0 },  // Channel 3 starts at address C0
            { 4, 0xB0 },  // Channel 4 starts at address B0
            { 5, 0xA0 },  // Channel 5 starts at address A0
            { 6, 0x90 },  // Channel 6 starts at address 90
            { 7, 0x80 },  // Channel 7 starts at address 80
            { 8, 0x70 },  // Channel 8 starts at address 70
            { 9, 0x60 },  // Channel 9 starts at address 60
            { 10, 0x50 }, // Channel 10 starts at address 50
            { 11, 0x40 }, // Channel 11 starts at address 40
            { 12, 0x30 }, // Channel 12 starts at address 30
            { 13, 0x20 }, // Channel 13 starts at address 20
            { 14, 0x10 }, // Channel 14 starts at address 10
            { 15, 0x00 }, // Channel 15 starts at address 00
            { 16, 0xF0 }  // Channel 16 starts at address F0
        };

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
                RowHeadersWidth = 80,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.Black },
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                StandardTab = true
            };

            // Create columns for 8 bytes per channel
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

            // Create 16 rows for 16 channels
            for (int channel = 1; channel <= 16; channel++)
            {
                hexGrid.Rows.Add();
                int rowIndex = channel - 1;
                // Show both channel number and hex address in row header
                hexGrid.Rows[rowIndex].HeaderCell.Value = $"Ch{channel:D2} [{ChannelToAddress[channel]:X2}]";
                hexGrid.Rows[rowIndex].Height = 20;
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

            // Wire up events after grid is configured
            this.Load += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    hexGrid.CellEndEdit += HexGrid_CellEndEdit;
                    hexGrid.CellFormatting += HexGrid_CellFormatting;
                    hexGrid.SelectionChanged += HexGrid_SelectionChanged;
                    hexGrid.CellDoubleClick += HexGrid_CellDoubleClick;
                    hexGrid.KeyDown += HexGrid_KeyDown;
                    hexGrid.CellPainting += HexGrid_CellPainting;
                    hexGrid.MouseDown += HexGrid_MouseDown;
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

            // Status Strip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusFilePath = new ToolStripStatusLabel("");
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(statusFilePath);
            Controls.Add(statusStrip);

            UpdateHexDisplay();
            
            // Initialize messages
            this.Load += InitializeMessages;
            this.Shown += ShowAdditionalMessages;
        }

        private void InitializeMessages(object? sender, EventArgs e)
        {
            LogMessage("=== X2212 Programmer Started ===");
            LogMessage("Data displayed in LITTLE-ENDIAN format (as stored in RGR files)");
            LogMessage("Channel mapping: Ch1=E0, Ch2=D0, Ch3=C0...Ch15=00, Ch16=F0");
            LogMessage("Ready for operations");
        }

        private void ShowAdditionalMessages(object? sender, EventArgs e)
        {
            LogMessage($"LPT Base Address: 0x{_lptBaseAddress:X4}");
            LogMessage("Use File menu to load .RGR files");
            LogMessage("Use Device menu for X2212 operations");
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

        /// <summary>
        /// CRITICAL METHOD: Maps data from file/EEPROM to display grid
        /// The data in _currentData is stored linearly (0-127)
        /// We need to display it according to channel mapping
        /// </summary>
        private void UpdateHexDisplay()
        {
            if (hexGrid?.Rows == null) return;
            
            try
            {
                // For each channel (1-16), display the data from its corresponding address
                for (int channel = 1; channel <= 16 && channel <= hexGrid.Rows.Count + 1; channel++)
                {
                    int rowIndex = channel - 1;  // Grid row index (0-15)
                    var row = hexGrid.Rows[rowIndex];
                    if (row?.Cells == null) continue;
                    
                    // Get the starting address for this channel
                    int startAddress = ChannelToAddress[channel];
                    
                    // Calculate the offset in our 128-byte array
                    int dataOffset = GetDataOffsetForAddress(startAddress);
                    
                    StringBuilder ascii = new StringBuilder(8);
                    
                    // Display 8 bytes for this channel
                    for (int byteIndex = 0; byteIndex < 8 && byteIndex < row.Cells.Count; byteIndex++)
                    {
                        int finalOffset = dataOffset + byteIndex;
                        if (finalOffset >= 128) finalOffset = finalOffset % 128;
                        
                        byte val = _currentData[finalOffset];
                        
                        var hexCell = row.Cells[byteIndex];
                        if (hexCell != null)
                        {
                            hexCell.Value = $"{val:X2}";
                        }
                        
                        char c = (val >= 32 && val <= 126) ? (char)val : '.';
                        ascii.Append(c);
                    }
                    
                    // Update ASCII cell
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
        }

        /// <summary>
        /// Convert EEPROM address to array offset
        /// </summary>
        protected int GetDataOffsetForAddress(int address)
        {
            // For a 128-byte EEPROM:
            // Addresses 0x00-0x7F map to array indices 0-127
            // Addresses 0x80-0xFF map by subtracting 0x80
            
            if (address >= 0x80)
            {
                // High addresses wrap to upper part of array
                return address - 0x80;  // E0->60, F0->70
            }
            else
            {
                // Low addresses map directly
                return address;
            }
        }

        /// <summary>
        /// Convert array offset back to EEPROM address
        /// </summary>
        protected int GetAddressForDataOffset(int offset)
        {
            // This is the reverse of GetDataOffsetForAddress
            if (offset >= 0x60)  // 96 decimal
            {
                // Upper part of array maps to high addresses
                return offset + 0x80;  // 60->E0, 70->F0
            }
            else
            {
                // Lower part maps directly
                return offset;
            }
        }
    }
}
