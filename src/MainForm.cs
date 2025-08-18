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
    private readonly MenuStrip _menu = new MenuStrip();
    private readonly ToolStripMenuItem _fileMenu = new ToolStripMenuItem("File");
    private readonly ToolStripMenuItem _deviceMenu = new ToolStripMenuItem("Device");

    private readonly ToolStripMenuItem _miOpen = new ToolStripMenuItem("Open .RGR…");
    private readonly ToolStripMenuItem _miSaveAs = new ToolStripMenuItem("Save As .RGR…");
    private readonly ToolStripMenuItem _miExit = new ToolStripMenuItem("Exit");

    private readonly ToolStripMenuItem _miRead = new ToolStripMenuItem("Read (Safe)");
    private readonly ToolStripMenuItem _miVerify = new ToolStripMenuItem("Verify");
    private readonly ToolStripMenuItem _miWrite = new ToolStripMenuItem("Write (Program + Store)");

    // --- Top area ---
    private readonly Panel _topPanel = new Panel();
    private readonly TableLayoutPanel _topLayout = new TableLayoutPanel();

    private readonly FlowLayoutPanel _baseRow = new FlowLayoutPanel();
    private readonly Label _lblBase = new Label();
    private readonly TextBox _tbBase = new TextBox();
    private readonly TextBox _log = new TextBox();

    // --- Grid ---
    private readonly DataGridView _grid = new DataGridView();

    // Settings
    private AppSettings _settings = AppSettings.Load();

    // Current base address
    private ushort _baseAddress = 0xA800;

    // Data buffer for current personality (128 bytes)
    private byte[] _currentData = new byte[128];
    private bool _hasData = false;
    private string _loadedFormat = "ASCII-hex"; // or "binary"

    private string _lastFolder = AppSettings.Load().LastRgrFolder;

    public MainForm()
    {
        this.Text = "X2212 Programmer";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MainMenuStrip = _menu;
        // Apply saved window geometry (if present)
        try
        {
            if (_settings.WindowW > 0 && _settings.WindowH > 0)
            {
                this.StartPosition = FormStartPosition.Manual;
                var bounds = new Rectangle(
                    Math.Max(_settings.WindowX, 50),
                    Math.Max(_settings.WindowY, 50),
                    Math.Max(_settings.WindowW, 820),
                    Math.Max(_settings.WindowH, 560));
                // Ensure it lands on any screen
                var onScreen = false;
                foreach (var scr in Screen.AllScreens)
                    if (scr.WorkingArea.IntersectsWith(bounds)) { onScreen = true; break; }
                if (!onScreen) bounds.Location = new Point(100, 100);
                this.Bounds = bounds;
            }
        }
        catch { }


        // Menus
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _deviceMenu });
        _menu.Dock = DockStyle.Top;
        this.Controls.Add(_menu);

        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _miOpen, _miSaveAs, new ToolStripSeparator(), _miExit });
        _miOpen.Click += OnOpenClicked;
        _miSaveAs.Click += OnSaveAsClicked;
        _miExit.Click += (s, e) => this.Close();

        _deviceMenu.DropDownItems.AddRange(new ToolStripItem[] { _miRead, _miVerify, _miWrite });
        _miRead.Click += OnReadClicked;
        _miVerify.Click += OnVerifyClicked;
        _miWrite.Click += OnWriteClicked;

        // Top panel + layout
        _topPanel.Dock = DockStyle.Top;
        _topPanel.Height = 140;
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

        _tbBase.Text = string.IsNullOrWhiteSpace(_settings.LptBaseText) ? "0xA800" : _settings.LptBaseText;
        _tbBase.Width = 100;
        _tbBase.Margin = new Padding(0, 4, 0, 0);
        _tbBase.Leave += OnTbBaseLeave;
        _tbBase.KeyDown += OnTbBaseKeyDown;

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
        this.Controls.Add(_topPanel);

        // Grid
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = false;
        _grid.ScrollBars = ScrollBars.None;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.RowTemplate.Height = 22;
        _grid.Dock = DockStyle.Top;

        BuildGrid();
        this.Controls.Add(_grid);

        _menu.BringToFront();
        this.FormClosing += (s, e) =>
        {
            try
            {
                // Persist last folder & base address
                _settings.LastRgrFolder = _lastFolder;
                _settings.LptBaseText = _tbBase.Text.Trim();

                // Persist geometry (use RestoreBounds if minimized/maximized)
                var r = (this.WindowState == FormWindowState.Normal) ? this.Bounds : this.RestoreBounds;
                _settings.WindowX = r.X;
                _settings.WindowY = r.Y;
                _settings.WindowW = r.Width;
                _settings.WindowH = r.Height;

                _settings.Save();
            }
            catch { }
        };


        // Events
        this.Load += OnLoad;
        this.Shown += OnShown;
        this.ResizeEnd += OnResizeEnd;
        _grid.DataError += Grid_DataError;
    }

    private void OnLoad(object sender, EventArgs e) => InitialProbe();

    private void OnShown(object sender, EventArgs e)
    {
        this.BeginInvoke(new Action(() =>
        {
            EnsureSixteenVisibleRows();
            EnsureMinWidth();
            ForceTopRow();
        }));
    }

    private void OnResizeEnd(object sender, EventArgs e)
    {
        EnsureSixteenVisibleRows();
        EnsureMinWidth();
        ForceTopRow();
    }

    private void BuildGrid()
    {
        _grid.Columns.Clear();

        var ch = new DataGridViewTextBoxColumn { HeaderText = "CH", Width = 50, ReadOnly = true };
        var tx = new DataGridViewTextBoxColumn { HeaderText = "Tx MHz", Width = 120 };
        var rx = new DataGridViewTextBoxColumn { HeaderText = "Rx MHz", Width = 120 };

        var txtone = new DataGridViewComboBoxColumn
        {
            HeaderText = "Tx Tone",
            Width = 120,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        };
        txtone.Items.AddRange(ToneAndFreq.ToneMenu);

        var rxtone = new DataGridViewComboBoxColumn
        {
            HeaderText = "Rx Tone",
            Width = 120,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        };
        rxtone.Items.AddRange(ToneAndFreq.ToneMenu);

        var bits = new DataGridViewTextBoxColumn { HeaderText = "Hex", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };

        _grid.Columns.AddRange(new DataGridViewColumn[] { ch, tx, rx, txtone, rxtone, bits });
        foreach (DataGridViewColumn c in _grid.Columns) c.SortMode = DataGridViewColumnSortMode.NotSortable;

        _grid.Rows.Clear();
        for (int i = 1; i <= 16; i++)
        {
            int idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells[0].Value = i.ToString("D2");
            _grid.Rows[idx].Cells[3].Value = "0";
            _grid.Rows[idx].Cells[4].Value = "0";
        }
        ForceTopRow();
    }

    private void ForceTopRow()
    {
        if (_grid.Rows.Count == 0) return;
        try
        {
            _grid.ClearSelection();
            _grid.FirstDisplayedScrollingRowIndex = 0;
            int focusCol = (_grid.ColumnCount > 1) ? 1 : 0;
            _grid.CurrentCell = _grid.Rows[0].Cells[focusCol];
            _grid.Rows[0].Selected = true;
        }
        catch { }
    }

    private void EnsureSixteenVisibleRows()
    {
        if (_grid.Rows.Count == 0) return;
        int rowHeight = Math.Max(_grid.RowTemplate.Height, 20);
        int headerH = Math.Max(_grid.ColumnHeadersHeight, 22);
        int desiredGridHeight = headerH + (rowHeight * 16) + 2;
        _grid.Height = desiredGridHeight;

        int menuH = _menu.Height;
        int topH = _topPanel.Height;
        int desiredClientHeight = menuH + topH + desiredGridHeight;
        this.ClientSize = new Size(this.ClientSize.Width, desiredClientHeight);
    }

    private void EnsureMinWidth()
    {
        try
        {
            int colsWidth = _grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            int desiredGridWidth = colsWidth + 4;
            int chrome = this.Width - this.ClientSize.Width;
            int desiredClientWidth = Math.Max(desiredGridWidth, 820);
            this.ClientSize = new Size(desiredClientWidth, this.ClientSize.Height);
            this.MinimumSize = new Size(desiredClientWidth + chrome, this.MinimumSize.Height);
        }
        catch { }
    }

    private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
    {
        try
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn)
            {
                _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                e.ThrowException = false;
            }
        }
        catch { }
    }

    // ------------- File Open / Save -------------
    private void OnOpenClicked(object sender, EventArgs e)
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Title = "Open RGR Personality";
            dlg.Filter = "RGR files (*.RGR)|*.RGR";
            dlg.DefaultExt = "RGR";
            dlg.AddExtension = true;
            dlg.CheckFileExists = true;
            if (Directory.Exists(_lastFolder)) dlg.InitialDirectory = _lastFolder;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _lastFolder = Path.GetDirectoryName(dlg.FileName) ?? _lastFolder;
                _settings.LastRgrFolder = _lastFolder;
                _settings.Save();
                TryLoadRgr(dlg.FileName);
            }
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
            Array.Copy(logical, 0, _currentData, 0, 128);

            LogLine("Opened: " + path);
            LogLine("Format: " + mode + "; file bytes: " + fileBytes.Length + "; logical bytes: " + logical.Length);

            PopulateGridBitPatterns(_currentData);
            PopulateGridFrequencies(_currentData);
            PopulateGridTones(_currentData);
            ForceTopRow();

            // NEW: Gold self-check
            string goldMsg;
            if (ToneAndFreq.SelfTestAgainstGold(_currentData, out goldMsg))
                LogLine(goldMsg);
            else
                LogLine(goldMsg);

            this.Text = "X2212 Programmer — " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            LogLine("Error opening file: " + ex.Message);
            MessageBox.Show(this, "Failed to open file:\n" + ex.Message, "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool LooksAsciiHex(string s)
    {
        int hexCount = 0;
        foreach (char ch in s)
        {
            if (char.IsWhiteSpace(ch)) continue;
            if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')) hexCount++;
            else return false;
        }
        return (hexCount % 2) == 0 && hexCount >= 2;
    }

    private static byte[] DecodeRgrBytes(byte[] fileBytes, out string mode)
    {
        try
        {
            string text = Encoding.UTF8.GetString(fileBytes);
            if (LooksAsciiHex(text))
            {
                string compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
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
        catch { }
        mode = "binary";
        return fileBytes;
    }

    private void OnSaveAsClicked(object sender, EventArgs e)
    {
        if (!_hasData)
        {
            MessageBox.Show(this, "No data to save yet. Open an .RGR first.", "Save As",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using (SaveFileDialog dlg = new SaveFileDialog())
        {
            dlg.Title = "Save RGR Personality";
            dlg.Filter = "RGR files (*.RGR)|*.RGR";
            dlg.DefaultExt = "RGR";
            dlg.AddExtension = true;
            dlg.OverwritePrompt = true;
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
                        StringBuilder sb = new StringBuilder(_currentData.Length * 2);
                        for (int i = 0; i < _currentData.Length; i++)
                            sb.Append(_currentData[i].ToString("X2", CultureInfo.InvariantCulture));
                        bytesToWrite = Encoding.ASCII.GetBytes(sb.ToString());
                    }

                    File.WriteAllBytes(dlg.FileName, bytesToWrite);
                    _lastFolder = Path.GetDirectoryName(dlg.FileName) ?? _lastFolder;
                    _settings.LastRgrFolder = _lastFolder;
                    _settings.Save();
                _settings.LastRgrFolder = _lastFolder;
                _settings.Save();
                    LogLine("Saved: " + dlg.FileName + " (" + bytesToWrite.Length + " bytes; format " + _loadedFormat + ")");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed to save file:\n" + ex.Message, "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    // ---- Populate grid ----
    private void PopulateGridBitPatterns(byte[] data)
    {
        int channels = Math.Min(16, data.Length / 8);
        for (int ch = 0; ch < 16; ch++) _grid.Rows[ch].Cells[5].Value = "";

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
                data[baseIdx + 7].ToString("X2"));
            _grid.Rows[ch].Cells[5].Value = pattern;
        }
    }

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
            byte B3 = data[baseIdx + 7];

            string txTone = ToneAndFreq.TxToneMenuValue(A2, B3);
            _grid.Rows[ch].Cells[3].Value = txTone;
            _grid.Rows[ch].Cells[4].Value = "0";
        }

        for (int r = channels; r < 16; r++)
        {
            _grid.Rows[r].Cells[3].Value = "0";
            _grid.Rows[r].Cells[4].Value = "0";
        }
    }

    // ---- Device actions (stubs that call X2212Io) ----
    private void OnReadClicked(object sender, EventArgs e)
    {
        try
        {
            LogLine("Reading device (safe)...");
            byte[] nibs = X2212Io.ReadAllNibbles(_baseAddress, LogLine);
            byte[] bytes128 = X2212Io.CompressNibblesToBytes(nibs);
            _currentData = new byte[128];
            Array.Copy(bytes128, _currentData, 128);
            _hasData = true;

            PopulateGridBitPatterns(_currentData);
            PopulateGridFrequencies(_currentData);
            PopulateGridTones(_currentData);
            ForceTopRow();
            LogLine("Read complete.");
        }
        catch (Exception ex)
        {
            LogLine("Read error: " + ex.Message);
            MessageBox.Show(this, "Read error:\n" + ex.Message, "Read (Safe)", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnVerifyClicked(object sender, EventArgs e)
    {
        if (!_hasData)
        {
            MessageBox.Show(this, "Open an .RGR first.", "Verify",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            byte[] nibs = X2212Io.ExpandToNibbles(_currentData);
            LogLine("Verifying device...");
            int failIndex;
            bool ok = X2212Io.VerifyNibbles(_baseAddress, nibs, out failIndex, LogLine);
            LogLine(ok ? "Verify OK." : ("Verify failed at nibble " + failIndex.ToString("000") + "."));
        }
        catch (Exception ex)
        {
            LogLine("Verify error: " + ex.Message);
            MessageBox.Show(this, "Verify error:\n" + ex.Message, "Verify", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnWriteClicked(object sender, EventArgs e)
    {
        if (!_hasData)
        {
            MessageBox.Show(this, "Open an .RGR first.", "Write",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            byte[] nibs = X2212Io.ExpandToNibbles(_currentData);
            LogLine("Programming device (256 nibbles)...");
            X2212Io.ProgramNibbles(_baseAddress, nibs, LogLine);
            LogLine("Programming complete. Issuing STORE...");
            X2212Io.DoStore(_baseAddress, LogLine);
            LogLine("STORE complete.");
        }
        catch (Exception ex)
        {
            LogLine("Write error: " + ex.Message);
            MessageBox.Show(this, "Write error:\n" + ex.Message, "Write", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---- Logging/driver ----
    private void ClearLog() { _log.Text = string.Empty; }

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

    private void OnTbBaseLeave(object sender, EventArgs e) => ReprobeBase();

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
            _baseAddress = parsed;
        else
            _tbBase.Text = string.IsNullOrWhiteSpace(_settings.LptBaseText) ? "0xA800" : _settings.LptBaseText;

        ClearLog();
        LogLine("Driver: Checking… [Gray]");
        ProbeDriverAndLog();
    }

    private void ProbeDriverAndLog()
    {
        string detail;
        bool ok = Lpt.TryProbe(_baseAddress, out detail);
        LogLine(ok ? "Driver: OK [LimeGreen]" + detail : "Driver: NOT LOADED [Red]" + detail);
    }

    private static bool TryParsePort(string text, out ushort val)
    {
        val = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string t = text.Trim();
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);

        ushort hex;
        if (ushort.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex))
        { val = hex; return true; }

        ushort dec;
        if (ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out dec))
        { val = dec; return true; }

        return false;
    }

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
