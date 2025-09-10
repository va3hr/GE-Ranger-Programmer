using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace GE_Ranger_Programmer
{
    public static class X2212Io
    {
        // Ports: base+0 = DATA, base+1 = STATUS, base+2 = CONTROL
        private const short DATA = 0;
        private const short STAT = 1;
        private const short CTRL = 2;

        // Control register states (based on your BASIC code)
        // More conservative timing for reliable programming

        private const byte CTRL_IDLE = 4;      // Safe idle state
        private const byte CTRL_WRITE1 = 5;    // Write phase 1
        private const byte CTRL_WRITE2 = 7;    // Write phase 2
        private const byte CTRL_READ = 6;      // Read enable
        private const byte CTRL_STORE = 0;     // Store to EEPROM

        // Tunable microsecond delays (safe margins for Windows)
        public static int UsSetup = 10;      // address/data setup
        public static int UsPulse = 10;      // Iontrol pulse gaps
        public static int MsStore = 15;      // STORE hold time
        
      
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")] 
        private static extern short Inp32(short port);
        
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")] 
        private static extern void Out32(short port, short data);

        private static void DelayUs(int us)
        {
            if (us <= 0) return;
            long ticks = Stopwatch.Frequency * us / 1_000_000;
            long start = Stopwatch.GetTimestamp();
            while (Stopwatch.GetTimestamp() - start < ticks) { /* spin */ }
        }

        /// <summary>
        /// Set the port to safe idle state
        /// </summary>
        public static void SetIdle(ushort baseAddr)
        {
            short ctrlPort = (short)(baseAddr + CTRL);
            Out32(ctrlPort, CTRL_IDLE);
        }

        /// <summary>
        /// Programs the 256 nibbles using the BASIC control sequence.
        /// </summary>
        public static void ProgramNibbles(ushort baseAddr, byte[] nibs, Action<string>? log = null)
        {
            if (nibs.Length != 256)
                throw new ArgumentException("Expected 256 nibbles");

            short dataPort = (short)(baseAddr + DATA);
            short ctrlPort = (short)(baseAddr + CTRL);

            log?.Invoke("Starting program sequence...");

            for (int i = 0; i < 256; i++)
            {
                byte addr = (byte)i;
                byte data = (byte)(nibs[i] & 0x0F);

                // Set address and data
                Out32(dataPort, (short)(addr | (data << 8)));
                DelayUs(UsSetup);

                // Write pulse sequence
                Out32(ctrlPort, CTRL_WRITE1);
                DelayUs(UsPulse);
                Out32(ctrlPort, CTRL_WRITE2);
                DelayUs(UsPulse);
                Out32(ctrlPort, CTRL_IDLE);
                DelayUs(UsPulse);

                if ((i % 32) == 31)
                    log?.Invoke($"Programmed {i + 1}/256 nibbles");
            }

            log?.Invoke("Program sequence completed");
        }

        /// <summary>
        /// Reads all 256 nibbles from the device.
        /// </summary>
        public static byte[] ReadAllNibbles(ushort baseAddr, Action<string>? log = null)
        {
            short dataPort = (short)(baseAddr + DATA);
            short statPort = (short)(baseAddr + STAT);
            short ctrlPort = (short)(baseAddr + CTRL);
            
            byte[] nibbles = new byte[256];

            log?.Invoke("Starting read sequence...");

            for (int i = 0; i < 256; i++)
            {
                // Set address
                Out32(dataPort, (short)i);
                DelayUs(UsSetup);

                // Enable read
                Out32(ctrlPort, CTRL_READ);
                DelayUs(UsPulse);

                // Read data from status port (upper 4 bits)
                short status = Inp32(statPort);
                nibbles[i] = (byte)((status >> 4) & 0x0F);

                // Return to idle
                Out32(ctrlPort, CTRL_IDLE);
                DelayUs(UsPulse);

                if ((i % 32) == 31)
                    log?.Invoke($"Read {i + 1}/256 nibbles");
            }

            log?.Invoke("Read sequence completed");
            return nibbles;
        }

        /// <summary>
        /// Verifies nibbles against device content.
        /// </summary>
        public static bool VerifyNibbles(ushort baseAddr, byte[] expectedNibbles, out int failIndex, Action<string>? log = null)
        {
            failIndex = -1;
            if (expectedNibbles.Length != 256)
                throw new ArgumentException("Expected 256 nibbles");

            log?.Invoke("Starting verify sequence...");

            byte[] readNibbles = ReadAllNibbles(baseAddr, null);

            for (int i = 0; i < 256; i++)
            {
                if ((readNibbles[i] & 0x0F) != (expectedNibbles[i] & 0x0F))
                {
                    failIndex = i;
                    log?.Invoke($"Verify failed at nibble {i}: expected {expectedNibbles[i]:X}, got {readNibbles[i]:X}");
                    return false;
                }

                if ((i % 32) == 31)
                    log?.Invoke($"Verified {i + 1}/256 nibbles");
            }

            log?.Invoke("Verify sequence completed - all nibbles match");
            return true;
        }

        /// <summary>
        /// Sends the STORE command to save RAM to EEPROM.
        /// </summary>
        public static void DoStore(ushort baseAddr, Action<string>? log = null)
        {
            short ctrlPort = (short)(baseAddr + CTRL);

            log?.Invoke("Sending STORE command...");

            // STORE pulse
            Out32(ctrlPort, CTRL_STORE);
            Thread.Sleep(MsStore); // Hold for milliseconds
            Out32(ctrlPort, CTRL_IDLE);

            log?.Invoke("STORE command completed");
        }

        /// <summary>
        /// Probes for X2212 device presence.
        /// </summary>
        public static bool ProbeDevice(ushort baseAddr, out string reason, Action<string>? log = null)
        {
            reason = "";
            
            try
            {
                log?.Invoke("Probing device...");

                // Test 1: Try to write and read back a test pattern
                byte[] testPattern = { 0x05, 0x0A, 0x03, 0x0C };
                byte[] readBack = new byte[4];

                short dataPort = (short)(baseAddr + DATA);
                short statPort = (short)(baseAddr + STAT);
                short ctrlPort = (short)(baseAddr + CTRL);

                // Write test pattern to first 4 addresses
                for (int i = 0; i < 4; i++)
                {
                    Out32(dataPort, (short)(i | (testPattern[i] << 8)));
                    DelayUs(UsSetup);
                    Out32(ctrlPort, CTRL_WRITE1);
                    DelayUs(UsPulse);
                    Out32(ctrlPort, CTRL_WRITE2);
                    DelayUs(UsPulse);
                    Out32(ctrlPort, CTRL_IDLE);
                    DelayUs(UsPulse);
                }

                // Read back test pattern
                for (int i = 0; i < 4; i++)
                {
                    Out32(dataPort, (short)i);
                    DelayUs(UsSetup);
                    Out32(ctrlPort, CTRL_READ);
                    DelayUs(UsPulse);
                    short status = Inp32(statPort);
                    readBack[i] = (byte)((status >> 4) & 0x0F);
                    Out32(ctrlPort, CTRL_IDLE);
                    DelayUs(UsPulse);
                }

                // Check if pattern matches
                bool patternMatch = true;
                for (int i = 0; i < 4; i++)
                {
                    if (readBack[i] != testPattern[i])
                    {
                        patternMatch = false;
                        break;
                    }
                }

                if (patternMatch)
                {
                    reason = "Test pattern write/read successful";
                    log?.Invoke("Device probe successful - X2212 detected");
                    return true;
                }
                else
                {
                    reason = "Test pattern mismatch - device not responding correctly";
                    log?.Invoke("Device probe failed - pattern mismatch");
                    return false;
                }
            }
            catch (Exception ex)
            {
                reason = $"Probe error: {ex.Message}";
                log?.Invoke($"Device probe failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts 128 bytes to 256 nibbles (expands each byte to 2 nibbles).
        /// </summary>
        public static byte[] ExpandToNibbles(byte[] bytes)
        {
            if (bytes.Length != 128)
                throw new ArgumentException("Expected 128 bytes");

            byte[] nibbles = new byte[256];
            for (int i = 0; i < 128; i++)
            {
                nibbles[i * 2] = (byte)((bytes[i] >> 4) & 0x0F);     // High nibble
                nibbles[i * 2 + 1] = (byte)(bytes[i] & 0x0F);       // Low nibble
            }
            return nibbles;
        }

        /// <summary>
        /// Converts 256 nibbles to 128 bytes (compresses pairs of nibbles to bytes).
        /// </summary>
        public static byte[] CompressNibblesToBytes(byte[] nibbles)
        {
            if (nibbles.Length != 256)
                throw new ArgumentException("Expected 256 nibbles");

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
        /// Performs a complete read-modify-write cycle for testing.
        /// </summary>
        public static bool TestDevice(ushort baseAddr, Action<string>? log = null)
        {
            try
            {
                log?.Invoke("Starting device test...");

                // Read current content
                byte[] originalNibbles = ReadAllNibbles(baseAddr, log);
                
                // Create test pattern
                byte[] testNibbles = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    testNibbles[i] = (byte)(i & 0x0F);
                }

                // Write test pattern
                ProgramNibbles(baseAddr, testNibbles, log);

                // Verify test pattern
                bool verifyOk = VerifyNibbles(baseAddr, testNibbles, out int failIndex, log);
                
                if (!verifyOk)
                {
                    log?.Invoke($"Test failed during verify at nibble {failIndex}");
                    return false;
                }

                // Restore original content
                ProgramNibbles(baseAddr, originalNibbles, log);

                log?.Invoke("Device test completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"Device test failed: {ex.Message}");
                return false;
            }
        }
    }
}



