using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

// Main program UI for the X2212 programmer.
// Style rules (per Peter):
// • Explicit, human-readable names.
// • Deterministic calls to decoding/diagnostic helpers.
// • No guessing, no hidden bridges, no varargs hacks.

public class MainForm : Form
{
    // ---------------------------------------------------------------------
    // Fields (UI + state)
    // ---------------------------------------------------------------------

    private string _lastRgrFolder = "";
    private ushort _baseAddress = 0xA800;

    private readonly MenuStrip _menu = new MenuStrip();
    private readonly ToolStripMenuItem _fileMenu = new ToolStripMenuItem("File");
    private readonly ToolStripMenuItem _deviceMenu = new ToolStripMenuItem("Device");
    private readonly ToolStripMenuItem _openItem = new ToolStripMenuItem("Open…");
    private readonly ToolStripMenuItem _saveAsItem = new ToolStripMenuItem("Save As…");
    private readonly ToolStripMenuItem _exitItem = new ToolStripMenuItem("Exit");

    private readonly Panel _topPanel = new Panel();
    private readonly TableLayoutPanel _topLayout = new TableLayoutPanel();

    private readonly FlowLayoutPanel _baseRow = new FlowLayoutPanel();
    private readonly Label _lblBase = new Label();
    private readonly TextBox _tbBase = new TextBox();
    private readonly TextBox _log = new TextBox();

    private readonly DataGridView _grid = new DataGridView();
    private readonly System.Windows.Forms.Timer _firstLayoutNudge = new System.Windows.Forms.Timer();

    // The 16 channels occupy 8 bytes each (A0..A3, B0..B3) = 128 logical bytes
    private byte[] _logical128 = new byte[128];

    // ---------------------------------------------------------------------
    // Construction & layout
    // ---------------------------------------------------------------------

    public MainForm()
    {
        Text = "X2212 Programmer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 610);

        // Menu
        _openItem.Click += (_, __) => DoOpen();
        _saveAsItem.Click += (_, __) => DoSaveAs();
        _exitItem.Click += (_, __) => Close();
        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _openItem, _saveAsItem, new ToolStripSeparator(), _exitItem });
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Bo_
