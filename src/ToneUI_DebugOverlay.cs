// ToneUI Debug Overlay — shows indices next to tone labels so you can verify against DOS.
// No UI redesign; just call FormatTx(...) and FormatRx(...) where you currently bind cell text.

using System;
using System.Globalization;

namespace X2212.Tones
{
    public static class ToneUIDebug
    {
        static string LabelOrQ(double? v)
        {
            if (v == null) return "?";
            if (Math.Abs(v.Value) < 1e-12) return "0";
            return v.Value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string FormatTx(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            int idx = BitExact_Indexer.TxIndex(A3,A2,A1,A0,B3,B2,B1,B0);
            if (idx == 0) return "0 [idx 0]";
            double? tone = null;
            if (ChannelGuardTones.TxIndexToTone != null &&
                ChannelGuardTones.TxIndexToTone.TryGetValue(idx, out var t)) tone = t;
            return $"{LabelOrQ(tone)} [idx {idx}]";
        }

        public static string FormatRx(byte A3, byte A2, byte B3)
        {
            // RX index = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3]; Follow ignored, Bank honored via maps if provided.
            int i5=(A3>>6)&1, i4=(A3>>7)&1, i3=(A3>>0)&1, i2=(A3>>1)&1, i1=(A3>>2)&1, i0=(A3>>3)&1;
            int idx = (i5<<5)|(i4<<4)|(i3<<3)|(i2<<2)|(i1<<1)|i0;
            if (idx == 0) return "0 [idx 0]";

            double? tone = null;
            int bank = (B3>>1)&1;
            int key  = (bank<<6)|idx;
            if (ChannelGuardTones.RxIndexToTone_Bank0 != null)
            {
                if (ChannelGuardTones.RxIndexToTone_Bank0.TryGetValue(key, out var tb)) tone = tb;
                else if (ChannelGuardTones.RxIndexToTone_Bank0.TryGetValue(idx, out var t)) tone = t;
            }
            return $"{LabelOrQ(tone)} [idx {idx}]";
        }
    }
}
