// -----------------------------------------------------------------------------
// ToneLock.cs — GE Rangr (.RGR) tone decode & labels
// -----------------------------------------------------------------------------
// Standards:
//   • Canonical tone list is frozen (no 114.1).
//   • Nibble-based encoding/decoding using reverse-engineered data.
//   • Clean separation between UI (frequency strings) and EEPROM (nibbles).
//   • Uses same channel byte approach as FreqLock for consistency.
// -----------------------------------------------------------------------------

using System;

namespace GE_Ranger_Programmer
{
    public static class ToneLock
    {
        // Canonical CTCSS table: index 0 is "0" (no tone). No "114.1".
        private static readonly string[] CanonicalTonesNoZero =
        {
            "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
            "88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9",
            "114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2",
            "151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7"
        };

        // UI menus (no "0" here — UI shows "0" as blank/null)
        public static readonly string[] ToneMenuTx = CanonicalTonesNoZero;
        public static readonly string[] ToneMenuRx = CanonicalTonesNoZero;

        // Index → label (index 0 = "0")
        private static readonly string[] ToneByIndex = BuildToneByIndex();
        public static string[] Cg => ToneByIndex; // back-compat exposure

        private static string[] BuildToneByIndex()
        {
            var map = new string[CanonicalTonesNoZero.Length + 1];
            map[0] = "0";
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++) map[i + 1] = CanonicalTonesNoZero[i];
            return map;
        }

        // -----------------------------------------------------------------------------
        // NIBBLE DATA STRUCTURES - Based on CSV reverse engineering
        // TX: E8L, EDL, EEL, EFL (4 nibbles) | RX: E0L, E6L, E7L (3 nibbles)
        // -----------------------------------------------------------------------------
        
        // TX Tone nibbles at E8L, EDL, EEL, EFL (Channel 1 addresses) 
        private static readonly byte[,] TxToneNibbles = new byte[34, 4] // Index 0 = no tone, 1-33 = actual tones
        {
            {0x0, 0x0, 0x0, 0x0}, // Index 0 - No tone
            {0x6, 0x0, 0x7, 0x1}, // Index 1 - 67.0 Hz
            {0x6, 0x4, 0xD, 0x3}, // Index 2 - 71.9 Hz
            {0x6, 0x0, 0x2, 0x3}, // Index 3 - 74.4 Hz
            {0x6, 0x4, 0x8, 0x4}, // Index 4 - 77.0 Hz
            {0x6, 0x4, 0xB, 0x5}, // Index 5 - 79.7 Hz
            {0x6, 0x0, 0x1, 0x5}, // Index 6 - 82.5 Hz
            {0x6, 0x4, 0x6, 0x6}, // Index 7 - 85.4 Hz
            {0x6, 0x4, 0xC, 0x7}, // Index 8 - 88.5 Hz
            {0x6, 0x0, 0x3, 0x7}, // Index 9 - 91.5 Hz
            {0x6, 0x4, 0x9, 0x8}, // Index 10 - 94.8 Hz
            {0x6, 0x4, 0x2, 0x8}, // Index 11 - 97.4 Hz
            {0x6, 0x0, 0xC, 0x9}, // Index 12 - 100.0 Hz
            {0x6, 0x4, 0x3, 0x9}, // Index 13 - 103.5 Hz
            {0x6, 0x0, 0xB, 0xA}, // Index 14 - 107.2 Hz
            {0x6, 0x4, 0x3, 0xA}, // Index 15 - 110.9 Hz
            {0x6, 0x0, 0xC, 0xB}, // Index 16 - 114.8 Hz
            {0x6, 0x4, 0x4, 0xB}, // Index 17 - 118.8 Hz
            {0x6, 0x4, 0xD, 0xC}, // Index 18 - 123.0 Hz
            {0x6, 0x4, 0x6, 0xC}, // Index 19 - 127.3 Hz
            {0x6, 0x4, 0xF, 0xD}, // Index 20 - 131.8 Hz
            {0x6, 0x0, 0x9, 0xD}, // Index 21 - 136.5 Hz
            {0x6, 0x0, 0x3, 0xD}, // Index 22 - 141.3 Hz
            {0x6, 0x0, 0xD, 0xE}, // Index 23 - 146.2 Hz
            {0x6, 0x0, 0x7, 0xE}, // Index 24 - 151.4 Hz
            {0x6, 0x4, 0x1, 0xE}, // Index 25 - 156.7 Hz
            {0x6, 0x0, 0xC, 0xF}, // Index 26 - 162.2 Hz
            {0x6, 0x0, 0x7, 0xF}, // Index 27 - 167.9 Hz
            {0x6, 0x0, 0x2, 0xF}, // Index 28 - 173.8 Hz
            {0x7, 0x0, 0xD, 0x0}, // Index 29 - 179.9 Hz
            {0x7, 0x4, 0x8, 0x0}, // Index 30 - 186.2 Hz
            {0x7, 0x4, 0x3, 0x0}, // Index 31 - 192.8 Hz
            {0x7, 0x0, 0xD, 0x1}, // Index 32 - 203.5 Hz
            {0x7, 0x4, 0x8, 0x1}  // Index 33 - 210.7 Hz
        };

        // RX Tone nibbles at E0L, E6L, E7L (Channel 1 addresses)
        private static readonly byte[,] RxToneNibbles = new byte[34, 3] // Index 0 = no tone, 1-33 = actual tones
        {
            {0x0, 0x0, 0x0}, // Index 0 - No tone
            {0x6, 0x7, 0x1}, // Index 1 - 67.0 Hz
            {0x6, 0xE, 0x3}, // Index 2 - 71.9 Hz
            {0x6, 0x2, 0x3}, // Index 3 - 74.4 Hz
            {0x6, 0x7, 0x4}, // Index 4 - 77.0 Hz
            {0x6, 0xC, 0x5}, // Index 5 - 79.7 Hz
            {0x6, 0x1, 0x5}, // Index 6 - 82.5 Hz
            {0x6, 0x7, 0x6}, // Index 7 - 85.4 Hz
            {0x6, 0xC, 0x7}, // Index 8 - 88.5 Hz
            {0x6, 0x3, 0x7}, // Index 9 - 91.5 Hz
            {0x6, 0xA, 0x8}, // Index 10 - 94.8 Hz
            {0x6, 0x3, 0x8}, // Index 11 - 97.4 Hz
            {0x6, 0xC, 0x9}, // Index 12 - 100.0 Hz
            {0x6, 0x4, 0x9}, // Index 13 - 103.5 Hz
            {0x6, 0xB, 0xA}, // Index 14 - 107.2 Hz
            {0x6, 0x3, 0xA}, // Index 15 - 110.9 Hz
            {0x6, 0xC, 0xB}, // Index 16 - 114.8 Hz
            {0x6, 0x4, 0xB}, // Index 17 - 118.8 Hz
            {0x6, 0xD, 0xC}, // Index 18 - 123.0 Hz
            {0x6, 0x6, 0xC}, // Index 19 - 127.3 Hz
            {0x6, 0xF, 0xD}, // Index 20 - 131.8 Hz
            {0x6, 0x9, 0xD}, // Index 21 - 136.5 Hz
            {0x6, 0x3, 0xD}, // Index 22 - 141.3 Hz
            {0x6, 0xD, 0xE}, // Index 23 - 146.2 Hz
            {0x6, 0x7, 0xE}, // Index 24 - 151.4 Hz
            {0x6, 0x2, 0xE}, // Index 25 - 156.7 Hz
            {0x6, 0xC, 0xF}, // Index 26 - 162.2 Hz
            {0x6, 0x7, 0xF}, // Index 27 - 167.9 Hz
            {0x6, 0x2, 0xF}, // Index 28 - 173.8 Hz
            {0x7, 0xD, 0x0}, // Index 29 - 179.9 Hz
            {0x7, 0x8, 0x0}, // Index 30 - 186.2 Hz
            {0x7, 0x4, 0x0}, // Index 31 - 192.8 Hz
            {0x7, 0xD, 0x1}, // Index 32 - 203.5 Hz
            {0x7, 0x9, 0x1}  // Index 33 - 210.7 Hz
        };

        // -----------------------------------------------------------------------------
        // PRIMARY TONE EXTRACTION METHODS - Uses same approach as FreqLock
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Get transmit tone using the same channel byte approach as FreqLock
        /// </summary>
        /// <param name="chA0">Channel byte A0</param>
        /// <param name="chA1">Channel byte A1</param>
        /// <param name="chA2">Channel byte A2</param>
        /// <param name="chA3">Channel byte A3</param>
        /// <param name="chB0">Channel byte B0</param>
        /// <param name="chB1">Channel byte B1</param>
        /// <param name="chB2">Channel byte B2</param>
        /// <param name="chB3">Channel byte B3</param>
        /// <returns>Tone frequency string or "0" for no tone</returns>
        public static string GetTransmitToneFromChannelBytes(byte chA0, byte chA1, byte chA2, byte chA3, 
                                                           byte chB0, byte chB1, byte chB2, byte chB3)
        {
            // Extract nibbles using BigEndian helpers (same approach as FreqLock)
            // TODO: Map these to the correct nibble positions based on your channel layout
            byte txE8L = (byte)BigEndian.Hi(chA0);  // Adjust based on actual channel mapping
            byte txEDL = (byte)BigEndian.Lo(chA3);  
            byte txEEL = (byte)BigEndian.Hi(chA2);  
            byte txEFL = (byte)BigEndian.Lo(chA1);  
            
            int txToneIndex = FindTxToneIndexByNibbles(txE8L, txEDL, txEEL, txEFL);
            return GetFrequencyFromToneIndex(txToneIndex);
        }

        /// <summary>
        /// Get receive tone using the same channel byte approach as FreqLock
        /// </summary>
        /// <param name="chA0">Channel byte A0</param>
        /// <param name="chA1">Channel byte A1</param>
        /// <param name="chA2">Channel byte A2</param>
        /// <param name="chA3">Channel byte A3</param>
        /// <param name="chB0">Channel byte B0</param>
        /// <param name="chB1">Channel byte B1</param>
        /// <param name="chB2">Channel byte B2</param>
        /// <param name="chB3">Channel byte B3</param>
        /// <returns>Tone frequency string or "0" for no tone</returns>
        public static string GetReceiveToneFromChannelBytes(byte chA0, byte chA1, byte chA2, byte chA3, 
                                                          byte chB0, byte chB1, byte chB2, byte chB3)
        {
            // Extract nibbles using BigEndian helpers (same approach as FreqLock)
            // TODO: Map these to the correct nibble positions based on your channel layout
            byte rxE0L = (byte)BigEndian.Hi(chA0);  // Adjust based on actual channel mapping
            byte rxE6L = (byte)BigEndian.Lo(chA2);  
            byte rxE7L = (byte)BigEndian.Hi(chA1);  
            
            int rxToneIndex = FindRxToneIndexByNibbles(rxE0L, rxE6L, rxE7L);
            return GetFrequencyFromToneIndex(rxToneIndex);
        }

        // -----------------------------------------------------------------------------
        // NIBBLE LOOKUP METHODS
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Find TX tone index by matching nibble values from EEPROM
        /// </summary>
        /// <param name="e8l">Nibble at E8L position</param>
        /// <param name="edl">Nibble at EDL position</param>
        /// <param name="eel">Nibble at EEL position</param>
        /// <param name="efl">Nibble at EFL position</param>
        /// <returns>Tone index (0-33) or 0 if not found</returns>
        public static int FindTxToneIndexByNibbles(byte e8l, byte edl, byte eel, byte efl)
        {
            for (int i = 0; i < TxToneNibbles.GetLength(0); i++)
            {
                if (TxToneNibbles[i, 0] == e8l && 
                    TxToneNibbles[i, 1] == edl && 
                    TxToneNibbles[i, 2] == eel && 
                    TxToneNibbles[i, 3] == efl)
                {
                    return i;
                }
            }
            return 0; // Default to no tone if not found
        }

        /// <summary>
        /// Find RX tone index by matching nibble values from EEPROM
        /// </summary>
        /// <param name="e0l">Nibble at E0L position</param>
        /// <param name="e6l">Nibble at E6L position</param>
        /// <param name="e7l">Nibble at E7L position</param>
        /// <returns>Tone index (0-33) or 0 if not found</returns>
        public static int FindRxToneIndexByNibbles(byte e0l, byte e6l, byte e7l)
        {
            for (int i = 0; i < RxToneNibbles.GetLength(0); i++)
            {
                if (RxToneNibbles[i, 0] == e0l && 
                    RxToneNibbles[i, 1] == e6l && 
                    RxToneNibbles[i, 2] == e7l)
                {
                    return i;
                }
            }
            return 0; // Default to no tone if not found
        }

        // -----------------------------------------------------------------------------
        // UI HELPER METHODS
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Convert tone frequency string to index for UI dropdown selection
        /// </summary>
        /// <param name="frequencyString">Frequency as string (e.g., "67.0")</param>
        /// <returns>Tone index (0-33) or 0 if not found</returns>
        public static int GetToneIndexFromFrequency(string frequencyString)
        {
            if (string.IsNullOrEmpty(frequencyString) || frequencyString == "0")
                return 0;
                
            for (int i = 0; i < CanonicalTonesNoZero.Length; i++)
            {
                if (CanonicalTonesNoZero[i] == frequencyString)
                    return i + 1; // Add 1 because index 0 is reserved for "no tone"
            }
            return 0; // Default to no tone if not found
        }

        /// <summary>
        /// Convert tone index back to frequency string for UI display
        /// </summary>
        /// <param name="toneIndex">Tone index (0-33)</param>
        /// <returns>Frequency string or "0" for no tone</returns>
        public static string GetFrequencyFromToneIndex(int toneIndex)
        {
            if (toneIndex <= 0 || toneIndex >= ToneByIndex.Length)
                return "0";
            return ToneByIndex[toneIndex];
        }

        /// <summary>
        /// Get tone label for display (includes "0" for no tone)
        /// </summary>
        /// <param name="toneIndex">Tone index (0-33)</param>
        /// <returns>Tone label string</returns>
        public static string GetToneLabel(int toneIndex)
        {
            if (toneIndex >= 0 && toneIndex < ToneByIndex.Length)
                return ToneByIndex[toneIndex];
            return "Err";
        }

        // -----------------------------------------------------------------------------
        // EEPROM WRITE METHODS - For future EEPROM writing functionality
        // -----------------------------------------------------------------------------
        
        /// <summary>
        /// Get TX tone nibbles for writing to EEPROM at E8L, EDL, EEL, EFL positions
        /// </summary>
        /// <param name="toneIndex">Tone index (0=no tone, 1-33=actual tones)</param>
        /// <returns>4-byte array with nibbles [E8L, EDL, EEL, EFL]</returns>
        public static byte[] GetTxToneNibbles(int toneIndex)
        {
            if (toneIndex < 0 || toneIndex >= TxToneNibbles.GetLength(0))
                return new byte[] { 0x0, 0x0, 0x0, 0x0 }; // Default to no tone
                
            return new byte[] 
            { 
                TxToneNibbles[toneIndex, 0], // E8L
                TxToneNibbles[toneIndex, 1], // EDL  
                TxToneNibbles[toneIndex, 2], // EEL
                TxToneNibbles[toneIndex, 3]  // EFL
            };
        }

        /// <summary>
        /// Get RX tone nibbles for writing to EEPROM at E0L, E6L, E7L positions  
        /// </summary>
        /// <param name="toneIndex">Tone index (0=no tone, 1-33=actual tones)</param>
        /// <returns>3-byte array with nibbles [E0L, E6L, E7L]</returns>
        public static byte[] GetRxToneNibbles(int toneIndex)
        {
            if (toneIndex < 0 || toneIndex >= RxToneNibbles.GetLength(0))
                return new byte[] { 0x0, 0x0, 0x0 }; // Default to no tone
                
            return new byte[] 
            { 
                RxToneNibbles[toneIndex, 0], // E0L
                RxToneNibbles[toneIndex, 1], // E6L
                RxToneNibbles[toneIndex, 2]  // E7L
            };
        }

        // -----------------------------------------------------------------------------
        // LEGACY COMPATIBILITY METHODS - For backward compatibility
        // -----------------------------------------------------------------------------

        /// <summary>
        /// LEGACY: Get transmit tone label (routes to new channel byte method)
        /// </summary>
        public static string GetTransmitToneLabel(byte rowA3, byte rowA2, byte rowA1, byte rowA0, 
                                                byte rowB3, byte rowB2, byte rowB1, byte rowB0)
        {
            return GetTransmitToneFromChannelBytes(rowA0, rowA1, rowA2, rowA3, rowB0, rowB1, rowB2, rowB3);
        }

        /// <summary>
        /// LEGACY: Get receive tone label (routes to new channel byte method)
        /// </summary>
        public static string GetReceiveToneLabel(byte rowA3)
        {
            // For RX, we only have rowA3, so extract what we can
            // This may need adjustment based on actual channel mapping
            return GetReceiveToneFromChannelBytes(0, 0, 0, rowA3, 0, 0, 0, 0);
        }

        /// <summary>
        /// Check if Squelch Tail Elimination is enabled
        /// </summary>
        /// <param name="rowA3">Channel byte A3</param>
        /// <returns>True if STE is enabled</returns>
        public static bool IsSquelchTailEliminationEnabled(byte rowA3)
        {
            return ((rowA3 >> 7) & 1) == 1;
        }
       
        /// <summary>
        /// Legacy diagnostic method (placeholder)
        /// </summary>
        public static (int[] Bits, int Index) InspectTransmitBits(
            byte A0, byte A1, byte A2, byte A3,
            byte B0, byte B1, byte B2, byte B3)
        {
            // Temporary placeholder that returns empty data
            int[] bits = new int[6] { 0, 0, 0, 0, 0, 0 };
            int index = 0;
            return (bits, index);
        }
    }
}
