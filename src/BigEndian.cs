namespace GE_Ranger_Programmer
{ // <-- This opening brace is required

    public static class BigEndian
    {
        // This is the method your RgrCodec.cs needs
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

        // This is the method used by MainForm.cs
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

        // Other methods from your repository
        public static ushort SwapUInt16(ushort value)
        {
            return (ushort)(((value & 0xFF) << 8) | ((value >> 8) & 0xFF));
        }

        public static short SwapInt16(short value)
        {
            return (short)SwapUInt16((ushort)value);
        }
    }

} // <-- This closing brace is required
