using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        // Core data fields
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

            CreateMenus();
            CreateStatusBar();
            CreateTopPanel();
            CreateHexGrid();
            CreateMessageBox();
            
            UpdateHexDisplay();
            
            // Initialize messages
            this.Load += InitializeMessages;
            this.Shown += ShowAdditionalMessages;
        }

        private void CreateMenus()
        {
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
        }

        private void CreateStatusBar()
        {
            // Create StatusStrip FIRST for proper docking
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
        }

        private void CreateTopPanel()
        {
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

            topPanel.Controls.AddRange(new Control[] { lblLptBase, txtLptBase, lblDevice, txtDevice, lblChannel, txtChannel });
            Controls.Add(topPanel);
        }

        private void CreateHexGrid()
        {
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
            for (int i = 0; i < 8; i++)
            {
                hexGrid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = $"byte{i}",
                    HeaderText = $"{i:X}",
                    Width = 35,
                    MaxInputLength = 2,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }

            hexGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ASCII",
                HeaderText = "ASCII",
                Width = 100,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            // Create rows with channel addresses
            string[] channelAddresses = { "E0", "D0", "C0", "B0", "A0", "90", "80", "70", 
                                          "60", "50", "40", "30", "20", "10", "00", "F0" };
            
            for (int row = 0; row < 16; row++)
            {
                hexGrid.Rows.Add();
                hexGrid.Rows[row].HeaderCell.Value = channelAddresses[row];
                hexGrid.Rows[row].Height = 20;
            }

            // Wire up events (implementations will be in partial classes)
            hexGrid.CellEndEdit += HexGrid_CellEndEdit;
            hexGrid.CellFormatting += HexGrid_CellFormatting;
            hexGrid.SelectionChanged += HexGrid_SelectionChanged;
            hexGrid.MouseDown += HexGrid_MouseDown;
            hexGrid.KeyDown += HexGrid_KeyDown;

            Controls.Add(hexGrid);
        }

        private void CreateMessageBox()
        {
            txtMessages = new TextBox
            {
                Dock = DockStyle.Fill, // Fill remaining space
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.Lime
            };
            Controls.Add(txtMessages);
        }

        private void InitializeMessages(object? sender, EventArgs e)
        {
            LogMessage("=== X2212 Programmer Started ===");
            LogMessage("Message system working properly");
            LogMessage("Ready for operations");
        }

        private void ShowAdditionalMessages(object? sender, EventArgs e)
        {
            LogMessage($"LPT Base Address: 0x{_lptBaseAddress:X4}");
            LogMessage("Use File menu to load .RGR files");
            LogMessage("Use Device menu for X2212 operations");
        }

        // Core utility methods
        private void LogMessage(string msg)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(LogMessage), msg);
                    return;
                }

                if (txtMessages == null || txtMessages.IsDisposed) return;

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
                txtMessages.Update();
                txtMessages.Refresh();
            }
            catch
            {
                // Silent fail
            }
        }

        private void SetStatus(string status)
        {
            try
            {
                if (statusLabel != null && !statusLabel.IsDisposed)
                    statusLabel.Text = status;
            }
            catch
            {
                // Silent fail
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
                LogMessage($"Warning: Driver not found - {ex.Message}");
            }
        }

        // Placeholder methods - will be implemented in partial classes
        private void OnFileOpen(object? sender, EventArgs e) { /* Will implement in FileOperations */ }
        private void OnFileSaveAs(object? sender, EventArgs e) { /* Will implement in FileOperations */ }
        private void OnCopyRow(object? sender, EventArgs e) { /* Will implement in DataManagement */ }
        private void OnPasteToSelected(object? sender, EventArgs e) { /* Will implement in DataManagement */ }
        private void OnUndo(object? sender, EventArgs e) { /* Will implement in DataManagement */ }
        private void OnExit(object? sender, EventArgs e) { Application.Exit(); }
        
        private void OnDeviceRead(object? sender, EventArgs e) { /* Will implement in DeviceOperations */ }
        private void OnDeviceWrite(object? sender, EventArgs e) { /* Will implement in DeviceOperations */ }
        private void OnDeviceVerify(object? sender, EventArgs e) { /* Will implement in DeviceOperations */ }
        private void OnDeviceStore(object? sender, EventArgs e) { /* Will implement in DeviceOperations */ }
        private void OnDeviceProbe(object? sender, EventArgs e) { /* Will implement in DeviceOperations */ }
        
        private void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e) { /* Will implement in EventHandlers */ }
        private void HexGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e) { /* Will implement in EventHandlers */ }
        private void HexGrid_SelectionChanged(object? sender, EventArgs e) { /* Will implement in EventHandlers */ }
        private void HexGrid_MouseDown(object? sender, MouseEventArgs e) { /* Will implement in EventHandlers */ }
        private void HexGrid_KeyDown(object? sender, KeyEventArgs e) { /* Will implement in EventHandlers */ }
        
        private void UpdateHexDisplay() { /* Will implement in DataManagement */ }
        private void UpdateLptBase() { /* Will implement in FileOperations */ }
        private void LoadSettings() { /* Will implement in FileOperations */ }
        private void SaveUndoState() { /* Will implement in DataManagement */ }
    }
}
