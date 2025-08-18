using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class MainForm : Form
{
    // --- Controls ---
    private MenuStrip _menu = new MenuStrip();
    private ToolStripMenuItem _fileMenu = new ToolStripMenuItem("File");
    private ToolStripMenuItem _deviceMenu = new ToolStripMenuItem("Device");

    private Panel _topPanel = new Panel();
    private TableLayoutPanel _topLayout = new TableLayoutPanel();

    private FlowLayoutPanel _baseRow = new FlowLayoutPanel();
    private Label _lblBase = new Label();
    private TextBox _tbBase = new TextBox();
    private TextBox _log = new TextBox();

    private DataGridView _grid = new DataGridView();

    // Current base address
    private ushort _baseAddress = 0xA800;

    public MainForm()
    {
        // Form
        this.Text = "X2212 Programmer";
        this.StartPosition = FormStartPosition.CenterScreen;

        // Menu
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Top;
        this.Controls.Add(_menu);

        // Top panel + layout
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

        _baseRow.Controls.Add(_lblBase);
        _baseRow.Controls.Add(_tbBase);

        // Log (right)
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Dock = DockStyle.Fill;
        _log.WordWrap = false;

        _topLayout.Controls.Add(_baseRow, 0, 0);
        _topLayout.Controls.Add(_log, 1, 0);
        _topPanel.Controls.Add(_topLayout);
        this.Controls.Add(_topPanel);

        // Grid
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = false;               // CH is RO; others editable
        _grid.ScrollBars = ScrollBars.None;   // show all 16; no scrolling
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.RowTemplate.Height = 22;        // fixed row height for consistent sizing
        _grid.Dock = DockStyle.Top;           // fixed height; we will size it to exactly 16 rows + header

        BuildGrid();
        this.Controls.Add(_grid);

        // ---- Events ----
        this.Load += OnLoad;
        this.Shown += OnShown;      // will pin row 01 after layout
        this.ResizeEnd += OnResizeEnd;

        _tbBase.Leave += OnTbBaseLeave;
        _tbBase.KeyDown += OnTbBaseKeyDown;
    }

    private void OnLoad(object sender, EventArgs e)
    {
        InitialProbe();
    }

    private void OnShown(object sender, EventArgs e)
    {
        // Post-layout pin using BeginInvoke to run after the first paint cycle
        this.BeginInvoke(new Action(() =>
        {
            EnsureSixteenVisibleRows();
            ForceTopRow();
        }));
    }

    private void OnResizeEnd(object sender, EventArgs e)
    {
        EnsureSixteenVisibleRows();
        ForceTopRow();
    }

    private void BuildGrid()
    {
        _grid.Columns.Clear();

        // Columns: CH (RO), Tx MHz, Rx MHz, Tx Tone, Rx Tone, Bit Pattern
        DataGridViewTextBoxColumn ch = new DataGridViewTextBoxColumn();
        ch.HeaderText = "CH";
        ch.Width = 50;
        ch.ReadOnly = true;

        DataGridViewTextBoxColumn tx = new DataGridViewTextBoxColumn();
        tx.HeaderText = "Tx MHz";
        tx.Width = 120;

        DataGridViewTextBoxColumn rx = new DataGridViewTextBoxColumn();
        rx.HeaderText = "Rx MHz";
        rx.Width = 120;

        DataGridViewTextBoxColumn txtone = new DataGridViewTextBoxColumn();
        txtone.HeaderText = "Tx Tone";
        txtone.Width = 120;

        DataGridViewTextBoxColumn rxtone = new DataGridViewTextBoxColumn();
        rxtone.HeaderText = "Rx Tone";
        rxtone.Width = 120;

        DataGridViewTextBoxColumn bits = new DataGridViewTextBoxColumn();
        bits.HeaderText = "Bit Pattern";
        bits.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _grid.Columns.AddRange(new DataGridViewColumn[] { ch, tx, rx, txtone, rxtone, bits });

        foreach (DataGridViewColumn c in _grid.Columns)
        {
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        _grid.Rows.Clear();
        for (int i = 1; i <= 16; i++)
        {
            int idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells[0].Value = i.ToString("D2"); // 01..16
        }

        // Set initial focus and scroll position immediately after creating rows
        ForceTopRow();
    }

    private void ForceTopRow()
    {
        if (_grid.Rows.Count == 0) return;

        try
        {
            _grid.ClearSelection();
            _grid.FirstDisplayedScrollingRowIndex = 0; // show CH 01 at top
            int focusCol = (_grid.ColumnCount > 1) ? 1 : 0; // Tx MHz if present
            _grid.CurrentCell = _grid.Rows[0].Cells[focusCol];
            _grid.Rows[0].Selected = true;
        }
        catch
        {
            // ignore early layout timing
        }
    }

    // Size the grid itself so exactly 16 rows + header are visible (no vertical scrolling).
    private void EnsureSixteenVisibleRows()
    {
        if (_grid.Rows.Count == 0) return;

        int rowHeight = Math.Max(_grid.RowTemplate.Height, 20);
        int headerH   = Math.Max(_grid.ColumnHeadersHeight, 22);
        int desiredGridHeight = headerH + (rowHeight * 16) + 2; // +2 border/fudge

        _grid.Height = desiredGridHeight;

        // Make the window tall enough so the grid fits under the top panel + menu
        int menuH  = _menu.Height;
        int topH   = _topPanel.Height;
        int desiredClientHeight = menuH + topH + desiredGridHeight;

        this.ClientSize = new Size(this.ClientSize.Width, desiredClientHeight);
    }

    // ---- Logging + driver ----
    private void ClearLog()
    {
        _log.Text = string.Empty;
    }

    private void LogLine(string msg)
    {
        if (_log.TextLength > 0) _log.AppendText(Environment.NewLine);
        _log.AppendText(msg);
    }

    private void InitialProbe()
    {
        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void OnTbBaseLeave(object sender, EventArgs e)
    {
        ReprobeBase();
    }

    private void OnTbBaseKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            ReprobeBase();
        }
    }

    private void ReprobeBase()
    {
        ushort parsed;
        if (TryParsePort(_tbBase.Text.Trim(), out parsed))
        {
            _baseAddress = parsed;
        }
        else
        {
            _tbBase.Text = "0xA800"; // fallback
        }

        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void ProbeDriverAndLog()
    {
        string detail;
        bool ok = Lpt.TryProbe(_baseAddress, out detail);
        if (ok)
        {
            LogLine("Driver: OK [LimeGreen]" + detail);
        }
        else
        {
            LogLine("Driver: NOT LOADED [Red]" + detail);
        }
    }

    private static bool TryParsePort(string text, out ushort val)
    {
        val = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string t = text.Trim();
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            t = t.Substring(2);
        }

        ushort hex;
        if (ushort.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex))
        {
            val = hex;
            return true;
        }

        ushort dec;
        if (ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out dec))
        {
            val = dec;
            return true;
        }

        return false;
    }

    // --- Low-level I/O (InpOutx64) ---
    private static class Lpt
    {
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        private static extern short Inp32(short portAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        private static extern void Out32(short portAddress, short data);

        public static bool TryProbe(ushort baseAddr, out string detail)
        {
            try
            {
                short addr = (short)baseAddr;
                short value = Inp32(addr);
                Out32(addr, value);
                detail = "  (DLL loaded; probed 0x" + baseAddr.ToString("X4") + ")";
                return true;
            }
            catch (DllNotFoundException)
            {
                detail = "  (inpoutx64.dll not found)";
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                detail = "  (Inp32/Out32 exports not found)";
                return false;
            }
            catch (BadImageFormatException)
            {
                detail = "  (bad DLL architecture — ensure x64)";
                return false;
            }
            catch (Exception ex)
            {
                detail = "  (probe completed with non-fatal exception: " + ex.GetType().Name + ")";
                return true;
            }
        }
    }
}
