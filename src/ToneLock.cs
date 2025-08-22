#nullable enable
using System;
using System.Collections.Generic;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        public static readonly string[] ToneMenuTx = new string[] { "0", "67.0", "71.9", "74.4", "77.0", "79.7", "82.5", "85.4", "88.5", "91.5", "94.8", "97.4", "100.0", "103.5", "107.2", "110.9", "114.8", "118.8", "123.0", "127.3", "131.8", "136.5", "141.3", "146.2", "151.4", "156.7", "162.2", "167.9", "173.8", "179.9", "186.2", "192.8", "203.5", "210.7" };
        public static readonly string[] ToneMenuRx = ToneMenuTx;
        public static readonly string[] ToneMenuAll = ToneMenuTx;

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
            [(1, 1, 1)] = "210.7",
        };

        public static string TxToneFromBytes(byte B0, byte B2, byte B3)
        {
            bool present = (B3 & 0x80) != 0;
            int k0 = (B0 >> 4) & 1;
            int k1 = (B2 >> 2) & 1;
            int k2 = B3 & 0x7F;
            if (!present) return "0";
            if (k0==0 && k1==0 && k2==0) return "0";
            return TxKeyToTone.TryGetValue((k0,k1,k2), out var tone) ? tone : "?";
        }

        public static string TxToneFromBytes(byte A1, byte B1) => "?";

        private static int RxIndex(byte A3)
        {
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
            if (idx == 0)
                return follow ? (string.IsNullOrWhiteSpace(txToneIfFollow) ? "0" : txToneIfFollow) : "0";
            return "?";
        }

        public static string RxToneFromBytes(byte A3, byte A2, byte B3) => RxToneFromBytes(A3, B3, "0");

        public static string TxIndexToDisplay(int index) => (index >= 0 && index < ToneMenuTx.Length) ? ToneMenuTx[index] : "?";
        public static string RxIndexToDisplay(int index) => (index >= 0 && index < ToneMenuRx.Length) ? ToneMenuRx[index] : "?";
    }
}