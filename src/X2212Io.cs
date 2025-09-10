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
        private const byte CTRL_DATA_LATCH = 0x05; // Latch data (first write phase)
        private const byte CTRL_ADDR_WRITE = 0x07; // Write address (second write phase)
        private const byte CTRL_READ_ENABLE = 0x06; // Enable read operation
        private const byte CTRL_STORE_CMD = 0x00;   // Store RAM to EEPROM

        // Timing parameters (microseconds) - EXTENDED FOR LONG CABLE
        public static int SetupTime_us = 100;    // Increased for cable capacitance
        public static int PulseWidth_us = 100;   // Wider pulses for signal integrity  
        public static int HoldTime_us = 50;      // Adequate hold time
        public static int StoreTime_ms = 25;     // Extra margin for store operation

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

        private static void LogDebug(string message)
        {
            if (_debugLogging)
                Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff}: {message}");
        }

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

        public static void SetIdle(ushort baseAddress)
        {
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
        }

        // CORRECTED WRITE SEQUENCE (DATA first, then ADDRESS)
        private static void WriteNibble(ushort baseAddress, byte address, byte nibble, bool debug = false)
        {
            short dataPort = (short)(baseAddress + DATA_PORT);
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            if (debug || _debugLogging)
                LogDebug($"WriteNibble: Addr={address:X2}, Data={nibble:X1}");
            
            // Ensure idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
            
            // DATA first
            Out32(dataPort, (short)(nibble & 0x0F));
            DelayMicroseconds(SetupTime_us);
            
            // Latch data
            Out32(ctrlPort, CTRL_DATA_LATCH);
            DelayMicroseconds(PulseWidth_us);
            
            // ADDRESS second
            Out32(dataPort, address);
            DelayMicroseconds(SetupTime_us);
            
            // Write address
            Out32(ctrlPort, CTRL_ADDR_WRITE);
            DelayMicroseconds(PulseWidth_us);
            
            // Additional pulse
            Out32(ctrlPort, CTRL_DATA_LATCH);
            DelayMicroseconds(PulseWidth_us);
            
            // Return to idle
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(HoldTime_us);
        }

        // CORRECTED READ SEQUENCE with proper bit extraction
        private static byte ReadNibble(ushort baseAddress, byte address)
        {
            short dataPort = (short)(baseAddress + DATA_PORT);
            short statPort = (short)(baseAddress + STAT_PORT);
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            if (_debugLogging)
                LogDebug($"ReadNibble: Addr={address:X2}");
            
            // Ensure idle state
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(SetupTime_us);
            
            // Put address on bus
            Out32(dataPort, address);
            DelayMicroseconds(SetupTime_us);
            
            // Enable read
            Out32(ctrlPort, CTRL_READ_ENABLE);
            DelayMicroseconds(PulseWidth_us);
            
            // Read from status port (BASIC: (INP(LPT + 1) / 8) AND 15)
            short statusValue = Inp32(statPort);
            byte nibble = (byte)((statusValue >> 3) & 0x0F); // Shift right by 3, mask lower 4 bits
            
            // Return to idle
            Out32(ctrlPort, CTRL_IDLE);
            DelayMicroseconds(HoldTime_us);
            
            if (_debugLogging)
                LogDebug($"  Read value: {nibble:X1}");
            
            return nibble;
        }

        // ========== NEW ENDIAN CONVERSION METHODS ========== //

        /// <summary>
        /// Convert big-endian nibbles from X2212 to little-endian bytes for application
        /// </summary>
        private static byte[] ConvertBigEndianNibblesToLittleEndianBytes(byte[] bigEndianNibbles)
        {
            if (bigEndianNibbles == null || bigEndianNibbles.Length != 256)
                throw new ArgumentException("Must provide exactly 256 nibbles");
            
            byte[] littleEndianBytes = new byte[128];
            
            for (int i = 0; i < 128; i++)
            {
                // Big-endian: high nibble first, then low nibble
                byte highNibble = bigEndianNibbles[i * 2];
                byte lowNibble = bigEndianNibbles[i * 2 + 1];
                
                // Convert to little-endian byte
                littleEndianBytes[i] = (byte)((highNibble << 4) | lowNibble);
            }
            
            return littleEndianBytes;
        }

        /// <summary>
        /// Convert little-endian bytes from application to big-endian nibbles for X2212
        /// </summary>
        private static byte[] ConvertLittleEndianBytesToBigEndianNibbles(byte[] littleEndianBytes)
        {
            if (littleEndianBytes == null || littleEndianBytes.Length != 128)
                throw new ArgumentException("Must provide exactly 128 bytes");
            
            byte[] bigEndianNibbles = new byte[256];
            
            for (int i = 0; i < 128; i++)
            {
                // Extract nibbles from little-endian byte
                byte highNibble = (byte)((littleEndianBytes[i] >> 4) & 0x0F);
                byte lowNibble = (byte)(littleEndianBytes[i] & 0x0F);
                
                // Store in big-endian order: high nibble first, then low nibble
                bigEndianNibbles[i * 2] = highNibble;
                bigEndianNibbles[i * 2 + 1] = lowNibble;
            }
            
            return bigEndianNibbles;
        }

        // ========== ENHANCED PUBLIC METHODS WITH ENDIAN CONVERSION ========== //

        /// <summary>
        /// Read all 256 nibbles from X2212 (returns big-endian nibbles - backward compatible)
        /// </summary>
        public static byte[] ReadAllNibbles(ushort baseAddress, Action<string>? log = null)
        {
            byte[] nibbles = new byte[256];
            
            log?.Invoke("Reading X2212 (big-endian nibbles)...");
            
            for (int i = 0; i < 256; i++)
            {
                nibbles[i] = ReadNibble(baseAddress, (byte)i);
                
                if ((i + 1) % 32 == 0)
                    log?.Invoke($"Read {i + 1}/256 nibbles");
            }
            
            log?.Invoke("Read complete - 256 big-endian nibbles retrieved");
            return nibbles;
        }

        /// <summary>
        /// Read all data from X2212 and return as little-endian bytes (NEW)
        /// </summary>
        public static byte[] ReadAllBytes(ushort baseAddress, Action<string>? log = null)
        {
            log?.Invoke("Reading X2212 and converting to little-endian bytes...");
            
            // Read big-endian nibbles from hardware
            byte[] bigEndianNibbles = ReadAllNibbles(baseAddress, log);
            
            // Convert to little-endian bytes for application
            byte[] littleEndianBytes = ConvertBigEndianNibblesToLittleEndianBytes(bigEndianNibbles);
            
            log?.Invoke("Conversion complete - 128 little-endian bytes ready");
            return littleEndianBytes;
        }

        /// <summary>
        /// Program all 256 nibbles to the X2212 (backward compatible - expects big-endian nibbles)
        /// </summary>
        public static void ProgramNibbles(ushort baseAddress, byte[] nibbles, Action<string>? log = null)
        {
            if (nibbles == null || nibbles.Length != 256)
                throw new ArgumentException("Must provide exactly 256 nibbles");
            
            log?.Invoke("Programming X2212 (big-endian nibbles)...");
            
            for (int i = 0; i < 256; i++)
            {
                WriteNibble(baseAddress, (byte)i, nibbles[i]);
                
                if ((i + 1) % 32 == 0)
                    log?.Invoke($"Programmed {i + 1}/256 nibbles");
            }
            
            log?.Invoke("Programming complete - 256 big-endian nibbles written");
        }

        /// <summary>
        /// Program little-endian bytes to X2212 (NEW - converts to big-endian internally)
        /// </summary>
        public static void ProgramBytes(ushort baseAddress, byte[] littleEndianBytes, Action<string>? log = null)
        {
            if (littleEndianBytes == null || littleEndianBytes.Length != 128)
                throw new ArgumentException("Must provide exactly 128 bytes");
            
            log?.Invoke("Converting little-endian bytes to big-endian nibbles...");
            
            // Convert to big-endian nibbles for hardware
            byte[] bigEndianNibbles = ConvertLittleEndianBytesToBigEndianNibbles(littleEndianBytes);
            
            log?.Invoke("Programming X2212 with converted nibbles...");
            ProgramNibbles(baseAddress, bigEndianNibbles, log);
            
            log?.Invoke("Programming complete - 128 bytes converted and written");
        }

        // ========== EXISTING METHODS (UNCHANGED) ========== //

        public static bool VerifyNibbles(ushort baseAddress, byte[] expectedNibbles, 
                                        out int failAddress, Action<string>? log = null)
        {
            failAddress = -1;
            
            if (expectedNibbles == null || expectedNibbles.Length != 256)
                throw new ArgumentException("Must provide exactly 256 nibbles");
            
            log?.Invoke("Verifying big-endian nibbles...");
            
            for (int i = 0; i < 256; i++)
            {
                byte readValue = ReadNibble(baseAddress, (byte)i);
                byte expectedValue = (byte)(expectedNibbles[i] & 0x0F);
                
                if (readValue != expectedValue)
                {
                    failAddress = i;
                    log?.Invoke($"Verification failed at address {i:X2}: Expected {expectedValue:X1}, Read {readValue:X1}");
                    return false;
                }
            }
            
            log?.Invoke("Verification successful - all 256 nibbles match");
            return true;
        }

        /// <summary>
        /// Verify little-endian bytes against X2212 content (NEW)
        /// </summary>
        public static bool VerifyBytes(ushort baseAddress, byte[] expectedBytes, 
                                     out int failAddress, Action<string>? log = null)
        {
            failAddress = -1;
            
            if (expectedBytes == null || expectedBytes.Length != 128)
                throw new ArgumentException("Must provide exactly 128 bytes");
            
            log?.Invoke("Converting expected bytes to big-endian for verification...");
            
            // Convert expected bytes to big-endian nibbles
            byte[] expectedNibbles = ConvertLittleEndianBytesToBigEndianNibbles(expectedBytes);
            
            return VerifyNibbles(baseAddress, expectedNibbles, out failAddress, log);
        }

        public static void DoStore(ushort baseAddress, Action<string>? log = null)
        {
            short ctrlPort = (short)(baseAddress + CTRL_PORT);
            
            log?.Invoke("Executing STORE command...");
            
            Out32(ctrlPort, CTRL_STORE_CMD);
            Thread.Sleep(StoreTime_ms);
            Out32(ctrlPort, CTRL_IDLE);
            
            log?.Invoke($"STORE complete ({StoreTime_ms}ms)");
        }

        public static bool ProbeDevice(ushort baseAddress, out string diagnosticInfo, 
                                      Action<string>? log = null)
        {
            diagnosticInfo = "";
            
            try
            {
                log?.Invoke("Probing for X2212 device...");
                
                byte[] testPattern = { 0x5, 0xA, 0x3, 0xC };
                byte[] readBack = new byte[4];
                
                for (int i = 0; i < 4; i++)
                    readBack[i] = ReadNibble(baseAddress, (byte)i);
                
                for (int i = 0; i < 4; i++)
                    WriteNibble(baseAddress, (byte)i, testPattern[i]);
                
                bool success = true;
                for (int i = 0; i < 4; i++)
                {
                    byte value = ReadNibble(baseAddress, (byte)i);
                    if (value != testPattern[i])
                    {
                        success = false;
                        diagnosticInfo = $"Mismatch at {i}: wrote {testPattern[i]:X1}, read {value:X1}";
                        break;
                    }
                }
                
                for (int i = 0; i < 4; i++)
                    WriteNibble(baseAddress, (byte)i, readBack[i]);
                
                if (success)
                {
                    diagnosticInfo = "X2212 device detected and responding correctly";
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                diagnosticInfo = $"Probe exception: {ex.Message}";
                return false;
            }
        }

        public static byte[] ExpandToNibbles(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 128)
                throw new ArgumentException("Must provide exactly 128 bytes");
            
            return ConvertLittleEndianBytesToBigEndianNibbles(bytes);
        }

        public static byte[] CompressNibblesToBytes(byte[] nibbles)
        {
            if (nibbles == null || nibbles.Length != 256)
                throw new ArgumentException("Must provide exactly 256 nibbles");
            
            return ConvertBigEndianNibblesToLittleEndianBytes(nibbles);
        }

        // ========== TIMING CALIBRATION METHODS ========== //

        /// <summary>
        /// Test different timing values to find what works best for the current PC
        /// </summary>
        public static void TimingCalibrationTest(ushort baseAddress, Action<string>? log = null)
        {
            log?.Invoke("=== TIMING CALIBRATION TEST ===");
            log?.Invoke("Testing different timing values to find optimal settings");
            
            // Save original timing values
            int originalSetup = SetupTime_us;
            int originalPulse = PulseWidth_us;
            int originalHold = HoldTime_us;
            
            try
            {
                // Test addresses and patterns (use higher addresses to avoid disturbing working data)
                byte testAddress = 0xF0; // Use high addresses for calibration
                byte[] testPatterns = { 0x5, 0xA, 0x3, 0xC }; // Distinctive pattern
                
                int[] timingValues = { 10, 25, 50, 75, 100, 150, 200, 250, 500 };
                
                foreach (int timing in timingValues)
                {
                    log?.Invoke($"\nTesting timing: {timing}µs");
                    
                    // Set test timing
                    SetupTime_us = timing;
                    PulseWidth_us = timing;
                    HoldTime_us = timing / 2;
                    
                    int successCount = 0;
                    byte currentAddress = testAddress;
                    
                    foreach (byte testData in testPatterns)
                    {
                        try
                        {
                            // Write test pattern
                            WriteNibble(baseAddress, currentAddress, testData);
                            
                            // Read back
                            byte readBack = ReadNibble(baseAddress, currentAddress);
                            
                            if (readBack == testData)
                            {
                                successCount++;
                                log?.Invoke($"  Addr {currentAddress:X2}: Wrote {testData:X1}, Read {readBack:X1} ✓");
                            }
                            else
                            {
                                log?.Invoke($"  Addr {currentAddress:X2}: Wrote {testData:X1}, Read {readBack:X1} ✗");
                            }
                            
                            currentAddress++; // Move to next address
                        }
                        catch (Exception ex)
                        {
                            log?.Invoke($"  Error at address {currentAddress:X2}: {ex.Message}");
                        }
                    }
                    
                    log?.Invoke($"  Success rate: {successCount}/{testPatterns.Length}");
                    
                    if (successCount == testPatterns.Length)
                    {
                        log?.Invoke($"  ✓ OPTIMAL TIMING FOUND: {timing}µs");
                        log?.Invoke($"  Recommended settings: Setup={timing}µs, Pulse={timing}µs, Hold={timing/2}µs");
                        break;
                    }
                    else if (successCount >= testPatterns.Length / 2)
                    {
                        log?.Invoke($"  ~ Acceptable timing: {timing}µs ({successCount}/{testPatterns.Length} passed)");
                    }
                }
            }
            finally
            {
                // Always restore original timing values
                SetupTime_us = originalSetup;
                PulseWidth_us = originalPulse;
                HoldTime_us = originalHold;
                
                log?.Invoke($"\nRestored original timing: Setup={originalSetup}µs, Pulse={originalPulse}µs, Hold={originalHold}µs");
            }
            
            log?.Invoke("=== CALIBRATION COMPLETE ===");
        }

        /// <summary>
        /// Quick calibration to find minimum working timing
        /// </summary>
        public static int FindMinimumWorkingTiming(ushort baseAddress, Action<string>? log = null)
        {
            log?.Invoke("=== FINDING MINIMUM WORKING TIMING ===");
            
            int originalSetup = SetupTime_us;
            int originalPulse = PulseWidth_us;
            int originalHold = HoldTime_us;
            
            int[] testTimings = { 5, 10, 15, 20, 25, 30, 40, 50, 75, 100 };
            int minWorkingTiming = -1;
            
            try
            {
                byte testAddress = 0xFA;
                byte testData = 0x5;
                
                foreach (int timing in testTimings)
                {
                    SetupTime_us = timing;
                    PulseWidth_us = timing;
                    HoldTime_us = timing / 2;
                    
                    try
                    {
                        // Test write and read
                        WriteNibble(baseAddress, testAddress, testData);
                        byte readBack = ReadNibble(baseAddress, testAddress);
                        
                        if (readBack == testData)
                        {
                            minWorkingTiming = timing;
                            log?.Invoke($"✓ Timing {timing}µs works");
                            break;
                        }
                        else
                        {
                            log?.Invoke($"✗ Timing {timing}µs failed (wrote {testData:X1}, read {readBack:X1})");
                        }
                    }
                    catch
                    {
                        log?.Invoke($"✗ Timing {timing}µs failed with error");
                    }
                    
                    testAddress++; // Use different address for each test
                }
                
                if (minWorkingTiming > 0)
                {
                    log?.Invoke($"\nMinimum working timing: {minWorkingTiming}µs");
                    log?.Invoke($"Recommended safe timing: {minWorkingTiming * 2}µs");
                }
                else
                {
                    log?.Invoke("No working timing found in test range");
                }
                
                return minWorkingTiming;
            }
            finally
            {
                SetupTime_us = originalSetup;
                PulseWidth_us = originalPulse;
                HoldTime_us = originalHold;
            }
        }

        /// <summary>
        /// Apply calibrated timing settings
        /// </summary>
        public static void ApplyTimingSettings(int setupTime, int pulseWidth, int holdTime, Action<string>? log = null)
        {
            SetupTime_us = setupTime;
            PulseWidth_us = pulseWidth;
            HoldTime_us = holdTime;
            
            log?.Invoke($"Applied timing: Setup={setupTime}µs, Pulse={pulseWidth}µs, Hold={holdTime}µs");
        }

        /// <summary>
        /// Get current timing settings
        /// </summary>
        public static string GetCurrentTiming()
        {
            return $"Setup={SetupTime_us}µs, Pulse={PulseWidth_us}µs, Hold={HoldTime_us}µs";
        }

        // Existing DiagnosticWriteTest and TestDevice methods remain unchanged...
        // [Keep the existing DiagnosticWriteTest and TestDevice methods here]
    }
}
