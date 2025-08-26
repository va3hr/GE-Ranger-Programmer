// TxToneCodec_Banked.cs
// Simple shim that delegates to known bank-specific TX codecs.
// Keeps TX path separate; frequency and RX code untouched.

namespace X2212
{
    public static class TxToneCodec_Banked
    {
        public static int DecodeIndex(byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, byte b1, byte b0)
        {
            int idx;
            // Try known bank-specific codecs in priority order
            idx = TxToneCodec_9827.DecodeIndex(a3,a2,a1,a0,b3,b2,b1,b0);
            if (idx >= 0) return idx;
            idx = TxToneCodec_9C27.DecodeIndex(a3,a2,a1,a0,b3,b2,b1,b0);
            if (idx >= 0) return idx;
            return -1;
        }

        public static bool TryEncodeIndex(int targetIndex, byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, ref byte b1, ref byte b0)
        {
            if (TxToneCodec_9827.TryEncodeIndex(targetIndex, a3,a2,a1,a0,b3,b2, ref b1, ref b0)) return true;
            if (TxToneCodec_9C27.TryEncodeIndex(targetIndex, a3,a2,a1,a0,b3,b2, ref b1, ref b0)) return true;
            return false;
        }
    }
}
