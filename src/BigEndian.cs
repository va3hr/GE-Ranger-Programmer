namespace GE_Ranger_Programnamespace GE_Ranger_Programmer
{
    public static class BigEndian
    {
        // This is the method your RgrCodec.cs needs.
        public static byte BitMsb(byte value)
        {
            byte result = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    result |= (byte)(1 << (7 - i));
                }
            }
            return result;
        }

        public static ushort SwapUInt16(ushort value)
        {
            return (ushort)(((value & 0xFF) << 8) | ((value >> 8) & 0xFF));
        }

        public static short SwapInt16(short value)
        {
            return (short)SwapUInt16((ushort)value);
        }
        
        // This is the method we have been using in MainForm.cs
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
