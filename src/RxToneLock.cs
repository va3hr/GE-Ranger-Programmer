#nullable enable
using System;

namespace RangrApp.Locked
{
    /// <summary>RX-only decode/encode. Follow flag handled here; bank reserved for full map.</summary>
    public static class RxToneLock
    {
        public static readonly string[] ToneMenuRx = new string[] { "0", "67.0", "71.9", "74.4", "77.0", "79.7", "82.5", "85.4", "88.5", "91.5", "94.8", "97.4", "100.0", "103.5", "107.2", "110.9", "114.8", "118.8", "123.0", "127.3", "131.8", "136.5", "141.3", "146.2", "151.4", "156.7", "162.2", "167.9", "173.8", "179.9", "186.2", "192.8", "203.5", "210.7" };

        private static int RxIndex(byte A3)
        {
            // 6-bit window: A3 bits [6,7,0,1,2,3] => idx[5..0] (from project notes)
            int b5 = (A3 >> 6) & 1;
            int b4 = (A3 >> 7) & 1;
            int b3 = (A3 >> 0) & 1;
            int b2 = (A3 >> 1) & 1;
            int b1 = (A3 >> 2) & 1;
            int b0 = (A3 >> 3) & 1;
            return (b5<<5)|(b4<<4)|(b3<<3)|(b2<<2)|(b1<<1)|b0;
        }
        private static bool RxFollow(byte B3) => (B3 & 0x01) != 0;
        private static int  RxBank(byte B3)   => (B3 >> 1) & 1;

        public static string RxToneFromBytes(byte A3, byte B3, string txToneIfFollow)
        {
            int idx = RxIndex(A3);
            bool follow = RxFollow(B3);
            if (idx==0) return follow ? (string.IsNullOrWhiteSpace(txToneIfFollow) ? "0" : txToneIfFollow) : "0";
            return "?"; // until the full (bank,idx)->tone table is slotted in
        }

        public static string RxToneFromBytes(byte A3, byte A2, byte B3) => RxToneFromBytes(A3, B3, "0");

        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string? display)
        {
            display ??= "0";
            if (display == "0") { A3 = 0; A2 = 0; B3 = (byte)(B3 & ~0x01); return true; }
            if (string.Equals(display, "follow", StringComparison.OrdinalIgnoreCase)) { B3 = (byte)(B3 | 0x01); return true; }
            return false;
        }
    }
}
