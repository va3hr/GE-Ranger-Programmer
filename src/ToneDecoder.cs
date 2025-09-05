using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public class ToneDecoder
    {
        private class ToneMapping {
            public string Name { get; set; } = "";
            public List<int> TxNibbles { get; } = new List<int>();
            public List<int> RxNibbles { get; } = new List<int>();
        }
        private readonly List<ToneMapping> _toneTable = new List<ToneMapping>();
        private static readonly int[] TxByteOffsets = { 0x8, 0xD, 0xE, 0xF };
        private static readonly int[] RxByteOffsets = { 0x0, 0x6, 0x7 };

        public ToneDecoder(string csvFilePath)
        {
            try {
                var lines = File.ReadAllLines(csvFilePath).Skip(1);
                foreach (var line in lines) {
                    var columns = line.Split(',');
                    if (columns.Length < 12) continue;
                    var mapping = new ToneMapping { Name = columns[0].Trim() };
                    for (int i = 1; i <= 4; i++)
                        if (int.TryParse(columns[i].Trim(), System.Globalization.NumberStyles.HexNumber, null, out int n))
                            mapping.TxNibbles.Add(n);
                    for (int i = 9; i <= 11; i++)
                        if (int.TryParse(columns[i].Trim(), System.Globalization.NumberStyles.HexNumber, null, out int n))
                            mapping.RxNibbles.Add(n);
                    _toneTable.Add(mapping);
                }
            } catch (Exception ex) {
                MessageBox.Show($"Error loading tone file: {ex.Message}", "Error");
            }
        }

        public string GetTone(byte[] fileData, int channelBaseAddress, bool isTx)
        {
            var offsets = isTx ? TxByteOffsets : RxByteOffsets;
            var extractedNibbles = new List<int>();
            foreach (var offset in offsets) {
                int absAddr = channelBaseAddress + offset;
                if (absAddr >= fileData.Length) return "Err";
                extractedNibbles.Add(fileData[absAddr] & 0x0F);
            }
            foreach (var tone in _toneTable) {
                var nibblesToCompare = isTx ? tone.TxNibbles : tone.RxNibbles;
                if (nibblesToCompare.Count > 0 && nibblesToCompare.SequenceEqual(extractedNibbles)) {
                    return tone.Name;
                }
            }
            return "Err";
        }
    }
}
