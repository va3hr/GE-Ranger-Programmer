// MainForm.cs - Core UI and Initialization
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
        
        // UI Controls - Made nullable
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
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.Black }
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

            // Create rows
            string[] channelAddresses = { "E0", "D0", "C0", "B0", "A0", "90", "80", "70", 
                                          "60", "50", "40", "30", "20", "10", "00", "F0" };
            
            for (int row = 0; row < 16; row++)
            {
                hexGrid.Rows.Add();
                hexGrid.Rows[row].HeaderCell.Value = channelAddresses[row];
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

            hexGrid.CellEndEdit += HexGrid_CellEndEdit;
            hexGrid.CellFormatting += HexGrid_CellFormatting;
            hexGrid.SelectionChanged += HexGrid_SelectionChanged;
            hexGrid.CellDoubleClick += HexGrid_CellDoubleClick;
            hexGrid.KeyDown += HexGrid_KeyDown;
            hexGrid.CellPainting += HexGrid_CellPainting;
            hexGrid.MouseDown += HexGrid_MouseDown;
            Controls.Add(hexGrid);

            // Message Box - ENSURE PROPER COLORS AND POSITIONING
            txtMessages = new TextBox
            {
                Dock = DockStyle.Fill, // Fill remaining space after StatusStrip, TopPanel, and HexGrid
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.Lime  // BRIGHT GREEN TEXT
            };
            Controls.Add(txtMessages); // Add LAST so it fills remaining space

            UpdateHexDisplay();
            
            // Initialize messages
            this.Load += InitializeMessages;
            this.Shown += ShowAdditionalMessages;
        }

        private void InitializeMessages(object? sender, EventArgs e)
        {
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

        // NOTE: All event handlers and operations are implemented in the partial class files:
        // - File/device operations: MainForm.Events.cs
        // - Data operations: MainForm.Operations.cs
    }
}
}
}
