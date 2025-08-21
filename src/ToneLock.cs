// ToneLock.cs — drop‑in module for GE‑Ranger‑Programmer (WinForms, .NET 8, x64)
// PURPOSE: All tone manipulation lives here. Frequency math remains in FreqLock.cs (frozen).
// STATUS: Implements Tx (keyed by right‑side bytes) and Rx (banked index from A3),
//         plus safe setters and registration helpers. Unknowns display "?".
// POLICY:
//   • Tx present flag:           B3.bit7 == 1 when Tx ≠ 0
//   • Tx key (selector fields):  key = (B0.bit4, B2.bit2, (B3 & 0x7F))  // ignore bit7 in key
//   • Tx tones:                  key → CG index (0‑based) → classic CG tone list (TxCgList)
//   • Rx index (6‑bit):          from A3 in bit order [6,7,0,1,2,3]  (MSB→LSB)
//   • Rx bank selector:          B3.bit1  (0 or 1)
//   • Rx idx 0:                  display "0" (no tone). Follow‑Tx flag TBD (default B3.bit0).
//   • Display:                   Unmapped key/index → "?" (never guess)
//
// GUARANTEES:
//   • This file does not touch frequency bits or math.
//   • Public surface is small and stable for UI + file parser callers.
//
// REV: 2025‑08‑21 (matches current project memory)

using System;
using System.Collections.Generic;
using System.Linq;

namespace GE.Ranger.Programmer.Core
{
    internal static class ToneLock
    {
        // ==========================
        //  Public API (stable)
        // ==========================

        /// <summary>
        /// Decode Tx tone from right‑side bytes. Returns true if decode ran; unknown keys yield tone="?" and cgIndex=-1.
        /// When Tx present flag is 0, tone="0" and cgIndex=0.
        /// </summary>
        public static bool TryDecodeTx(byte B0, byte B2, byte B3, out int cgIndex, out string tone, out string keyString)
        {
            bool present = (B3 & 0x80) != 0;
            if (!present)
            {
                cgIndex = 0; tone = "0"; keyString = "(—)"; return true;
            }

            int b0b4 = (B0 >> 4) & 0x1;
            int b2b2 = (B2 >> 2) & 0x1;
            int b3lo7 = B3 & 0x7F; // ignore bit7 in key
            int key = MakeTxKey(b0b4, b2b2, b3lo7);
            keyString = TxKeyToString(key);

            if (TxKeyToCgIndex.TryGetValue(key, out cgIndex))
            {
                tone = TxCgList.ElementAtOrDefault(cgIndex) ?? "?";
            }
            else
            {
                cgIndex = -1; tone = "?";
            }
            return true;
        }

        /// <summary>
        /// Decode Rx tone from A3 (index) and B3 (bank). Returns (bank, idx, tone). idx==0 → tone="0".
        /// Unknown (bank,idx) yields tone="?".
        /// </summary>
        public static (int bank, int idx, string tone) DecodeRx(byte A3, byte B3)
        {
            int bank = (B3 >> 1) & 0x1;
            int idx = ComposeRxIndexFromA3(A3);
            if (idx == 0)
            {
                return (bank, 0, "0");
            }
            if (RxBankedIndexToTone.TryGetValue((bank, idx), out var t))
            {
                return (bank, idx, t);
            }
            return (bank, idx, "?");
        }

        /// <summary>
        /// Safely set Tx tone. cgIndex==0 clears present flag (Tx=0). For cgIndex>0, requires a known key mapping;
        /// returns false if no (key) exists yet for the given cgIndex. This does NOT alter unrelated bits.
        /// </summary>
        public static bool TrySetTxTone(ref byte B0, ref byte B2, ref byte B3, int cgIndex)
        {
            if (cgIndex <= 0)
            {
                // Tx = 0: clear present flag and leave other bits unchanged
                B3 = (byte)(B3 & 0x7F);
                return true;
            }

            if (!TxIndexToKey.TryGetValue(cgIndex, out var key))
            {
                // No known key for this index yet.
                return false;
            }

            int b0b4 = ExtractB0b4FromKey(key);
            int b2b2 = ExtractB2b2FromKey(key);
            int b3lo7 = ExtractB3Lo7FromKey(key);

            // Set bits accordingly; preserve other bits
            SetBit(ref B0, 4, b0b4);
            SetBit(ref B2, 2, b2b2);
            B3 = (byte)((B3 & 0x80) | b3lo7); // overwrite low 7 bits, preserve bit7 for now
            B3 |= 0x80; // ensure present flag
            return true;
        }

        /// <summary>
        /// Safely set Rx index and bank. idx is 0..63 (0 means "0"). Bank is 0/1. Optionally set/clear a follow‑Tx flag
        /// (default candidate bit = B3.bit0). Unknown follow‑Tx policy → leave as default.
        /// </summary>
        public static void SetRxIndex(ref byte A3, ref byte B3, int bank, int idx, bool? followTx = null, byte followFlagMask = 0x01)
        {
            if (idx < 0 || idx > 63) throw new ArgumentOutOfRangeException(nameof(idx));
            if (bank != 0 && bank != 1) throw new ArgumentOutOfRangeException(nameof(bank));

            // Write bank
            SetBit(ref B3, 1, bank);

            // Write A3 per [6,7,0,1,2,3]
            // b5→A3.bit6, b4→A3.bit7, b3→A3.bit0, b2→A3.bit1, b1→A3.bit2, b0→A3.bit3
            int b5 = (idx >> 5) & 1;
            int b4 = (idx >> 4) & 1;
            int b3 = (idx >> 3) & 1;
            int b2 = (idx >> 2) & 1;
            int b1 = (idx >> 1) & 1;
            int b0 = (idx >> 0) & 1;

            SetBit(ref A3, 6, b5);
            SetBit(ref A3, 7, b4);
            SetBit(ref A3, 0, b3);
            SetBit(ref A3, 1, b2);
            SetBit(ref A3, 2, b1);
            SetBit(ref A3, 3, b0);

            // Optional follow‑Tx flag handling when idx==0
            if (followTx.HasValue)
            {
                if (idx == 0)
                {
                    if (followTx.Value) B3 |= followFlagMask; else B3 = (byte)(B3 & ~followFlagMask);
                }
                else
                {
                    // If an idx is non‑zero, clear follow flag to be safe.
                    B3 = (byte)(B3 & ~followFlagMask);
                }
            }
        }

        /// <summary>
        /// Register a new Tx key→CG index mapping at runtime (e.g., from TXMAP files).
        /// Also registers a default reverse mapping for TrySetTxTone if none exists.
        /// </summary>
        public static void RegisterTxKey(int b0b4, int b2b2, int b3lo7, int cgIndex)
        {
            int key = MakeTxKey(b0b4, b2b2, b3lo7);
            TxKeyToCgIndex[key] = cgIndex;
            if (!TxIndexToKey.ContainsKey(cgIndex))
            {
                TxIndexToKey[cgIndex] = key; // first key wins for write‑back
            }
        }

        // ==========================
        //  Tables & helpers
        // ==========================

        // Classic CG tone list for Tx (0‑based). Index 0 = "0"
        public static readonly string[] TxCgList = new string[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // PARTIAL map: (b0b4,b2b2,b3&0x7F) → CG index (0‑based). Extend as you discover keys.
        public static readonly Dictionary<int, int> TxKeyToCgIndex = new()
        {
            // Proven pairs from project memory:
            // (0,1,0x53)→idx2 (71.9), (0,1,0x47)→idx8 (88.5), (0,0,0x1D)→idx21 (136.5), (1,1,0x01)→idx33 (210.7)
            // (0,0,0x4B)→idx16 (114.8), (0,0,0x49)→idx12 (100.0), (1,0,0x51)→idx32 (203.5)
            { MakeTxKey(0,1,0x53),  2 },
            { MakeTxKey(0,1,0x47),  8 },
            { MakeTxKey(0,0,0x1D), 21 },
            { MakeTxKey(1,1,0x01), 33 },
            { MakeTxKey(0,0,0x4B), 16 },
            { MakeTxKey(0,0,0x49), 12 },
            { MakeTxKey(1,0,0x51), 32 },
        };

        // Reverse map used by TrySetTxTone. First registered key for an index is kept (write‑back is personality‑specific).
        public static readonly Dictionary<int, int> TxIndexToKey = new()
        {
            {  2, MakeTxKey(0,1,0x53) },
            {  8, MakeTxKey(0,1,0x47) },
            { 12, MakeTxKey(0,0,0x49) },
            { 16, MakeTxKey(0,0,0x4B) },
            { 21, MakeTxKey(0,0,0x1D) },
            { 32, MakeTxKey(1,0,0x51) },
            { 33, MakeTxKey(1,1,0x01) },
        };

        // Banked Rx index→tone (partial; extend with RXMAP files). Index 0 handled in DecodeRx.
        public static readonly Dictionary<(int bank, int idx), string> RxBankedIndexToTone = new()
        {
            // Known RANGR6M2 facts so far:
            { (0, 63), "114.8" },  // idx63 bank0 → 114.8
            { (1, 63), "162.2" },  // idx63 bank1 → 162.2
            { (1, 21), "131.8" },  // bank1: idx21 → 131.8
            { (0, 35), "127.3" },  // bank0: idx35 → 127.3
            { (0,  3), "107.2" },  // bank0: idx3  → 107.2
        };

        // ===== helpers =====
        private static int MakeTxKey(int b0b4, int b2b2, int b3lo7)
            => ((b0b4 & 1) << 9) | ((b2b2 & 1) << 8) | (b3lo7 & 0x7F);

        private static string TxKeyToString(int key)
        {
            int b0b4 = (key >> 9) & 1; int b2b2 = (key >> 8) & 1; int b3 = key & 0x7F;
            return $"({b0b4},{b2b2},0x{b3:X2})";
        }

        private static int ExtractB0b4FromKey(int key) => (key >> 9) & 1;
        private static int ExtractB2b2FromKey(int key) => (key >> 8) & 1;
        private static int ExtractB3Lo7FromKey(int key) => key & 0x7F;

        private static int ComposeRxIndexFromA3(byte A3)
        {
            // bits: [6,7,0,1,2,3] → b5..b0
            int b5 = (A3 >> 6) & 1;
            int b4 = (A3 >> 7) & 1;
            int b3 = (A3 >> 0) & 1;
            int b2 = (A3 >> 1) & 1;
            int b1 = (A3 >> 2) & 1;
            int b0 = (A3 >> 3) & 1;
            return (b5 << 5) | (b4 << 4) | (b3 << 3) | (b2 << 2) | (b1 << 1) | (b0 << 0);
        }

        private static void SetBit(ref byte b, int bit, int value)
        {
            if (value == 0) b = (byte)(b & ~(1 << bit)); else b = (byte)(b | (1 << bit));
        }
    }
}