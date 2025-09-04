using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RangrApp.Locked
{
    /// <summary>Byte slots within a single channel's 16-byte row: A0..A7 (0..7), B0..B7 (8..15).</summary>
    public enum RowByteSlot : byte
    {
        A0 = 0, A1 = 1, A2 = 2, A3 = 3,  B0 = 4, B1 = 5, B2 = 6, B3 = 7,
        A4 = 8, A5 = 9, A6 = 10, A7 = 11, B4 = 12, B5 = 13, B6 = 14, B7 = 15
    }

    /// <summary>Descriptive TX nibble bundle (low nibbles only); channel-agnostic.</summary>
    public readonly struct TxNibbleSet
    {
        public readonly byte HousekeepingNibble0Low; // shown as E8L (display/preserve only)
        public readonly byte HousekeepingNibble1Low; // shown as EDL (display/preserve only)
        public readonly byte ToneCodeHighLow;        // EE.low — used for TX selection
        public readonly byte ToneCodeLowLow;         // EF.low — used for TX selection

        public TxNibbleSet(byte hk0, byte hk1, byte ee, byte ef)
        {
            HousekeepingNibble0Low = (byte)(hk0 & 0xF);
            HousekeepingNibble1Low = (byte)(hk1 & 0xF);
            ToneCodeHighLow        = (byte)(ee  & 0xF);
            ToneCodeLowLow         = (byte)(ef  & 0xF);
        }

        public byte ComposeTxCode() => (byte)((ToneCodeHighLow << 4) | (ToneCodeLowLow & 0xF));
    }

    /// <summary>Descriptive RX nibble bundle (low nibbles only); channel-agnostic.</summary>
    public readonly struct RxNibbleSet
    {
        public readonly byte E0Low;
        public readonly byte E6Low;
        public readonly byte E7Low;

        public RxNibbleSet(byte e0, byte e6, byte e7)
        {
            E0Low = (byte)(e0 & 0xF);
            E6Low = (byte)(e6 & 0xF);
            E7Low = (byte)(e7 & 0xF);
        }

        public int ComposeRxKey() => (E0Low << 8) | (E6Low << 4) | E7Low;
    }

    /// <summary>Declarative mapping of which row bytes supply low nibbles for TX/RX decoding.</summary>
    public sealed class ToneNibbleLayout
    {
        // Defaults: TX selection = A2.low (EE), A3.low (EF); RX selection = B0.low (E0), B2.low (E6), B3.low (E7).
        // Housekeeping (display/preserve only): A0.low (E8L), A1.low (EDL).
        public RowByteSlot TxHousekeeping0Source { get; set; } = RowByteSlot.A0;
        public RowByteSlot TxHousekeeping1Source { get; set; } = RowByteSlot.A1;
        public RowByteSlot TxToneCodeHighSource { get; set; } = RowByteSlot.A2;
        public RowByteSlot TxToneCodeLowSource  { get; set; } = RowByteSlot.A3;

        public RowByteSlot RxE0Source { get; set; } = RowByteSlot.B0;
        public RowByteSlot RxE6Source { get; set; } = RowByteSlot.B2;
        public RowByteSlot RxE7Source { get; set; } = RowByteSlot.B3;
    }

    internal static class ToneLock
    {
        public static readonly ToneNibbleLayout Layout = new ToneNibbleLayout();

        // TX: (EE<<4)|EF → label
        private static readonly Dictionary<byte, string> _txLabelByCode = new Dictionary<byte, string>
        {
            { 0x00, "0" }, { 0x71, "67.0" }, { 0x39, "103.5" }, { 0x7E, "151.4" }, { 0x81, "210.7" } // acceptance spot checks
        };

        // RX: (E0<<8)|(E6<<4)|E7 → label
        private static readonly Dictionary<int, string> _rxLabelByKey = new Dictionary<int, string>();

        // Public menus used by MainForm
        public static string[] ToneMenuTx => BuildMenuFromLabels(_txLabelByCode.Values);
        public static string[] ToneMenuRx => BuildMenuFromLabels(_rxLabelByKey.Values);

        private static string[] BuildMenuFromLabels(IEnumerable<string> labels)
        {
            var set = new HashSet<string>(labels);
            set.Remove("0");
            var list = set.ToList();
            list.Sort((a,b) => Numeric(a).CompareTo(Numeric(b)));
            return list.ToArray();
        }

        private static double Numeric(string s)
        {
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            return double.MaxValue;
        }

        // ---------------- CSV loading ----------------
        // Expected columns (case-insensitive): Tx Tone, E8L, EDL, EEL, EFL, Rx Tone, E0L, E6L, E7L
        public static void LoadCtcssCsv(TextReader reader)
        {
            var header = reader.ReadLine();
            if (header == null) return;
            var cols = SplitCsv(header);
            var idx = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            for (int i=0;i<cols.Count;i++) if (!idx.ContainsKey(cols[i])) idx[cols[i]] = i;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cells = SplitCsv(line);

                if (TryGetString(cells, idx, "Tx Tone", out var txLab) &&
                    TryGetNibble (cells, idx, "EEL", out var ee) &&
                    TryGetNibble (cells, idx, "EFL", out var ef))
                {
                    var code = (byte)(((ee & 0xF) << 4) | (ef & 0xF));
                    _txLabelByCode[code] = Canon(txLab);
                }

                if (TryGetString(cells, idx, "Rx Tone", out var rxLab) &&
                    TryGetNibble (cells, idx, "E0L", out var e0) &&
                    TryGetNibble (cells, idx, "E6L", out var e6) &&
                    TryGetNibble (cells, idx, "E7L", out var e7))
                {
                    var key = ((e0 & 0xF) << 8) | ((e6 & 0xF) << 4) | (e7 & 0xF);
                    _rxLabelByKey[key] = Canon(rxLab);
                }
            }
        }

        public static bool TryLoadCtcssCsv(string path, out string error)
        {
            try
            {
                using var r = new StreamReader(path);
                LoadCtcssCsv(r);
                error = string.Empty;
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        // ---------------- Row extraction (16-byte channel rows) ----------------
        public static TxNibbleSet ExtractTxNibbles(ReadOnlySpan<byte> row16)
        {
            byte hk0 = LowNibbleOf(row16[(int)Layout.TxHousekeeping0Source]);
            byte hk1 = LowNibbleOf(row16[(int)Layout.TxHousekeeping1Source]);
            byte ee  = LowNibbleOf(row16[(int)Layout.TxToneCodeHighSource]);
            byte ef  = LowNibbleOf(row16[(int)Layout.TxToneCodeLowSource ]);
            return new TxNibbleSet(hk0, hk1, ee, ef);
        }

        public static RxNibbleSet ExtractRxNibbles(ReadOnlySpan<byte> row16)
        {
            byte e0 = LowNibbleOf(row16[(int)Layout.RxE0Source]);
            byte e6 = LowNibbleOf(row16[(int)Layout.RxE6Source]);
            byte e7 = LowNibbleOf(row16[(int)Layout.RxE7Source]);
            return new RxNibbleSet(e0, e6, e7);
        }

        // ---------------- Label helpers (strict) ----------------
        public static string GetTxLabel(byte eeLowNibble, byte efLowNibble)
        {
            byte code = (byte)(((eeLowNibble & 0xF) << 4) | (efLowNibble & 0xF));
            if (_txLabelByCode.TryGetValue(code, out var lbl)) return lbl;
            return code == 0 ? "0" : "Err";
        }

        public static string GetRxLabel(byte e0LowNibble, byte e6LowNibble, byte e7LowNibble)
        {
            int key = ((e0LowNibble & 0xF) << 8) | ((e6LowNibble & 0xF) << 4) | (e7LowNibble & 0xF);
            if (_rxLabelByKey.TryGetValue(key, out var lbl)) return lbl;
            return (e0LowNibble | e6LowNibble | e7LowNibble) == 0 ? "0" : "Err";
        }

        // ---------------- UI helpers ----------------
        public static string NibblesHL(byte b) => $"{(byte)(b >> 4):X1}|{(byte)(b & 0xF):X1}";

        public static string FormatRowHex(ReadOnlySpan<byte> row16)
        {
            string block0 = string.Join(" ", row16.Slice(0,8).ToArray().Select(x => x.ToString("X2")));
            string block1 = string.Join(" ", row16.Slice(8,8).ToArray().Select(x => x.ToString("X2")));
            return $"{block0}   {block1}";
        }

        public static string SteDisplayFromFLow(byte fLowNibble) => ((fLowNibble & 0x8) != 0) ? "Y" : "";
        public static bool   IsFlagF2Set(byte fLowNibble) => (fLowNibble & 0x4) != 0;
        public static byte   LowNibbleOf(byte b) => (byte)(b & 0xF);

        // Convenience for MainForm (non-legacy; returns the values you want to display)
        public static (byte E8L, byte EDL, byte EEL, byte EFL, byte TxCode, string TxLabel) InspectTransmitFromBlock(ReadOnlySpan<byte> row16)
        {
            var tx = ExtractTxNibbles(row16);
            byte code = tx.ComposeTxCode();
            string label = GetTxLabel(tx.ToneCodeHighLow, tx.ToneCodeLowLow);
            return (tx.HousekeepingNibble0Low, tx.HousekeepingNibble1Low, tx.ToneCodeHighLow, tx.ToneCodeLowLow, code, label);
        }

        public static (byte E0L, byte E6L, byte E7L, string RxLabel) InspectReceiveFromBlock(ReadOnlySpan<byte> row16)
        {
            var rx = ExtractRxNibbles(row16);
            string label = GetRxLabel(rx.E0Low, rx.E6Low, rx.E7Low);
            return (rx.E0Low, rx.E6Low, rx.E7Low, label);
        }

        // ---------------- CSV utilities ----------------
        private static List<string> SplitCsv(string line)
        {
            var res = new List<string>(); if (line == null) return res;
            bool inQ=false; var cell=new System.Text.StringBuilder();
            for (int i=0;i<line.Length;i++)
            {
                char c=line[i];
                if (c=='\"') { if (inQ && i+1<line.Length && line[i+1]=='\"'){cell.Append('\"'); i++;} else inQ=!inQ; }
                else if (c==',' && !inQ) { res.Add(cell.ToString()); cell.Clear(); }
                else cell.Append(c);
            }
            res.Add(cell.ToString());
            return res;
        }

        private static bool TryGetString(IReadOnlyList<string> cells, IDictionary<string,int> idx, string col, out string text)
        {
            text = string.Empty;
            if (!idx.TryGetValue(col, out var i) || i<0 || i>=cells.Count) return false;
            text = cells[i] ?? string.Empty;
            return true;
        }

        private static bool TryGetNibble(IReadOnlyList<string> cells, IDictionary<string,int> idx, string col, out byte val)
        {
            val = 0;
            if (!idx.TryGetValue(col, out var i) || i<0 || i>=cells.Count) return false;
            var s = (cells[i] ?? "").Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (byte.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hx)) { val = (byte)(hx & 0xF); return true; }
            if (byte.TryParse(s, NumberStyles.Integer,   CultureInfo.InvariantCulture, out var dv)) { val = (byte)(dv & 0xF); return true; }
            if (s.Length==1)
            {
                char ch=s[0];
                if (ch>='0' && ch<='9') { val=(byte)(ch-'0'); return true; }
                if (ch>='A' && ch<='F') { val=(byte)(10+ch-'A'); return true; }
                if (ch>='a' && ch<='f') { val=(byte)(10+ch-'a'); return true; }
            }
            return false;
        }

        private static string Canon(string raw) => raw.Trim()=="0" ? "0" : raw.Trim();
    }
}