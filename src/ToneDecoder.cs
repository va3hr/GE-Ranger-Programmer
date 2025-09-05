using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public class ToneDecoder
{
    private class ToneMapping
    {
        // FIX: Initialize Name property to an empty string to resolve the CS8618 warning.
        public string Name { get; set; } = string.Empty;
        public List<int> TxNibbles { get; } = new List<int>();
        public List<int> RxNibbles { get; } = new List<int>();
    }

    private readonly List<ToneMapping> _toneTable = new List<ToneMapping>();
    
    private static readonly int[] TxByteOffsets = { 0x8, 0xD, 0xE, 0xF };
    private static readonly int[] RxByteOffsets = { 0x0, 0x6, 0x7 };

    public ToneDecoder(string csvFilePath)
    {
        try
        {
            var lines = File.ReadAllLines(csvFilePath).Skip(1); 

            foreach (var line in lines)
            {
                var columns = line.Split(',');
                if (columns.Length < 9) continue;

                var mapping = new ToneMapping { Name = columns[0].Trim() };

                for (int i = 1; i <= 4; i++)
                {
                    if (int.TryParse(columns[i].Trim(), System.Globalization.NumberStyles.HexNumber, null, out int nibble))
                    {
                        mapping.TxNibbles.Add(nibble);
                    }
                }

                for (int i = 9; i <= 11; i++)
                {
                     if (int.TryParse(columns[i].Trim(), System.Globalization.NumberStyles.HexNumber, null, out int nibble))
                    {
                        mapping.RxNibbles.Add(nibble);
                    }
                }
                _toneTable.Add(mapping);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tone file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public string GetTone(byte[] fileData, int channelBaseAddress, bool isTx)
    {
        var offsets = isTx ? TxByteOffsets : RxByteOffsets;
        var extractedNibbles = new List<int>();

        foreach (var offset in offsets)
        {
            int absoluteAddress = channelBaseAddress + offset;
            if (absoluteAddress >= fileData.Length) return "Err";

            int nibble = fileData[absoluteAddress] & 0x0F;
            extractedNibbles.Add(nibble);
        }

        foreach (var tone in _toneTable)
        {
            var nibblesToCompare = isTx ? tone.TxNibbles : tone.RxNibbles;
            if (nibblesToCompare.SequenceEqual(extractedNibbles))
            {
                return tone.Name;
            }
        }

        return "Err";
    }
}
