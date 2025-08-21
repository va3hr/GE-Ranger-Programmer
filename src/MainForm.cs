using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using RangrApp.Locked;
public class MainForm : Form
{
    // ===== State =====
    private string _lastRgrFolder = "";
    private ushort _baseAddress = 0xA800;

    // ===== UI =====
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

    // last-loaded logical 128 bytes
    private byte[] _logical128 = new byte[128];

    public MainForm()
    {
        // ===== LAYOUT LOCK (unchanged) =====
        Text = "X2212 Programmer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 610);

        // Bottom-left menu
        _openItem.Click += (_, __) => DoOpen();
        _saveAsItem.Click += (_, __) => DoSaveAs();
        _exitItem.Click += (_, __) => Close();
        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _openItem, _saveAsItem, new ToolStripSeparator(), _exitItem });
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Bottom;
        MainMenuStrip = _menu;
        Controls.Add(_menu);

        // Top area
        _topPanel.Dock = DockStyle.Top;
        _topPanel.Height = 150;
        _topPanel.Padding = new Padding(8, 4, 8, 4);

        _topLayout.Dock = DockStyle.Fill;
        _topLayout.ColumnCount = 2;
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

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
        _tbBase.Leave += (_, __) => ReprobeBase();
        _tbBase.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ReprobeBase(); } };

        _baseRow.Controls.Add(_lblBase);
        _baseRow.Controls.Add(_tbBase);

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Dock = DockStyle.Fill;
        _log.WordWrap = false;

        _topLayout.Controls.Add(_baseRow, 0, 0);
        _topLayout.Controls.Add(_log, 1, 0);
        _topPanel.Controls.Add(_topLayout);
        Controls.Add(_topPanel);

        // Grid
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.ScrollBars = ScrollBars.None;
        _grid.Dock = DockStyle.Top;

        BuildGrid();
        Controls.Add(_grid);

        // Events
        Load += (_, __) => InitialProbe();
        Shown += (_, __) => { _firstLayoutNudge.Enabled = true; };
        _firstLayoutNudge.Interval = 50;
        _firstLayoutNudge.Tick += (s, e) =>
        {
            _firstLayoutNudge.Enabled = false;
            SizeGridForSixteenRows();
            ForceTopRow();
        };
        ResizeEnd += (_, __) =>
        {
            SizeGridForSixteenRows();
            ForceTopRow();
        };
        _grid.SizeChanged += (_, __) => ForceTopRow();

        // Silence ComboBox invalid-value popups
        _grid.DataError += (s, e) => { e.ThrowException = false; e.Cancel = true; };

        // Initial layout
        SizeGridForSixteenRows();
        ForceTopRow();
    }

    // ===== Grid construction =====
    private void BuildGrid()
    {
        _grid.Columns.Clear();

        var ch = new DataGridViewTextBoxColumn { HeaderText = "CH", Width = 50, ReadOnly = true };
        var tx = new DataGridViewTextBoxColumn { HeaderText = "Tx MHz", Width = 120 };
        var rx = new DataGridViewTextBoxColumn { HeaderText = "Rx MHz", Width = 120 };

        var txTone = new DataGridViewComboBoxColumn
        {
            HeaderText = "Tx Tone",
            Width = 120,
            DataSource = ToneLock.ToneMenuAll,
            ValueType = typeof(string),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Standard
        };
        txTone.DefaultCellStyle.NullValue = "0";

        var rxTone = new DataGridViewComboBoxColumn
        {
            HeaderText = "Rx Tone",
            Width = 120,
            DataSource = ToneLock.ToneMenuAll,
            ValueType = typeof(string),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Standard
        };
        rxTone.DefaultCellStyle.NullValue = "0";

        var cct = new DataGridViewTextBoxColumn { HeaderText = "cct", Width = 50, ReadOnly = true };
        var ste = new DataGridViewTextBoxColumn { HeaderText = "ste", Width = 50, ReadOnly = true };
        var bits = new DataGridViewTextBoxColumn { HeaderText = "Hex", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true };

        _grid.Columns.AddRange(new DataGridViewColumn[] { ch, tx, rx, txTone, rxTone, cct, ste, bits });

        foreach (DataGridViewColumn c in _grid.Columns)
            c.SortMode = DataGridViewColumnSortMode.NotSortable;

        _grid.Rows.Clear();
        for (int i = 1; i <= 16; i++)
        {
            int idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells[0].Value = i.ToString("D2");
            _grid.Rows[idx].Cells[3].Value = "0";
            _grid.Rows[idx].Cells[4].Value = "0";
        }
    }

    private void SizeGridForSixteenRows()
    {
        if (_grid.Rows.Count == 0) return;
        int rowH = _grid.Rows[0].Height;
        int headerH = _grid.ColumnHeadersHeight;
        int desired = headerH + (rowH * 16) + 2;
        _grid.Height = desired;

        if (ClientSize.Width < 1000)
            ClientSize = new Size(1000, ClientSize.Height);
    }

    private void ForceTopRow()
    {
        if (_grid.Rows.Count == 0) return;
        try
        {
            _grid.ClearSelection();
            _grid.FirstDisplayedScrollingRowIndex = 0;
            _grid.CurrentCell = _grid.Rows[0].Cells[1];
        }
        catch { }
    }

    // ===== Logging =====
    private void ClearLog() => _log.Text = string.Empty;
    private void LogLine(string msg)
    {
        if (_log.TextLength > 0) _log.AppendText(Environment.NewLine);
        _log.AppendText(msg);
    }

    // ===== Driver probe =====
    private void InitialProbe()
    {
        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void ReprobeBase()
    {
        if (TryParsePort(_tbBase.Text.Trim(), out ushort parsed)) _baseAddress = parsed;
        else _tbBase.Text = "0xA800";
        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void ProbeDriverAndLog()
    {
        bool ok = Lpt.TryProbe(_baseAddress, out string detail);
        if (ok) LogLine("Driver: OK [LimeGreen]" + detail);
        else LogLine("Driver: NOT LOADED [Red]" + detail);
    }

    private static bool TryParsePort(string text, out ushort val)
    {
        val = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string t = text.Trim();
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);
        if (ushort.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort hex)) { val = hex; return true; }
        if (ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort dec)) { val = dec; return true; }
        return false;
    }

    private static class Lpt
    {
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")] private static extern short Inp32(short portAddress);
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")] private static extern void Out32(short portAddress, short data);

        public static bool TryProbe(ushort baseAddr, out string detail)
        {
            try
            {
                short a = (short)baseAddr;
                short v = Inp32(a);
                Out32(a, v);
                detail = $"  (DLL loaded; probed 0x{baseAddr:X4})";
                return true;
            }
            catch (DllNotFoundException) { detail = "  (inpoutx64.dll not found)"; return false; }
            catch (EntryPointNotFoundException) { detail = "  (Inp32/Out32 exports not found)"; return false; }
            catch (BadImageFormatException) { detail = "  (bad DLL architecture — ensure x64)"; return false; }
            catch (Exception ex) { detail = $"  (probe completed with non-fatal exception: {ex.GetType().Name})"; return true; }
        }
    }

    // ===== File I/O =====
    private void DoOpen()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open RGR",
            Filter = "Ranger RGR (*.RGR)|*.RGR",
            InitialDirectory = Directory.Exists(_lastRgrFolder)
                ? _lastRgrFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var bytes = File.ReadAllBytes(dlg.FileName);
            _logical128 = DecodeRgr(bytes);
            PopulateGridFromLogical(_logical128);

            _lastRgrFolder = Path.GetDirectoryName(dlg.FileName) ?? _lastRgrFolder;
            LogLine("Opened: " + dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Failed to open file:\r\n" + ex.Message, "Open RGR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DoSaveAs()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Save RGR As",
            Filter = "Ranger RGR (*.RGR)|*.RGR",
            FileName = "NEW.RGR",
            InitialDirectory = Directory.Exists(_lastRgrFolder)
                ? _lastRgrFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllBytes(dlg.FileName, _logical128 ?? new byte[128]);
            _lastRgrFolder = Path.GetDirectoryName(dlg.FileName) ?? _lastRgrFolder;
            LogLine("Saved: " + dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Failed to save file:\r\n" + ex.Message, "Save RGR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool LooksAsciiHex(string text)
    {
        int hex = 0;
        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch)) continue;
            if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')) hex++;
            else return false;
        }
        return hex >= 2 && (hex % 2) == 0;
    }

    private static byte[] DecodeRgr(byte[] fileBytes)
    {
        // Try ASCII-hex first
        try
        {
            string text = Encoding.UTF8.GetString(fileBytes);
            if (LooksAsciiHex(text))
            {
                string compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
                int n = compact.Length / 2;
                byte[] logical = new byte[Math.Min(128, n)];
                for (int i = 0; i < logical.Length; i++)
                    logical[i] = byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return logical;
            }
        }
        catch { }
        // Binary
        return fileBytes.Take(128).ToArray();
    }

    private void PopulateGridFromLogical(byte[] logical128)
    {
        for (int ch = 0; ch < 16; ch++)
        {
            int i = ch * 8;
            byte A0 = logical128[i + 0];
            byte A1 = logical128[i + 1];
            byte A2 = logical128[i + 2];
            byte A3 = logical128[i + 3];
            byte B0 = logical128[i + 4];
            byte B1 = logical128[i + 5];
            byte B2 = logical128[i + 6];
            byte B3 = logical128[i + 7];

            // Hex column (A0..B3)
            string hex = $"{A0:X2} {A1:X2} {A2:X2} {A3:X2}  {B0:X2} {B1:X2} {B2:X2} {B3:X2}";
            _grid.Rows[ch].Cells[7].Value = hex;

            // Frequencies (locked)
            double tx = FreqLock.TxMHzLocked(A0, A1, A2);
            double rx;
            try { rx = FreqLock.RxMHzLocked(B0, B1, B2); }
            catch { rx = FreqLock.RxMHz(B0, B1, B2, tx); }

            _grid.Rows[ch].Cells[1].Value = tx.ToString("0.000", CultureInfo.InvariantCulture);
            _grid.Rows[ch].Cells[2].Value = rx.ToString("0.000", CultureInfo.InvariantCulture);

            // NEW: tones via ToneLock

            // TX tone (locked window you previously used)
            string txTone = ToneLock.TxToneFromBytes(A2, B3);
            _grid.Rows[ch].Cells[3].Value = txTone;

            // RX tone (provisional window; big-endian mask in ToneLock)
            string rxTone = ToneLock.RxToneFromBytes(B1, B2, B3);
            _grid.Rows[ch].Cells[4].Value = rxTone;

            // cct (current heuristic) and ste
            int cctVal = (B3 >> 5) & 0x07;
            _grid.Rows[ch].Cells[5].Value = cctVal.ToString(CultureInfo.InvariantCulture);

            string steVal = ((A3 & 0x80) != 0) ? "Y" : "";
            _grid.Rows[ch].Cells[6].Value = steVal;
        }
        ForceTopRow();
    }
}
