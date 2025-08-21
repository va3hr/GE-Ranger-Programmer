// ToneLock.cs — GE Rangr (RANGR6M family) tone encode/decode helpers
// .NET 8 / WinForms compatible. Self-contained (no frequency logic touched).
//
// Summary of what this implements (based on our reverse‑engineering sessions):
// • Image = 16 channels × 8 bytes/channel = 128 bytes total. Each channel has A0..A3, B0..B3.
// • Storage is nibble‑big‑endian (high nibble first when streaming to a 4‑bit device like X2212).
// • File block order for this “RANGR6M2” personality is NOT linear. Use ScreenToFileBlock[] below.
// • RX tone index: 6 bits spread in A3 in this bit order (bit5..bit0) = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3].
// • RX bank selector: B3.bit1 (0/1). Index 0 displays as "0" (no tone).
// • RX "follow‑TX when idx==0": a small right‑side flag. We expose it as a configurable bit
//   (FollowTxBit setting) so you can flip it instantly once pinned on your unit.
// • TX tone (gold CG table only for TX): key = (B0.bit4, B2.bit2, B3&0x7F) with B3.bit7 = present.
//   We include a few known key->index pairs and allow registering more without changing call sites.
//
// API surface:
//   ToneLock.DecodeTx(byte[] img, int ch) -> (index?, text)
//   ToneLock.TrySetTxTone(byte[] img, int ch, int cgIndex, (int,int,int)? preferredKey=null) -> bool
//   ToneLock.DecodeRx(byte[] img, int ch) -> (idx, bank, followTx, text)
//   ToneLock.SetRxTone(byte[] img, int ch, int idx, int bank, bool? followTx=null)
//   ToneLock.CloneImage, ToneLock.ToX2212Nibbles, ToneLock.FromX2212Nibbles, ToneLock.WriteRgr
//
// Notes:
// • This module only touches tone bits. Leave frequency code in FreqLock.cs (frozen) untouched.
// • If your unit uses a slightly different TX keying, just RegisterTxKey() the new mapping.
// • RX tone strings come from per‑bank maps. Unknown entries show "?" per our UI policy.
// • Follow‑TX shows the TX tone when idx==0 and the follow flag bit is set.
//
// Peter—drop this into your project as ToneLock.cs and call from MainForm.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // ---------------------------- Constants & Tables ----------------------------

        // Gold CG table for TX only (index 0..33)
        public static readonly string[] CgTx = {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8","97.4",
            "100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // Screen channel (1..16) -> file block (1..16) for this RANGR6M2 family.
        // Index 0 is unused to keep 1-based addressing readable.
        private static readonly int[] ScreenToFileBlock =
            { 0, 7,3,1,4,2,5,6,8,15,9,10,12,14,11,13,16 };

        // RX index bit order in A3: bit5..bit0 => A3.6, A3.7, A3.0, A3.1, A3.2, A3.3
        private static byte SetRxIndexA3(byte a3, int idx6)
        {
            idx6 &= 0x3F;
            int b5=(idx6>>5)&1, b4=(idx6>>4)&1, b3=(idx6>>3)&1, b2=(idx6>>2)&1, b1=(idx6>>1)&1, b0=(idx6>>0)&1;
            a3 = (byte)((a3 & ~(1<<6)) | (b5<<6));
            a3 = (byte)((a3 & ~(1<<7)) | (b4<<7));
            a3 = (byte)((a3 & ~(1<<0)) | (b3<<0));
            a3 = (byte)((a3 & ~(1<<1)) | (b2<<1));
            a3 = (byte)((a3 & ~(1<<2)) | (b1<<2));
            a3 = (byte)((a3 & ~(1<<3)) | (b0<<3));
            return a3;
        }
        private static int GetRxIndexA3(byte a3)
        {
            int b5=(a3>>6)&1, b4=(a3>>7)&1, b3=(a3>>0)&1, b2=(a3>>1)&1, b1=(a3>>2)&1, b0=(a3>>3)&1;
            return (b5<<5) | (b4<<4) | (b3<<3) | (b2<<2) | (b1<<1) | (b0<<0);
        }

        // B3 helpers
        private static byte SetRxBankB3(byte b3, int bank01) => (byte)((b3 & ~(1<<1)) | ((bank01 & 1)<<1));
        private static int  GetRxBankB3(byte b3) => (b3>>1) & 1;
        private static byte SetTxPresentB3(byte b3, bool present) => present ? (byte)(b3 | 0x80) : (byte)(b3 & 0x7F);
        private static bool GetTxPresentB3(byte b3) => (b3 & 0x80) != 0;

        // ---- Configurable "follow-TX when idx==0" flag bit (to be pinned on your unit).
        // Default guess: B3.bit0 (set => follow-TX). Change here once you confirm.
        public static (int byteIndex, int bit) FollowTxBit = (7, 0); // 4:B0, 5:B1, 6:B2, 7:B3

        private static bool GetFollowTx(byte[] image128, int off)
        {
            var (bi, bit) = FollowTxBit;
            byte val = image128[off + bi - 4]; // off points at A0; A0..A3 (0..3), B0..B3 (4..7)
            return ((val >> bit) & 1) == 1;
        }
        private static void SetFollowTx(byte[] image128, int off, bool on)
        {
            var (bi, bit) = FollowTxBit;
            ref byte val = ref image128[off + bi - 4];
            if (on) val = (byte)(val |  (1<<bit));
            else    val = (byte)(val & ~(1<<bit));
        }

        // ---------------------------- RX tone maps ----------------------------
        // These are per‑bank lists (index 0..63). Unknown entries are "?" per UI policy.
        // Index 0 is always "0". Fill in more as you map them; these cover the indices
        // we’ve actually seen on your RANGR6M2 image.
        public static readonly string[] RxBank0 = new string[64]; // bank=0
        public static readonly string[] RxBank1 = new string[64]; // bank=1

        static ToneLock()
        {
            // Defaults
            for (int i = 0; i < 64; i++) { RxBank0[i] = "?"; RxBank1[i] = "?"; }
            RxBank0[0] = "0"; RxBank1[0] = "0";

            // Observed on your unit / image family:
            // bank 0
            RxBank0[ 3] = "107.2";
            RxBank0[35] = "127.3";
            RxBank0[63] = "114.8";

            // bank 1
            RxBank1[21] = "131.8";
            RxBank1[63] = "162.2";
        }

        private static string RxToneText(int idx, int bank, string fallbackWhenZero)
        {
            if (idx == 0) return fallbackWhenZero; // "0" or (when follow is active) the TX tone
            var arr = bank == 0 ? RxBank0 : RxBank1;
            return (idx >= 0 && idx < 64 && arr[idx] != null) ? arr[idx] : "?";
        }

        // ---------------------------- TX keyed mapping ----------------------------

        // Key = (B0.bit4, B2.bit2, B3 & 0x7F)
        private static (int,int,int) GetTxKey(byte b0, byte b2, byte b3) =>
            ((b0>>4)&1, (b2>>2)&1, b3 & 0x7F);

        // Known key->index pairs for this personality. You can register more at runtime.
        private static readonly Dictionary<(int,int,int), int> TxKeyToIndex = new()
        {
            // From your TX sanity images + probes
            {(0,0,0x4B), 13}, // 103.5
            {(0,0,0x4F), 17}, // 114.8
            {(0,1,0x6C), 19}, // 127.3
            {(0,0,0x2A), 20}, // 131.8
            {(0,0,0x3A), 20}, // 131.8 (factory alt)
            {(0,1,0x7D), 20}, // 131.8 (alt with b2b2=1)
            {(0,1,0x39), 24}, // 162.2 (variant observed)
            {(0,1,0x28), 13}, // 103.5 (alt)
        };

        // Preferred index->key mapping used when writing (you can override per call).
        private static readonly Dictionary<int,(int,int,int)> TxIndexToKey = new()
        {
            {13,(0,0,0x4B)},  // 103.5
            {17,(0,0,0x4F)},  // 114.8
            {19,(0,1,0x6C)},  // 127.3
            {20,(0,0,0x2A)},  // 131.8 (default; you can choose (0,0,0x3A) or (0,1,0x7D))
            {24,(0,1,0x39)},  // 162.2
        };

        public static void RegisterTxKey(int cgIndex, (int b0b4,int b2b2,int b3lo7) key, bool preferForWrite=true)
        {
            TxKeyToIndex[key] = cgIndex;
            if (preferForWrite) TxIndexToKey[cgIndex] = key;
        }

        // ---------------------------- Public Decode API ----------------------------

        /// <summary>Decode TX (index & text) for screen channel 1..16.</summary>
        public static (int? cgIndex, string text) DecodeTx(byte[] image128, int screenCh1to16)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk * 8;
            byte b0 = image128[off+4], b2 = image128[off+6], b3 = image128[off+7];

            if (!GetTxPresentB3(b3)) return (0, "0");
            var key = GetTxKey(b0,b2,b3);
            if (TxKeyToIndex.TryGetValue(key, out int idx))
                return (idx, idx >= 0 && idx < CgTx.Length ? CgTx[idx] : "?");
            return (null, "?");
        }

        /// <summary>Decode RX (idx, bank, follow, text) for screen channel 1..16.</summary>
        public static (int idx, int bank, bool followTx, string text) DecodeRx(byte[] image128, int screenCh1to16)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk * 8;
            byte a3 = image128[off+3], b3 = image128[off+7];
            int idx = GetRxIndexA3(a3);
            int bank = GetRxBankB3(b3);

            // Follow-TX behavior when idx==0
            bool follow = false;
            if (idx == 0)
            {
                follow = GetFollowTx(image128, off);
                if (follow)
                {
                    var (txIdx, txText) = DecodeTx(image128, screenCh1to16);
                    return (idx, bank, true, txText ?? "0");
                }
            }
            string text = RxToneText(idx, bank, "0");
            return (idx, bank, follow, text);
        }

        // ---------------------------- Public Encode API ----------------------------

        /// <summary>
        /// Set the TX tone by CG index (0=no tone). Optionally pass a concrete key variant.
        /// Only tone bits are modified.
        /// </summary>
        public static bool TrySetTxTone(byte[] image128, int screenCh1to16, int cgIndex, (int b0b4,int b2b2,int b3lo7)? preferredKey = null)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk * 8;
            ref byte B0 = ref image128[off+4];
            ref byte B2 = ref image128[off+6];
            ref byte B3 = ref image128[off+7];

            if (cgIndex == 0)
            {
                B3 = SetTxPresentB3(B3, false); // off
                return true;
            }

            (int b0b4,int b2b2,int b3lo7) key;
            if (preferredKey.HasValue) key = preferredKey.Value;
            else if (!TxIndexToKey.TryGetValue(cgIndex, out key))
                return false; // unknown for this personality

            // Apply
            B3 = SetTxPresentB3(B3, true);
            B3 = (byte)((B3 & 0x80) | (key.b3lo7 & 0x7F));
            B0 = (byte)((B0 & ~(1<<4)) | ((key.b0b4 & 1) << 4));
            B2 = (byte)((B2 & ~(1<<2)) | ((key.b2b2 & 1) << 2));
            return true;
        }

        /// <summary>Set RX index (0..63), bank (0/1), and optional follow‑TX flag when idx==0.</summary>
        public static void SetRxTone(byte[] image128, int screenCh1to16, int rxIndex0to63, int bank0or1, bool? followTx = null)
        {
            int blk = ScreenToFileBlock[screenCh1to16] - 1;
            int off = blk * 8;
            ref byte A3 = ref image128[off+3];
            ref byte B3 = ref image128[off+7];

            A3 = SetRxIndexA3(A3, rxIndex0to63);
            B3 = SetRxBankB3(B3, bank0or1);

            if (followTx.HasValue && rxIndex0to63 == 0)
                SetFollowTx(image128, off, followTx.Value);
        }

        // ---------------------------- Utilities ----------------------------

        public static byte[] CloneImage(byte[] image128)
        {
            var b = new byte[128];
            Buffer.BlockCopy(image128, 0, b, 0, 128);
            return b;
        }

        /// <summary>Convert 128-byte image to 256 nibble values (high nibble first) for an X2212.</summary>
        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            var nibbles = new byte[256];
            int p=0;
            for (int i=0;i<128;i++)
            {
                byte b = image128[i];
                nibbles[p++] = (byte)((b>>4)&0xF);
                nibbles[p++] = (byte)(b & 0xF);
            }
            return nibbles;
        }

        /// <summary>Rebuild the 128-byte image from 256 nibbles (high nibble first).</summary>
        public static byte[] FromX2212Nibbles(byte[] nibbles256)
        {
            if (nibbles256 == null || nibbles256.Length != 256) throw new ArgumentException("Need 256 nibbles");
            var b = new byte[128];
            int p=0;
            for (int i=0;i<128;i++)
            {
                int hi = nibbles256[p++] & 0xF;
                int lo = nibbles256[p++] & 0xF;
                b[i] = (byte)((hi<<4) | lo);
            }
            return b;
        }

        public static void WriteRgr(string path, byte[] image128) => File.WriteAllBytes(path, image128);
    }
}
