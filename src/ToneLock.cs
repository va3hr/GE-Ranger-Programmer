
// ======================= DO NOT EDIT =======================
// ToneLock.cs — Centralized TX/RX tone decoding for GE‑Rangr (X2212)
// Big‑endian, MSB‑first. Keeps TX/RX logic separate. Frequency code untouched.
// This file also includes minimal back‑compat shims used elsewhere in the repo.
// ============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

public static class ToneLock
{
    // Canonical CTCSS list (index 0..33). Keep order in sync with DOS menu.
    public static readonly string[] Cg = new string[]
    {
        "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
        "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
        "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
        "179.9","186.2","192.8","203.5","210.7"
    };

    // ---------- Mapping constants (MSB-first bit positions) ----------
    // NOTE: These arrays define **which bit** feeds each of the 6 index bits.
    // Order is [bit5, bit4, bit3, bit2, bit1, bit0] (bit5 = MSB of 6-bit index).
    // Each entry is a tuple: (srcByteId, msbIndex, invert)
    //   srcByteId: 0=A3,1=A2,2=A1,3=A0,4=B3,5=B2,6=B1,7=B0
    //   msbIndex : bit index 7..0 (MSB-first within the chosen byte)
    //   invert   : 0=as-is, 1=invert
    //
    // These defaults are placeholders; adjust to the **exact** known map.
    // Having them here keeps the change surface tiny and centralized.
    private static readonly (int src, int msb, int inv)[] TX_MAP = new (int,int,int)[]
    {
        (2,7,0), (2,5,0), (2,3,0),  // A1 b7,b5,b3 -> bits 5..3
        (6,7,0), (6,5,0), (6,3,0)   // B1 b7,b5,b3 -> bits 2..0
    };

    private static readonly (int src, int msb, int inv)[] RX_MAP = new (int,int,int)[]
    {
        (0,7,0), (0,5,0), (1,7,0),  // A3 b7,b5 and A2 b7
        (4,7,0), (4,5,0), (4,3,0)   // B3 b7,b5,b3
    };

    // Follow bit (applies only if RX index==0 as derived from map)
    private const int RX_FOLLOW_SRC = 4; // B3
    private const int RX_FOLLOW_MSB = 0; // B3.bit0 (MSB-first index 7..0, so bit0 == LSB)
    private const bool RX_FOLLOW_INVERT = false;

    // ---------- Public decoding API (used by MainForm/RgrCodec) ----------
    // Returns human-readable tone strings (e.g., "100.0") for TX, RX.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (string TxTone, string RxTone) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
        // Build 8-byte array for bit selection
        byte[] src = new byte[] { A3, A2, A1, A0, B3, B2, B1, B0 };

        int txIndex = GatherIndex(src, TX_MAP);
        int rxIndex = GatherIndex(src, RX_MAP);

        // Apply RX follow rule only when RX index==0.
        if (rxIndex == 0 && GetBit(src[RX_FOLLOW_SRC], RX_FOLLOW_MSB) ^ (RX_FOLLOW_INVERT ? true : false))
        {
            // Follow TX
            return (IndexToTone(txIndex), IndexToTone(txIndex));
        }
        else
        {
            return (IndexToTone(txIndex), IndexToTone(rxIndex));
        }
    }

    // ---------- Back-compat shims (legacy callers) ----------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TxToneFromBytes(byte A1, byte B1)
    {
        int idx = GatherIndex(new byte[] {0,0,A1,0,0,0,B1,0}, TX_MAP);
        return IndexToTone(idx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RxToneFromBytes(byte A3, byte B3, string txTone)
    {
        // We don't have A2 here in the legacy call; assume A2=0. If your legacy path needs A2,
        // ensure callers use DecodeChannel instead.
        int idx = GatherIndex(new byte[] {A3,0,0,0,B3,0,0,0}, RX_MAP);
        if (idx == 0 && GetBit(B3, RX_FOLLOW_MSB) ^ (RX_FOLLOW_INVERT ? true : false))
            return txTone;
        return IndexToTone(idx);
    }

    // Some callers cache the last bytes. We keep a tiny cache so older code still works.
    private static byte cA3, cA2, cA1, cA0, cB3, cB2, cB1, cB0;
    public static void SetLastChannel(byte A3, byte A2, byte A1, byte A0, byte B3, byte B2, byte B1, byte B0)
    {
        cA3=A3; cA2=A2; cA1=A1; cA0=A0; cB3=B3; cB2=B2; cB1=B1; cB0=B0;
    }

    // Optional legacy three-arg shim if anything still calls it.
    public static string RxToneFromBytes(byte A3, byte B3, bool followOn, string txTone)
    {
        int idx = GatherIndex(new byte[] {A3,0,0,0,B3,0,0,0}, RX_MAP);
        if (idx == 0 && followOn) return txTone;
        return IndexToTone(idx);
    }

    // Menus (older UI code sometimes fetches these)
    public static string[] ToneMenuTx() => Cg;
    public static string[] ToneMenuRx() => Cg;

    // Utility expected by RgrCodec in older revisions
    public static string ToAsciiHex256(byte[] bytes)
    {
        if (bytes == null) return string.Empty;
        char[] c = new char[bytes.Length*2];
        int i=0;
        foreach (var b in bytes)
        {
            var s = b.ToString("X2", CultureInfo.InvariantCulture);
            c[i++]=s[0]; c[i++]=s[1];
        }
        return new string(c);
    }

    // Utility: Convert 128 bytes to 32 "big-endian nibbles" pairs (hi/lo per byte)
    public static int[] ToX2212Nibbles(byte[] bytes128)
    {
        if (bytes128 == null || bytes128.Length != 128)
            throw new ArgumentException("Expected 128 bytes", nameof(bytes128));
        var n = new int[256];
        int j=0;
        for (int i=0;i<128;i++)
        {
            byte b = bytes128[i];
            n[j++] = (b >> 4) & 0xF; // Hi nibble (MSB-first)
            n[j++] = b & 0xF;        // Lo nibble
        }
        return n;
    }

    // ---------- Internals ----------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GatherIndex(byte[] src, (int s,int msb,int inv)[] map)
    {
        int v = 0;
        for (int i = 0; i < 6; i++)
        {
            var m = map[i];
            bool bit = GetBit(src[m.s], m.msb);
            if (m.inv!=0) bit = !bit;
            v = (v << 1) | (bit ? 1 : 0);
        }
        // Clamp to range
        if (v < 0) v = 0;
        if (v >= Cg.Length) v = 0;
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool GetBit(byte b, int msbIndex)
    {
        // MSB-first indexing: msbIndex 7..0
        if ((uint)msbIndex > 7) throw new ArgumentOutOfRangeException(nameof(msbIndex));
        int lsbIndex = 7 - msbIndex;
        return ((b >> lsbIndex) & 1) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string IndexToTone(int idx)
    {
        if ((uint)idx >= (uint)Cg.Length) return "0";
        return Cg[idx];
    }
}
