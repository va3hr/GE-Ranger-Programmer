using System; 
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class MainForm : Form
{
    // --- Controls ---
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _fileMenu = new("File");
    private readonly ToolStripMenuItem _deviceMenu = new("Device");

    // Top area: left = base address row; right = scrollable message box
    private readonly Panel _topPanel = new() { Dock = DockStyle.Top, Height = 140, Padding = new Padding(8, 4, 8, 4) };
    private readonly TableLayoutPanel _topLayout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
    };

    private readonly FlowLayoutPanel _baseRow = new()
    {
        FlowDirection = FlowDirection.LeftToRight,
        Dock = DockStyle.Fill,
        AutoSize = true,
        WrapContents = false,
        Padding = new Padding(0),
        Margin = new Padding(0)
    };

    private readonly Label _lblBase = new() { Text = "LPT Base:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
    private readonly TextBox _tbBase = new() { Text = "0xA800", Width = 100, Margin = new Padding(0, 4, 0, 0) };
    private readonly TextBox _log = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        WordWrap = false
    };

    // Main grid
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        ReadOnly = false // CH is read-only; others editable
    };

    // Current base address
    private ushort _baseAddress = 0xA800;

    public MainForm()
    {
        Text = "X2212 Programmer";
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterScreen;

        // Menus (top-left)
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Top;
        Controls.Add(_menu);

        // Top layout (base address + message box)
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _baseRow.Controls.Add(_lblBase);
        _baseRow.Controls.Add(_tbBase);

        _topLayout.Controls.Add(_baseRow, 0, 0);
        _topLayout.Controls.Add(_log, 1, 0);
        _topPanel.Controls.Add(_topLayout);
        Controls.Add(_topPanel);
