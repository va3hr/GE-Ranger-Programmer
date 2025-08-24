// RxToneCodec_Banked.cs — auto-generated from RXMAP_A/B.
using System;
namespace X2212 {
  public static class RxToneCodec_Banked {
    // Returns -1 if bank not recognized; else 0..33 index (33=210.7 handled separately by caller).
    public static int DecodeIndex(byte a3, byte a2, byte a1, byte a0, byte b3, byte b2, byte b1, byte b0) {
      int key = (a1<<8) | a2;
      switch (key) {
        case 0x2C27: return DecodeVia(a0, M_2C_27, C_2C_27);
        case 0x2C37: return DecodeVia(a0, M_2C_37, C_2C_37);
        case 0x2D37: return DecodeVia(a0, M_2D_37, C_2D_37);
        case 0x2E17: return DecodeVia(a0, M_2E_17, C_2E_17);
        case 0x6737: return DecodeVia(a0, M_67_37, C_67_37);
        case 0x6C37: return DecodeVia(a0, M_6C_37, C_6C_37);
        case 0x6D37: return DecodeVia(a0, M_6D_37, C_6D_37);
        case 0x9C27: return DecodeVia(a0, M_9C_27, C_9C_27);
        case 0xA447: return DecodeVia(a0, M_A4_47, C_A4_47);
        case 0xA547: return DecodeVia(a0, M_A5_47, C_A5_47);
        case 0xAC37: return DecodeVia(a0, M_AC_37, C_AC_37);
        case 0xAE37: return DecodeVia(a0, M_AE_37, C_AE_37);
        case 0xE637: return DecodeVia(a0, M_E6_37, C_E6_37);
        case 0xE737: return DecodeVia(a0, M_E7_37, C_E7_37);
        case 0xEC37: return DecodeVia(a0, M_EC_37, C_EC_37);
        case 0xEF37: return DecodeVia(a0, M_EF_37, C_EF_37);
        default: return -1; } }
    private static int DecodeVia(byte a0, byte[,] M, byte[] C) {
      if (a0==0) return 0;
      int idx=0;
      for (int bit=0; bit<6; bit++){ int acc=C[bit]; for(int k=0;k<8;k++){ int x=((a0>>(7-k))&1); acc ^= (M[bit,k] & x);} idx |= (acc&1)<<bit; }
      return idx; }
        private static readonly byte[,] M_2C_27 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_2C_27 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_2C_37 = new byte[6,8]
        {
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_2C_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_2D_37 = new byte[6,8]
        {
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_2D_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_2E_17 = new byte[6,8]
        {
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_2E_17 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_67_37 = new byte[6,8]
        {
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_67_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_6C_37 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 1, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_6C_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_6D_37 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_6D_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_9C_27 = new byte[6,8]
        {
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_9C_27 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_A4_47 = new byte[6,8]
        {
            { 0, 1, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_A4_47 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_A5_47 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 1, 0, 0, 0, 0 },
            { 0, 0, 0, 1, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_A5_47 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_AC_37 = new byte[6,8]
        {
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 1, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_AC_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_AE_37 = new byte[6,8]
        {
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_AE_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_E6_37 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 1, 0, 0, 0 },
            { 0, 1, 0, 0, 1, 0, 0, 0 },
            { 0, 0, 0, 0, 1, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_E6_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_E7_37 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 1, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 1, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_E7_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_EC_37 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_EC_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[,] M_EF_37 = new byte[6,8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static readonly byte[] C_EF_37 = new byte[6] { 0, 0, 0, 0, 0, 0 };
  }
}