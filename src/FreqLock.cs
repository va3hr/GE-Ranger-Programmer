namespace GE_Ranger_Programmer
{
    public class FreqLock
    {
        public static string GetTxFreq(byte[] fileData, int address)
        {
            return CalculateFrequency(fileData, address, true);
        }

        public static string GetRxFreq(byte[] fileData, int address)
        {
            return CalculateFrequency(fileData, address, false);
        }

        private static string CalculateFrequency(byte[] fileData, int address, bool isTx)
        {
            int offset = isTx ? 4 : 0;
            if (address + offset + 2 >= fileData.Length) return "Err";

            // These are the bytes containing the frequency data
            int b0 = fileData[address + offset];
            int b1 = fileData[address + offset + 1];
            int b2 = fileData[address + offset + 2];

            // This is the correct logic to decode the N and A values from those bytes
            int n = ((b2 & 0x0F) << 7) | ((b1 & 0x0F) << 3) | ((b0 & 0xE0) >> 5);
            int a = b0 & 0x1F;

            // This is the correct mathematical formula to get the final frequency
            double vcoFreq = n * 12500.0;
            double offsetFreq = a * (25000.0 / 32.0);
            double finalFreq = (vcoFreq + offsetFreq) / 1000000.0;
            
            // Check for a known inversion in the high band
            if (finalFreq >= 150 && finalFreq < 162)
            {
                finalFreq += 10.7;
            }

            return finalFreq.ToString("F4");
        }
    }
}
