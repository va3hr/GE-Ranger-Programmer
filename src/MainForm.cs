using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class MainForm : Form
{
    // --- Menus ---
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _fileMenu = new("File");
    private readonly ToolStripMenuItem _deviceMenu = new("Device");

    private readonly ToolStripMenuItem _miOpen = new("Open .RGR…");
    private readonly ToolStripMenuItem _miSaveAs = new("Save As .RGR…");
    private readonly ToolStripMenuItem _miExit = new("Exit");

    // Device actions (Recall removed; Store happens automatically after Write)
    private readonly ToolStripMenuItem _miRead = new("Read (Safe)");
    private readonly ToolStripMenuItem _miVerify = new("Verify");
    private readonly ToolStripMenuItem _miWrite = new("Write (Program + Store)");

    // --- Top area (base address + log) ---
    private readonly Panel _topPanel = new();
    private readonly TableLayoutPanel _topLayout = new();

    private readonly FlowLayoutPanel _baseRow = new();
    private readonly Label _lblBase = new();
    private readonly TextBox _tbBase = new();
    private readonly TextBox _log = new();

    // --- Grid ---
    private readonly DataGridView _grid = new();

    // Current base address
    private ushort _baseAddress = 0xA800;

    // Data buffer for current personality (128 bytes)
    private byte[] _currentData = new byte[128];
    private bool _hasData = false;
    private string _loadedFormat = "ASCII-hex"; // or "binary"

    // Remember last folder for open/save
    private string _lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public MainForm()
    {
        Text = "X2212 Programmer";
        StartPosition = FormStartPosition.CenterScreen;
        MainMenuStrip = _menu;
        KeyPreview = true;

        // ----- Menu -----
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Top;
        Controls.Add(_menu);

        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _miOpen, _miSaveAs, new ToolStripSeparator(), _miExit });
        _miOpen.ShortcutKeys = Keys.Control | Keys.O;
        _miSaveAs.ShortcutKeys = Keys.Control | Keys.S;
        _miOpen.Click += OnOpenClicked;
        _miSaveAs.Click += OnSaveAsClicked;
        _miExit.Click += (s, e) => Close();

        _deviceMenu.DropDownItems.AddRange(new ToolStripItem[] { _miRead, _miVerify, _miWrite });
        _miRead.Click += OnReadClicked;
        _miVerify.Click += OnVerifyClicked;
        _miWrite.Click += OnWriteClicked;

        // ----- Top panel + layout -----
        _topPanel.Dock = DockStyle.Top;
        _topPanel.Height = 140;
        _topPanel.Padding = new Padding(8, 4, 8, 4);

        _topLayout.Dock = DockStyle.Fill;
        _topLayout.ColumnCount = 2;
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Base row (left)
        _baseRow.FlowDirection = FlowDirection.LeftToRight;
        _baseRow.Dock = DockStyle.Fill;
        _baseRow.AutoSize = true;
        _baseRow.WrapContents = false;
        _baseRow.Padding = new Padding(0);
        _baseRow.Margin = new Padding(0);

        _lblBase.Text = "LPT Base:";
        _lblBase.AutoSize = true;
        _lblBase.Margin = new Padding(0, 8, 6, 0);

        _tbBase.Text = "0xA800";
        _tbBase.Width = 100;
        _tbBase.Margin = new Padding(0, 4, 0, 0);
        _tbBase.Leave += OnTbBaseLeave;
        _tbBase.KeyDown += OnTbBaseKeyDown;

        _baseRow.Controls.AddRange(new Control[] { _lblBase, _tbBase });

        // Log (right)
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log_
