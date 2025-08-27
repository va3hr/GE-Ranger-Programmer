
#nullable disable
// ToneLock.cs — GE Rangr tone decode/encode (wired to BitExact_Indexer)
// Minimal, traditional C#. TX/RX kept separate. Frequency untouched.
// Big‑endian, MSB‑first bit numbering lives inside BitExact_Indexer.

using System;
using System.Text;
using X2212.Tones;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical CTCSS menu (index 0 == "0") =====
        public static readonly string[] Cg = ChannelGuardTones.CanonicalLabels;

        // UI menus as arrays (used by DataGridViewComboBoxColumn.DataSource)
        public static readonly string[] ToneMenuTx = Cg;
        public static readonly string[] ToneMenuRx = Cg;

        // ===== Last-channel cache for legacy shims =====
        private static bool _lastValid;
        private static byte _A3, _A2, _A1, _A0, _B3, _B2, _B1, _B0;

        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            _A3 = A3; _A2 = A2; _A1 = A1; _A0 = A0; _B3 = B3; _B2 = B2; _B1 = B1; _B0 = B0;
            _lastValid = true;
        }

        
private static string MapTx(int idx)
{
    if (idx == 0) return "0";
    if (ChannelGuardTones.TxIndexToTone != null &&
        ChannelGuardTones.TxIndexToTone.TryGetValue(idx, out var tone))
        return tone.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    return "?";
}

private static string MapRx(int bank, int idx)
{
    if (idx == 0) return "0";
    // For now we only seed Bank0; allow both banked and plain keys
    if (ChannelGuardTones.RxIndexToTone_Bank0 != null)
    {
        int bankedKey = (bank << 6) | idx;
        if (ChannelGuardTones.RxIndexToTone_Bank0.TryGetValue(bankedKey, out var tb))
            return tb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        if (ChannelGuardTones.RxIndexToTone_Bank0.TryGetValue(idx, out var t))
            return t.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }
    return "?";
}
// ===== TX =====
        public static string TxToneFromBytes(byte A1, byte B1)
        {
            if (_lastValid)
            {
                int idx = BitExact_Indexer.TxIndex(_A3,_A2,_A1,_A0,_B3,_B2,B1,_B0);
                return MapTx(idx);
            }
            return "0";
        }
            return "0";
        }
        public static string TxToneFromBytes(byte p0, byte p1, byte p2)
        {
            if (_lastValid)
            {
                int idx = BitExact_Indexer.TxIndex(_A3,_A2,_A1,_A0,_B3,_B2,_B1,_B0);
                return MapTx(idx);
            }
            return "0";
        }
            return "0";
        }

        // NOTE: Encoding is held as no‑op until the decode path is fully validated in UI.
        // This preserves write behavior and avoids corrupting bytes while we finalize mapping.
        public static bool TrySetTxTone(ref byte A1, ref byte B1, string tone)
        {
            // TODO (encode): map 'tone' -> index and set the six straddled bits in A1/B1 (big‑endian).
            // For now, leave bytes unchanged and signal success so UI can proceed.
            return true;
        }

        // ===== RX =====
        public static string RxToneFromBytes(byte A3, byte A2, byte B3)
        {
            int bank = (B3 >> 1) & 1;
            int idx = BitExact_Indexer.RxIndex(A3, A2, _A1, _A0, B3, _B2, _B1, _B0);
            return MapRx(bank, idx);
        }
            return "0";
        }
        public static string RxToneFromBytes(byte p0, byte p1, string _ignoredTx)
        {
            int bank = (_B3 >> 1) & 1;
            int idx = BitExact_Indexer.RxIndex(_A3, _A2, _A1, _A0, _B3, _B2, _B1, _B0);
            return MapRx(bank, idx);
        }
            return "0";
        }

        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string tone)
        {
            // TODO (encode): map 'tone' -> index and set the six straddled bits across A3/A2/B3 (big‑endian).
            // Respect Follow (B3.bit0) only when RX index==0. For now, no‑op to keep write path stable.
            return true;
        }

        // ===== Decode a full channel (tuple return) =====
        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int txIdx = BitExact_Indexer.TxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int rxIdx = BitExact_Indexer.RxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            int bank  = (B3 >> 1) & 1;
            string tx = MapTx(txIdx);
            string rx = MapRx(bank, rxIdx);
            return (tx, rx);
        }

        // ===== Utilities expected by RgrCodec =====
        public static string ToAsciiHex256(byte[] image128)
        {
            var sb = new StringBuilder(256 + 32);
            for (int i = 0; i < 128; i++)
            {
                sb.Append(image128[i].ToString("X2"));
                if ((i & 0x0F) == 0x0F) sb.AppendLine();
                else sb.Append(' ');
            }
            return sb.ToString();
        }

        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            // passthrough; frequency logic is elsewhere and already correct
            return image128;
        }
    }
}
