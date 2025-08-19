
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ToneLock: single source of truth for tone menus and (for now) safe Rx-tone probing.
/// This file is *standalone* and does not touch FreqLock.cs.
/// </summary>
public static class ToneLock
{
    /// <summary>
    /// Canonical 38 CTCSS tones used by many radios.
    /// Indexing rule for UI:
    ///   - "0"   : no tone
    ///   - "?"   : unknown / out of range
    ///   - 1..38 : map to CTCSS38[idx-1]
    /// </summary>
    public static readonly string[] CTCSS38 = new[]
    {
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
        "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8",
        "136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2",
        "192.8","203.5","210.7","218.1","225.7","233.6","241.8","250.3"
    };

    /// <summary>
    /// Menu for the DataGridView ComboBoxes.
    /// Always includes "0" and "?" before the ordered set of tones.
    /// </summary>
    public static readonly string[] ToneMenuAll =
        (new[] { "0", "?" }).Concat(CTCSS38).ToArray();

    /// <summary>
    /// Returns the display string for a 0..38 index. Any value outside that set -> "?".
    /// 0 -> "0" (no tone). 1..38 -> CTCSS value.
    /// </summary>
    public static string ToneFromIndex(int idx)
    {
        if (idx == 0) return "0";
        if (idx >= 1 && idx <= 38) return CTCSS38[idx - 1];
        return "?";
    }

    /// <summary>
    /// Big-endian bit helper: read a window of N bits starting at bitOffset (0 = MSB of first byte).
    /// </summary>
    private static int ReadBitsBE(byte[] bytes, int bitOffset, int bitCount)
    {
        int val = 0;
        for (int i = 0; i < bitCount; i++)
        {
            int globalBit = bitOffset + i;
            int byteIndex = globalBit / 8;
            int bitInByte = 7 - (globalBit % 8);
            int bit = (bytes[byteIndex] >> bitInByte) & 1;
            val = (val << 1) | bit;
        }
        return val;
    }

    /// <summary>
    /// EXPERIMENTAL Rx tone probe.
    /// We don't yet know the definitive packing, so this returns a dictionary
    /// of several plausible 6-bit big-endian windows across B1..B3 plus variations.
    /// Use this to compare against the DOS screen and lock the correct rule.
    /// </summary>
    public static Dictionary<string, int> ProbeRxIndexCandidates(byte B1, byte B2, byte B3)
    {
        var result = new Dictionary<string, int>();

        // Hypothesis A: low-6 bits of B2 (classic but likely wrong for this set)
        result["B2 low6"] = B2 & 0x3F;

        // Hypothesis B: B3 low5 with extend via B2 bit5 (5+1 bits)
        result["B3 low5 + B2.b5"] = (B3 & 0x1F) | ((B2 & 0x20) != 0 ? 0x20 : 0);

        // Hypothesis C: sliding 6-bit big-endian windows across B1|B2|B3
        var bytes = new[] { B1, B2, B3 };
        for (int offset = 0; offset <= 16; offset++) // 0..16 gives 17 windows of 6 bits
        {
            result[$"Win(B1..B3)@{offset}"] = ReadBitsBE(bytes, offset, 6);
        }

        // Hypothesis D: sliding across B0 isn't included here; keep this focused on B1..B3.
        return result;
    }

    /// <summary>
    /// Safe, non-destructive decode: returns "0" only if we are confident it's zero;
    /// otherwise returns "?" so the UI won't display misleading values.
    /// </summary>
    public static string DecodeRxToneSafe(byte B1, byte B2, byte B3)
    {
        // Heuristic: if *every* candidate says 0 or the bytes pattern is all-zero-ish,
        // show "0", otherwise "?" and let the user confirm.
        var c = ProbeRxIndexCandidates(B1, B2, B3);
        bool allZero = c.Values.All(v => v == 0);
        if (allZero) return "0";

        // If one obvious candidate lands in 1..38 and the rest are nonsense,
        // prefer the 6-bit window at offset 4 (this one matched Ch01 on our sample).
        int v4 = c["Win(B1..B3)@4"];
        if (v4 >= 1 && v4 <= 38)
            return ToneFromIndex(v4);

        // Otherwise punt to "?".
        return "?";
    }
}
