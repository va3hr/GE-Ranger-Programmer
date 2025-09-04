using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RangrApp.Locked
{
    public enum RowByteSlot : byte
    {
        A0=0,A1=1,A2=2,A3=3, B0=4,B1=5,B2=6,B3=7,
        A4=8,A5=9,A6=10,A7=11, B4=12,B5=13,B6=14,B7=15
    }

    public readonly struct TxNibblePattern
    {
        public readonly byte E8Low, EDLow, EELow, EFLow;
        public TxNibblePattern(byte e8, byte ed, byte ee, byte ef)
        { E8Low=(byte)(e8&0xF); EDLow=(byte)(ed&0xF); EELow=(byte)(ee&0xF); EFLow=(byte)(ef&0xF); }
        public ushort Key() => (ushort)(((E8Low&0xF)<<12)|((EDLow&0xF)<<8)|((EELow&0xF)<<4)|(EFLow&0xF));
    }

    public readonly struct RxNibblePattern
    {
        public readonly byte E0Low, E6Low, E7Low;
        public RxNibblePattern(byte e0, byte e6, byte e7)
        { E0Low=(byte)(e0&0xF); E6Low=(byte)(e6&0xF); E7Low=(byte)(e7&0xF); }
        public int Key() => ((E0Low&0xF)<<8)|((E6Low&0xF)<<4)|(E7Low&0xF);
    }

    public sealed class ToneNibbleLayout
    {
        public RowByteSlot TxHousekeeping0Source { get; set; } = RowByteSlot.A4; // 0x08 → E8L
        public RowByteSlot TxHousekeeping1Source { get; set; } = RowByteSlot.B5; // 0x0D → EDL
        public RowByteSlot TxToneCodeHighSource { get; set; } = RowByteSlot.B6;  // 0x0E → EEL
        public RowByteSlot TxToneCodeLowSource  { get; set; } = RowByteSlot.B7;  // 0x0F → EFL

        public RowByteSlot RxE0Source { get; set; } = RowByteSlot.A0;            // 0x00 → E0L
        public RowByteSlot RxE6Source { get; set; } = RowByteSlot.B2;            // 0x06 → E6L
        public RowByteSlot RxE7Source { get; set; } = RowByteSlot.B3;            // 0x07 → E7L
    }

    internal static class ToneLock
    {
        public static readonly ToneNibbleLayout Layout = new ToneNibbleLayout();

        private static readonly Dictionary<string, TxNibblePattern> _txByLabel = new Dictionary<string, TxNibblePattern>
        {
            ["67.0"] = new TxNibblePattern(6, 0, 7, 1),
            ["71.9"] = new TxNibblePattern(6, 4, 13, 3),
            ["74.4"] = new TxNibblePattern(6, 0, 2, 3),
            ["77.0"] = new TxNibblePattern(6, 4, 8, 4),
            ["79.7"] = new TxNibblePattern(6, 4, 11, 5),
            ["82.5"] = new TxNibblePattern(6, 0, 1, 5),
            ["85.4"] = new TxNibblePattern(6, 4, 6, 6),
            ["88.5"] = new TxNibblePattern(6, 4, 12, 7),
            ["91.5"] = new TxNibblePattern(6, 0, 3, 7),
            ["94.8"] = new TxNibblePattern(6, 4, 9, 8),
            ["97.4"] = new TxNibblePattern(6, 4, 2, 8),
            ["100.0"] = new TxNibblePattern(6, 0, 12, 9),
            ["103.5"] = new TxNibblePattern(6, 4, 3, 9),
            ["107.2"] = new TxNibblePattern(6, 0, 11, 10),
            ["110.9"] = new TxNibblePattern(6, 4, 3, 10),
            ["114.8"] = new TxNibblePattern(6, 0, 12, 11),
            ["118.8"] = new TxNibblePattern(6, 4, 4, 11),
            ["123.0"] = new TxNibblePattern(6, 4, 13, 12),
            ["127.3"] = new TxNibblePattern(6, 4, 6, 12),
            ["131.8"] = new TxNibblePattern(6, 4, 15, 13),
            ["136.5"] = new TxNibblePattern(6, 0, 9, 13),
            ["141.3"] = new TxNibblePattern(6, 0, 3, 13),
            ["146.2"] = new TxNibblePattern(6, 0, 13, 14),
            ["151.4"] = new TxNibblePattern(6, 0, 7, 14),
            ["156.7"] = new TxNibblePattern(6, 4, 1, 14),
            ["162.2"] = new TxNibblePattern(6, 0, 12, 15),
            ["167.9"] = new TxNibblePattern(6, 0, 7, 15),
            ["173.8"] = new TxNibblePattern(6, 0, 2, 15),
            ["179.9"] = new TxNibblePattern(7, 0, 13, 0),
            ["186.2"] = new TxNibblePattern(7, 4, 8, 0),
            ["192.8"] = new TxNibblePattern(7, 4, 3, 0),
            ["203.5"] = new TxNibblePattern(7, 0, 13, 1),
            ["210.7"] = new TxNibblePattern(7, 4, 8, 1),
        };
        private static readonly Dictionary<ushort, string> _txLabelByPattern = new Dictionary<ushort, string>
        {
            [0x6071] = "67.0",
            [0x64D3] = "71.9",
            [0x6023] = "74.4",
            [0x6484] = "77.0",
            [0x64B5] = "79.7",
            [0x6015] = "82.5",
            [0x6466] = "85.4",
            [0x64C7] = "88.5",
            [0x6037] = "91.5",
            [0x6498] = "94.8",
            [0x6428] = "97.4",
            [0x60C9] = "100.0",
            [0x6439] = "103.5",
            [0x60BA] = "107.2",
            [0x643A] = "110.9",
            [0x60CB] = "114.8",
            [0x644B] = "118.8",
            [0x64DC] = "123.0",
            [0x646C] = "127.3",
            [0x64FD] = "131.8",
            [0x609D] = "136.5",
            [0x603D] = "141.3",
            [0x60DE] = "146.2",
            [0x607E] = "151.4",
            [0x641E] = "156.7",
            [0x60CF] = "162.2",
            [0x607F] = "167.9",
            [0x602F] = "173.8",
            [0x70D0] = "179.9",
            [0x7480] = "186.2",
            [0x7430] = "192.8",
            [0x70D1] = "203.5",
            [0x7481] = "210.7",
            [0x0000] = "0",
        };
        private static readonly Dictionary<string, RxNibblePattern> _rxByLabel = new Dictionary<string, RxNibblePattern>
        {
            ["67.0"] = new RxNibblePattern(6, 7, 1),
            ["71.9"] = new RxNibblePattern(6, 14, 3),
            ["74.4"] = new RxNibblePattern(6, 2, 3),
            ["77.0"] = new RxNibblePattern(6, 7, 4),
            ["79.7"] = new RxNibblePattern(6, 12, 5),
            ["82.5"] = new RxNibblePattern(6, 1, 5),
            ["85.4"] = new RxNibblePattern(6, 7, 6),
            ["88.5"] = new RxNibblePattern(6, 12, 7),
            ["91.5"] = new RxNibblePattern(6, 3, 7),
            ["94.8"] = new RxNibblePattern(6, 10, 8),
            ["97.4"] = new RxNibblePattern(6, 3, 8),
            ["100.0"] = new RxNibblePattern(6, 12, 9),
            ["103.5"] = new RxNibblePattern(6, 4, 9),
            ["107.2"] = new RxNibblePattern(6, 11, 10),
            ["110.9"] = new RxNibblePattern(6, 3, 10),
            ["114.8"] = new RxNibblePattern(6, 12, 11),
            ["118.8"] = new RxNibblePattern(6, 4, 11),
            ["123.0"] = new RxNibblePattern(6, 13, 12),
            ["127.3"] = new RxNibblePattern(6, 6, 12),
            ["131.8"] = new RxNibblePattern(6, 15, 13),
            ["136.5"] = new RxNibblePattern(6, 9, 13),
            ["141.3"] = new RxNibblePattern(6, 3, 13),
            ["146.2"] = new RxNibblePattern(6, 13, 14),
            ["151.4"] = new RxNibblePattern(6, 7, 14),
            ["156.7"] = new RxNibblePattern(6, 2, 14),
            ["162.2"] = new RxNibblePattern(6, 12, 15),
            ["167.9"] = new RxNibblePattern(6, 7, 15),
            ["173.8"] = new RxNibblePattern(6, 2, 15),
            ["179.9"] = new RxNibblePattern(7, 13, 0),
            ["186.2"] = new RxNibblePattern(7, 8, 0),
            ["192.8"] = new RxNibblePattern(7, 4, 0),
            ["203.5"] = new RxNibblePattern(7, 13, 1),
            ["210.7"] = new RxNibblePattern(7, 9, 1),
        };
        private static readonly Dictionary<int, string> _rxLabelByKey = new Dictionary<int, string>
        {
            [0x671] = "67.0",
            [0x6E3] = "71.9",
            [0x623] = "74.4",
            [0x674] = "77.0",
            [0x6C5] = "79.7",
            [0x615] = "82.5",
            [0x676] = "85.4",
            [0x6C7] = "88.5",
            [0x637] = "91.5",
            [0x6A8] = "94.8",
            [0x638] = "97.4",
            [0x6C9] = "100.0",
            [0x649] = "103.5",
            [0x6BA] = "107.2",
            [0x63A] = "110.9",
            [0x6CB] = "114.8",
            [0x64B] = "118.8",
            [0x6DC] = "123.0",
            [0x66C] = "127.3",
            [0x6FD] = "131.8",
            [0x69D] = "136.5",
            [0x63D] = "141.3",
            [0x6DE] = "146.2",
            [0x67E] = "151.4",
            [0x62E] = "156.7",
            [0x6CF] = "162.2",
            [0x67F] = "167.9",
            [0x62F] = "173.8",
            [0x7D0] = "179.9",
            [0x780] = "186.2",
            [0x740] = "192.8",
            [0x7D1] = "203.5",
            [0x791] = "210.7",
            [0x000] = "0",
        };

        public static string[] ToneMenuTx => _txByLabel.Keys.Where(k=>k!="0").OrderBy(v=>double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        public static string[] ToneMenuRx => _rxByLabel.Keys.Where(k=>k!="0").OrderBy(v=>double.Parse(v, CultureInfo.InvariantCulture)).ToArray();

        public static (TxNibblePattern tx, string label) ReadTxFromRow(ReadOnlySpan<byte> row16)
        {
            byte e8 = LowNibbleOf(row16[(int)Layout.TxHousekeeping0Source]);
            byte ed = LowNibbleOf(row16[(int)Layout.TxHousekeeping1Source]);
            byte ee = LowNibbleOf(row16[(int)Layout.TxToneCodeHighSource]);
            byte ef = LowNibbleOf(row16[(int)Layout.TxToneCodeLowSource ]);
            var pat = new TxNibblePattern(e8,ed,ee,ef);
            string label = _txLabelByPattern.TryGetValue(pat.Key(), out var lbl) ? lbl : ((e8|ed|ee|ef)==0? "0":"Err");
            return (pat, label);
        }

        public static (RxNibblePattern rx, string label) ReadRxFromRow(ReadOnlySpan<byte> row16)
        {
            byte e0 = LowNibbleOf(row16[(int)Layout.RxE0Source]);
            byte e6 = LowNibbleOf(row16[(int)Layout.RxE6Source]);
            byte e7 = LowNibbleOf(row16[(int)Layout.RxE7Source]);
            var pat = new RxNibblePattern(e0,e6,e7);
            string label = _rxLabelByKey.TryGetValue(pat.Key(), out var lbl) ? lbl : ((e0|e6|e7)==0? "0":"Err");
            return (pat, label);
        }

        public static bool ApplyTxToneByLabel(Span<byte> row16, string label)
        {
            if (!_txByLabel.TryGetValue(label, out var pat)) return false;
            WriteLowNibble(ref row16[(int)Layout.TxHousekeeping0Source], pat.E8Low);
            WriteLowNibble(ref row16[(int)Layout.TxHousekeeping1Source], pat.EDLow);
            WriteLowNibble(ref row16[(int)Layout.TxToneCodeHighSource], pat.EELow);
            WriteLowNibble(ref row16[(int)Layout.TxToneCodeLowSource ], pat.EFLow);
            return true;
        }

        public static bool ApplyRxToneByLabel(Span<byte> row16, string label)
        {
            if (!_rxByLabel.TryGetValue(label, out var pat)) return false;
            WriteLowNibble(ref row16[(int)Layout.RxE0Source], pat.E0Low);
            WriteLowNibble(ref row16[(int)Layout.RxE6Source], pat.E6Low);
            WriteLowNibble(ref row16[(int)Layout.RxE7Source], pat.E7Low);
            return true;
        }

        public static string NibblesHL(byte b) => $"{(byte)(b>>4):X1}|{(byte)(b&0xF):X1}";
        public static string FormatRowHex(ReadOnlySpan<byte> row16)
        {
            string left  = string.Join(" ", row16.Slice(0,8).ToArray().Select(x => x.ToString("X2")));
            string right = string.Join(" ", row16.Slice(8,8).ToArray().Select(x => x.ToString("X2")));
            return left + "   " + right;
        }
        public static string SteDisplayFromFLow(byte fLowNibble) => ((fLowNibble&0x8)!=0)? "Y":"";
        public static bool   IsFlagF2Set(byte fLowNibble) => (fLowNibble&0x4)!=0;
        public static byte   LowNibbleOf(byte b) => (byte)(b & 0xF);
        private static void  WriteLowNibble(ref byte b, byte low) => b = (byte)((b & 0xF0) | (low & 0x0F));
    }
}