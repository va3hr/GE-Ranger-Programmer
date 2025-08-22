#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        public static readonly string[] ToneMenuTx = new string[] {
            "0", "67.0", "71.9", "74.4", "77.0", "79.7", "82.5", "85.4", "88.5", "91.5", "94.8", "97.4", "100.0", "103.5", "107.2", "110.9", "114.8", "118.8", "123.0", "127.3", "131.8", "136.5", "141.3", "146.2", "151.4", "156.7", "162.2", "167.9", "173.8", "179.9", "186.2", "192.8", "203.5", "210.7"
        };
        public static readonly string[] ToneMenuRx = ToneMenuTx;
        public static readonly string[] ToneMenuAll = ToneMenuTx;

        // --- TX mapping: key = (B0.bit4, B2.bit2, B3 & 0x7F) -> tone display
        private static readonly Dictionary<(int,int,int), string> TxKeyToTone = new()
        {
    [(0, 1, 58)] = "67.0",
    [(0, 0, 58)] = "71.9",
    [(0, 1, 57)] = "74.4",
    [(0, 0, 73)] = "77.0",
    [(0, 1, 40)] = "79.7",
    [(0, 1, 24)] = "82.5",
    [(0, 0, 55)] = "85.4",
    [(0, 1, 71)] = "88.5",
    [(0, 1, 102)] = "91.5",
    [(0, 0, 21)] = "94.8",
    [(0, 1, 53)] = "97.4",
    [(0, 1, 100)] = "100.0",
    [(0, 0, 35)] = "103.5",
    [(0, 1, 83)] = "107.2",
    [(0, 0, 113)] = "110.9",
    [(0, 0, 75)] = "114.8",
    [(1, 1, 1)] = "210.7",
        };

        // last-channel bytes (for status/debug)
        private static byte LA3, LA2, LA1, LA0, LB3, LB2, LB1, LB0;
        public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
        {
            LA3=A3; LA2=A2; LA1=A1; LA0=A0; LB3=B3; LB2=B2; LB1=B1; LB0=B0;
        }

        // === Display helpers ===
        public static string TxIndexToDisplay(int index) => (index>=0 && index<ToneMenuTx.Length) ? ToneMenuTx[index] : "?";
        public static string RxIndexToDisplay(int index) => (index>=0 && index<ToneMenuRx.Length) ? ToneMenuRx[index] : "?";

        // === ASCII hex helpers ===
        public static string ToAsciiHex256(byte[] data)
        {
            var sb = new StringBuilder(data.Length*2);
            for (int i=0;i<data.Length;i++)
                sb.Append(data[i].ToString("X2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            // Placeholder pass-through; real X2212 nibble ordering can be applied later.
            // Safe for UI/testing: does not alter files.
            return (byte[])image128.Clone();
        }

        // === Tx decode ===
        public static string TxToneFromBytes(byte B0, byte B2, byte B3)
        {
            int b0_4 = (B0 >> 4) & 1;
            int b2_2 = (B2 >> 2) & 1;
            int code7 = B3 & 0x7F;
            if (b0_4==0 && b2_2==0 && code7==0) return "0"; // explicit no-tone
            return TxKeyToTone.TryGetValue((b0_4,b2_2,code7), out var tone) ? tone : "?";
        }

        // Legacy two-byte form should never guess; keep it conservative.
        public static string TxToneFromBytes(byte A1, byte B1) => "?";

        // === Rx decode ===
        private static int RxIndex(byte A3)
        {
            // The 6-bit window is a nibble-rotated packing; use the permutation we captured:
            int b5 = (A3 >> 6) & 1;
            int b4 = (A3 >> 7) & 1;
            int b3 = (A3 >> 0) & 1;
            int b2 = (A3 >> 1) & 1;
            int b1 = (A3 >> 2) & 1;
            int b0 = (A3 >> 3) & 1;
            return (b5<<5)|(b4<<4)|(b3<<3)|(b2<<2)|(b1<<1)|b0;
        }
        private static bool RxFollow(byte B3) => (B3 & 0x01) != 0;
        private static int RxBank(byte B3) => (B3 >> 1) & 1;

        public static string RxToneFromBytes(byte A3, byte B3, string txToneIfFollow)
        {
            int idx = RxIndex(A3);
            bool follow = RxFollow(B3);
            if (idx==0)
                return follow ? (string.IsNullOrWhiteSpace(txToneIfFollow) ? "0" : txToneIfFollow) : "0";

            // Until we finish the full derived map (bank+idx -> label), keep it honest:
            return "?";
        }

        // Legacy 3-byte form (A3,A2,B3) delegates to 2-arg with no follow source.
        public static string RxToneFromBytes(byte A3, byte A2, byte B3) => RxToneFromBytes(A3, B3, "0");

        // === Setters (encode) — safe minimal: support clearing to 0; else signal unknown (false) ===
        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string? rxTone)
        {
            if (string.IsNullOrWhiteSpace(rxTone) || rxTone=="0" || rxTone==".")
            {
                // clear index to 0; preserve follow bit
                int follow = B3 & 0x01;
                A3 = 0; // index -> 0
                B3 = (byte)((B3 & ~0x03) | follow); // keep follow, clear bank
                return true;
            }
            return false; // let UI show "?" for unknown encode
        }

        public static bool TrySetTxTone(ref byte A3, ref byte A2, ref byte B3, string? txTone)
        {
            if (string.IsNullOrWhiteSpace(txTone) || txTone=="0" || txTone==".")
            {
                // explicit no-tone triplet
                B3 = 0x00;
                return true;
            }
            // we don't yet have full inverse map; return false so caller shows "?"
            return false;
        }

        // Status shims (some code calls these; keep as no-ops here)
        public static void SetStatusText(string? text) { }
        public static void SetStatusText(object? _, string? text) { }
        public static void SetStatusChannel(int ch) { }
        public static void SetStatusChannel(object? _, int ch) { }
    }
}
