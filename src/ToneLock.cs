#nullable enable
// ToneLock_patched.cs — conservative decode to prevent wrong tones from appearing.
// Namespace and method names match existing MainForm.cs usage.

using System;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ===== Canonical CTCSS menu (index 0 == "0") =====
        public static readonly string[] ToneMenuTx = new string[]
        {
            "0",
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8",
            "203.5","210.7"
        };

        public static readonly string[] ToneMenuRx = ToneMenuTx; // same display list for UI

        // Back-compat menus used by some binding code
        public static readonly string[] ToneMenuAll = ToneMenuTx;

        // ===== TX decode (B0,B2,B3) =====
        // B3.bit7 is "present". If not set => "0". If set but we don't have a verified map => "?".
        public static string TxToneFromBytes(byte B0, byte B2, byte B3)
        {
            bool present = (B3 & 0x80) != 0;
            if (!present) return "0";
            // We used to guess from partial pairs; that produced WRONG values.
            // Until the verified triple map is reattached, return "?" instead of an incorrect tone.
            return "?";
        }

        // ===== RX decode (A3,B3,txToneIfFollow) =====
        // six-bit index in A3<5:0>; bank = B3.bit1; follow = B3.bit0.
        public static string RxToneFromBytes(byte A3, byte B3, string txToneIfFollow)
        {
            int idx = A3 & 0x3F;
            bool follow = (B3 & 0x01) != 0;
            if (idx == 0) return follow ? (string.IsNullOrEmpty(txToneIfFollow) ? "0" : txToneIfFollow) : "0";

            // We will plug in the full derived (idx,bank) → tone mapping next.
            // For now, to avoid misleading output, show "?" for non-zero indices.
            return "?";
        }

        // ===== Helper shims kept for older call-sites =====
        // Some older code calls 2-arg forms; keep them delegating to the verified forms above.
        public static string TxToneFromBytes(byte A1, byte B1) => "?"; // deprecated pair; avoid wrong values
        public static string RxToneFromBytes(byte A3, byte A2, byte B3) => RxToneFromBytes(A3, B3, "0");

        // Menu helpers (if needed by UI)
        public static string TxIndexToDisplay(int index) => (index >= 0 && index < ToneMenuTx.Length) ? ToneMenuTx[index] : "?";
        public static string RxIndexToDisplay(int index) => (index >= 0 && index < ToneMenuRx.Length) ? ToneMenuRx[index] : "?";
    }
}
