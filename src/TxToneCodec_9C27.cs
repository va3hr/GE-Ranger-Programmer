// TxToneCodec_9C27.cs
// Bank-specific TX tone codec for (A1=0x9C, A2=0x27).
// Encodes/decodes TX tone index using (B1,B0).
// TX/RX remain strictly separate; frequency code untouched.

namespace X2212
{
    public static class TxToneCodec_9C27
    {
        // Returns -1 if not this bank or mapping unknown. Index 0 means "no TX tone".
        public static int DecodeIndex(byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, byte b1, byte b0)
        {
            if (a1 != 0x9C || a2 != 0x27) return -1;
            switch ((b1, b0))
            {
                case (0x90, 0x71): return 1;
                case (0x94, 0x66): return 7;
                case (0x90, 0xc9): return 12;
                case (0x90, 0xcb): return 16;
                case (0x94, 0x80): return 30;
                default: return -1;
            }
        }

        // Try to encode: writes b1/b0 for a known index; returns true if handled.
        public static bool TryEncodeIndex(int idx, byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, ref byte b1, ref byte b0)
        {
            if (a1 != 0x9C || a2 != 0x27) return false;
            switch (idx)
            {
                case 1: b1 = 0x90; b0 = 0x71; return true;
                case 7: b1 = 0x94; b0 = 0x66; return true;
                case 12: b1 = 0x90; b0 = 0xc9; return true;
                case 16: b1 = 0x90; b0 = 0xcb; return true;
                case 30: b1 = 0x94; b0 = 0x80; return true;
                default: return false;
            }
        }
    }
}
