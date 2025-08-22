using System;
using System.Collections.Generic;

namespace GE_Ranger_Programmer
{
    /// <summary>
    /// Central place for tone menus and simple helpers.
    /// NOTE: This file only provides data/menus to fix build errors.
    /// Mapping/codec logic stays in RgrCodec.
    /// </summary>
    public static class ToneLock
    {
        // Display string used for 'no tone' in the UI
        public const string NoneDisplay = "0";
        public const string UnknownDisplay = "?";

        /// <summary>
        /// Complete tone menu used by both Tx/Rx dropdowns.
        /// First entry is '0' (no tone), then the standard EIA tones.
        /// </summary>
        public static readonly string[] ToneMenuAll = new[]
        {
            NoneDisplay,
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2",
            "110.9","114.8","118.8","123.0","127.3","131.8","136.5",
            "141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // Separate arrays in case you want to bind Tx/Rx independently later.
        public static IReadOnlyList<string> ToneMenuTx => ToneMenuAll;
        public static IReadOnlyList<string> ToneMenuRx => ToneMenuAll;

        /// <summary>
        /// Helper to safely format an index into ToneMenuAll. Returns "?" if out of range.
        /// </summary>
        public static string SafeIndexToDisplay(int index)
        {
            if (index < 0 || index >= ToneMenuAll.Length) return UnknownDisplay;
            return ToneMenuAll[index];
        }
    }
}
