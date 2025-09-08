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
        private const byte CTRL_IDLE = 4;      // Safe idle state
        private const byte CTRL_WRITE1 = 5;    // Write phase 1
        private const byte CTRL_WRITE2 = 7;    // Write phase 2
        private const byte CTRL_READ = 6;      // Read enable
        private const byte CTRL_STORE = 0;     // Store to EEPROM

        // Tunable microsecond delays (safe margins for Windows)
        public static int UsSetup = 2;       // address/data setup
        public static int UsPulse = 3;       // control pulse gaps
        public static int MsStore = 10;      // STORE hold time

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
            if (nibs == null || nibs.Length < 256) 
                throw new ArgumentException("Need 256 nibbles");
            
            short dataPort = (short)(baseAddr + DATA);
            short ctrlPort = (short)(baseAddr + CTRL);

            // Start in idle
            Out32(ctrlPort, CTRL_IDLE);
            DelayUs(UsSetup);

            for (int i = 0; i < 256; i++)
            {
                byte val = (byte)(nibs[i] & 0x0F);

                // Output data nibble
                Out32(dataPort, val);
                DelayUs(UsSetup);

                // Pulse control for write
                Out32(ctrlPort, CTRL_WRITE1);
                DelayUs(UsPulse);

                // Output address
                Out32(dataPort, (byte)i);
                DelayUs(UsSetup);

                // Complete write cycle
                Out32(ctrlPort, CTRL_WRITE2);
                DelayUs(UsPulse);

                Out32(ctrlPort, CTRL_WRITE1);
                DelayUs(UsPulse);

                Out32(ctrlPort, CTRL_IDLE);
                DelayUs(UsPulse);

                // Progress reporting
                if (log != null && (i % 32 == 31)) 
                    log($"Programmed {i+1}/256 nibbles");
            }
            
            log?.Invoke("Programming complete");
        }

        /// <summary>
        /// Verifies the 256 nibbles against expected values.
        /// </summary>
        public static bool VerifyNibbles(ushort baseAddr, byte[] expectedNibs, 
                                        out int firstFailIndex, Action<string>? log = null)
        {
            if (expectedNibs == null || expectedNibs.Length < 256) 
                throw new ArgumentException("Need 256 nibbles");
            
            short dataPort = (short)(baseAddr + DATA);
            short statPort = (short)(baseAddr + STAT);
            short ctrlPort = (short)(baseAddr + CTRL);

            Out32(ctrlPort, CTRL_IDLE);
            DelayUs(UsSetup);

            for (int i = 0; i < 256; i++)
            {
                // Set address
                Out32(dataPort, (byte)i);
                DelayUs(UsSetup);

                // Enable read
                Out32(ctrlPort, CTRL_READ);
                DelayUs(UsPulse);

                // Read nibble from status port (upper 4 bits)
                int readNib = ((int)Inp32(statPort) >> 3) & 0x0F;

                // Return to idle
                Out32(ctrlPort, CTRL_IDLE);
                DelayUs(UsPulse);

                int expected = expectedNibs[i] & 0x0F;
                if (readNib != expected)
                {
                    firstFailIndex = i;
                    log?.Invoke($"Verify mismatch @ nibble {i}: read {readNib:X}, expected {expected:X}");
                    return false;
                }

                if (log != null && (i % 32 == 31)) 
                    log($"Verified {i+1}/256 nibbles");
            }

            firstFailIndex = -1;
            log?.Invoke("Verify complete - all match");
            return true;
        }

        /// <summary>
        /// Non-destructive read of all 256 nibbles.
        /// </summary>
        public static byte[] ReadAllNibbles(ushort baseAddr, Action<string>? log = null)
        {
            short dataPort = (short)(baseAddr + DATA);
            short statPort = (short)(baseAddr + STAT);
            short ctrlPort = (short)(baseAddr + CTRL);

            var nibs = new byte[256];

            Out32(ctrlPort, CTRL_IDLE);
            DelayUs(UsSetup);

            for (int i = 0; i < 256; i++)
            {
                // Set address
                Out32(dataPort, (byte)i);
                DelayUs(UsSetup);

                // Enable read
                Out32(ctrlPort, CTRL_READ);
                DelayUs(UsPulse);

                // Read nibble from status port
                int readNib = ((int)Inp32(statPort) >> 3) & 0x0F;
                nibs[i] = (byte)readNib;

                // Return to idle
                Out32(ctrlPort, CTRL_IDLE);
                DelayUs(UsPulse);

                if (log != null && (i % 32 == 31)) 
                    log($"Read {i+1}/256 nibbles");
            }

            log?.Invoke("Read complete");
            return nibs;
        }

        /// <summary>
        /// Probe for X2212 presence by reading multiple addresses.
        /// </summary>
        public static bool ProbeDevice(ushort baseAddr, out string reason, Action<string>? log = null)
        {
            short dataPort = (short)(baseAddr + DATA);
            short statPort = (short)(baseAddr + STAT);
            short ctrlPort = (short)(baseAddr + CTRL);

            try
            {
                // Test addresses
                int[] testAddrs = { 0x00, 0x01, 0x02, 0x03, 0x0F, 0x1F, 0x3F, 0x7F, 0xFF };
                var pass1 = new List<int>();
                var pass2 = new List<int>();

                // Safe idle
                Out32(ctrlPort, CTRL_IDLE);
                DelayUs(UsSetup);

                // Read test addresses twice
                foreach (var addr in testAddrs)
                {
                    Out32(dataPort, (byte)addr);
                    DelayUs(UsSetup);
                    Out32(ctrlPort, CTRL_READ);
                    DelayUs(UsPulse);
                    int nib = ((int)Inp32(statPort) >> 3) & 0x0F;
                    Out32(ctrlPort, CTRL_IDLE);
                    DelayUs(UsPulse);
                    pass1.Add(nib);
                }

                // Second pass
                foreach (var addr in testAddrs)
                {
                    Out32(dataPort, (byte)addr);
                    DelayUs(UsSetup);
                    Out32(ctrlPort, CTRL_READ);
                    DelayUs(UsPulse);
                    int nib = ((int)Inp32(statPort) >> 3) & 0x0F;
                    Out32(ctrlPort, CTRL_IDLE);
                    DelayUs(UsPulse);
                    pass2.Add(nib);
                }

                // Check for consistency
                bool stable = true;
                for (int i = 0; i < testAddrs.Length; i++)
                {
                    if (pass1[i] != pass2[i])
                    {
                        stable = false;
                        break;
                    }
                }

                // Check for variety (not all same value)
                var distinct = new HashSet<int>(pass1);
                bool varied = distinct.Count >= 2;

                if (stable && varied)
                {
                    reason = $"Stable reads with {distinct.Count} distinct values";
                    return true;
                }
                else if (!stable)
                {
                    reason = "Unstable reads between passes";
                    return false;
                }
                else
                {
                    reason = $"Stuck value (all read as 0x{pass1[0]:X})";
                    return false;
                }
            }
            catch (Exception ex)
            {
                reason = $"Exception: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// STORE command - transfers RAM to EEPROM
        /// </summary>
        public static void DoStore(ushort baseAddr, Action<string>? log = null)
        {
            short ctrlPort = (short)(baseAddr + CTRL);
            
            log?.Invoke("Initiating STORE cycle...");
            
            // Pulse control to 0 for STORE
            Out32(ctrlPort, CTRL_STORE);
            Thread.Sleep(MsStore); // Hold for store time
            
            // Return to idle
            Out32(ctrlPort, CTRL_IDLE);
            Thread.Sleep(1);
            
            log?.Invoke("STORE complete");
        }

        /// <summary>
        /// RECALL command - transfers EEPROM to RAM (automatic on power-up)
        /// </summary>
        public static void DoRecall(ushort baseAddr, Action<string>? log = null)
        {
            // X2212 does recall automatically on power-up
            // Manual recall might not be needed, but we can toggle power if supported
            log?.Invoke("RECALL: X2212 recalls automatically on power-up");
        }

        // Helper methods for nibble/byte conversion
        
        /// <summary>
        /// Expand 128 bytes to 256 nibbles
        /// </summary>
        public static byte[] ExpandToNibbles(byte[] data128)
        {
            if (data128 == null || data128.Length < 128) 
                throw new ArgumentException("Need 128 bytes");
            
            var nibs = new byte[256];
            for (int i = 0; i < 128; i++)
            {
                byte b = data128[i];
                nibs[i * 2] = (byte)((b >> 4) & 0x0F);      // High nibble
                nibs[i * 2 + 1] = (byte)(b &
