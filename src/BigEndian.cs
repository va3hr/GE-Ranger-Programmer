// ======================= DO NOT EDIT =======================
// BigEndian.cs — Canonical big‑endian helpers for X2212 project.
// Purpose: keep nibble/bit extraction *centralized* so frequency/tone
// decoders never re‑implement this logic differently.
// Numbering conventions:
//   • Nibbles: Hi(b) = b[7:4], Lo(b) = b[3:0]  (big‑endian nibbles)
//   • Bits (MSB‑first): BitMsb(b,7) is b7 (MSB), BitMsb(b,0) is b0 (LSB).
//   • Bits (LSB‑first): BitLsb(b,0) is b0 (LSB), BitLsb(b,7) is b7 (MSB).
// ============================================================
using System;
using System.Runtime.CompilerServices;

namespace GE_Ranger_Programmer
{
    public static class BigEndian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Hi(byte b) => (b >> 4) & 0xF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Lo(byte b) => b & 0xF;

        // MSB-first bit (7..0). Example: BitMsb(0b1000_0000, 7) == true.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BitMsb(byte b, int msbIndex)
        {
            if ((uint)msbIndex > 7) throw new ArgumentOutOfRangeException(nameof(msbIndex));
            int lsbIndex = 7 - msbIndex;
            return ((b >> lsbIndex) & 1) != 0;
        }

        // LSB-first bit (0..7). Example: BitLsb(0b0000_0001, 0) == true.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BitLsb(byte b, int lsbIndex)
        {
            if ((uint)lsbIndex > 7) throw new ArgumentOutOfRangeException(nameof(lsbIndex));
            return ((b >> lsbIndex) & 1) != 0;
        }

        // Extract a field using MSB-first numbering: field is b[msbStart : msbStart-width+1].
        // Example: BitsMsb(0b1011_1100, msbStart:7, width:3) -> 0b101 (5).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BitsMsb(byte b, int msbStart, int width)
        {
            if (width <= 0 || width > 8) throw new ArgumentOutOfRangeException(nameof(width));
            if (msbStart < 0 || msbStart > 7) throw new ArgumentOutOfRangeException(nameof(msbStart));
            int lsbStart = (7 - msbStart);
            int lsbEnd = lsbStart + width - 1;
            if (lsbEnd > 7) throw new ArgumentOutOfRangeException(nameof(width), "Field overruns byte");
            int mask = (1 << width) - 1;
            return (b >> lsbStart) & mask;
        }
    }
}


