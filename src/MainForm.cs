using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        // Core data fields
        private ushort _lptBaseAddress = 0xA800;
        private byte[] _currentData = new byte[128]; // 16 channels × 8 bytes each
        private string _lastFilePath = "";
        private bool _dataModified = false;
        private byte[] _clipboardRow = new byte[8];
        private byte[] _undoData = new byte[128]; // Fixed: Added missing field
        private int _currentChannel = 1; // Fixed: Added missing field
        private int _lastSelectedRow = 0; // Fixed: Added missing field

        // UI Controls - initialized in InitializeComponent
        private MenuStrip menuStrip = null!;
        private ToolStripMenuItem fileMenu = null!;
        private ToolStripMenuItem editMenu = null!;
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

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            InitializeData();
            InitializeSafety();
        }

        private void InitializeComponent()
        {
            Text = "X2212 Programmer";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            
            CreateMenuStrip();
            CreateTopPanel();
            CreateHexGrid();
            CreateMessageArea();
            CreateStatusStrip();
            
            Controls.Add(statusStrip); // Add StatusStrip first
            Controls.Add(txtMessages);
            Controls.Add(hexGrid);
            Controls.Add(topPanel);
            Controls.Add(menuStrip);
            
            MainMenuStrip = menuStrip;
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();
            
            // File Menu
            fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open .RGR...", null, OnFileOpen);
            fileMenu.DropDownItems.Add("Save As .RGR...", null, OnFileSaveAs);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, OnExit);
            
            // Edit Menu
            editMenu = new ToolStripMenuItem("Edit");
            editMenu.DropDownItems.Add("Undo\tCtrl+Z", null, OnUndo);
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Copy Row", null, OnCopyRow);
            editMenu.DropDownItems.Add("Paste to Selected Rows", null, OnPasteToSelected);
            editMenu.DropDownItems.Add("Clear Selection", null, OnClearSelection);
            editMenu.DropDownItems.Add("Fill All", null, OnFillAll);
            
            // Device Menu
            deviceMenu = new ToolStripMenuItem("Device");
            deviceMenu.DropDownItems.Add("Probe", null, OnDeviceProbe);
            deviceMenu.DropDownItems.Add(new ToolStripSeparator());
            deviceMenu.DropDownItems.Add("Read from X2212", null, OnDeviceRead);
            deviceMenu.DropDownItems.Add("Write to X2212", null, OnDeviceWrite);
            deviceMenu.DropDownItems.Add("Verify X2212", null, OnDeviceVerify);
            deviceMenu.DropDownItems.Add("Store to EEPROM", null, OnDeviceStore);
            
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, deviceMenu });
        }

        private void CreateTopPanel()
        {
            topPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = SystemColors.Control
            };

            // LPT Base Address
            lblLptBase = new Label
            {
                Text = "LPT Base:",
                Location = new Point(10, 12),
                Size = new Size(70, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtLptBase = new TextBox
            {
                Location = new Point(85, 10),
                Size = new Size(80, 20),
                Text = $"0x{_lptBaseAddress:X4}"
            };
            txtLptBase.TextChanged += OnLptBaseChanged;

            // Device Type
            lblDevice = new Label
            {
                Text = "Device:",
                Location = new Point(180, 12),
                Size = new Size(50, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtDevice = new TextBox
            {
                Location = new Point(235, 10),
                Size = new Size(60, 20),
                Text = "X2212",
                ReadOnly = true
            };

            // Channel Display
            lblChannel = new Label
            {
                Text = "Channel:",
                Location = new Point(310, 12),
                Size = new Size(55, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtChannel = new TextBox
            {
                Location = new Point(370, 10),
                Size = new Size(60, 20),
                ReadOnly = true
            };

            topPanel.Controls.AddRange(new Control[] 
            { 
                lblLptBase, txtLptBase, lblDevice, txtDevice, lblChannel, txtChannel 
            });
        }

        private void CreateHexGrid()
        {
            hexGrid = new DataGridView
            {
                Location = new Point(0, 40),
                Size = new Size(580, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.Black,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.Black,
                    ForeColor = Color.Lime,
                    Font = new Font("Consolas", 10),
                    SelectionBackColor = Color.Navy,
                    SelectionForeColor = Color.White
                },
                GridColor = Color.DarkGreen,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = true,
                ColumnHeadersVisible = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // Fixed: Create 8 columns for bytes (not 16 nibbles)
            for (int i = 0; i < 8; i++)
            {
                hexGrid.Columns.Add($"Byte{i}", $"{i:X}");
                hexGrid.Columns[i].Width = 40;
            }
            
            // Add ASCII column
            hexGrid.Columns.Add("ASCII", "ASCII");
            hexGrid.Columns[8].Width = 80;

            // Create 16 rows for channels
            for (int i = 0; i < 16; i++)
            {
                int rowIndex = hexGrid.Rows.Add();
                hexGrid.Rows[rowIndex].HeaderCell.Value = GetChannelAddress(i);
            }

            // Wire up events (implementations are in partial classes)
            hexGrid.CellEndEdit += HexGrid_CellEndEdit;
            hexGrid.CellFormatting += HexGrid_CellFormatting;
            hexGrid.SelectionChanged += HexGrid_SelectionChanged;
            hexGrid.MouseDown += HexGrid_MouseDown;
            hexGrid.KeyDown += HexGrid_KeyDown;
        }

        private void CreateMessageArea()
        {
            txtMessages = new TextBox
            {
                Location = new Point(590, 40),
                Size = new Size(400, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusStrip.Items.Add(statusLabel);
        }

        private void InitializeData()
        {
            // Initialize with default values
            for (int i = 0; i < 128; i++)
            {
                _currentData[i] = 0xFF;
            }
            UpdateHexDisplay();
            UpdateChannelDisplay();
        }

        private void InitializeSafety()
        {
            LogMessage("X2212 Programmer initialized");
            LogMessage($"LPT Base Address: 0x{_lptBaseAddress:X4}");
            LogMessage("Ready for device operations");
        }

        private string GetChannelAddress(int row)
        {
            // Channel addressing: Ch1=E0, Ch2=D0, ..., Ch15=00, Ch16=F0
            if (row < 15)
                return $"{(0xE0 - row * 0x10):X2}";
            else
                return "F0"; // Ch16
        }

        private int GetChannelNumber(int row)
        {
            return row + 1; // Ch1 through Ch16
        }

        // Fixed: Added missing utility methods
        private void UpdateChannelDisplay()
        {
            if (txtChannel != null)
            {
                txtChannel.Text = $"Ch{_currentChannel}";
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
        }

        // Partial method declarations (these will be implemented in partial classes)
        partial void OnFileOpen(object? sender, EventArgs e);
        partial void OnFileSaveAs(object? sender, EventArgs e);
        partial void OnLptBaseChanged(object? sender, EventArgs e);
        partial void LoadSettings();
        partial void LogMessage(string message);
        
        partial void OnCopyRow(object? sender, EventArgs e);
        partial void OnPasteToSelected(object? sender, EventArgs e);
        partial void OnUndo(object? sender, EventArgs e);
        partial void OnClearSelection(object? sender, EventArgs e);
        partial void OnFillAll(object? sender, EventArgs e);
        partial void UpdateHexDisplay();
        partial void SaveUndoState();
        
        partial void OnDeviceRead(object? sender, EventArgs e);
        partial void OnDeviceWrite(object? sender, EventArgs e);
        partial void OnDeviceVerify(object? sender, EventArgs e);
        partial void OnDeviceStore(object? sender, EventArgs e);
        partial void OnDeviceProbe(object? sender, EventArgs e);
        
        partial void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e);
        partial void HexGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e);
        partial void HexGrid_SelectionChanged(object? sender, EventArgs e);
        partial void HexGrid_MouseDown(object? sender, MouseEventArgs e);
        partial void HexGrid_KeyDown(object? sender, KeyEventArgs e);

        private void OnExit(object? sender, EventArgs e)
        {
            // TODO: Check for unsaved changes before exiting
            Application.Exit();
        }
    }
}
