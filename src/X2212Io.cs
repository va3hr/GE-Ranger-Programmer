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
        private const byte CTRL_DATA_LATCH = 0x05; // Latch data (first write phase) - CORRECTED
        private const byte CTRL_ADDR_WRITE = 0x07; // Write address (second write phase) - CORRECTED
        private const byte CTRL_READ_ENABLE = 0x06; // Enable read operation
        private const byte CTRL_STORE_CMD = 0x00;   // Store RAM to EEPROM

        // Timing parameters (microseconds) - EXTENDED FOR LONG CABLE
        
public static int SetupTime_us = 150;    // 150µs setup time
public static int PulseWidth_us = 150;   // 150µs pulse width  
public static int HoldTime_us = 75;      // 75µs hold time
public static int StoreTime_ms = 25;     // 25ms store time
        

        // Debug logging
        private static bool _debugLogging = false;

        // P/Invoke declarations for parallel port access
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        private static extern short Inp32(short port);

        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        private static extern void Out32(short port, short data);

        /// <summary>
        /// Enable or disable debug logging
        /// </summary>
        public static void EnableDebugLogging(bool enable)
        {
            _debugLogging = enable;
        }

        /// <summary>
        /// Debug log message
        /// </summary>
        private static void LogDebug(string message)
        {
            if (_debugLogging)
                Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff}: {message}");
        }

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
            log?.Invoke($"Timing: Setup={SetupTime_us}µs, Pulse={PulseWidth_us}µs, Hold={HoldTime_us}µs");
            
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
        /// Write a single nibble to the X2212 (CORRECTED sequence based on BASIC program)
        /// </summary>
        private static void WriteNibble(ushort baseAddress, byte address, byte nibble, bool debug = false)
        {
            short dataPort = (short)(baseAddress + DATA_PORT);
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            if (debug || _debugLogging)
            {
                LogDebug($"WriteNibble: Addr={address:X2}, Data={nibble:X1}");
            }
            
            // Ensure we're in idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
            
            // CORRECTED SEQUENCE: DATA first, then ADDRESS (matches BASIC program)
            // Step 1: Put DATA on the data bus (BASIC: OUT LPT, WriteBuffer(I))
            Out32(dataPort, (short)(nibble & 0x0F));
            DelayMicroseconds(SetupTime_us);
            
            // Step 2: Pulse control to latch the data (BASIC: OUT LPT + 2, 5)
            Out32(ctrlPort, CTRL_DATA_LATCH);
            DelayMicroseconds(PulseWidth_us);
            
            // Step 3: Put ADDRESS on the data bus (BASIC: OUT LPT, I)
            Out32(dataPort, address);
            DelayMicroseconds(SetupTime_us);
            
            // Step 4: Pulse control to write the address (BASIC: OUT LPT + 2, 7)
            Out32(ctrlPort, CTRL_ADDR_WRITE);
            DelayMicroseconds(PulseWidth_us);
            
            // Step 5: Additional pulse from BASIC program (OUT LPT + 2, 5)
            Out32(ctrlPort, CTRL_DATA_LATCH);
            DelayMicroseconds(PulseWidth_us);
            
            // Step 6: Return to idle state (BASIC: OUT LPT + 2, 4)
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(HoldTime_us);
            
            if (debug || _debugLogging)
            {
                // Immediate read-back for debugging
                byte readBack = ReadNibble(baseAddress, address);
                LogDebug($"  Read back: {readBack:X1} (should be {nibble:X1})");
            }
        }

        /// <summary>
        /// Read a single nibble from the X2212
        /// </summary>
        private static byte ReadNibble(ushort baseAddress, byte address)
        {
            short dataPort = (short)(baseAddress + DATA_PORT);
            short statPort = (short)(baseAddress + STAT_PORT);
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            if (_debugLogging)
            {
                LogDebug($"ReadNibble: Addr={address:X2}");
            }
            
            // Ensure we're in idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
            
            // Step 1: Put ADDRESS on the data bus
            Out32(dataPort, address);
            DelayMicroseconds(SetupTime_us);
            
            // Step 2: Enable read operation (BASIC: OUT LPT + 2, 6)
            Out32(ctrlPort, CTRL_READ_ENABLE);
            DelayMicroseconds(PulseWidth_us);
            
            // Step 3: Read the nibble from status port (upper 4 bits)
            short statusValue = Inp32(statPort);
            byte nibble = (byte)((statusValue >> 4) & 0x0F);
            
            // Step 4: Return to idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(HoldTime_us);
            
            if (_debugLogging)
            {
                LogDebug($"  Read value: {nibble:X1}");
            }
            
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
            log?.Invoke($"Using timing: Setup={SetupTime_us}µs, Pulse={PulseWidth_us}µs");
            
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
                    WriteNibble(baseAddress, (byte)i, testPattern[i]);
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
                    WriteNibble(baseAddress, (byte)i, readBack[i]);
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
        /// Diagnostic function to test different write sequences
        /// </summary>
        public static void DiagnosticWriteTest(ushort baseAddress, Action<string>? log = null)
        {
            log?.Invoke("=== DIAGNOSTIC WRITE TEST ===");
            log?.Invoke($"Timing: Setup={SetupTime_us}µs, Pulse={PulseWidth_us}µs, Hold={HoldTime_us}µs");
            
            short dataPort = (short)(baseAddress + DATA_PORT);
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            // Test 1: Write a simple pattern to first 4 locations
            log?.Invoke("\nTest 1: Writing 5,A,3,C to addresses 0-3");
            byte[] testData = { 0x5, 0xA, 0x3, 0xC };
            
            for (int i = 0; i < 4; i++)
            {
                log?.Invoke($"  Writing {testData[i]:X1} to address {i}");
                WriteNibble(baseAddress, (byte)i, testData[i], true);
            }
            
            // Read back
            log?.Invoke("\nReading back addresses 0-3:");
            for (int i = 0; i < 4; i++)
            {
                byte value = ReadNibble(baseAddress, (byte)i);
                bool match = (value == testData[i]);
                log?.Invoke($"  Addr {i}: Read {value:X1}, Expected {testData[i]:X1} - {(match ? "OK" : "FAIL")}");
            }
            
            // Test 2: Try alternative control sequence (reversed)
            log?.Invoke("\nTest 2: Alternative write sequence (data first, then address)");
            for (int i = 4; i < 8; i++)
            {
                byte testValue = (byte)(i & 0x0F);
                log?.Invoke($"  Alt-Writing {testValue:X1} to address {i}");
                
                // Try data-first sequence
                Out32(ctrlPort, CTRL_IDLE);
                DelayMicroseconds(SetupTime_us);
                
                Out32(dataPort, testValue);  // Data first
                DelayMicroseconds(SetupTime_us);
                
                Out32(ctrlPort, CTRL_DATA_LATCH);
                DelayMicroseconds(PulseWidth_us);
                
                Out32(dataPort, (byte)i);    // Address second
                DelayMicroseconds(SetupTime_us);
                
                Out32(ctrlPort, CTRL_ADDR_WRITE);
                DelayMicroseconds(PulseWidth_us);
                
                Out32(ctrlPort, CTRL_IDLE);
                DelayMicroseconds(HoldTime_us);
                
                // Read back
                byte readValue = ReadNibble(baseAddress, (byte)i);
                log?.Invoke($"    Read back: {readValue:X1}");
            }
            
            // Test 3: Check if control bits are correct
            log?.Invoke("\nTest 3: Control signal verification");
            log?.Invoke($"  CTRL_IDLE = 0x{CTRL_IDLE:X2} (binary: {Convert.ToString(CTRL_IDLE, 2).PadLeft(8, '0')})");
            log?.Invoke($"  CTRL_DATA_LATCH = 0x{CTRL_DATA_LATCH:X2} (binary: {Convert.ToString(CTRL_DATA_LATCH, 2).PadLeft(8, '0')})");
            log?.Invoke($"  CTRL_ADDR_WRITE = 0x{CTRL_ADDR_WRITE:X2} (binary: {Convert.ToString(CTRL_ADDR_WRITE, 2).PadLeft(8, '0')})");
            log?.Invoke($"  CTRL_READ_ENABLE = 0x{CTRL_READ_ENABLE:X2} (binary: {Convert.ToString(CTRL_READ_ENABLE, 2).PadLeft(8, '0')})");
            
            // Test 4: Try with much longer delays
            log?.Invoke("\nTest 4: Extended timing test (100µs delays)");
            for (int i = 8; i < 12; i++)
            {
                byte testValue = (byte)(0xF - (i & 0x0F));
                log?.Invoke($"  Writing {testValue:X1} to address {i} with 100µs delays");
                
                Out32(ctrlPort, CTRL_IDLE);
                DelayMicroseconds(100);
                
                Out32(dataPort, (byte)i);     // Address
                DelayMicroseconds(100);
                
                Out32(ctrlPort, CTRL_DATA_LATCH);
                DelayMicroseconds(100);
                
                Out32(dataPort, testValue);   // Data
                DelayMicroseconds(100);
                
                Out32(ctrlPort, CTRL_ADDR_WRITE);
                DelayMicroseconds(100);
                
                Out32(ctrlPort, CTRL_IDLE);
                DelayMicroseconds(100);
                
                byte readValue = ReadNibble(baseAddress, (byte)i);
                bool match = (readValue == testValue);
                log?.Invoke($"    Read: {readValue:X1}, Expected: {testValue:X1} - {(match ? "OK" : "FAIL")}");
            }
            
            log?.Invoke("\n=== DIAGNOSTIC TEST COMPLETE ===");
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

