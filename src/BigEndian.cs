namespace GE_Ranger_Programmer
{
    public static class BigEndian
    {
        public static byte[] SwapBytes(byte[] data)
        {
            byte[] swapped = new byte[data.Length];
            for (int i = 0; i < data.Length; i += 2)
            {
                if (i + 1 < data.Length)
                {
                    swapped[i] = data[i + 1];
                    swapped[i + 1] = data[i];
                }
            }
            return swapped;
        }
    }
}
