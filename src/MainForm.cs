using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using RangrApp.Locked;

namespace RangrApp
{
    public partial class MainForm : Form
    {
        private readonly DataGridView _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        private readonly TextBox _log = new TextBox { Dock = DockStyle.Bottom, Multiline = true, Height = 120, ScrollBars = ScrollBars.Vertical };
        private int _channelCount;

        public MainForm()
        {
            InitializeComponent();
            BuildGrid();
            Controls.Add(_grid);
            Controls.Add(_log);

            // Load image / program here (project’s existing logic)
            // Set _channelCount accordingly:
            _channelCount = GetChannelCount();
            FillRows();
        }

        private void BuildGrid()
        {
            _grid.Columns.Clear();
            _grid.RowHeadersVisible = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.VirtualMode = false;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ch", HeaderText = "Ch", Width = 36, ReadOnly = true });

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tx MHz", HeaderText = "Tx MHz", Width = 70, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rx MHz", HeaderText = "Rx MHz", Width = 70, ReadOnly = true });

            // TX and RX Tone menus populated from baked maps
            var txCol = new DataGridViewComboBoxColumn { Name = "Tx Tone", HeaderText = "Tx Tone", Width = 80, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            txCol.Items.AddRange(ToneLock.ToneMenuTx.Cast<object>().ToArray());
            _grid.Columns.Add(txCol);

            var rxCol = new DataGridViewComboBoxColumn { Name = "Rx Tone", HeaderText = "Rx Tone", Width = 80, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            rxCol.Items.AddRange(ToneLock.ToneMenuRx.Cast<object>().ToArray());
            _grid.Columns.Add(rxCol);

            // Low‐nibble displays (read-only)
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E8L", HeaderText = "E8L", Width = 40, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EDL", HeaderText = "EDL", Width = 40, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EEL", HeaderText = "EEL", Width = 40, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EFL", HeaderText = "EFL", Width = 40, ReadOnly = true });

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E0L", HeaderText = "E0L", Width = 40, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E6L", HeaderText = "E6L", Width = 40, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "E7L", HeaderText = "E7L", Width = 40, ReadOnly = true });

            // GE-style HEX display
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HEX", HeaderText = "A0..A3 B0..B3 | A4..A7 B4..B7", Width = 420, ReadOnly = true });

            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
        }

        private void FillRows()
        {
            _grid.Rows.Clear();
            _grid.Rows.Add(_channelCount);
            for (int ch = 0; ch < _channelCount; ch++)
            {
                _grid.Rows[ch].Cells["Ch"].Value = ch + 1;
                RefreshRow(ch);
            }
        }

        private void RefreshRow(int ch)
        {
            // Acquire the 16-byte channel row in the standard order: A0..A7 B0..B7
            var row16 = ReadChannelRow16(ch);

            // ---- Frequencies (always from A0..A2 and B0..B2) ----
            byte A0 = row16[0], A1 = row16[1], A2 = row16[2];
            byte B0 = row16[8 + 0 - 8 + 4]; // clarity: B0 is index 4 in our row layout (A0..A7 = 0..7, B0..B7 = 8..15)
            B0 = row16[4];
            byte B1 = row16[5], B2 = row16[6];

            double txMHz = FreqLock.TxMHz(A0, A1, A2);
            double rxMHz = FreqLock.RxMHz(B0, B1, B2);
            _grid.Rows[ch].Cells["Tx MHz"].Value = txMHz.ToString("F4");
            _grid.Rows[ch].Cells["Rx MHz"].Value = rxMHz.ToString("F4");

            // ---- TX / RX decode via canonical helpers ----
            var (txPat, txLabel) = ToneLock.ReadTxFromRow(row16);
            var (rxPat, rxLabel) = ToneLock.ReadRxFromRow(row16);

            // Display low nibbles
            _grid.Rows[ch].Cells["E8L"].Value = txPat.E8Low.ToString("X1");
            _grid.Rows[ch].Cells["EDL"].Value = txPat.EDLow.ToString("X1");
            _grid.Rows[ch].Cells["EEL"].Value = txPat.EELow.ToString("X1");
            _grid.Rows[ch].Cells["EFL"].Value = txPat.EFLow.ToString("X1");

            _grid.Rows[ch].Cells["E0L"].Value = rxPat.E0Low.ToString("X1");
            _grid.Rows[ch].Cells["E6L"].Value = rxPat.E6Low.ToString("X1");
            _grid.Rows[ch].Cells["E7L"].Value = rxPat.E7Low.ToString("X1");

            // Assign tone combos. "0" shows as NullValue so the cell appears empty when off.
            var txCell = (DataGridViewComboBoxCell)_grid.Rows[ch].Cells["Tx Tone"];
            var rxCell = (DataGridViewComboBoxCell)_grid.Rows[ch].Cells["Rx Tone"];

            if (txLabel == "0") { txCell.Style.NullValue = "0"; txCell.Value = null; }
            else if (MenuContains(txLabel, ToneLock.ToneMenuTx)) txCell.Value = txLabel;
            else { txCell.Style.NullValue = "Err"; txCell.Value = null; }

            if (rxLabel == "0") { rxCell.Style.NullValue = "0"; rxCell.Value = null; }
            else if (MenuContains(rxLabel, ToneLock.ToneMenuRx)) rxCell.Value = rxLabel;
            else { rxCell.Style.NullValue = "Err"; rxCell.Value = null; }

            // Hex block in GE layout
            _grid.Rows[ch].Cells["HEX"].Value = ToneLock.FormatRowHex(row16);
        }

        private static bool MenuContains(string label, string[] menu) =>
            menu != null && Array.IndexOf(menu, label) >= 0;

        // Commit ComboBox edits immediately so CellValueChanged fires as soon as user picks a tone
        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName != "Tx Tone" && colName != "Rx Tone") return;

            int ch = e.RowIndex;
            var row16 = ReadChannelRow16(ch); // mutable buffer

            if (colName == "Tx Tone")
            {
                var cell = _grid.Rows[ch].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
                var label = (cell?.Value as string) ?? "0";
                if (!ToneLock.ApplyTxToneByLabel(row16, label))
                {
                    _log.AppendText($"TX tone not found: '{label}'\r\n");
                }
            }
            else // Rx Tone
            {
                var cell = _grid.Rows[ch].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
                var label = (cell?.Value as string) ?? "0";
                if (!ToneLock.ApplyRxToneByLabel(row16, label))
                {
                    _log.AppendText($"RX tone not found: '{label}'\r\n");
                }
            }

            // Persist the row and refresh the display
            WriteChannelRow16(ch, row16);
            RefreshRow(ch);
        }

        // ----------------- Glue to your existing persistence -----------------
        // Replace these two with your project’s row accessors. Everything else is clean and channel-agnostic.

        private byte[] ReadChannelRow16(int channelIndex)
        {
            // Must return bytes as: A0..A7 (0..7), B0..B7 (8..15)
            // Hook into your existing image/codec here. Example:
            // return ProgramImage.GetChannelRow16(channelIndex);
            return GetRow16FromProject(channelIndex);
        }

        private void WriteChannelRow16(int channelIndex, byte[] row16)
        {
            // Persist back to your program image. Example:
            // ProgramImage.SetChannelRow16(channelIndex, row16);
            SetRow16InProject(channelIndex, row16);
        }

        private int GetChannelCount()
        {
            // Return number of channels from your existing image
            return GetChannelCountFromProject();
        }

        // ---------- PROJECT-SPECIFIC STUBS (replace with your actual calls) ----------
        // These are named to make the swap trivial and obvious.

        private byte[] GetRow16FromProject(int ch) => throw new NotImplementedException("Wire this to your existing image reader (A0..A7 B0..B7).");
        private void SetRow16InProject(int ch, byte[] row16) => throw new NotImplementedException("Wire this to your existing image writer.");
        private int GetChannelCountFromProject() => throw new NotImplementedException("Return your actual channel count.");
    }
}
