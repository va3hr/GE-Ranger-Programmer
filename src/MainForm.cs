using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using RangrApp.Locked; // ToneLock
namespace GE_Ranger_Programmer
{
public class MainForm : Form
{
    private string _lastRgrFolder = "";
    private ushort _lptBaseAddress = 0xA800;

    private readonly MenuStrip _menu = new MenuStrip();
    private readonly ToolStripMenuItem _fileMenu = new ToolStripMenuItem("File");
    private readonly ToolStripMenuItem _deviceMenu = new ToolStripMenuItem("Device");
    private readonly ToolStripMenuItem _openItem = new ToolStripMenuItem("Open…");
    private readonly ToolStripMenuItem _saveAsItem = new ToolStripMenuItem("Save As…");
    private readonly ToolStripMenuItem _exitItem = new ToolStripMenuItem("Exit");

    private readonly Panel _topPanel = new Panel();
    private readonly TableLayoutPanel _topLayout = new TableLayoutPanel();

    private readonly FlowLayoutPanel _baseRow = new FlowLayoutPanel();
    private readonly Label _labelLptBase = new Label();
    private readonly TextBox _textboxLptBase = new TextBox();
    private readonly TextBox _log = new TextBox();

    private readonly DataGridView _grid = new DataGridView();
    private readonly System.Windows.Forms.Timer _firstLayoutNudge = new System.Windows.Forms.Timer();

    private byte[] _logical128 = new byte[128];

    public MainForm()
    {
        Text = "X2212 Programmer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 610);

        _openItem.Click += (_, __) => DoOpen();
        _saveAsItem.Click += (_, __) => DoSaveAs();
        _exitItem.Click += (_, __) => Close();
        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _openItem, _saveAsItem, new ToolStripSeparator(), _exitItem });
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Bottom;
        MainMenuStrip = _menu;
        Controls.Add(_menu);

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

        _labelLptBase.Text = "LPT Base:";
        _labelLptBase.AutoSize = true;
        _labelLptBase.Margin = new Padding(0, 8, 6, 0);

        _textboxLptBase.Text = "0xA800";
        _textboxLptBase.Width = 100;
        _textboxLptBase.Margin = new Padding(0, 4, 0, 0);
        _textboxLptBase.Leave += (_, __) => ReprobeBase();
        _textboxLptBase.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ReprobeBase(); } };

        _baseRow.Controls.Add(_labelLptBase);
        _baseRow.Controls.Add(_textboxLptBase);

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Dock = DockStyle.Fill;
        _log.WordWrap = false;

        _topLayout.Controls.Add(_baseRow, 0, 0);
        _topLayout.Controls.Add(_log, 1, 0);
        _topPanel.Controls.Add(_topLayout);
        Controls.Add(_topPanel);

        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.ScrollBars = ScrollBars.None;
        _grid.Dock = DockStyle.Top;

        BuildGrid();
        Controls.Add(_grid);

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

        _grid.DataError += (s, e) =>
        {
            _log.AppendText("\r\nDataError at row " + e.RowIndex + " col " + e.ColumnIndex);
            e.ThrowException = false;
            e.Cancel = true;
        };

        SizeGridForSixteenRows();
        ForceTopRow();
    }

    private void BuildGrid()
    {
        _grid.Columns.Clear();

        var ch = new DataGridViewTextBoxColumn { HeaderText = "CH", Width = 50, ReadOnly = true };
        var tx = new DataGridViewTextBoxColumn { HeaderText = "Tx MHz", Width = 120 };
        var rx = new DataGridViewTextBoxColumn { HeaderText = "Rx MHz", Width = 120 };

        var txTone = new DataGridViewComboBoxColumn
        {
            HeaderText = "Tx Tone",
            Name = "Tx Tone",
            Width = 120,
            DataSource = ToneLock.ToneMenuTx,
            ValueType = typeof(string),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Standard
        };
        txTone.DefaultCellStyle.NullValue = "ERR";

        var rxTone = new DataGridViewComboBoxColumn
        {
            HeaderText = "Rx Tone",
            Name = "Rx Tone",
            Width = 120,
            DataSource = ToneLock.ToneMenuRx,
            ValueType = typeof(string),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Standard
        };
        rxTone.DefaultCellStyle.NullValue = "ERR";

        var cct = new DataGridViewTextBoxColumn { HeaderText = "cct", Width = 50, ReadOnly = true };
        var ste = new DataGridViewTextBoxColumn { HeaderText = "ste", Width = 50, ReadOnly = true };
        var raw = new DataGridViewTextBoxColumn { HeaderText = "Hex", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true };

        _grid.Columns.AddRange(new DataGridViewColumn[] { ch, tx, rx, txTone, rxTone, cct, ste, raw });
        foreach (DataGridViewColumn c in _grid.Columns) c.SortMode = DataGridViewColumnSortMode.NotSortable;

        _grid.Rows.Clear();
        for (int i = 1; i <= 16; i++)
        {
            int idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells[0].Value = i.ToString("D2");
            _grid.Rows[idx].Cells[3].Value = null;
            _grid.Rows[idx].Cells[4].Value = null;
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

    private void ClearLog() => _log.Text = string.Empty;
    private void LogLine(string msg)
    {
        if (_log.TextLength > 0) _log.AppendText(Environment.NewLine);
        _log.AppendText(msg);
    }

    // ---- LPT probe
    private void InitialProbe()
    {
        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void ReprobeBase()
    {
        if (TryParsePort(_textboxLptBase.Text.Trim(), out ushort parsed)) _lptBaseAddress = parsed;
        else _textboxLptBase.Text = "0xA800";
        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void ProbeDriverAndLog()
    {
        bool ok = Lpt.TryProbe(_lptBaseAddress, out string detail);
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

    // ---- Open/Save
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

    // ---- RGR decode
    private static bool LooksAsciiHex(string text)
    {
        int hexChars = 0;
        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch)) continue;
            bool isHex = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
            if (!isHex) return false;
            hexChars++;
        }
        return hexChars >= 2 && (hexChars % 2) == 0;
    }

    private static byte[] DecodeRgr(byte[] fileBytes)
    {
        try
        {
            string text = Encoding.UTF8.GetString(fileBytes);
            if (LooksAsciiHex(text))
            {
                string compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
                int n = Math.Min(128, compact.Length / 2);
                byte[] logical = new byte[n];
                for (int i = 0; i < n; i++)
                    logical[i] = byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return logical.Length == 128 ? logical : logical.Concat(new byte[128 - logical.Length]).ToArray();
            }
        }
        catch { /* fall through */ }

        // Raw binary fallback
        return fileBytes.Take(128).Concat(Enumerable.Repeat((byte)0, Math.Max(0, 128 - fileBytes.Length))).ToArray();
    }

    // ---- Populate UI
    private void PopulateGridFromLogical(byte[] logical128)
    {
        // Screen→file row mapping
        int[] screenToFile = { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };

        _log.AppendText("\r\n-- ToneDiag start --");

        for (int ch = 0; ch < 16; ch++)
        {
            int fileRowIndex = screenToFile[ch];
            int baseOffset = fileRowIndex * 8;

            byte rowA0 = logical128[baseOffset + 0];
            byte rowA1 = logical128[baseOffset + 1];
            byte rowA2 = logical128[baseOffset + 2];
            byte rowA3 = logical128[baseOffset + 3];
            byte rowB0 = logical128[baseOffset + 4];
            byte rowB1 = logical128[baseOffset + 5];
            byte rowB2 = logical128[baseOffset + 6];
            byte rowB3 = logical128[baseOffset + 7];

            string rawHex = $"{rowA0:X2} {rowA1:X2} {rowA2:X2} {rowA3:X2}  {rowB0:X2} {rowB1:X2} {rowB2:X2} {rowB3:X2}";
            _grid.Rows[ch].Cells[7].Value = rawHex;

            double txMHz = FreqLock.TxMHzLocked(rowA0, rowA1, rowA2);
            double rxMHz;
            try { rxMHz = FreqLock.RxMHzLocked(rowB0, rowB1, rowB2); }
            catch { rxMHz = FreqLock.RxMHz(rowB0, rowB1, rowB2, txMHz); }

            _grid.Rows[ch].Cells[1].Value = txMHz.ToString("0.000", CultureInfo.InvariantCulture);
            _grid.Rows[ch].Cells[2].Value = rxMHz.ToString("0.000", CultureInfo.InvariantCulture);

            string txTone = ToneLock.GetTransmitToneLabel(rowA3, rowA2, rowA1, rowA0, rowB3, rowB2, rowB1, rowB0);
            string rxTone = ToneLock.GetReceiveToneLabel(rowA3);

            var txCell = (DataGridViewComboBoxCell)_grid.Rows[ch].Cells["Tx Tone"];
            if (txTone == "0") { txCell.Style.NullValue = "0"; txCell.Value = null; }
            else if (MenuContains(txTone, ToneLock.ToneMenuTx)) { txCell.Value = txTone; }
            else { txCell.Style.NullValue = "Err"; txCell.Value = null; }

            var rxCell = (DataGridViewComboBoxCell)_grid.Rows[ch].Cells["Rx Tone"];
            if (rxTone == "0") { rxCell.Style.NullValue = "0"; rxCell.Value = null; }
            else if (MenuContains(rxTone, ToneLock.ToneMenuRx)) { rxCell.Value = rxTone; }
            else { rxCell.Style.NullValue = "Err"; rxCell.Value = null; }

            int cctVal = (rowB3 >> 5) & 0x07;
            _grid.Rows[ch].Cells[5].Value = cctVal.ToString(CultureInfo.InvariantCulture);

            bool steEnabled = ToneLock.IsSquelchTailEliminationEnabled(rowA3);
            _grid.Rows[ch].Cells[6].Value = steEnabled ? "Y" : "";

            var txInspect = ToneLock.InspectTransmitBits(rowA3, rowA2, rowA1, rowA0, rowB3, rowB2, rowB1, rowB0);
            int chNo = ch + 1;
            string bits = $"[{txInspect.Bits[0]} {txInspect.Bits[1]} {txInspect.Bits[2]} {txInspect.Bits[3]} {txInspect.Bits[4]} {txInspect.Bits[5]}]";
            _log.AppendText("\r\nCH" + chNo.ToString("D2") +
                            "  TX idx=" + txInspect.Index.ToString(CultureInfo.InvariantCulture) +
                            " " + bits +
                            "  → '" + txTone + "'" +
                            "  RX='" + rxTone + "'" +
                            "  STE=" + (steEnabled ? "1" : "0"));
        }

        _log.AppendText("\r\n-- ToneDiag end --");
        ForceTopRow();
    }

    private static bool MenuContains(string label, string[] menu)
    {
        if (string.IsNullOrEmpty(label)) return false;
        for (int i = 0; i < menu.Length; i++) if (menu[i] == label) return true;
        return false;
    }
}

}
