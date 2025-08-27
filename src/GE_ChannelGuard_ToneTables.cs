// GE Channel Guard — Canonical Tone Tables
// Source: Provided "STANDARD TONE FREQUENCIES Hz" image (33 tones).
// This file corrects label/ordering drift. Use these values verbatim for UI lists and mappings.
// No helpers, no endianness here — just data.

namespace X2212.Tones
{
    public static class ChannelGuardTones
    {
        // Canonical set (first element 0.0 for "no tone").
        // EXACT values and labels per the image; keep one decimal rendering.
        public static readonly double[] Canonical = new double[]
        {
            0.0,
            67.0, 71.9, 74.4, 77.0, 79.7, 82.5, 85.4,
            88.5, 91.5, 94.8, 97.4, 100.0, 103.5, 107.2, 110.9,
            114.8, 118.8, 123.0, 127.3, 131.8, 136.5, 141.3, 146.2,
            151.4, 156.7, 162.2, 167.9, 173.8, 179.9, 186.2, 192.8,
            203.5, 210.7
        };

        // Optional: UI-friendly string labels (one decimal, InvariantCulture).
        public static readonly string[] CanonicalLabels = new string[]
        {
            "0",
            "67.0", "71.9", "74.4", "77.0", "79.7", "82.5", "85.4",
            "88.5", "91.5", "94.8", "97.4", "100.0", "103.5", "107.2", "110.9",
            "114.8", "118.8", "123.0", "127.3", "131.8", "136.5", "141.3", "146.2",
            "151.4", "156.7", "162.2", "167.9", "173.8", "179.9", "186.2", "192.8",
            "203.5", "210.7"
        };

        // Index→tone dictionaries: fill-in as you validate actual hardware index codes.
        // These are seeded with pairs derived from your TX/RX fixtures today.
        // NOTE: Keys are the 6-bit indices produced by your bit windows (NOT array offsets).
        public static readonly System.Collections.Generic.Dictionary<int,double> TxIndexToTone
            = new System.Collections.Generic.Dictionary<int,double>
        {
            // TX fixtures (channel 15 in TX1_*): 
            // 67.0→1, 100.0→9, 114.8→11, 85.4→22, 186.2→48
            {  1,  67.0 },
            {  9, 100.0 },
            { 11, 114.8 },
            { 22,  85.4 },
            { 48, 186.2 },
            // Add more as you verify; use values only from Canonical[] above.
        };

        public static readonly System.Collections.Generic.Dictionary<int,double> RxIndexToTone_Bank0
            = new System.Collections.Generic.Dictionary<int,double>
        {
            // RX fixtures (earlier): (idx 24)→210.7, (idx 40)→67.0
            { 24, 210.7 },
            { 40,  67.0 },
            // Add more as needed; use banked key (bank<<6)|idx if you use banked maps.
        };
    }
}
