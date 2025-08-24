// TxToneCodec_9827.cs
// Bank-specific TX tone codec for (A1=0x98, A2=0x27).
// Encodes/decodes TX tone index using (B1,B0).
// TX and RX remain strictly separate; frequency code untouched.

namespace X2212
{
    public static class TxToneCodec_9827
    {
        // Returns -1 if not this bank or mapping unknown. Index 0 means "no TX tone".
        public static int DecodeIndex(byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, byte b1, byte b0)
        {
            if (a1 != 0x98 || a2 != 0x27) return -1;
            switch ((b1, b0))
            {
                case (0x94, 0xd3): return 2;
                case (0x94, 0xc7): return 8;
                case (0x90, 0xc9): return 12;
                case (0x90, 0x9d): return 21;
                case (0x90, 0x7e): return 24;
                case (0x94, 0x81): return 33;
                default: return -1;
            }
        }

        // Try to encode: writes b1/b0 for a known index; returns true if handled.
        public static bool TryEncodeIndex(int idx, byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, ref byte b1, ref byte b0)
        {
            if (a1 != 0x98 || a2 != 0x27) return false;
            switch (idx)
            {
                case 2: b1 = 0x94; b0 = 0xd3; return true;
                case 8: b1 = 0x94; b0 = 0xc7; return true;
                case 12: b1 = 0x90; b0 = 0xc9; return true;
                case 21: b1 = 0x90; b0 = 0x9d; return true;
                case 24: b1 = 0x90; b0 = 0x7e; return true;
                case 33: b1 = 0x94; b0 = 0x81; return true;
                default: return false;
            }
        }
    }
}
