// SPDX-License-Identifier: MIT
// GE Rangr Programmer — RX bit-level decode/encode (namespace version).

#nullable enable
using System;

namespace RangrApp.Locked;

public static class RxToneCodec
{
    // 2 banks × 64 slots; null entries mean "unknown" → display "?"
    static readonly string?[][] rxMap = { new string?[64], new string?[64] };

    static RxToneCodec()
    {
        InitRxMap();
    }

    /// Decode RX to display text from raw A3/B3 bytes and the already-decoded TX display (for Follow).
    public static string DecodeRxTone(byte A3, byte B3, string txDisplayForFollow)
    {
        // idx[5..0] = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3] (MSB→LSB)
        int b5 = (A3 >> 6) & 1;
        int b4 = (A3 >> 7) & 1;
        int b3 = (A3 >> 0) & 1;
        int b2 = (A3 >> 1) & 1;
        int b1 = (A3 >> 2) & 1;
        int b0 = (A3 >> 3) & 1;
        int idx = (b5 << 5) | (b4 << 4) | (b3 << 3) | (b2 << 2) | (b1 << 1) | b0;

        int bank = (B3 >> 1) & 1;
        bool follow = (B3 & 1) != 0;

        if (idx == 0)
            return follow ? txDisplayForFollow : "0";

        var tone = rxMap[bank][idx];
        return tone ?? "?";
    }

    /// Encode RX back into A3/B3. Only RX bits and follow/bank are touched.
    public static (byte newA3, byte newB3) EncodeRxTone(byte A3, byte B3, string display, bool follow, int bank)
    {
        if (!RxToneLock.TryDisplayToIndex(display, out var idx))
            idx = 0;

        // Write follow flag (B3.bit0)
        if (follow) B3 = (byte)(B3 | 0x01);
        else        B3 = (byte)(B3 & ~0x01);

        // Write bank (B3.bit1)
        if ((bank & 1) != 0) B3 = (byte)(B3 | 0x02);
        else                 B3 = (byte)(B3 & ~0x02);

        // Clear RX index bits in A3 (LSB positions {1,0,7,6,5,4})
A3 = (byte)(A3 & ~((1<<1)|(1<<0)|(1<<7)|(1<<6)|(1<<5)|(1<<4)));
if (idx == 0)
            return (A3, B3);

        // Write idx bits MSB→LSB into A3.{6,7,0,1,2,3}
        if (((idx >> 5) & 1) != 0) A3 |= (1<<6);
        if (((idx >> 4) & 1) != 0) A3 |= (1<<7);
        if (((idx >> 3) & 1) != 0) A3 |= (1<<0);
        if (((idx >> 2) & 1) != 0) A3 |= (1<<1);
        if (((idx >> 1) & 1) != 0) A3 |= (1<<2);
        if (((idx >> 0) & 1) != 0) A3 |= (1<<3);

        return (A3, B3);
    }

    private static void InitRxMap()
    {
        // bank 0
        rxMap[0][ 3] = "173.8";
        rxMap[0][10] = "94.8";
        rxMap[0][11] = "162.2";
        rxMap[0][17] = "82.5";
        rxMap[0][34] = "100.0";
        rxMap[0][38] = "91.5";
        rxMap[0][40] = "110.9";
        rxMap[0][45] = "192.8";
        rxMap[0][51] = "186.2";
        rxMap[0][55] = "156.7";
        rxMap[0][56] = "203.5";
        rxMap[0][60] = "107.2";
        rxMap[0][61] = "114.8";
        rxMap[0][63] = "141.3";

        // bank 1
        rxMap[1][ 0] = "210.7";
        rxMap[1][ 1] = "79.7";
        rxMap[1][ 5] = "67.0";
        rxMap[1][ 7] = "146.2";
        rxMap[1][12] = "103.5";
        rxMap[1][14] = "85.4";
        rxMap[1][15] = "131.8";
        rxMap[1][16] = "123.0";
        rxMap[1][21] = "71.9";
        rxMap[1][27] = "167.9";
        rxMap[1][32] = "118.8";
        rxMap[1][35] = "179.9";
        rxMap[1][39] = "151.4";
        rxMap[1][41] = "74.4";
        rxMap[1][47] = "136.5";
        rxMap[1][48] = "127.3";
        rxMap[1][57] = "77.0";
        rxMap[1][58] = "97.4";
        rxMap[1][62] = "88.5";
    }
}
