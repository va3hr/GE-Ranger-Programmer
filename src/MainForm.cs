using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public sealed class MainForm : Form
    {
        private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false };
        private readonly BindingList<RgrCodec.Channel> _rows = new();

        private readonly Label _lblBase = new();
        private readonly TextBox _tbBase = new();
        private readonly Label _driverStatus = new() { AutoSize = true };

        private readonly FlowLayoutPanel _top = new();
        private readonly FlowLayoutPanel _baseRow = new();

        private string? _currentPath;

        public MainForm()
        {
            Text = "X2212 Programmer";
            Width = 1080;
            Height = 720;

            BuildTop();
            BuildGrid();

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(_top, 0, 0);
            root.Controls.Add(_grid, 0, 1);
            Controls.Add(root);

            Load += (_, __) => ProbeDriver();
        }

        // ===== Top (LPT Base + status) =====
        private void BuildTop()
        {
            _top.AutoSize = true;
            _top.FlowDirection = FlowDirection.LeftToRight;
            _top.WrapContents = false;

            _baseRow.AutoSize = true;
            _baseRow.FlowDirection = FlowDirection.LeftToRight;
            _baseRow.WrapContents = false;

            _lblBase.Text = "LPT Base:";
            _tbBase.Text = "0xA800";
            _tbBase.Width = 80;

            _baseRow.Controls.Add(_lblBase);
            _baseRow.Controls.Add(_tbBase);
            _baseRow.Controls.Add(_driverStatus);

            _top.Controls.Add(_baseRow);
        }

        // ===== Grid =====
        private void BuildGrid()
        {
            _grid.Columns.Clear();

            var ch = new DataGridViewTextBoxColumn { HeaderText = "CH", Width = 50, ReadOnly = true, DataPropertyName = "Number" };
            var tx = new DataGridViewTextBoxColumn { HeaderText = "Tx MHz", Width = 110, DataPropertyName = "TxMHz" };
            var rx = new DataGridViewTextBoxColumn { HeaderText = "Rx MHz", Width = 110, DataPropertyName = "RxMHz" };

            var txTone = new DataGridViewComboBoxColumn
            {
                HeaderText = "Tx Tone",
                Width = 110,
                DataSource = ToneLock.ToneMenuAll,
                ValueType = typeof(string),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Standard,
                DataPropertyName = "TxToneDisplay",
                ReadOnly = true
            };
            txTone.DefaultCellStyle.NullValue = "?";

            var rxTone = new DataGridViewComboBoxColumn
            {
                HeaderText = "Rx Tone",
                Width = 110,
                DataSource = ToneLock.ToneMenuAll,
                ValueType = typeof(string),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Standard,
                DataPropertyName = "RxToneDisplay",
                ReadOnly = true
            };
            rxTone.DefaultCellStyle.NullValue = "?";

            var cct = new DataGridViewTextBoxColumn { HeaderText = "cct", Width = 40, DataPropertyName = "Cct" };
            var ste = new DataGridViewTextBoxColumn { HeaderText = "ste", Width = 40, DataPropertyName = "Ste" };
            var hex = new DataGridViewTextBoxColumn { HeaderText = "Hex", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true };

            _grid.Columns.AddRange(ch, tx, rx, txTone, rxTone, cct, ste, hex);
            _grid.DataSource = _rows;
            _grid.AutoGenerateColumns = false;

            _grid.CellFormatting += GridOnCellFormatting;
        }

        private void GridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].HeaderText is "Tx Tone" && e.Value is null)
            {
                e.Value = "?";
                e.FormattingApplied = true;
            }
            else if (_grid.Columns[e.ColumnIndex].HeaderText is "Rx Tone" && e.Value is null)
            {
                e.Value = "?";
                e.FormattingApplied = true;
            }
        }

        // ===== File I/O =====
        private void LoadRgr(string path)
        {
            _rows.Clear();
            _currentPath = path;

            var list = RgrCodec.Load(path);

            foreach (var ch in list)
            {
                _rows.Add(ch);
            }

            RecomputeHexColumn();
        }

        private void SaveRgr()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;
            RgrCodec.Save(_currentPath, _rows);
            RecomputeHexColumn();
        }

        private void RecomputeHexColumn()
        {
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                _grid.Rows[i].Cells[^1].Value = "";
            }
        }

        // ===== Driver probe =====
        private void ProbeDriver()
        {
            if (TryParseBase(_tbBase.Text, out var baseAddr) && InpOut.TryProbe(baseAddr, out var detail))
                _driverStatus.Text = $"Driver: OK [LimeGreen]{detail}";
            else
                _driverStatus.Text = "Driver: Checking... [Gray]";
        }

        private static bool TryParseBase(string s, out ushort value)
        {
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                s = s[2..];
            if (ushort.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var v))
            {
                value = v;
                return true;
            }
            value = 0;
            return false;
        }

        // ===== Native access wrapper =====
        private static class InpOut
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
                    detail = $" (DLL loaded; probed 0x{baseAddr:X4})";
                    return true;
                }
                catch
                {
                    detail = "";
                    return false;
                }
            }
        }

        // Public helpers you can call from your existing menu/toolbar.
        public void UiOpen(string path) => LoadRgr(path);
        public void UiSave() => SaveRgr();
    }
}
