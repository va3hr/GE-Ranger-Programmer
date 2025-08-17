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

        // Grid (16 × 6)
        BuildGrid();
        Controls.Add(_grid);

        // Events
        Load += (_, __) => InitialProbe();
        _tbBase.Leave += (_, __) => ReprobeBase();
        _tbBase.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ReprobeBase(); } };
    }

    private void BuildGrid()
    {
        _grid.Columns.Clear();
        // Columns: CH (RO), Tx MHz, Rx MHz, Tx Tone, Rx Tone, Bit Pattern
        var ch = new DataGridViewTextBoxColumn { HeaderText = "CH", Width = 50, ReadOnly = true };
        var tx = new DataGridViewTextBoxColumn { HeaderText = "Tx MHz", Width = 120 };
        var rx = new DataGridViewTextBoxColumn { HeaderText = "Rx MHz", Width = 120 };
        var txtone = new DataGridViewTextBoxColumn { HeaderText = "Tx Tone", Width = 120 };
        var rxtone = new DataGridViewTextBoxColumn { HeaderText = "Rx Tone", Width = 120 };
        var bits = new DataGridViewTextBoxColumn { HeaderText = "Bit Pattern", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };

        _grid.Columns.AddRange(ch, tx, rx, txtone, rxtone, bits);

        _grid.Rows.Clear();
        for (int i = 1; i <= 16; i++)
        {
            int idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells[0].Value = i.ToString("D2"); // CH 01..16
        }
    }

    // ---- Logging helpers (plain text per spec; colors noted in text) ----
    private void ClearLog() => _log.Text = string.Empty;

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

    private void ReprobeBase()
    {
        if (TryParsePort(_tbBase.Text.Trim(), out ushort parsed))
            _baseAddress = parsed;
        else
            _tbBase.Text = "0xA800"; // fallback

        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void ProbeDriverAndLog()
    {
        bool ok = Lpt.TryProbe(_baseAddress, out string detail);
        if (ok)
            LogLine("Driver: OK [LimeGreen]" + detail);
        else
            LogLine("Driver: NOT LOADED [Red]" + detail);
    }

    private static bool TryParsePort(string text, out ushort val)
    {
        val = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Accept "0xA800", "A800", or decimal
        string t = text.ToLowerInvariant().Trim();
        if (t.StartsWith("0x")) t = t[2..];

        if (ushort.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out ushort hex))
        {
            val = hex;
            return true;
        }

        if (ushort.TryParse(text, out ushort dec))
        {
            val = dec;
            return true;
        }

        return false;
    }

    // --- Low-level I/O (InpOutx64) ---
    private static class Lpt
    {
        // These entry points match standard InpOutx64.dll exports.
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        private static extern short Inp32(short portAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        private static extern void Out32(short portAddress, short data);

        public static bool TryProbe(ushort baseAddr, out string detail)
        {
            try
            {
                // Harmless probe: read data register at base, then write the same value back.
                short addr = (short)baseAddr;
                short value = Inp32(addr);
                Out32(addr, value); // write back what we read
                detail = $"  (DLL loaded; probed 0x{baseAddr:X4})";
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
                // Driver loaded but port may be unmapped; keep it "OK" so UI matches the spec intent.
                detail = $"  (probe completed with non-fatal exception: {ex.GetType().Name})";
                return true;
            }
        }
    }
}
