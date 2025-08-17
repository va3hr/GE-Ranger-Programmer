using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Globalization;

public class MainForm : Form
{
    // --- Controls ---
    private readonly MenuStrip _menu = new MenuStrip();
    private readonly ToolStripMenuItem _fileMenu = new ToolStripMenuItem("File");
    private readonly ToolStripMenuItem _deviceMenu = new ToolStripMenuItem("Device");

    // Top area: left = base address row; right = scrollable message box
    private readonly Panel _topPanel = new Panel { Dock = DockStyle.Top, Height = 140, Padding = new Padding(8, 4, 8, 4) };
    private readonly TableLayoutPanel _topLayout = new TableLayoutPanel();

    private readonly FlowLayoutPanel _baseRow = new FlowLayoutPanel
    {
        FlowDirection = FlowDirection.LeftToRight,
        Dock = DockStyle.Fill,
        AutoSize = true,
        WrapContents = false,
        Padding = new Padding(0),
        Margin = new Padding(0)
    };

    private readonly Label _lblBase = new Label { Text = "LPT Base:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
    private readonly TextBox _tbBase = new TextBox { Text = "0xA800", Width = 100, Margin = new Padding(0, 4, 0, 0) };
    private readonly TextBox _log = new TextBox
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        WordWrap = false
    };

    // Main grid
    private readonly DataGridView _grid = new DataGridView
    {
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        ReadOnly = false,            // CH is read-only; others editable
        ScrollBars = ScrollBars.None // show all 16; no scrollbars
    };

    // Current base address
    private ushort _baseAddress = 0xA800;

    public MainForm()
    {
        Text = "X2212 Programmer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 560);

        // Menu
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Top;
        Controls.Add(_menu);

        // Top layout
        _topLayout.Dock = DockStyle.Fill;
        _topLayout.ColumnCount = 2;
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _baseRow.Controls.Add(_lblBase);
        _baseRow.Controls.Add(_tbBase);

        _topLayout.Controls.Add(_baseRow, 0, 0);
        _topLayout.Controls.Add(_log, 1, 0);
        _topPanel.Controls.Add(_topLayout);
        Controls.Add(_topPanel);

        // Grid
        BuildGrid();

        // Put grid below the top panel and let it fill the rest
        _grid.Dock = DockStyle.Fill;
        Controls.Add(_grid);

        // Events
ells[focusCol];
