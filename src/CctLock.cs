using System;
namespace GE_Ranger_Programmer
{
public static class CctLock
{
    // Decode the 3-bit CCT from the upper bits of B3.
    // Returns 0..7; anything outside that range is treated as unknown.
    public static int DecodeCct(byte B3)
    {
        // B3: [b7 b6 b5 b4 b3 b2 b1 b0]
        // CCT is in b7..b5 (MSBs). This matches the earlier working build.
        return (B3 >> 5) & 0x07;
    }

    public static string DecodeCctText(byte B3)
    {
        int v = DecodeCct(B3);
        if (v < 0 || v > 7) return "?";
        return v.ToString();
    }

    // STE flag: we keep the earlier interpretation (A3 bit7).
    // Returns "Y" when set, "" when clear.
    public static string DecodeSteText(byte A3)
    {
        return ((A3 & 0x80) != 0) ? "Y" : "";
    }
}

}

