#nullable enable
using System;
using System.Collections.Generic;

namespace RangrApp.Locked
{
    /// <summary>TX-only decode/encode. Keep this separate from RX to avoid regressions.</summary>
    public static class TxToneLock
    {
        public static readonly string[] ToneMenuTx = new string[] { "0", "67.0", "71.9", "74.4", "77.0", "79.7", "82.5", "85.4", "88.5", "91.5", "94.8", "97.4", "100.0", "103.5", "107.2", "110.9", "114.8", "118.8", "123.0", "127.3", "131.8", "136.5", "141.3", "146.2", "151.4", "156.7", "162.2", "167.9", "173.8", "179.9", "186.2", "192.8", "203.5", "210.7" };

        // Final scheme will be derived strictly from project data (6-bit code).
        // For now, we expose both the previously observed 3-field key and a slot for the final 6-bit index.
        // DO NOT modify menus or UI elsewhere.

        // --- CURRENT WORKING KEY (subject to correction with your DOS-backed TXMAP runs) ---
        // We maintain a dictionary so we never "guess". Unknown => "?" and you see it in the grid.
        private static readonly Dictionary<(int,int,int), string> KeyToTone = new() { /* filled as we confirm */ };
        private static readonly Dictionary<string,(int,int,int)> ToneToKey = new(StringComparer.Ordinal);

        public static string TxToneFromBytes(byte B0, byte B2, byte B3)
        {
            // If we keep the 3-field key, compute it here.
            int k0 = (B0 >> 4) & 1;
            int k1 = (B2 >> 2) & 1;
            int k2 = B3 & 0x7F;
            if (k0==0 && k1==0 && k2==0) return "0";
            return KeyToTone.TryGetValue((k0,k1,k2), out var tone) ? tone : "?";
        }

        // Legacy form seen in older branches; never guess from (A1,B1)
        public static string TxToneFromBytes(byte A1, byte B1) => "?";

        public static bool TrySetTxTone(ref byte B0, ref byte B2, ref byte B3, string? display)
        {
            display ??= "0";
            if (display == "0") { B3 = (byte)(B3 & 0x7F); return true; }
            if (!ToneToKey.TryGetValue(display, out var key)) return false;
            // write key safely
            B3 = (byte)((B3 & 0x80) | (key.Item3 & 0x7F)); // low-7
            // selector bits
            if (key.Item1==0) B0 = (byte)(B0 & ~(1<<4)); else B0 = (byte)(B0 | (1<<4));
            if (key.Item2==0) B2 = (byte)(B2 & ~(1<<2)); else B2 = (byte)(B2 | (1<<2));
            return true;
        }

        // Utility to allow us to inject the confirmed map at runtime if needed
        public static void ReplaceKeyMap(Dictionary<(int,int,int), string> map)
        {
            KeyToTone.Clear();
            foreach (var kv in map) KeyToTone[kv.Key] = kv.Value;
            ToneToKey.Clear();
            foreach (var kv in map) if (kv.Value != "0") ToneToKey[kv.Value] = kv.Key;
        }
    }
}
