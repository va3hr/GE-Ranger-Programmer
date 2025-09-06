// -----------------------------------------------------------------------------
// ToneDiag.cs — GE Rangr tone diagnostics (nibble-based)
// -----------------------------------------------------------------------------
// Diagnostic utilities for inspecting tone nibble data and EEPROM operations
// -----------------------------------------------------------------------------

using System;
using System.Text;

namespace GE_Ranger_Programmer
{
    public static class ToneDiag
    {
        /// <summary>
        /// Inspect TX tone nibbles and return diagnostic information
        /// </summary>
        /// <param name="e8l">E8L nibble value</param>
        /// <param name="edl">EDL nibble value</param>
        /// <param name="eel">EEL nibble value</param>
        /// <param name="efl">EFL nibble value</param>
        /// <returns>Diagnostic string with nibble values and matched tone</returns>
        public static string InspectTxNibbles(byte e8l, byte edl, byte eel, byte efl)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"TX Nibbles: E8L={e8l:X}, EDL={edl:X}, EEL={eel:X}, EFL={efl:X}");
            
            int toneIndex = ToneLock.FindTxToneIndexByNibbles(e8l, edl, eel, efl);
            string toneLabel = ToneLock.GetToneLabel(toneIndex);
            
            sb.AppendLine($"Matched Tone Index: {toneIndex}");
            sb.AppendLine($"Matched Tone: {toneLabel} Hz");
            
            if (toneIndex == 0)
                sb.AppendLine("Status: No tone or unrecognized nibble pattern");
            else
                sb.AppendLine("Status: Valid tone detected");
                
            return sb.ToString();
        }

        /// <summary>
        /// Inspect RX tone nibbles and return diagnostic information
        /// </summary>
        /// <param name="e0l">E0L nibble value</param>
        /// <param name="e6l">E6L nibble value</param>
        /// <param name="e7l">E7L nibble value</param>
        /// <returns>Diagnostic string with nibble values and matched tone</returns>
        public static string InspectRxNibbles(byte e0l, byte e6l, byte e7l)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"RX Nibbles: E0L={e0l:X}, E6L={e6l:X}, E7L={e7l:X}");
            
            int toneIndex = ToneLock.FindRxToneIndexByNibbles(e0l, e6l, e7l);
            string toneLabel = ToneLock.GetToneLabel(toneIndex);
            
            sb.AppendLine($"Matched Tone Index: {toneIndex}");
            sb.AppendLine($"Matched Tone: {toneLabel} Hz");
            
            if (toneIndex == 0)
                sb.AppendLine("Status: No tone or unrecognized nibble pattern");
            else
                sb.AppendLine("Status: Valid tone detected");
                
            return sb.ToString();
        }

        /// <summary>
        /// Inspect a complete channel's tone configuration
        /// </summary>
        /// <param name="channelNumber">Channel number (1-16)</param>
        /// <param name="txE8L">TX E8L nibble</param>
        /// <param name="txEDL">TX EDL nibble</param>
        /// <param name="txEEL">TX EEL nibble</param>
        /// <param name="txEFL">TX EFL nibble</param>
        /// <param name="rxE0L">RX E0L nibble</param>
        /// <param name="rxE6L">RX E6L nibble</param>
        /// <param name="rxE7L">RX E7L nibble</param>
        /// <returns>Complete diagnostic report for the channel</returns>
        public static string InspectChannelTones(int channelNumber, 
            byte txE8L, byte txEDL, byte txEEL, byte txEFL,
            byte rxE0L, byte rxE6L, byte rxE7L)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Channel {channelNumber} Tone Diagnostics ===");
            sb.AppendLine();
            
            // Channel addresses
           // int[] txAddresses = ToneLock.GetTxNibbleAddresses(channelNumber);
           // int[] rxAddresses = ToneLock.GetRxNibbleAddresses(channelNumber);
            
            sb.AppendLine("Expected EEPROM Addresses:");
            sb.AppendLine($"  TX: E8={txAddresses[0]:X4}, ED={txAddresses[1]:X4}, EE={txAddresses[2]:X4}, EF={txAddresses[3]:X4}");
            sb.AppendLine($"  RX: E0={rxAddresses[0]:X4}, E6={rxAddresses[1]:X4}, E7={rxAddresses[2]:X4}");
            sb.AppendLine();
            
            // TX Analysis
            sb.AppendLine("TX Tone Analysis:");
            sb.Append(InspectTxNibbles(txE8L, txEDL, txEEL, txEFL));
            sb.AppendLine();
            
            // RX Analysis  
            sb.AppendLine("RX Tone Analysis:");
            sb.Append(InspectRxNibbles(rxE0L, rxE6L, rxE7L));
            
            return sb.ToString();
        }

        /// <summary>
        /// Generate a tone lookup table for debugging
        /// </summary>
        /// <param name="includeTx">Include TX tone table</param>
        /// <param name="includeRx">Include RX tone table</param>
        /// <returns>Formatted tone lookup table</returns>
        public static string GenerateToneLookupTable(bool includeTx = true, bool includeRx = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== GE Ranger Tone Lookup Table ===");
            sb.AppendLine();
            
            if (includeTx)
            {
                sb.AppendLine("TX Tones (E8L, EDL, EEL, EFL):");
                sb.AppendLine("Index | Freq   | E8L | EDL | EEL | EFL");
                sb.AppendLine("------|--------|-----|-----|-----|----");
                
                for (int i = 0; i < 34; i++)
                {
                    byte[] nibbles = ToneLock.GetTxToneNibbles(i);
                    string freq = ToneLock.GetToneLabel(i);
                    sb.AppendLine($"{i,5} | {freq,6} | {nibbles[0],3:X} | {nibbles[1],3:X} | {nibbles[2],3:X} | {nibbles[3],3:X}");
                }
                sb.AppendLine();
            }
            
            if (includeRx)
            {
                sb.AppendLine("RX Tones (E0L, E6L, E7L):");
                sb.AppendLine("Index | Freq   | E0L | E6L | E7L");
                sb.AppendLine("------|--------|-----|-----|----");
                
                for (int i = 0; i < 34; i++)
                {
                    byte[] nibbles = ToneLock.GetRxToneNibbles(i);
                    string freq = ToneLock.GetToneLabel(i);
                    sb.AppendLine($"{i,5} | {freq,6} | {nibbles[0],3:X} | {nibbles[1],3:X} | {nibbles[2],3:X}");
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Validate tone data integrity
        /// </summary>
        /// <returns>Validation report</returns>
        public static string ValidateToneData()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Tone Data Validation ===");
            sb.AppendLine();
            
            int txErrors = 0;
            int rxErrors = 0;
            
            // Test TX round-trip conversion
            sb.AppendLine("TX Tone Round-trip Test:");
            for (int i = 0; i < 34; i++)
            {
                byte[] nibbles = ToneLock.GetTxToneNibbles(i);
                int foundIndex = ToneLock.FindTxToneIndexByNibbles(nibbles[0], nibbles[1], nibbles[2], nibbles[3]);
                
                if (foundIndex != i)
                {
                    sb.AppendLine($"  ERROR: Index {i} → Nibbles [{nibbles[0]:X},{nibbles[1]:X},{nibbles[2]:X},{nibbles[3]:X}] → Index {foundIndex}");
                    txErrors++;
                }
            }
            if (txErrors == 0)
                sb.AppendLine("  All TX tones passed round-trip test");
            
            sb.AppendLine();
            
            // Test RX round-trip conversion
            sb.AppendLine("RX Tone Round-trip Test:");
            for (int i = 0; i < 34; i++)
            {
                byte[] nibbles = ToneLock.GetRxToneNibbles(i);
                int foundIndex = ToneLock.FindRxToneIndexByNibbles(nibbles[0], nibbles[1], nibbles[2]);
                
                if (foundIndex != i)
                {
                    sb.AppendLine($"  ERROR: Index {i} → Nibbles [{nibbles[0]:X},{nibbles[1]:X},{nibbles[2]:X}] → Index {foundIndex}");
                    rxErrors++;
                }
            }
            if (rxErrors == 0)
                sb.AppendLine("  All RX tones passed round-trip test");
            
            sb.AppendLine();
            sb.AppendLine($"Summary: {txErrors} TX errors, {rxErrors} RX errors");
            
            if (txErrors == 0 && rxErrors == 0)
                sb.AppendLine("Status: All tone data is valid");
            else
                sb.AppendLine("Status: ERRORS DETECTED - Check tone lookup tables");
                
            return sb.ToString();
        }

        /// <summary>
        /// Test a frequency string conversion
        /// </summary>
        /// <param name="frequencyString">Frequency to test (e.g., "67.0")</param>
        /// <returns>Conversion test results</returns>
        public static string TestFrequencyConversion(string frequencyString)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Frequency Conversion Test: {frequencyString} ===");
            sb.AppendLine();
            
            // Convert frequency to index
            int toneIndex = ToneLock.GetToneIndexFromFrequency(frequencyString);
            sb.AppendLine($"Frequency '{frequencyString}' → Tone Index: {toneIndex}");
            
            // Get nibbles for both TX and RX
            byte[] txNibbles = ToneLock.GetTxToneNibbles(toneIndex);
            byte[] rxNibbles = ToneLock.GetRxToneNibbles(toneIndex);
            
            sb.AppendLine($"TX Nibbles: [{txNibbles[0]:X}, {txNibbles[1]:X}, {txNibbles[2]:X}, {txNibbles[3]:X}]");
            sb.AppendLine($"RX Nibbles: [{rxNibbles[0]:X}, {rxNibbles[1]:X}, {rxNibbles[2]:X}]");
            
            // Convert back to frequency
            string convertedBack = ToneLock.GetFrequencyFromToneIndex(toneIndex);
            sb.AppendLine($"Round-trip result: {convertedBack}");
            
            if (convertedBack == frequencyString)
                sb.AppendLine("Status: PASS - Round-trip conversion successful");
            else
                sb.AppendLine("Status: FAIL - Round-trip conversion failed");
                
            return sb.ToString();
        }
    }
}

