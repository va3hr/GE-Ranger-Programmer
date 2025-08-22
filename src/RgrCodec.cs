using System.Globalization;
using System.Text;

namespace GE_Ranger_Programmer;

/// <summary>
/// Minimal codec that models 16 channels and exposes static Load/instance Save used by MainForm.
/// Binary decode/encode of tones is intentionally conservative here so the UI comes up again.
/// </summary>
public class RgrCodec
{
    public sealed class Channel
    {
        public double TxMHz { get; set; }
        public double RxMHz { get; set; }
        // UI indices into ToneLock.ToneMenuAll; -1 means unknown/"?"; 0 means "0" (no tone).
        public int TxToneIndex { get; set; } = 0;
        public int RxToneIndex { get; set; } = 0;
        public int Cct { get; set; } = 0;
        public bool Ste { get; set; } = false;
        public byte[] Raw { get; set; } = Array.Empty<byte>();
    }

    public Channel[] Channels { get; } = Enumerable.Range(0, 16).Select(_ => new Channel()).ToArray();
    public byte[] OriginalBytes { get; private set; } = Array.Empty<byte>();
    public string? SourcePath { get; private set; }

    public static RgrCodec Load(string path)
    {
        var codec = new RgrCodec();
        codec.SourcePath = path;
        codec.OriginalBytes = File.ReadAllBytes(path);

        // Very light/defensive parse: attempt to locate 16x8-byte blocks; if not sure, leave zeros.
        // This preserves UI functionality while we keep the detailed tone mapping in a separate pass.
        int len = codec.OriginalBytes.Length;
        int recordSize = 8; // per-channel block length we identified earlier
        int channels = 16;

        if (len >= recordSize * channels)
        {
            for (int ch = 0; ch < channels; ch++)
            {
                // For now, do not attempt risky decode here—just keep Raw slice so hex column can show something.
                int offset = ch * recordSize;
                codec.Channels[ch].Raw = codec.OriginalBytes.Skip(offset).Take(recordSize).ToArray();
                // Leave other fields at defaults; the UI will show blank/0/? until we finish the robust decode.
            }
        }

        return codec;
    }

    public void Save(string path)
    {
        // Until full encode is finalized, write back OriginalBytes untouched to avoid corrupting files.
        // This guarantees round-tripping while UI edits are disabled.
        if (OriginalBytes.Length > 0)
        {
            File.WriteAllBytes(path, OriginalBytes);
        }
    }
}
