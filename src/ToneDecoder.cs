using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public class ToneDecoder
{
    // Represents a single tone's mapping from the CSV file
    private class ToneMapping
    {
        public string Name { get; set; }
        public List<int> TxNibbles { get; } = new List<int>();
        public List<int> RxNibbles { get; } = new List<int>();
    }

    private readonly List<ToneMapping> _toneTable = new List<ToneMapping>();
    
    // Relative offsets from the channel's base address for each nibble
    private static readonly int[] TxByteOffsets = { 0x8, 0xD, 0xE, 0xF };
    private static readonly int[] RxByteOffsets = { 0x0, 0x6, 0x7 };

    /// <summary>
    /// Loads the tone mapping rules from the provided CSV file.
    /// </summary>
    /// <param name="csvFilePath">Path to the ToneCodes CSV file.</param>
    public ToneDecoder(string csvFilePath)
    {
        try
        {
            var lines = File.ReadAllLines(csvFilePath).Skip(1); // Skip header row

            foreach (var line in lines)
            {
                var columns = line.Split(',');
                if (columns.Length < 9) continue;

                var mapping = new ToneMapping { Name = columns[0].Trim() };

                // Parse Tx Tones (4 nibbles)
                for (int i = 1; i <= 4; i++)
                {
                    if (int.TryParse(columns[i].Trim(), System.Globalization.NumberStyles.HexNumber, null, out int nibble))
                    {
                        mapping.TxNibbles.Add(nibble);
                    }
                }

                // Parse Rx Tones (3 nibbles)
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

    /// <summary>
    /// Decodes a tone from the file data based on the channel's address.
    /// </summary>
    /// <param name="fileData">The entire 128-byte file data (already converted from Big-Endian).</param>
    /// <param name="channelBaseAddress">The starting memory address of the channel (e.g., 0xE0).</param>
    /// <param name="isTx">True for TX tone, False for RX tone.</param>
    /// <returns>The tone name as a string, or "Err" if not found.</returns>
    public string GetTone(byte[] fileData, int channelBaseAddress, bool isTx)
    {
        var offsets = isTx ? TxByteOffsets : RxByteOffsets;
        var extractedNibbles = new List<int>();

        foreach (var offset in offsets)
        {
            int absoluteAddress = channelBaseAddress + offset;
            if (absoluteAddress >= fileData.Length) return "Err"; // Bounds check

            // We only care about the LOW nibble (last 4 bits)
            int nibble = fileData[absoluteAddress] & 0x0F;
            extractedNibbles.Add(nibble);
        }

        // Find a matching tone in our loaded table
        foreach (var tone in _toneTable)
        {
            var nibblesToCompare = isTx ? tone.TxNibbles : tone.RxNibbles;
            if (nibblesToCompare.SequenceEqual(extractedNibbles))
            {
                return tone.Name;
            }
        }

        return "Err"; // No match found
    }
}
