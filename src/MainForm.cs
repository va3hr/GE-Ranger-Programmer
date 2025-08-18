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
    // --- Controls ---
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _fileMenu = new("File");
    private readonly ToolStripMenuItem _deviceMenu = new("Device");

    private readonly ToolStripMenuItem _miOpen = new("Open .RGR…");
    private readonly ToolStripMenuItem _miSaveAs = new("Save As .RGR…");
    private readonly ToolStripMenuItem _miExit = new("Exit");

    private readonly Panel _topPanel = new();
    private readonly TableLayoutPanel _topLayout = new();

    private readonly FlowLayoutPanel _baseRow = new();
    private readonly Label _lblBase = new();
    private readonly TextBox _tbBase = new();
    private readonly TextBox _log = new();

    private readonly DataGridView _grid = new();

    // Current base address
    private ushort _baseAddress = 0xA800;

    // Data buffer for current personality (always 128 bytes)
    private byte[] _currentData = new byte[128];
    private bool _hasData = false;
    private string _loadedFormat = "ASCII-hex"; // "ASCII-hex" or "binary"

    // Remember last folder for dialogs
    private string _lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public MainForm()
    {
        // ===== Frozen screen & controls layout =====
        Text = "X2212 Programmer";
        StartPosition = FormStartPosition.CenterScreen;
        MainMenuStrip = _menu;  // ensure menu behavior
        KeyPreview = true;

        // Menu
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Top;
        Controls.Add(_menu);

        // File menu items
        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _miOpen, _miSaveAs, new ToolStripSeparator(), _miExit });
        _miOpen.ShortcutKeys = Keys.Control | Keys.O;
        _miSaveAs.ShortcutKeys = Keys.Control | Keys.S;
        _miOpen.Click += OnOpenClicked;
        _miSaveAs.Click += OnSaveAsClicked;
        _miExit.Click += (s, e) => Close();

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
        Controls.Add(_topPanel);

        // Grid
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = false;               // CH read-only; others editable later
        _grid.ScrollBars = ScrollBars.None;   // show all 16; no scrolling
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.RowTemplate.Height = 22;        // fixed row height for consistent sizing
        _grid.Dock = DockStyle.Top;           // fixed height; we size it to exactly 16 rows + header

        BuildGrid();
        Controls.Add(_grid);

        // Ensure menu stays visually on top
        _menu.BringToFront();

        // ---- Events ----
        Load += OnLoad;
        Shown += OnShown;      // will pin row 01 after layout
        ResizeEnd += OnResizeEnd;

        _tbBase.Leave += OnTbBaseLeave;
        _tbBase.KeyDown += OnTbBaseKeyDown;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        InitialProbe();
    }

    private void OnShown(object? sender, EventArgs e)
    {
        // Post-layout pin using BeginInvoke to run after the first paint cycle
        BeginInvoke(new Action(() =>
        {
            EnsureSixteenVisibleRows();
            ForceTopRow();
        }));
    }

    private void OnResizeEnd(object? sender, EventArgs e)
    {
        EnsureSixteenVisibleRows();
        ForceTopRow();
    }

    private void BuildGrid()
    {
        _grid.Columns.Clear();

        // Columns: CH (RO), Tx MHz, Rx MHz, Tx Tone, Rx Tone, Hex
        var ch = new DataGridViewTextBoxColumn { HeaderText = "CH", Width = 50, ReadOnly = true };
        var tx = new DataGridViewTextBoxColumn { HeaderText = "Tx MHz", Width = 120 };
        var rx = new DataGridViewTextBoxColumn { HeaderText = "Rx MHz", Width = 120 };
        var txtone = new DataGridViewTextBoxColumn { HeaderText = "Tx Tone", Width = 120 };
        var rxtone = new DataGridViewTextBoxColumn { HeaderText = "Rx Tone", Width = 120 };
        var bits = new DataGridViewTextBoxColumn { HeaderText = "Hex", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };

        _grid.Columns.AddRange(ch, tx, rx, txtone, rxtone, bits);

        foreach (DataGridViewColumn c in _grid.Columns)
            c.SortMode = DataGridViewColumnSortMode.NotSortable;

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
        catch { /* ignore early layout timing */ }
    }

    // Size the grid to show exactly 16 rows + header (no vertical scrolling).
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
        ClientSize = new Size(ClientSize.Width, desiredClientHeight);
    }

    // ---- File Open (.RGR only) ----
    private void OnOpenClicked(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open RGR Personality",
            Filter = "RGR files (*.RGR)|*.RGR",
            DefaultExt = "RGR",
            AddExtension = true,
            CheckFileExists = true
        };
        if (Directory.Exists(_lastFolder)) dlg.InitialDirectory = _lastFolder;

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _lastFolder = Path.GetDirectoryName(dlg.FileName) ?? _lastFolder;
            TryLoadRgr(dlg.FileName);
        }
    }

    private void TryLoadRgr(string path)
    {
        try
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            string mode;
            byte[] logical = DecodeRgrBytes(fileBytes, out mode);

            _loadedFormat = mode;
            _hasData = true;
            _currentData = new byte[128];
            if (logical.Length >= 128)
                Array.Copy(logical, 0, _currentData, 0, 128);
            else
                Array.Copy(logical, 0, _currentData, 0, logical.Length); // pad rest with 0

            LogLine($"Opened: {path}");
            LogLine($"Format: {mode}; file bytes: {fileBytes.Length}; logical bytes: {logical.Length}");

            PopulateGridBitPatterns(_currentData);
            PopulateGridFrequencies(_currentData);
            PopulateGridTones(_currentData);
            ForceTopRow();
            Text = "X2212 Programmer — " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            LogLine($"Error opening file: {ex.Message}");
            MessageBox.Show(this, "Failed to open file:\n" + ex.Message, "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool LooksAsciiHex(string s)
    {
        int hexCount = 0;
        foreach (char ch in s)
        {
            if (char.IsWhiteSpace(ch)) continue;
            if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))
            {
                hexCount++;
            }
            else
            {
                return false;
            }
        }
        return (hexCount % 2) == 0 && hexCount >= 2;
    }

    private static byte[] DecodeRgrBytes(byte[] fileBytes, out string mode)
    {
        // Try UTF-8 decode; if it looks like ASCII hex, parse; otherwise treat as binary.
        try
        {
            string text = Encoding.UTF8.GetString(fileBytes);
            if (LooksAsciiHex(text))
            {
                var compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
                int n = compact.Length / 2;
                byte[] bytes = new byte[n];
                for (int i = 0; i < n; i++)
                {
                    string two = compact.Substring(i * 2, 2);
                    bytes[i] = byte.Parse(two, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                mode = "ASCII-hex";
                return bytes;
            }
        }
        catch { /* fall through to binary */ }

        mode = "binary";
        return fileBytes;
    }

    private void PopulateGridBitPatterns(byte[] data)
    {
        int channels = Math.Min(16, data.Length / 8);
        for (int ch = 0; ch < 16; ch++)
        {
            _grid.Rows[ch].Cells[5].Value = "";
        }
        for (int ch = 0; ch < channels; ch++)
        {
            int baseIdx = ch * 8;
            string pattern = string.Format("{0} {1} {2} {3}  {4} {5} {6} {7}",
                data[baseIdx + 0].ToString("X2"),
                data[baseIdx + 1].ToString("X2"),
                data[baseIdx + 2].ToString("X2"),
                data[baseIdx + 3].ToString("X2"),
                data[baseIdx + 4].ToString("X2"),
                data[baseIdx + 5].ToString("X2"),
                data[baseIdx + 6].ToString("X2"),
                data[baseIdx + 7].ToString("X2")
            );
            _grid.Rows[ch].Cells[5].Value = pattern;
        }
    }

    // ---- Frequency decode (Model A: dataset-locked Δ maps) ----
    private void PopulateGridFrequencies(byte[] data)
    {
        int channels = Math.Min(16, data.Length / 8);
        for (int ch = 0; ch < channels; ch++)
        {
            int baseIdx = ch * 8;
            byte A0 = data[baseIdx + 0];
            byte A1 = data[baseIdx + 1];
            byte A2 = data[baseIdx + 2];
            byte B0 = data[baseIdx + 4];
            byte B1 = data[baseIdx + 5];
            byte B2 = data[baseIdx + 6];

            double tx = ToneAndFreq.TxMHz(A0, A1, A2);
            double rx = ToneAndFreq.RxMHz(B0, B1, B2, tx);

            _grid.Rows[ch].Cells[1].Value = tx.ToString("0.000", CultureInfo.InvariantCulture);
            _grid.Rows[ch].Cells[2].Value = rx.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }

    

    private void PopulateGridTones(byte[] data)
    {
        int channels = Math.Min(16, data.Length / 8);
        for (int ch = 0; ch < channels; ch++)
        {
            int baseIdx = ch * 8;
            byte A2 = data[baseIdx + 2];
            byte B2 = data[baseIdx + 6];
            byte B3 = data[baseIdx + 7];

            // TX tone from dataset-locked bit rule
            string txTone = ToneAndFreq.TxToneDisplay(A2, B3);
            _grid.Rows[ch].Cells[3].Value = txTone;

            // RX tone — gold dataset has 0; show blank for now
            _grid.Rows[ch].Cells[4].Value = string.Empty;
        }
    }

    // ---- Save As (.RGR only; preserves input format if possible) ----
    private void OnSaveAsClicked(object? sender, EventArgs e)
    {
        if (!_hasData)
        {
            MessageBox.Show(this, "No data to save yet. Open an .RGR first.", "Save As", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Save RGR Personality",
            Filter = "RGR files (*.RGR)|*.RGR",
            DefaultExt = "RGR",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (Directory.Exists(_lastFolder)) dlg.InitialDirectory = _lastFolder;

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                byte[] bytesToWrite;
                if (string.Equals(_loadedFormat, "binary", StringComparison.OrdinalIgnoreCase))
                {
                    bytesToWrite = _currentData;
                }
                else
                {
                    // ASCII-hex (256 characters, uppercase, no whitespace)
                    var sb = new StringBuilder(_currentData.Length * 2);
                    for (int i = 0; i < _currentData.Length; i++)
                        sb.Append(_currentData[i].ToString("X2", CultureInfo.InvariantCulture));
                    bytesToWrite = Encoding.ASCII.GetBytes(sb.ToString());
                }

                File.WriteAllBytes(dlg.FileName, bytesToWrite);
                _lastFolder = Path.GetDirectoryName(dlg.FileName) ?? _lastFolder;
                LogLine($"Saved: {dlg.FileName} ({bytesToWrite.Length} bytes; format {_loadedFormat})");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to save file:\n" + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ---- Logging + driver ----
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

    private void OnTbBaseLeave(object? sender, EventArgs e) => ReprobeBase();

    private void OnTbBaseKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            ReprobeBase();
        }
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
        if (ok) LogLine("Driver: OK [LimeGreen]" + detail);
        else    LogLine("Driver: NOT LOADED [Red]" + detail);
    }

    private static bool TryParsePort(string text, out ushort val)
    {
        val = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string t = text.Trim();
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t[2..];

        if (ushort.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort hex))
        { val = hex; return true; }

        if (ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort dec))
        { val = dec; return true; }

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
            { detail = "  (inpoutx64.dll not found)"; return false; }
            catch (EntryPointNotFoundException)
            { detail = "  (Inp32/Out32 exports not found)"; return false; }
            catch (BadImageFormatException)
            { detail = "  (bad DLL architecture — ensure x64)"; return false; }
            catch (Exception ex)
            { detail = "  (probe completed with non-fatal exception: " + ex.GetType().Name + ")"; return true; }
        }
    }
}
