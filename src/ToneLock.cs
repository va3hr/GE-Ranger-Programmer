namespace Rangr.Tones
{
    public static class ToneLock
    {
        // ---------------- RX ----------------
        public static int BuildReceiveToneIndex(byte A3) { ... }

        public static string GetReceiveToneLabel(byte A3, string[] canonicalLabels) { ... }

        // ---------------- TX ----------------
        public static int BuildTransmitToneIndex(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            int txToneIndex = 0;

            txToneIndex |= ((B3 >> 7) & 1) << 5;  // B3.7 → bit 5
            txToneIndex |= ((B3 >> 4) & 1) << 4;  // B3.4 → bit 4
            txToneIndex |= ((B0 >> 5) & 1) << 3;  // B0.5 → bit 3
            txToneIndex |= ((B0 >> 2) & 1) << 2;  // B0.2 → bit 2
            txToneIndex |= ((B0 >> 1) & 1) << 1;  // B0.1 → bit 1
            txToneIndex |= ((B0 >> 0) & 1) << 0;  // B0.0 → bit 0

            return txToneIndex;
        }

        public static string GetTransmitToneLabel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0,
            string[] canonicalLabels)
        {
            int txToneIndex = BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);

            if (txToneIndex == 0) return "0";
            if (txToneIndex >= 1 && txToneIndex <= 33) return canonicalLabels[txToneIndex];
            return "Err";
        }
    }
}
