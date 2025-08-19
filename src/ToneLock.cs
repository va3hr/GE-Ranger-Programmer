using System;
using System.Linq;
using System.Reflection;

public static class ToneLock
{
    // Canonical tone menu used for both decode and UI.
    // Index 0 = "0" (NONE), 1 = "?" (unknown), then standard CTCSS tones.
    public static readonly string[] ToneMenuAll = new[] {
        "0","?",
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5",
        "94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0",
        "127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9",
        "173.8","179.9","186.2","192.8","203.5","210.7"
    };

    // Decode TX tone. Prefer a project-provided method if available; fall back to simple rule.
    public static string DecodeTxTone(byte A2, byte B3)
    {
        try
        {
            // If your project defines ToneAndFreq.TxToneMenuValue(A2,B3), use it.
            var t = Type.GetType("ToneAndFreq");
            var m = t?.GetMethod("TxToneMenuValue", BindingFlags.Public | BindingFlags.Static);
            if (m != null)
            {
                var val = m.Invoke(null, new object[] { A2, B3 }) as string;
                if (string.IsNullOrWhiteSpace(val)) return "0";
                return ToneMenuAll.Contains(val) ? val : "?";
            }
        }
        catch { /* ignore and fall back */ }

        // Fallback: if low 5 bits of B3 are zero -> "0", otherwise unknown for now.
        int low5 = B3 & 0x1F;
        return low5 == 0 ? "0" : "?";
    }

    // Decode RX tone. Current working rule: use low-5 bits of B2 as an index into menu.
    // We keep this here so we can refine the mapping without touching UI code.
    public static string DecodeRxTone(byte A2, byte B2, byte B3)
    {
        int idx = B2 & 0x1F;
        if (idx >= 0 && idx < ToneMenuAll.Length) return ToneMenuAll[idx];
        return "?";
    }

    // Optional: dump bit fields to help reverse-engineer mapping against DOS screenshots.
    public static string ExplainBits(byte A2, byte B2, byte B3)
    {
        static string b(byte x) => Convert.ToString(x, 2).PadLeft(8, '0');
        return $"A2={b(A2)}  B2={b(B2)}  B3={b(B3)}  rxIdx(B2.low5)={(B2 & 0x1F)}";
    }
}
