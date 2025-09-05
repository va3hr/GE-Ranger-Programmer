using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
namespace RangrApp.Locked
{
public static class X2212Io
{
    // Ports: base+0 = DATA, base+1 = STATUS, base+2 = CONTROL
    private const short DATA = 0;
    private const short STAT = 1;
    private const short CTRL = 2;

    // Tunable microsecond delays (safe margins for Windows jitter)
    public static int UsSetup = 2;       // address/data setup
    public static int UsPulse = 3;       // control pulse gaps
    public static int MsStore = 10;      // STORE hold

    [DllImport("inpoutx64.dll", EntryPoint = "Inp32")] private static extern short Inp32(short port);
    [DllImport("inpoutx64.dll", EntryPoint = "Out32")] private static extern void  Out32(short port, short data);

    private static void DelayUs(int us)
    {
        if (us <= 0) return;
        long ticks = Stopwatch.Frequency * us / 1_000_000;
        long start = Stopwatch.GetTimestamp();
        while (Stopwatch.GetTimestamp() - start < ticks) { /* spin */ }
    }

    // --------------------- BASIC-faithful sequences ---------------------

    /// <summary>
    /// Programs the 256 nibbles using the BASIC control sequence.
    /// CONTROL values per original code: idle=4, write path: 5 -> 7 -> 5 -> 4
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

            Out32(dataPort, val);
            DelayUs(UsSetup);

            Out32(ctrlPort, 5);
            DelayUs(UsPulse);

            Out32(dataPort, (byte)i);
            DelayUs(UsSetup);

            Out32(ctrlPort, 7);
            DelayUs(UsPulse);

            Out32(ctrlPort, 5);
            DelayUs(UsPulse);

            Out32(ctrlPort, 4);
            DelayUs(UsPulse);

            if (log != null && (i % 32 == 31)) log($"Programmed {i+1}/256 nibbles");
        }
    }

    /// <summary>
    /// Verifies the 256 nibbles using the BASIC readback sequence.
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
            Out32(dataPort, (byte)i);
            DelayUs(UsSetup);

            Out32(ctrlPort, 6);
            DelayUs(UsPulse);

            int readNib = ((int)Inp32(statPort) / 8) & 0x0F;

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
    /// Non-destructive read of all 256 nibbles (safe). 
    /// Matches the BASIC verify path but stores values instead of comparing.
    /// </summary>
    public static byte[] ReadAllNibbles(ushort baseAddr, Action<string>? log = null)
    {
        short dataPort = (short)(baseAddr + DATA);
        short statPort = (short)(baseAddr + STAT);
        short ctrlPort = (short)(baseAddr + CTRL);

        var nibs = new byte[256];

        Out32(ctrlPort, 4);
        DelayUs(UsSetup);

        for (int i = 0; i < 256; i++)
        {
            Out32(dataPort, (byte)i);
            DelayUs(UsSetup);

            Out32(ctrlPort, 6);
            DelayUs(UsPulse);

            int readNib = ((int)Inp32(statPort) / 8) & 0x0F;
            nibs[i] = (byte)readNib;

            Out32(ctrlPort, 4);
            DelayUs(UsPulse);

            if (log != null && (i % 32 == 31)) log($"Read {i+1}/256 nibbles");
        }

        return nibs;
    }

    /// <summary>
    /// Non-destructive presence probe.
    /// Reads a set of addresses twice and looks for stable, non-stuck values.
    /// Heuristic: if two passes match AND at least two distinct nibble values are seen (not all 0x0 or 0xF),
    /// the device is likely present.
    /// </summary>
    public static bool ProbeDevice(ushort baseAddr, out string reason, Action<string>? log = null)
    {
        short dataPort = (short)(baseAddr + DATA);
        short statPort = (short)(baseAddr + STAT);
        short ctrlPort = (short)(baseAddr + CTRL);

        int[] addrs = new int[] { 0x00,0x01,0x02,0x03, 0x0F,0x1F,0x3F,0x7F,0xBF,0xFF };
        var pass1 = new List<int>();
        var pass2 = new List<int>();

        Func<int,int> readNib = (addr) =>
        {
            Out32(dataPort, (short)addr);
            DelayUs(UsSetup);
            Out32(ctrlPort, 6);
            DelayUs(UsPulse);
            int nib = ((int)Inp32(statPort) / 8) & 0x0F;
            Out32(ctrlPort, 4);
            DelayUs(UsPulse);
            return nib;
        };

        foreach (var a in addrs) pass1.Add(readNib(a));
        foreach (var a in addrs) pass2.Add(readNib(a));

        bool stable = true;
        for (int i = 0; i < addrs.Length; i++)
            if (pass1[i] != pass2[i]) { stable = false; break; }

        var distinct = new HashSet<int>(pass1);
        bool stuckAllZeroOrF = distinct.Count == 1 && (distinct.Contains(0x0) || distinct.Contains(0xF));

        if (stable && distinct.Count >= 2 && !stuckAllZeroOrF)
        {
            reason = $"stable reads; {distinct.Count} distinct values across addresses";
            return true;
        }
        else
        {
            reason = $"unstable or stuck values (distinct={distinct.Count})";
            return false;
        }
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
    /// RECALL: placeholder (wasn't in the BASIC listing). 
    /// </summary>
    public static void DoRecall(ushort baseAddr, Action<string>? log = null)
    {
        log?.Invoke("RECALL not defined in BASIC sequence (no-op).");
    }

    // --------------------- Helpers ---------------------

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

    public static byte[] CompressNibblesToBytes(byte[] nibs256)
    {
        if (nibs256 == null || nibs256.Length < 256) throw new ArgumentException("Need 256 nibbles");
        var bytes = new byte[128];
        for (int i = 0; i < 128; i++)
        {
            int hi = nibs256[i*2] & 0x0F;
            int lo = nibs256[i*2 + 1] & 0x0F;
            bytes[i] = (byte)((hi << 4) | lo);
        }
        return bytes;
    }
}

}
