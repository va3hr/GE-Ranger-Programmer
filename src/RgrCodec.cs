using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GE_Ranger_Programmer
{
    /// <summary>
    /// Minimal codec that models 16 channels and exposes static Load/instance Save used by MainForm.
    /// Binary decode/encode of tones is intentionally conservative here so the UI comes up again.
    /// </summary>
    public class RgrCodec
    {
        public sealed class Channel
        {
            public int Number { get; set; }   // 1..16 for UI
            public double TxMHz { get; set; }
            public double RxMHz { get; set; }

            // UI indices into ToneLock.ToneMenuAll; -1 means unknown/"?"; 0 means "0" (no tone).
            public int TxToneIndex { get; set; } = 0;
            public int RxToneIndex { get; set; } = 0;

            // Convenience read-only properties for the grid
            public string? TxToneDisplay => IndexToDisplay(TxToneIndex);
            public string? RxToneDisplay => IndexToDisplay(RxToneIndex);

            public int Cct { get; set; } = 0;
            public bool Ste { get; set; } = false;
            public byte[] Raw { get; set; } = Array.Empty<byte>();

            public string Hex => Raw is null || Raw.Length == 0
                ? string.Empty
                : BitConverter.ToString(Raw).Replace("-", " ");
        }

        public Channel[] Channels { get; } = Enumerable.Range(0, 16).Select(i => new Channel { Number = i + 1 }).ToArray();
        public byte[] OriginalBytes { get; private set; } = Array.Empty<byte>();
        public string? SourcePath { get; private set; }

        public static RgrCodec Load(string path)
        {
            var codec = new RgrCodec();
            codec.SourcePath = path;
            codec.OriginalBytes = File.ReadAllBytes(path);

            // Locate 16x8-byte blocks; if uncertain, just slice head of file.
            int recordSize = 8; // per-channel block length we identified during earlier reverse engineering
            int channels = 16;

            int available = Math.Min(codec.OriginalBytes.Length, recordSize * channels);
            for (int ch = 0; ch < channels; ch++)
            {
                int offset = ch * recordSize;
                if (offset + recordSize <= available)
                    codec.Channels[ch].Raw = codec.OriginalBytes.Skip(offset).Take(recordSize).ToArray();
                else
                    codec.Channels[ch].Raw = Array.Empty<byte>();

                // Leave the rest at defaults for now so the UI renders; full decode plugs in later.
                codec.Channels[ch].TxMHz = 0;
                codec.Channels[ch].RxMHz = 0;
                codec.Channels[ch].TxToneIndex = 0;
                codec.Channels[ch].RxToneIndex = 0;
                codec.Channels[ch].Cct = 0;
                codec.Channels[ch].Ste = false;
            }

            return codec;
        }

        public void Save(string path)
        {
            // Round-trip original bytes until the full encoder is finalized
            if (OriginalBytes.Length > 0)
                File.WriteAllBytes(path, OriginalBytes);
        }

        private static string? IndexToDisplay(int idx)
        {
            if (idx < 0) return "?";
            if (idx == 0) return "0";
            var arr = ToneLock.ToneMenuAll;
            return idx < arr.Length ? arr[idx] : "?";
        }
    }
}
