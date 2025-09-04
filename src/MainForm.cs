using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RangrApp.Locked; // ToneLock

public class MainForm : Form
{
    // --- UI ---
    private readonly MenuStrip _menu = new MenuStrip();
    private readonly ToolStripMenuItem _fileMenu = new ToolStripMenuItem("&File");
    private readonly ToolStripMenuItem _openItem = new ToolStripMenuItem("&Open .RGR…");
    private readonly ToolStripMenuItem _saveAsItem = new ToolStripMenuItem("&Save As…");
    private readonly ToolStripMenuItem _exitItem = new ToolStripMenuItem("E&xit");

    private readonly DataGridView _grid = new DataGridView();
    private readonly TextBox _log = new TextBox();

    // --- Image store (we keep it 256 bytes in-memory; 128B legacy is padded) ---
    private byte[] _image = new byte[256];
    private bool _imageIs256 = true;   // remember original size for SaveAs

    public MainForm()
    {
        Text = "GE Ranger Programmer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 640);

        _openItem.Click += (_, __) => DoOpen();
        _saveAsItem.Click += (_, __) => DoSaveAs();
        _exitItem.Click += (_, __) => Close();
        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _openItem, _saveAsItem, new ToolStripSeparator(), _exitItem });
        _menu.Items.Add(_fileMenu);
        Controls.Add(_menu);

        _grid.Dock = DockStyle.Fill;
        _grid.RowHeadersVisible = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
        _grid.CellValueChanged += Grid_CellValueChanged;
        Controls.Add(_grid);

        _log.Dock = DockStyle.Bottom;
        _log.Multiline = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Height = 130;
        Controls.Add(_log);

        BuildGrid();
        LoadBlankImage();
        RefreshAll();
    }

    // ---------------- Grid ----------------

    private void BuildGrid()
    {
        _grid.Columns.Clear();

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ch", HeaderText = "Ch", Width = 40, ReadOnly = true });

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tx MHz", HeaderText = "Tx MHz", Width = 80, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rx MHz", HeaderText = "Rx MHz", Width = 80, ReadOnly = true });

        var txCol = new DataGridViewComboBoxColumn { Name = "Tx Tone", HeaderText = "Tx Tone", Width = 88, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
        txCol.Items.AddRange(ToneLock.ToneMenuTx.Cast<object>().ToArray());
        _grid.Columns.Add(txCol);

        var rxCol = new DataGridViewComboBoxColumn { Name = "Rx Tone", HeaderText = "Rx Tone", Width = 88, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
        rxCol.Items.AddRange(ToneLock.ToneMenuRx.Cast<object>().ToArray());
        _grid.Columns.Add(rxCol);

        // TX nibbles (E8L, EDL, EEL, EFL)
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E8L", HeaderText = "E8L", Width = 44, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EDL", HeaderText = "EDL", Width = 44, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EEL", HeaderText = "EEL", Width = 44, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EFL", HeaderText = "EFL", Width = 44, ReadOnly = true });

        // RX nibbles (E0L, E6L, E7L)
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E0L", HeaderText = "E0L", Width = 44, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E6L", HeaderText = "E6L", Width = 44, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E7L", HeaderText = "E7L", Width = 44, ReadOnly = true });

        // GE style hex: A0..A3 B0..B3 | A4..A7 B4..B7
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HEX", HeaderText = "A0..A3 B0..B3 | A4..A7 B4..B7", Width = 450, ReadOnly = true });
    }

    private void RefreshAll()
    {
        _grid.Rows.Clear();
        _grid.Rows.Add(16);
        for (int ch = 0; ch < 16; ch++)
        {
            _grid.Rows[ch].Cells["Ch"].Value = ch + 1;
            RefreshRow(ch);
        }
        _grid.ClearSelection();
        if (_grid.Rows.Count > 0)
        {
            _grid.FirstDisplayedScrollingRowIndex = 0;
            _grid.CurrentCell = _grid.Rows[0].Cells[1];
        }
    }

    private void RefreshRow(int ch)
    {
        // Row order is the GE layout: A0..A7, B0..B7 (total 16 bytes)
        var row16 = SliceRow16(ch);

        // --- Frequencies always available (no branching) ---
        byte A0 = row16[0], A1 = row16[1], A2 = row16[2];
        byte B0 = row16[4], B1 = row16[5], B2 = row16[6];
        double tx = FreqLock.TxMHz(A0, A1, A2);
        double rx = FreqLock.RxMHz(B0, B1, B2);
        _grid.Rows[ch].Cells["Tx MHz"].Value = tx.ToString("0.0000", CultureInfo.InvariantCulture);
        _grid.Rows[ch].Cells["Rx MHz"].Value = rx.ToString("0.0000", CultureInfo.InvariantCulture);

        // --- Decode tones (canonical helpers) ---
        var (txPat, txLabel) = ToneLock.ReadTxFromRow(row16);
        var (rxPat, rxLabel) = ToneLock.ReadRxFromRow(row16);

        // TX nibbles
        _grid.Rows[ch].Cells["E8L"].Value = txPat.E8Low.ToString("X1");
        _grid.Rows[ch].Cells["EDL"].Value = txPat.EDLow.ToString("X1");
        _grid.Rows[ch].Cells["EEL"].Value = txPat.EELow.ToString("X1");
        _grid.Rows[ch].Cells["EFL"].Value = txPat.EFLow.ToString("X1");

        // RX nibbles
        _grid.Rows[ch].Cells["E0L"].Value = rxPat.E0Low.ToString("X1");
        _grid.Rows[ch].Cells["E6L"].Value = rxPat.E6Low.ToString("X1");
        _grid.Rows[ch].Cells["E7L"].Value = rxPat.E7Low.ToString("X1");

        // Tone dropdowns
        var txCell = (DataGridViewComboBoxCell)_grid.Rows[ch].Cells["Tx Tone"];
        if (txLabel == "0") { txCell.Style.NullValue = "0"; txCell.Value = null; }
        else if (Array.IndexOf(ToneLock.ToneMenuTx, txLabel) >= 0) txCell.Value = txLabel;
        else { txCell.Style.NullValue = "Err"; txCell.Value = null; }

        var rxCell = (DataGridViewComboBoxCell)_grid.Rows[ch].Cells["Rx Tone"];
        if (rxLabel == "0") { rxCell.Style.NullValue = "0"; rxCell.Value = null; }
        else if (Array.IndexOf(ToneLock.ToneMenuRx, rxLabel) >= 0) rxCell.Value = rxLabel;
        else { rxCell.Style.NullValue = "Err"; rxCell.Value = null; }

        // HEX column (GE ordering)
        _grid.Rows[ch].Cells["HEX"].Value = ToneLock.FormatRowHex(row16);
    }

    private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
        if (_grid.IsCurrentCellDirty)
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var name = _grid.Columns[e.ColumnIndex].Name;
        if (name != "Tx Tone" && name != "Rx Tone") return;

        int ch = e.RowIndex;
        var row16 = SliceRow16(ch); // mutable copy of current row

        if (name == "Tx Tone")
        {
            var txCell = _grid.Rows[ch].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
            var label = (txCell?.Value as string) ?? "0";
            if (!ToneLock.ApplyTxToneByLabel(row16, label))
                LogLine($"TX label not found: '{label}'");
        }
        else
        {
            var rxCell = _grid.Rows[ch].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
            var label = (rxCell?.Value as string) ?? "0";
            if (!ToneLock.ApplyRxToneByLabel(row16, label))
                LogLine($"RX label not found: '{label}'");
        }

        // Persist to backing image and refresh display
        StoreRow16(ch, row16);
        RefreshRow(ch);
    }

    // ---------------- Image I/O ----------------

    private void DoOpen()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Ranger Image (*.RGR)|*.RGR|All files (*.*)|*.*",
            Title = "Open Ranger .RGR"
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        var bytes = File.ReadAllBytes(ofd.FileName);
        _imageIs256 = bytes.Length >= 256;
        if (_imageIs256)
        {
            _image = bytes.Take(256).ToArray();
        }
        else
        {
            _image = new byte[256];
            Array.Copy(bytes, _image, Math.Min(bytes.Length, 128));
        }
        Text = $"GE Ranger Programmer — {Path.GetFileName(ofd.FileName)}";
        RefreshAll();
        LogLine($"Opened {bytes.Length} bytes ({(_imageIs256 ? "256" : "128")} mode)");
    }

    private void DoSaveAs()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Ranger Image (*.RGR)|*.RGR|All files (*.*)|*.*",
            Title = "Save Ranger .RGR"
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        byte[] outBytes = _imageIs256 ? _image.ToArray() : _image.Take(128).ToArray();
        File.WriteAllBytes(sfd.FileName, outBytes);
        LogLine($"Saved {outBytes.Length} bytes to {sfd.FileName}");
    }

    // ---------------- Row access (A0..A7 B0..B7) ----------------

    private byte[] SliceRow16(int channelIndex)
    {
        // GE/DOS layout: rows are stored at base addresses E0, D0, C0, ... down to 10 (one 16B row per channel)
        // ch 0 => 0xE0, ch 1 => 0xD0, ... ch 15 => 0x10
        byte baseAddr = (byte)((0xE0 - (channelIndex * 0x10)) & 0xFF);

        // If we originally opened a 256B image, slice directly.
        if (_imageIs256)
        {
            var block = new byte[16];
            for (int i = 0; i < 16; i++) block[i] = _image[(byte)(baseAddr + i)];
            return block;
        }

        // Legacy 128B: synthesize a 16B row from the 8B layout
        // Keep first 8 bytes as A0..A3 B0..B3, and mirror tone nibbles into EE/EF at 0x0E/0x0F.
        int ch = channelIndex;
        int baseOffset = ch * 8;
        byte a0 = SafeAt(_image, baseOffset + 0);
        byte a1 = SafeAt(_image, baseOffset + 1);
        byte a2 = SafeAt(_image, baseOffset + 2);
        byte a3 = SafeAt(_image, baseOffset + 3);
        byte b0 = SafeAt(_image, baseOffset + 4);
        byte b1 = SafeAt(_image, baseOffset + 5);
        byte b2 = SafeAt(_image, baseOffset + 6); // legacy EE candidate
        byte b3 = SafeAt(_image, baseOffset + 7); // legacy EF candidate

        var block16 = new byte[16];
        block16[0] = a0; block16[1] = a1; block16[2] = a2; block16[3] = a3;
        block16[4] = b0; block16[5] = b1; block16[6] = b2; block16[7] = b3;
        block16[0x08] = 0x00; // E8 host nibble lives in low part; leave byte value as-is (caller uses low-nibble only)
        block16[0x0D] = 0x00; // EDL
        block16[0x0E] = b2;   // EEL from legacy B2
        block16[0x0F] = b3;   // EFL from legacy B3
        return block16;
    }

    private void StoreRow16(int channelIndex, byte[] row16)
    {
        byte baseAddr = (byte)((0xE0 - (channelIndex * 0x10)) & 0xFF);

        if (_imageIs256)
        {
            for (int i = 0; i < 16; i++)
                _image[(byte)(baseAddr + i)] = row16[i];
            return;
        }

        // Legacy 128B round-trip: write A0..A3 B0..B3 back; carry EE/EF low nibbles into legacy B2/B3 low nibbles.
        int ch = channelIndex;
        int baseOffset = ch * 8;
        // A0..A3
        SetAt(ref _image, baseOffset + 0, row16[0]);
        SetAt(ref _image, baseOffset + 1, row16[1]);
        SetAt(ref _image, baseOffset + 2, row16[2]);
        SetAt(ref _image, baseOffset + 3, row16[3]);
        // B0..B3
        SetAt(ref _image, baseOffset + 4, row16[4]);
        SetAt(ref _image, baseOffset + 5, row16[5]);

        // Map EE/EF low nibbles
        byte eeLow = (byte)(row16[0x0E] & 0x0F);
        byte efLow = (byte)(row16[0x0F] & 0x0F);
        byte legacyB2 = SafeAt(_image, baseOffset + 6);
        byte legacyB3 = SafeAt(_image, baseOffset + 7);
        legacyB2 = (byte)((legacyB2 & 0xF0) | eeLow);
        legacyB3 = (byte)((legacyB3 & 0xF0) | efLow);
        SetAt(ref _image, baseOffset + 6, legacyB2);
        SetAt(ref _image, baseOffset + 7, legacyB3);
    }

    // ---------------- Utils ----------------

    private static byte SafeAt(byte[] a, int i) => (i >= 0 && i < a.Length) ? a[i] : (byte)0x00;
    private static void SetAt(ref byte[] a, int i, byte v)
    {
        if (i >= 0 && i < a.Length) a[i] = v;
    }

    private void LogLine(string msg)
    {
        if (_log.TextLength > 0) _log.AppendText(Environment.NewLine);
        _log.AppendText(msg);
    }
}
