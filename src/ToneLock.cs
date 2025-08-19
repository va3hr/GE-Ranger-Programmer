using System;
using System.Collections.Generic;
using System.Globalization;

public static class ToneLock
{
    // Canonical menu: slot 0="0" (none), slot 1="?" (unknown), then standard CTCSS.
    public static readonly string[] ToneMenuAll = new[]{
        "0","?",
        "67.0","69.3","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5",
        "94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0",
        "127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9",
        "173.8","179.9","186.2","192.8","203.5","210.7"
    };

    // 5-bit extractor (LSB→MSB), each tuple is (byteValue, bitIndex0..7)
    private static int Extract5((byte b, int bit) a0, (byte b, int bit) a1, (byte b, int bit) a2, (byte b, int bit) a3, (byte b, int bit) a4)
    {
        int v = 0;
        v |= ((a0.b >> a0.bit) & 1) << 0;
        v |= ((a1.b >> a1.bit) & 1) << 1;
        v |= ((a2.b >> a2.bit) & 1) << 2;
        v |= ((a3.b >> a3.bit) & 1) << 3;
        v |= ((a4.b >> a4.bit) & 1) << 4;
        return v & 0x1F;
    }

    // Map a 5-bit code to a tone string.
    // We keep this simple: code 0 => "0"; code 1..31 => map into the standard list (offset +1).
    private static string CodeToTone(int code)
    {
        if (code <= 0) return "0";
        int idx = code + 1; // account for slot 0="0"
        return (idx >= 0 && idx < ToneMenuAll.Length) ? ToneMenuAll[idx] : "?";
    }

    // ---------- TX (LOCKED) ----------
    // Bits: { A2.b0, A2.b1, A2.b7, B3.b1, B3.b2 }  (LSB→MSB)
    public static string DecodeTxTone(byte A2, byte B3)
    {
        int code = Extract5(
            (A2, 0),
            (A2, 1),
            (A2, 7),
            (B3, 1),
            (B3, 2)
        );
        return CodeToTone(code);
    }

    // ---------- RX (choose best of proven windows) ----------
    // Candidates you can test against your DOS screen. Once confirmed, we’ll freeze the winner.
    private static string RxFromLow5(byte B2, byte B3)
    {
        int code = B2 & 0x1F;
        return CodeToTone(code);
    }
    // Mirror of TX idea, but on B2+ B3
    private static string RxFromMirrorB7(byte B2, byte B3)
    {
        int code = Extract5(
            (B2, 0),
            (B2, 1),
            (B2, 7),
            (B3, 1),
            (B3, 2)
        );
        return CodeToTone(code);
    }
    // Variant using B2.b5 (seen on some images)
    private static string RxFromMirrorB5(byte B2, byte B3)
    {
        int code = Extract5(
            (B2, 0),
            (B2, 1),
            (B2, 5),
            (B3, 1),
            (B3, 2)
        );
        return CodeToTone(code);
    }

    // Pick the most plausible tone result:
    //  - If B2.low5 == 0 → "0" (no tone)
    //  - Prefer a value that is not "?" and not obviously out-of-set
    //  - Fallback to "?" if nothing looks valid
    public static string DecodeRxTone(byte B2, byte B3)
    {
        if ((B2 & 0x1F) == 0) return "0";

        string[] cands = {
            RxFromMirrorB7(B2,B3),
            RxFromMirrorB5(B2,B3),
            RxFromLow5(B2,B3)
        };
        foreach (var t in cands)
        {
            if (!string.IsNullOrEmpty(t) && t != "?") return t;
        }
        return "?";
    }

    // Optional: emit candidate codes/tones per channel to the app log for fast validation.
    public static void DebugDumpCandidates(byte[] logical128, Action<string> log)
    {
        if (logical128 == null || logical128.Length < 128 || log == null) return;
        log("RX decode candidates (per channel): low5 | mirB7 | mirB5");
        for (int ch = 0; ch < 16; ch++)
        {
            int i = ch * 8;
            byte B2 = logical128[i + 6];
            byte B3 = logical128[i + 7];
            string s = $"CH{(ch+1):D2}: {RxFromLow5(B2,B3),6} | {RxFromMirrorB7(B2,B3),6} | {RxFromMirrorB5(B2,B3),6}";
            log(s);
        }
    }
}
