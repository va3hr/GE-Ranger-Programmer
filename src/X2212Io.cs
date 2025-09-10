using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace GE_Ranger_Programmer
{
    /// <summary>
    /// X2212 NOVRAM I/O Driver
    /// 256x4 bit (256 nibbles) Non-Volatile Static RAM
    /// Uses parallel port for communication
    /// </summary>
    public static class X2212Io
    {
        // Parallel port register offsets
        private const short DATA_PORT = 0;   // Base+0: 8-bit bidirectional data
        private const short STAT_PORT = 1;   // Base+1: Status (read nibbles here)
        private const short CTRL_PORT = 2;   // Base+2: Control signals

        // Control register bit patterns for X2212 operations
        // These control the CE, WE, and STORE pins through the parallel port
        private const byte CTRL_IDLE = 0x04;      // All control lines inactive
        private const byte CTRL_ADDR_LATCH = 0x05; // Latch address (first write phase)
        private const byte CTRL_DATA_WRITE = 0x07; // Write data (second write phase)
        private const byte CTRL_READ_ENABLE = 0x06; // Enable read operation
        private const byte CTRL_STORE_CMD = 0x00;   // Store RAM to EEPROM

        // Timing parameters (microseconds) - Conservative values for reliability
        public static int SetupTime_us = 10;    // Address/data setup time before control change
        public static int PulseWidth_us = 10;   // Control signal pulse width
        public static int HoldTime_us = 5;      // Data hold time after control change
        public static int StoreTime_ms = 15;    // EEPROM store cycle time (10ms min per spec)

        // P/Invoke declarations for parallel port access
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        private static extern short Inp32(short port);

        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        private static extern void Out32(short port, short data);

        /// <summary>
        /// Microsecond delay using high-resolution timer
        /// </summary>
        private static void DelayMicroseconds(int microseconds)
        {
            if (microseconds <= 0) return;
            
            long ticksToWait = Stopwatch.Frequency * microseconds / 1_000_000;
            long startTicks = Stopwatch.GetTimestamp();
            
            while ((Stopwatch.GetTimestamp() - startTicks) < ticksToWait)
            {
                // Busy wait for precise timing
            }
        }

        /// <summary>
        /// Initialize the parallel port to a known state
        /// </summary>
        public static void Initialize(ushort baseAddress, Action<string>? log = null)
        {
            log?.Invoke("Initializing X2212 interface...");
            
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            short dataPort = (short)(baseAddress + DATA_PORT);
            
            // Set control lines to idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
            
            // Clear data port
            Out32(dataPort, 0x00);
            DelayMicroseconds(SetupTime_us);
            
            log?.Invoke("X2212 interface initialized");
        }

        /// <summary>
        /// Set the port to safe idle state (compatibility method)
        /// </summary>
        public static void SetIdle(ushort baseAddress)
        {
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
        }

        /// <summary>
        /// Write a single nibble to the X2212
        /// </summary>
        private static void WriteNibble(ushort baseAddress, byte address, byte nibble)
        {
            short dataPort = (short)(baseAddress + DATA_PORT);
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            // Ensure we're in idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
            
            // Step 1: Put ADDRESS on the data bus
            Out32(dataPort, address);
            DelayMicroseconds(SetupTime_us);
            
            // Step 2: Pulse control to latch the address
            Out32(ctrlPort, CTRL_ADDR_LATCH);
            DelayMicroseconds(PulseWidth_us);
            
            // Step 3: Put DATA nibble on the data bus (only lower 4 bits matter)
            Out32(dataPort, (short)(nibble & 0x0F));
            DelayMicroseconds(SetupTime_us);
            
            // Step 4: Pulse control to write the data
            Out32(ctrlPort, CTRL_DATA_WRITE);
            DelayMicroseconds(PulseWidth_us);
            
            // Step 5: Return to idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(HoldTime_us);
        }

        /// <summary>
        /// Read a single nibble from the X2212
        /// </summary>
        private static byte ReadNibble(ushort baseAddress, byte address)
        {
            short dataPort = (short)(baseAddress + DATA_PORT);
            short statPort = (short)(baseAddress + STAT_PORT);
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            // Ensure we're in idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
            
            // Step 1: Put ADDRESS on the data bus
            Out32(dataPort, address);
            DelayMicroseconds(SetupTime_us);
            
            // Step 2: Enable read operation
            Out32(ctrlPort, CTRL_READ_ENABLE);
            DelayMicroseconds(PulseWidth_us);
            
            // Step 3: Read the nibble from status port (upper 4 bits)
            short statusValue = Inp32(statPort);
            byte nibble = (byte)((statusValue >> 4) & 0x0F);
            
            // Step 4: Return to idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(HoldTime_us);
            
            return nibble;
        }

        /// <summary>
        /// Program all 256 nibbles to the X2212
        /// </summary>
        public static void ProgramNibbles(ushort baseAddress, byte[] nibbles, Action<string>? log = null)
        {
            if (nibbles == null || nibbles.Length != 256)
                throw new ArgumentException("Must provide exactly 256 nibbles");
            
            log?.Invoke("Starting X2212 programming...");
            
            for (int i = 0; i < 256; i++)
            {
                WriteNibble(baseAddress, (byte)i, nibbles[i]);
                
                // Progress reporting every 32 nibbles
                if ((i + 1) % 32 == 0)
                {
                    log?.Invoke($"Programmed {i + 1}/256 nibbles");
                }
            }
            
            log?.Invoke("Programming complete - 256 nibbles written");
        }

        /// <summary>
        /// Read all 256 nibbles from the X2212
        /// </summary>
        public static byte[] ReadAllNibbles(ushort baseAddress, Action<string>? log = null)
        {
            byte[] nibbles = new byte[256];
            
            log?.Invoke("Starting X2212 read...");
            
            for (int i = 0; i < 256; i++)
            {
                nibbles[i] = ReadNibble(baseAddress, (byte)i);
                
                // Progress reporting every 32 nibbles
                if ((i + 1) % 32 == 0)
                {
                    log?.Invoke($"Read {i + 1}/256 nibbles");
                }
            }
            
            log?.Invoke("Read complete - 256 nibbles retrieved");
            return nibbles;
        }

        /// <summary>
        /// Verify programmed data against expected values
        /// </summary>
        public static bool VerifyNibbles(ushort baseAddress, byte[] expectedNibbles, 
                                        out int failAddress, Action<string>? log = null)
        {
            failAddress = -1;
            
            if (expectedNibbles == null || expectedNibbles.Length != 256)
                throw new ArgumentException("Must provide exactly 256 nibbles");
            
            log?.Invoke("Starting verification...");
            
            for (int i = 0; i < 256; i++)
            {
                byte readValue = ReadNibble(baseAddress, (byte)i);
                byte expectedValue = (byte)(expectedNibbles[i] & 0x0F);
                
                if (readValue != expectedValue)
                {
                    failAddress = i;
                    log?.Invoke($"Verification failed at address {i:X2}: " +
                              $"Expected {expectedValue:X1}, Read {readValue:X1}");
                    return false;
                }
                
                // Progress reporting every 32 nibbles
                if ((i + 1) % 32 == 0)
                {
                    log?.Invoke($"Verified {i + 1}/256 nibbles");
                }
            }
            
            log?.Invoke("Verification successful - all 256 nibbles match");
            return true;
        }

        /// <summary>
        /// Execute STORE command to save RAM contents to EEPROM
        /// </summary>
        public static void DoStore(ushort baseAddress, Action<string>? log = null)
        {
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            log?.Invoke("Executing STORE command (RAM → EEPROM)...");
            
            // Assert STORE signal
            Out32(ctrlPort, CTRL_STORE_CMD);
            
            // Hold for required store time (10ms minimum per datasheet)
            Thread.Sleep(StoreTime_ms);
            
            // Return to idle
            Out32(ctrlPort, CTRL_IDLE);
            
            log?.Invoke($"STORE complete (held for {StoreTime_ms}ms)");
        }

        /// <summary>
        /// Test if X2212 device is present and responding
        /// </summary>
        public static bool ProbeDevice(ushort baseAddress, out string diagnosticInfo, 
                                      Action<string>? log = null)
        {
            diagnosticInfo = "";
            
            try
            {
                log?.Invoke("Probing for X2212 device...");
                
                // Test pattern: write distinctive values to first 4 locations
                byte[] testPattern = { 0x5, 0xA, 0x3, 0xC }; // Recognizable pattern
                byte[] readBack = new byte[4];
                
                // Save original values
                for (int i = 0; i < 4; i++)
                {
                    readBack[i] = ReadNibble(baseAddress, (byte)i);
                }
                log?.Invoke($"Original values: {string.Join(" ", Array.ConvertAll(readBack, x => x.ToString("X1")))}");
                
                // Write test pattern
                for (int i = 0; i < 4; i++)
                {
                    WriteNibble(baseAddress, (byte)i, testPattern[i], false);
                }
                
                // Read back and verify
                bool success = true;
                for (int i = 0; i < 4; i++)
                {
                    byte value = ReadNibble(baseAddress, (byte)i);
                    if (value != testPattern[i])
                    {
                        success = false;
                        diagnosticInfo = $"Pattern mismatch at address {i}: " +
                                       $"wrote {testPattern[i]:X1}, read {value:X1}";
                        break;
                    }
                }
                
                // Restore original values
                for (int i = 0; i < 4; i++)
                {
                    WriteNibble(baseAddress, (byte)i, readBack[i], false);
                }
                
                if (success)
                {
                    diagnosticInfo = "X2212 device detected and responding correctly";
                    log?.Invoke("Device probe successful");
                    return true;
                }
                else
                {
                    log?.Invoke($"Device probe failed: {diagnosticInfo}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                diagnosticInfo = $"Probe exception: {ex.Message}";
                log?.Invoke($"Device probe error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Convert 128 bytes to 256 nibbles (expand)
        /// </summary>
        public static byte[] ExpandToNibbles(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 128)
                throw new ArgumentException("Must provide exactly 128 bytes");
            
            byte[] nibbles = new byte[256];
            
            for (int i = 0; i < 128; i++)
            {
                nibbles[i * 2] = (byte)((bytes[i] >> 4) & 0x0F);     // High nibble
                nibbles[i * 2 + 1] = (byte)(bytes[i] & 0x0F);        // Low nibble
            }
            
            return nibbles;
        }

        /// <summary>
        /// Convert 256 nibbles to 128 bytes (compress)
        /// </summary>
        public static byte[] CompressNibblesToBytes(byte[] nibbles)
        {
            if (nibbles == null || nibbles.Length != 256)
                throw new ArgumentException("Must provide exactly 256 nibbles");
            
            byte[] bytes = new byte[128];
            
            for (int i = 0; i < 128; i++)
            {
                byte highNibble = (byte)(nibbles[i * 2] & 0x0F);
                byte lowNibble = (byte)(nibbles[i * 2 + 1] & 0x0F);
                bytes[i] = (byte)((highNibble << 4) | lowNibble);
            }
            
            return bytes;
        }

        /// <summary>
        /// Comprehensive device test - write, read, verify pattern
        /// </summary>
        public static bool TestDevice(ushort baseAddress, Action<string>? log = null)
        {
            try
            {
                log?.Invoke("=== Starting comprehensive device test ===");
                
                // Step 1: Save current contents
                log?.Invoke("Step 1: Reading current device contents...");
                byte[] originalData = ReadAllNibbles(baseAddress, log);
                
                // Step 2: Write ascending pattern (0,1,2...F,0,1,2...)
                log?.Invoke("Step 2: Writing test pattern (ascending)...");
                byte[] testPattern1 = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    testPattern1[i] = (byte)(i & 0x0F);
                }
                ProgramNibbles(baseAddress, testPattern1, log);
                
                // Step 3: Verify ascending pattern
                log?.Invoke("Step 3: Verifying test pattern...");
                if (!VerifyNibbles(baseAddress, testPattern1, out int fail1, log))
                {
                    log?.Invoke($"TEST FAILED at address {fail1:X2}");
                    return false;
                }
                
                // Step 4: Write inverse pattern
                log?.Invoke("Step 4: Writing inverse pattern...");
                byte[] testPattern2 = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    testPattern2[i] = (byte)(0x0F - (i & 0x0F));
                }
                ProgramNibbles(baseAddress, testPattern2, log);
                
                // Step 5: Verify inverse pattern
                log?.Invoke("Step 5: Verifying inverse pattern...");
                if (!VerifyNibbles(baseAddress, testPattern2, out int fail2, log))
                {
                    log?.Invoke($"TEST FAILED at address {fail2:X2}");
                    return false;
                }
                
                // Step 6: Restore original data
                log?.Invoke("Step 6: Restoring original contents...");
                ProgramNibbles(baseAddress, originalData, log);
                
                // Step 7: Verify restoration
                log?.Invoke("Step 7: Verifying restoration...");
                if (!VerifyNibbles(baseAddress, originalData, out int fail3, log))
                {
                    log?.Invoke($"RESTORE FAILED at address {fail3:X2}");
                    return false;
                }
                
                log?.Invoke("=== Device test PASSED ===");
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"Test error: {ex.Message}");
                return false;
            }
        }
    }
}
