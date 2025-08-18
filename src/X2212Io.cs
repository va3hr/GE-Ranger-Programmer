using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public static class X2212Io
{
    // Ports: base+0 = DATA, base+1 = STATUS, base+2 = CONTROL
    private const short DATA = 0;
    private const short STAT = 1;
    private const short CTRL = 2;

    // Tunable microsecond delays (roughly match BASIC DELAY=9 loops, but safe margins)
    public static int UsSetup = 2;       // setup between writes
    public static int UsPulse = 3;       // control pulse gaps
    public static int MsStore = 10;      // STORE hold (safe 10ms)

    [DllImport("inpoutx64.dll", EntryPoint = "Inp32")] private static extern short Inp32(short port);
    [DllImport("inpoutx64.dll", EntryPoint = "Out32")] private static extern void  Out32(short port, short data);

    private static void DelayUs(int us)
    {
        if (us <= 0) return;
        long ticks = Stopwatch.Frequency * us / 1_000_000;
        long start = Stopwatch.GetTimestamp();
        while (Stopwatch.GetTimestamp() - start < ticks) { /* spin */ }
    }

    /// <summary>
    /// Programs the 256 nibbles using the exact control sequence from the BASIC tool.
    /// CONTROL values: 4 (idle), 5, 7, 6, 0 — per original code.
    /// </summary>
    public static void ProgramNibbles(ushort baseAddr, byte[] nibs, Action<string>? log = null)
    {
        if (nibs == null || nibs.Length < 256) throw new ArgumentException("Need 256 nibbles");
        short dataPort = (short)(baseAddr + DATA);
        short ctrlPort = (short)(baseAddr + CTRL);

        // Idle
        Out32(ctrlPort, 4);
        DelayUs(UsSetup);

        for (int i = 0; i < 256; i++)
        {
            byte val = (byte)(nibs[i] & 0x0F);

            // OUT LPT, WriteBuffer(I)    ' data nibble
            Out32(dataPort, val);
            DelayUs(UsSetup);

            // OUT LPT+2, 5
            Out32(ctrlPort, 5);
            DelayUs(UsPulse);

            // OUT LPT, I                 ' address on data lines
            Out32(dataPort, (byte)i);
            DelayUs(UsSetup);

            // OUT LPT+2, 7
            Out32(ctrlPort, 7);
            DelayUs(UsPulse);

            // OUT LPT+2, 5
            Out32(ctrlPort, 5);
            DelayUs(UsPulse);

            // OUT LPT+2, 4
            Out32(ctrlPort, 4);
            DelayUs(UsPulse);

            if (log != null && (i % 32 == 31)) log($"Programmed {i+1}/256 nibbles");
        }
    }

    /// <summary>
    /// Verifies the 256 nibbles using the BASIC control/readback sequence.
    /// Returns true if all match; otherwise returns false and sets firstFailIndex.
    /// </summary>
    public static bool VerifyNibbles(ushort baseAddr, byte[] expectedNibs, out int firstFailIndex, Action<string>? log = null)
    {
        if (expectedNibs == null || expectedNibs.Length < 256) throw new ArgumentException("Need 256 nibbles");
        short dataPort = (short)(baseAddr + DATA);
        short statPort = (short)(baseAddr + STAT);
        short ctrlPort = (short)(baseAddr + CTRL);

        Out32(ctrlPort, 4);
        DelayUs(UsSetup);

        for (int i = 0; i < 256; i++)
        {
            // OUT LPT, I
            Out32(dataPort, (byte)i);
            DelayUs(UsSetup);

            // OUT LPT+2, 6
            Out32(ctrlPort, 6);
            DelayUs(UsPulse);

            // INP(LPT+1)/8 AND 15
            int readNib = ((int)Inp32(statPort) / 8) & 0x0F;

            // Back to idle
            Out32(ctrlPort, 4);
            DelayUs(UsPulse);

            int expected = expectedNibs[i] & 0x0F;
            if (readNib != expected)
            {
                firstFailIndex = i;
                log?.Invoke($"Verify mismatch @ {i:000}: read {readNib:X}, expected {expected:X}");
                return false;
            }

            if (log != null && (i % 32 == 31)) log($"Verified {i+1}/256 nibbles");
        }

        firstFailIndex = -1;
        return true;
    }

    /// <summary>
    /// STORE per BASIC: toggle control to 0 then back to idle.
    /// Uses a safe 10ms hold per datasheet.
    /// </summary>
    public static void DoStore(ushort baseAddr, Action<string>? log = null)
    {
        short ctrlPort = (short)(baseAddr + CTRL);
        Out32(ctrlPort, 0);
        Thread.Sleep(MsStore);
        Out32(ctrlPort, 4);
        Thread.Sleep(1);
        log?.Invoke("STORE pulse complete");
    }

    /// <summary>
    /// RECALL (if wired): pulse and wait a short time.
    /// We don't know the exact control value for recall from the BASIC, so this is a no-op placeholder.
    /// </summary>
    public static void DoRecall(ushort baseAddr, Action<string>? log = null)
    {
        // Placeholder — not used in the BASIC file
        log?.Invoke("RECALL not defined in BASIC sequence (no-op).");
    }

    public static byte[] ExpandToNibbles(byte[] data128)
    {
        if (data128 == null || data128.Length < 128) throw new ArgumentException("Need 128 bytes");
        var nibs = new byte[256];
        for (int i = 0; i < 128; i++)
        {
            byte b = data128[i];
            nibs[i*2    ] = (byte)((b >> 4) & 0x0F);
            nibs[i*2 + 1] = (byte)( b       & 0x0F);
        }
        return nibs;
    }
}
