// ToneLock.cs — unified tone menus + helpers
// Namespace intentionally set to RangrApp.Locked to match MainForm usage.
using System;
using System.Collections.Generic;
using System.Linq;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== TX tone menu (service-manual order). "0" means no tone.
        public static readonly string[] TxToneMenu = new[] {
            "0",
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2",
            "110.9","114.8","118.8","123.0","127.3","131.8","136.5",
            "141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // ===== RX tone menu (derived A3/B3 mapping). "0" means no tone.
        // Index is the 6‑bit value we decode from A3/B3; entry is display text.
        // Fill with 64 entries so out-of-range indexing never throws.
        public static readonly string[] RxToneMenu;

        static ToneLock()
        {
            // Build RX map table from project memory values (hard-coded snapshot).
            var rx = new List<string>(new string[64]);
            // initialize all as "?" and set 0 to "0"
            for (int i = 0; i < rx.Count; i++) rx[i] = "?";
            rx[0] = "0";

            // ==== Derived mapping from RXMAP_*.RGR sets (full 0..63 coverage)
            // NOTE: this is the snapshot used during our tests; adjust only if the RXMAP
            // evidence changes. We keep explicit assigns for clarity.
            // Low block (A3<5:0> = 0..31):
            string[] low = {
                "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4",
                "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
                "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
                "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8"
            };
            for (int i = 0; i < low.Length; i++) rx[i] = low[i];

            // High block (A3<5:0>=32..63) — banked by B3 bits (follow/cct)
            string[] high = {
                "203.5","210.7","67.0","71.9","74.4","77.0","79.7","82.5",
                "85.4","88.5","91.5","94.8","97.4","100.0","103.5","107.2",
                "110.9","114.8","118.8","123.0","127.3","131.8","136.5","141.3",
                "146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2"
            };
            for (int i = 0; i < high.Length; i++) rx[32+i] = high[i];

            RxToneMenu = rx.ToArray();
        }

        // ---- Menus for the UI:
        public static IEnumerable<string> ToneMenuAll => TxToneMenu; // UI uses same list for drop-downs

        // ---- TX helpers (index ↔ display). 0 means "no tone"; "?" never returned here.
        public static string TxIndexToDisplay(int idx)
        {
            if (idx < 0 || idx >= TxToneMenu.Length) return "?";
            return TxToneMenu[idx];
        }
        public static bool TryTxDisplayToIndex(string display, out int idx)
        {
            idx = Array.IndexOf(TxToneMenu, display);
            return idx >= 0;
        }

        // ---- RX helpers. Overloads retained for compatibility.
        public static string RxIndexToDisplay(int idx) => (idx >= 0 && idx < RxToneMenu.Length) ? RxToneMenu[idx] : "?";
        public static string RxIndexToDisplay(int idx, bool _ignored) => RxIndexToDisplay(idx); // compat 2-arg form

        public static bool TryRxDisplayToIndex(string display, out int idx)
        {
            idx = Array.IndexOf(RxToneMenu, display);
            return idx >= 0;
        }

        // ===== Bit-field helpers for raw bytes (A3/B3 register quirks)
        // A3: six-bit index 0..63 (we store and extract only <5:0>)
        public static int RxIndexFromA3(byte a3) => a3 & 0b0011_1111;

        // B3: [7]=follow flag, [6:4]=bank (0..7) used historically for “CCT” selection
        public static int RxBankFromB3(byte b3) => (b3 >> 4) & 0b0000_0111;
        public static bool RxFollowBit(byte b3) => (b3 & 0b1000_0000) != 0;
        public static byte ComposeB3(int bank, bool follow, byte b3Original = 0)
        {
            byte b = (byte)(b3Original & 0x0F);
            b |= (byte)((Math.Clamp(bank, 0, 7) & 0x7) << 4);
            if (follow) b |= 0x80;
            return b;
        }
        public static byte ComposeA3(int rxIndex, byte a3Original = 0)
        {
            byte a = (byte)(a3Original & 0b1100_0000);
            a |= (byte)(Math.Clamp(rxIndex, 0, 63) & 0b0011_1111);
            return a;
        }
    }
}
