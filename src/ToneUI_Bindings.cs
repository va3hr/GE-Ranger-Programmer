// X2212 — Tone UI Integration (surgical, no re-engineering)
using System;
using System.Globalization;

namespace X2212.Tones
{
    /// <summary>
    /// Supplies UI labels and decode helpers that strictly follow ChannelGuardTones.Canonical.
    /// Unknown indices render as "?"; exact zero renders as "0".
    /// </summary>
    public static class ToneUI
    {
        public static string[] CanonicalLabels => ChannelGuardTones.CanonicalLabels;

        static string LabelFor(double tone)
        {
            if (tone == 0.0) return "0";
            // Use the canonical one-decimal labels (guaranteed match)
            for (int i = 1; i < ChannelGuardTones.Canonical.Length; i++)
                if (Math.Abs(ChannelGuardTones.Canonical[i] - tone) < 1e-9)
                    return ChannelGuardTones.CanonicalLabels[i];
            // Fallback (shouldn't hit if dictionaries contain Canonical values)
            return tone.ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Decode TX tone label using the confirmed 6-bit TX index and the TxIndexToTone dictionary.
        /// Indices not present in the dictionary return "?".
        /// </summary>
        public static string DecodeTxLabel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int idx = BitExact_Indexer.TxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            if (idx == 0) return "0";
            if (ChannelGuardTones.TxIndexToTone != null &&
                ChannelGuardTones.TxIndexToTone.TryGetValue(idx, out var t))
                return LabelFor(t);
            return "?";
        }

        /// <summary>
        /// Decode RX tone label using the confirmed 6-bit RX index (A3-only).
        /// Follow bit is ignored (per current direction). Bank = B3.bit1 is honored.
        /// If not found in a banked key, falls back to plain idx. Unknown → "?".
        /// </summary>
        public static string DecodeRxLabel(byte A3, byte A2, byte B3)
        {
            // RX index = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]
            int i5=(A3>>6)&1, i4=(A3>>7)&1, i3=(A3>>0)&1, i2=(A3>>1)&1, i1=(A3>>2)&1, i0=(A3>>3)&1;
            int idx = (i5<<5)|(i4<<4)|(i3<<3)|(i2<<2)|(i1<<1)|i0;

            if (idx == 0) return "0";           // explicit 0; ignore Follow
            int bank = (B3 >> 1) & 1;
            int bankedKey = (bank << 6) | idx;

            // Prefer banked map if you later add one; today we seed Bank0 only.
            if (ChannelGuardTones.RxIndexToTone_Bank0 != null)
            {
                if (ChannelGuardTones.RxIndexToTone_Bank0.TryGetValue(bankedKey, out var tB)) return LabelFor(tB);
                if (ChannelGuardTones.RxIndexToTone_Bank0.TryGetValue(idx, out var t))        return LabelFor(t);
            }
            return "?";
        }
    }
}
