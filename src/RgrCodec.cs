using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GE_Ranger_Programmer
{
    /// <summary>
    /// Minimal loader/saver and channel model so the project builds.
    /// The hex packing/decoding can be filled in later.
    /// </summary>
    public static class RgrCodec
    {
        // ----- Data model the UI binds to -----
        public class Channel
        {
            public int    Ch  { get; set; }
            public double TxMHz { get; set; }
            public double RxMHz { get; set; }
            public string TxTone { get; set; } = ToneLock.NoneDisplay;
            public string RxTone { get; set; } = ToneLock.NoneDisplay;
            public byte   Cct { get; set; }
            public bool   Ste { get; set; }
            public string Hex { get; set; } = string.Empty;
        }

        // ----- File I/O stubs (kept simple to get you unblocked) -----
        public static BindingList<Channel> Load(string path)
        {
            var list = new BindingList<Channel>();

            if (!File.Exists(path))
            {
                // Return 16 empty channels
                for (int i = 0; i < 16; i++)
                    list.Add(new Channel { Ch = i + 1 });
                return list;
            }

            var ext = Path.GetExtension(path).ToLowerInvariant();
            // Very lightweight: if it's a CSV exported by the tool, read it back.
            if (ext == ".csv")
            {
                var lines = File.ReadAllLines(path);
                // skip header if present
                foreach (var ln in lines.Skip(1))
                {
                    var parts = ln.Split(',');
                    if (parts.Length < 8) continue;
                    list.Add(new Channel
                    {
                        Ch     = int.TryParse(parts[0], out var ch) ? ch : list.Count + 1,
                        TxMHz  = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var tx) ? tx : 0,
                        RxMHz  = double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ? rx : 0,
                        TxTone = parts[3],
                        RxTone = parts[4],
                        Cct    = byte.TryParse(parts[5], out var cct) ? cct : (byte)0,
                        Ste    = parts[6].Trim().Equals("Y", StringComparison.OrdinalIgnoreCase),
                        Hex    = parts[7]
                    });
                }
                // Ensure 16 rows
                while (list.Count < 16) list.Add(new Channel { Ch = list.Count + 1 });
                return list;
            }

            // Unknown format (e.g., .RGR) — just return empty shell rows so UI can open.
            for (int i = 0; i < 16; i++)
                list.Add(new Channel { Ch = i + 1 });

            return list;
        }

        public static void Save(string path, IEnumerable<Channel> channels)
        {
            // Save an easily-inspectable CSV that round-trips via Load above.
            using var sw = new StreamWriter(path, false);
            sw.WriteLine("CH,Tx MHz,Rx MHz,Tx Tone,Rx Tone,cct,ste,Hex");
            foreach (var ch in channels.OrderBy(c => c.Ch))
            {
                sw.WriteLine(string.Join(",",
                    ch.Ch,
                    ch.TxMHz.ToString(CultureInfo.InvariantCulture),
                    ch.RxMHz.ToString(CultureInfo.InvariantCulture),
                    ch.TxTone,
                    ch.RxTone,
                    ch.Cct,
                    ch.Ste ? "Y" : "N",
                    ch.Hex));
            }
        }
    }
}
