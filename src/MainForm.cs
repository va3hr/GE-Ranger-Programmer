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
            deviceMenu.DropDownItems.Add(new ToolStripSeparator());
            deviceMenu.DropDownItems.Add("Calibrate Timing", null, OnCalibrateT timing);
            
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

            // Hex Grid - CRITICAL FIX: Changed EditMode to prevent reentrant calls
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
                // CRITICAL: Set EditMode to prevent automatic cell navigation
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                // Prevent Tab from moving to next cell during edit
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

            // CRITICAL FIX: Wire up events AFTER grid is fully configured
            // and use BeginInvoke for event wiring to prevent initialization conflicts
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
                // Temporarily disable events to prevent reentrant calls during update
                hexGrid.CellValueChanged -= HexGrid_CellEndEdit;
                
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
            finally
            {
                // Re-enable events after update
                if (hexGrid != null)
                {
                    hexGrid.CellValueChanged += HexGrid_CellEndEdit;
                }
            }
        }
    }
}
