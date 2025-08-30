using System;
using RangrApp.Locked; // ToneLock lives here

public static class ToneDiag
{
    // Human‑readable, stable diagnostic row for the grid log.
    // Signature preserved to match MainForm.cs call site.
    public static string Row(
        int fileIdx, int screenRow,
        byte A0, byte A1, byte A2, byte A3,
        byte B0, byte B1, byte B2, byte B3,
        string? txLabelFromUi, string? rxLabelFromUi)
    {
        // Re‑order to the natural decode order we use everywhere: A3..A0, B3..B0
        string bytesA3toB0 =
            $"{A3:X2} {A2:X2} {A1:X2} {A0:X2}  {B3:X2} {B2:X2} {B1:X2} {B0:X2}";

        // Build indices using the locked windows in ToneLock
        int transmitToneIndex = ToneLock.BuildTransmitToneIndex(A3, A2, A1, A0, B3, B2, B1, B0);
        int receiveToneIndex  = ToneLock.BuildReceiveToneIndex(A3);

        // Labels computed strictly from ToneLock.Cg (index 0 == "0")
        string txLabelCalc = (transmitToneIndex >= 0 && transmitToneIndex < ToneLock.Cg.Length)
            ? ToneLock.Cg[transmitToneIndex] : "Err";
        string rxLabelCalc = (receiveToneIndex >= 0 && receiveToneIndex < ToneLock.Cg.Length)
            ? ToneLock.Cg[receiveToneIndex] : "Err";

        // Show source bits explicitly (i5..i0 order)
        string txSourceValues =
            $"{((B3 >> 7) & 1)} {((B3 >> 4) & 1)} {((B0 >> 5) & 1)} {((B0 >> 2) & 1)} {((B0 >> 1) & 1)} {((B0 >> 0) & 1)}";
        string rxSourceValues =
            $"{((A3 >> 6) & 1)} {((A3 >> 7) & 1)} {((A3 >> 0) & 1)} {((A3 >> 1) & 1)} {((A3 >> 2) & 1)} {((A3 >> 3) & 1)}";

        // Helpful extra flags
        string steFlag = (((A3 >> 7) & 1) == 1) ? "Y" : "N";
        int bank = (B3 >> 1) & 1;

        // Compose a friendly diagnostic line
        return
            $"row {screenRow:00} " +
            $"| bytes[A3..B0]={bytesA3toB0} " +
            $"| TX: sources [B3.7 B3.4 B0.5 B0.2 B0.1 B0.0] values(i5..i0)=[{txSourceValues}] index={transmitToneIndex} label={txLabelCalc} " +
            $"| RX: sources [A3.6 A3.7 A3.0 A3.1 A3.2 A3.3] values(i5..i0)=[{rxSourceValues}] index={receiveToneIndex} label={rxLabelCalc} " +
            $"| STE={steFlag} bank={bank}";
    }
}
