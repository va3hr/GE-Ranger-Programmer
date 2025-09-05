namespace GE_Ranger_Programmer
{
    public class FreqLock
    {
        public static string GetTxFreq(byte[] fileData, int address) => CalculateFrequency(fileData, address, true);
        public static string GetRxFreq(byte[] fileData, int address) => CalculateFrequency(fileData, address, false);

        private static string CalculateFrequency(byte[] fileData, int address, bool isTx)
        {
            int offset = isTx ? 4 : 0;
            if (address + offset + 2 >= fileData.Length) return "Err";
            int b0 = fileData[address + offset];
            int b1 = fileData[address + offset + 1];
            int b2 = fileData[address + offset + 2];
            int n = ((b2 & 0x0F) << 7) | ((b1 & 0x0F) << 3) | ((b0 & 0xE0) >> 5);
            int a = b0 & 0x1F;
            if (n < 296 || n > 1125) return "Err";
            double freq = (n * 12500.0 + (a * 25000.0 / 32.0)) / 1000000.0;
            return freq.ToString("F4");
        }
    }
}
