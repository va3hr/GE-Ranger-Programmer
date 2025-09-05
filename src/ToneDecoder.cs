using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GE_Ranger_Programmer
{
    public class ToneDecoder
    {
        // Private class to hold the tone patterns
        private class ToneMapping
        {
            public string Name { get; set; } = "";
            // The pattern of low nibbles that define this tone
            public int[] NibblePattern { get; set; } = new int[0];
        }

        private readonly List<ToneMapping> _txToneTable;
        private readonly List<ToneMapping> _rxToneTable;

        // Relative offsets from the channel's base address for each nibble
        private static readonly int[] TxByteOffsets = { 0x8, 0xD, 0xE, 0xF };
        private static readonly int[] RxByteOffsets = { 0x0, 0x6, 0x7 };

        public ToneDecoder()
        {
            // --- The tone data is now an embedded array, not a CSV file ---
            _txToneTable = new List<ToneMapping>
            {
                new ToneMapping { Name = "67.0", NibblePattern = new[] { 0x6, 0x0, 0x7, 0x1 } },
                new ToneMapping { Name = "71.9", NibblePattern = new[] { 0x6, 0x4, 0xD, 0x3 } },
                new ToneMapping { Name = "74.4", NibblePattern = new[] { 0x6, 0x0, 0x2, 0x3 } },
                new ToneMapping { Name = "77.0", NibblePattern = new[] { 0x6, 0x4, 0x8, 0x4 } },
                new ToneMapping { Name = "79.7", NibblePattern = new[] { 0x6, 0x4, 0xB, 0x5 } },
                new ToneMapping { Name = "82.5", NibblePattern = new[] { 0x6, 0x0, 0x1, 0x5 } },
                new ToneMapping { Name = "85.4", NibblePattern = new[] { 0x6, 0x4, 0x6, 0x6 } },
                new ToneMapping { Name = "88.5", NibblePattern = new[] { 0x6, 0x4, 0xC, 0x7 } },
                new ToneMapping { Name = "91.5", NibblePattern = new[] { 0x6, 0x0, 0x3, 0x7 } },
                new ToneMapping { Name = "94.8", NibblePattern = new[] { 0x6, 0x4, 0x9, 0x8 } },
                new ToneMapping { Name = "97.4", NibblePattern = new[] { 0x6, 0x4, 0x2, 0x8 } },
                new ToneMapping { Name = "100.0", NibblePattern = new[] { 0x6, 0x0, 0xC, 0x9 } },
                new ToneMapping { Name = "103.5", NibblePattern = new[] { 0x6, 0x4, 0x3, 0x9 } },
                new ToneMapping { Name = "107.2", NibblePattern = new[] { 0x6, 0x0, 0xB, 0xA } },
                new ToneMapping { Name = "110.9", NibblePattern = new[] { 0x6, 0x4, 0x5, 0xB } },
                new ToneMapping { Name = "114.8", NibblePattern = new[] { 0x6, 0x0, 0xA, 0xB } },
                new ToneMapping { Name = "118.8", NibblePattern = new[] { 0x6, 0x4, 0x7, 0xC } },
                new ToneMapping { Name = "123.0", NibblePattern = new[] { 0x6, 0x0, 0x9, 0xC } },
                new ToneMapping { Name = "127.3", NibblePattern = new[] { 0x6, 0x4, 0x1, 0xD } },
                new ToneMapping { Name = "131.8", NibblePattern = new[] { 0x6, 0x0, 0xD, 0xD } },
                new ToneMapping { Name = "136.5", NibblePattern = new[] { 0x6, 0x4, 0x4, 0xE } },
                new ToneMapping { Name = "141.3", NibblePattern = new[] { 0x6, 0x0, 0xE, 0xE } },
                new ToneMapping { Name = "146.2", NibblePattern = new[] { 0x6, 0x4, 0x2, 0xF } },
                new ToneMapping { Name = "151.4", NibblePattern = new[] { 0x6, 0x0, 0x8, 0xF } },
                new ToneMapping { Name = "156.7", NibblePattern = new[] { 0x6, 0x4, 0xB, 0x0 } },
                new ToneMapping { Name = "162.2", NibblePattern = new[] { 0x6, 0x0, 0xA, 0x0 } },
                new ToneMapping { Name = "167.9", NibblePattern = new[] { 0x6, 0x4, 0x7, 0x1 } },
                new ToneMapping { Name = "173.8", NibblePattern = new[] { 0x6, 0x0, 0x9, 0x1 } },
                new ToneMapping { Name = "179.9", NibblePattern = new[] { 0x6, 0x4, 0x1, 0x2 } },
                new ToneMapping { Name = "186.2", NibblePattern = new[] { 0x6, 0x0, 0xD, 0x2 } },
                new ToneMapping { Name = "192.8", NibblePattern = new[] { 0x6, 0x4, 0x4, 0x3 } },
                new ToneMapping { Name = "203.5", NibblePattern = new[] { 0x6, 0x0, 0x6, 0x2 } },
            };

            _rxToneTable = new List<ToneMapping>
            {
                new ToneMapping { Name = "67.0", NibblePattern = new[] { 0x6, 0x7, 0x1 } },
                new ToneMapping { Name = "71.9", NibblePattern = new[] { 0x6, 0xE, 0x3 } },
                new ToneMapping { Name = "74.4", NibblePattern = new[] { 0x6, 0x2, 0x3 } },
                new ToneMapping { Name = "77.0", NibblePattern = new[] { 0x6, 0x7, 0x4 } },
                new ToneMapping { Name = "79.7", NibblePattern = new[] { 0x6, 0xC, 0x5 } },
                new ToneMapping { Name = "82.5", NibblePattern = new[] { 0x6, 0x1, 0x5 } },
                new ToneMapping { Name = "85.4", NibblePattern = new[] { 0x6, 0x7, 0x6 } },
                new ToneMapping { Name = "88.5", NibblePattern = new[] { 0x6, 0xC, 0x7 } },
                new ToneMapping { Name = "91.5", NibblePattern = new[] { 0x6, 0x3, 0x7 } },
                new ToneMapping { Name = "94.8", NibblePattern = new[] { 0x6, 0xA, 0x8 } },
                new ToneMapping { Name = "97.4", NibblePattern = new[] { 0x6, 0x3, 0x8 } },
                new ToneMapping { Name = "100.0", NibblePattern = new[] { 0x6, 0xC, 0x9 } },
                new ToneMapping { Name = "103.5", NibblePattern = new[] { 0x6, 0x4, 0x9 } },
                new ToneMapping { Name = "107.2", NibblePattern = new[] { 0x6, 0xB, 0xA } },
                new ToneMapping { Name = "110.9", NibblePattern = new[] { 0x6, 0x5, 0xB } },
                new ToneMapping { Name = "114.8", NibblePattern = new[] { 0x6, 0xA, 0xB } },
                new ToneMapping { Name = "118.8", NibblePattern = new[] { 0x6, 0x7, 0xC } },
                new ToneMapping { Name = "123.0", NibblePattern = new[] { 0x6, 0x9, 0xC } },
                new ToneMapping { Name = "127.3", NibblePattern = new[] { 0x6, 0x1, 0xD } },
                new ToneMapping { Name = "131.8", NibblePattern = new[] { 0x6, 0xD, 0xD } },
                new ToneMapping { Name = "136.5", NibblePattern = new[] { 0x6, 0x4, 0xE } },
                new ToneMapping { Name = "141.3", NibblePattern = new[] { 0x6, 0xE, 0xE } },
                new ToneMapping { Name = "146.2", NibblePattern = new[] { 0x6, 0x2, 0xF } },
                new ToneMapping { Name = "151.4", NibblePattern = new[] { 0x6, 0x8, 0xF } },
                new ToneMapping { Name = "156.7", NibblePattern = new[] { 0x6, 0xB, 0x0 } },
                new ToneMapping { Name = "162.2", NibblePattern = new[] { 0x6, 0xA, 0x0 } },
                new ToneMapping { Name = "167.9", NibblePattern = new[] { 0x6, 0x7, 0x1 } },
                new ToneMapping { Name = "173.8", NibblePattern = new[] { 0x6, 0x9, 0x1 } },
                new ToneMapping { Name = "179.9", NibblePattern = new[] { 0x6, 0x1, 0x2 } },
                new ToneMapping { Name = "186.2", NibblePattern = new[] { 0x6, 0xD, 0x2 } },
                new ToneMapping { Name = "192.8", NibblePattern = new[] { 0x6, 0x4, 0x3 } },
                new ToneMapping { Name = "203.5", NibblePattern = new[] { 0x6, 0x6, 0x2 } },
            };
        }

        public string GetTone(byte[] fileData, int channelBaseAddress, bool isTx)
        {
            var offsets = isTx ? TxByteOffsets : RxByteOffsets;
            var table = isTx ? _txToneTable : _rxToneTable;

            var extractedNibbles = new List<int>();
            foreach (var offset in offsets)
            {
                int absAddr = channelBaseAddress + offset;
                if (absAddr >= fileData.Length) return "Err";
                extractedNibbles.Add(fileData[absAddr] & 0x0F);
            }

            // Find the tone in the table that matches the extracted pattern
            foreach (var tone in table)
            {
                if (tone.NibblePattern.SequenceEqual(extractedNibbles))
                {
                    return tone.Name;
                }
            }

            return "Err"; // Return "Err" if no match is found
        }
    }
}
