using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GE_Ranger_Programmer
{
    public sealed class RgrCodec
    {
        public sealed class Channel
        {
            public int Number { get; set; }               // 1..16
            public double TxMHz { get; set; }
            public double RxMHz { get; set; }
            public int TxToneIndex { get; set; } = 0;     // 0 => "0", -1 => unknown => "?"
            public int RxToneIndex { get; set; } = 0;
            public string TxToneDisplay => TxToneIndex <= 0 ? "0" : ToneLock.IndexToDisplay(TxToneIndex);
            public string RxToneDisplay => RxToneIndex <= 0 ? "0" : ToneLock.IndexToDisplay(RxToneIndex);
            public int Cct { get; set; } = 0;
            public bool Ste { get; set; } = false;
            public string Hex { get; set; } = string.Empty;
        }

        public List<Channel> Channels { get; } = CreateEmpty();

        public static List<Channel> CreateEmpty()
        {
            var rows = new List<Channel>(16);
            for (int i = 0; i < 16; i++)
                rows.Add(new Channel { Number = i + 1 });
            return rows;
        }

        public static RgrCodec Load(string path)
        {
            var c = new RgrCodec();

            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                // Conservative: if at least 128 bytes, slice 8 per channel and show in Hex column.
                if (bytes.Length >= 16 * 8)
                {
                    for (int ch = 0; ch < 16; ch++)
                    {
                        var slice = bytes.Skip(ch * 8).Take(8).ToArray();
                        c.Channels[ch].Hex = BitConverter.ToString(slice).Replace("-", " ");
                    }
                }
            }
            return c;
        }

        public void Save(string path)
        {
            // Non-destructive placeholder: write a CSV snapshot so user can confirm UI content.
            using var sw = new StreamWriter(path, false);
            sw.WriteLine("CH,TxMHz,RxMHz,TxTone,RxTone,cct,ste,Hex");
            foreach (var ch in Channels)
                sw.WriteLine($"{ch.Number},{ch.TxMHz},{ch.RxMHz},{ch.TxToneDisplay},{ch.RxToneDisplay},{ch.Cct},{(ch.Ste ? "Y" : "N")},{ch.Hex}");
        }
    }
}
