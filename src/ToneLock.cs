// Put this struct near the top of ToneLock (above the dictionaries)
public struct TxNibblePattern
{
    // These two must be preserved when writing back, even though they don't
    // participate in the tone label itself.
    public byte Housekeeping0Low;   // formerly “E8L” on CH1 layout
    public byte Housekeeping1Low;   // formerly “EDL” on CH1 layout

    // These two define the TX tone code: code = (ToneCodeHiLow << 4) | ToneCodeLoLow
    public byte ToneCodeHiLow;      // EE.low  (high nibble of the 8-bit code)
    public byte ToneCodeLoLow;      // EF.low  (low  nibble of the 8-bit code)

    public TxNibblePattern(byte housekeeping0Low, byte housekeeping1Low, byte toneCodeHiLow, byte toneCodeLoLow)
    {
        Housekeeping0Low = housekeeping0Low;
        Housekeeping1Low = housekeeping1Low;
        ToneCodeHiLow    = toneCodeHiLow;
        ToneCodeLoLow    = toneCodeLoLow;
    }
}

// Helpful labels for diagnostics/UI
public static readonly string[] TransmitPatternSourceNames =
{
    "Housekeeping0.low",
    "Housekeeping1.low",
    "ToneCodeHi.low (EE.low)",
    "ToneCodeLo.low (EF.low)"
};

// label → full nibble pattern (HK0, HK1, EE.low, EF.low)
private static readonly System.Collections.Generic.Dictionary<string, TxNibblePattern> ToneToTxPattern =
    new System.Collections.Generic.Dictionary<string, TxNibblePattern>(System.StringComparer.Ordinal)
{
    { "67.0",  new TxNibblePattern(7, 4, 1, 1) },
    { "71.9",  new TxNibblePattern(6,13,13, 3) },
    { "74.4",  new TxNibblePattern(6, 0, 2, 0) },
    { "77.0",  new TxNibblePattern(8, 4, 8, 4) },
    { "79.7",  new TxNibblePattern(6, 4,11, 5) },
    { "82.5",  new TxNibblePattern(6, 0, 1, 5) },
    { "85.4",  new TxNibblePattern(6, 6, 6, 6) },
    { "88.5",  new TxNibblePattern(6, 1,12, 7) },
    { "91.5",  new TxNibblePattern(6, 0, 3, 7) },
    { "94.8",  new TxNibblePattern(9, 8, 9, 8) },
    { "97.4",  new TxNibblePattern(6, 0, 3, 8) },
    { "100.0", new TxNibblePattern(12, 9,12, 9) },
    { "103.5", new TxNibblePattern(6, 0, 3, 9) },
    { "107.2", new TxNibblePattern(11,10,11,10) },
    { "110.9", new TxNibblePattern(6, 0, 3,10) },
    { "114.8", new TxNibblePattern(12,11,12,11) },
    { "118.8", new TxNibblePattern(6, 4, 4,11) },
    { "123.0", new TxNibblePattern(13,12,13,12) },
    { "127.3", new TxNibblePattern(6,12, 6,12) },
    { "131.8", new TxNibblePattern(15,13,15,13) },
    { "136.5", new TxNibblePattern(6, 0, 9,13) },
    { "141.3", new TxNibblePattern(6,13, 3,13) },
    { "146.2", new TxNibblePattern(13,14,13,14) },
    { "151.4", new TxNibblePattern(6,14, 7,14) },
    { "156.7", new TxNibblePattern(6, 0, 1,14) },
    { "162.2", new TxNibblePattern(12,15,12,15) },
    { "167.9", new TxNibblePattern(6,15, 7,15) },
    { "173.8", new TxNibblePattern(6,15, 2,15) },
    { "179.9", new TxNibblePattern(7, 0,13, 0) },
    { "186.2", new TxNibblePattern(7, 4, 8, 0) },
    { "192.8", new TxNibblePattern(7, 0, 1, 0) },
    { "203.5", new TxNibblePattern(7, 0,13, 1) },
    { "210.7", new TxNibblePattern(7, 4, 8, 1) },
};

// Build/read helpers with descriptive parameter names

// Returns the 8-bit TX code from EE/EF low nibbles.
public static byte BuildTransmitCodeFromToneCodeNibbles(byte toneCodeHiLow, byte toneCodeLoLow)
{
    return (byte)(((toneCodeHiLow & 0x0F) << 4) | (toneCodeLoLow & 0x0F));
}

// Returns label from EE/EF low nibbles.
public static string GetTransmitToneLabelFromToneCodeNibbles(byte toneCodeHiLow, byte toneCodeLoLow)
{
    byte code = BuildTransmitCodeFromToneCodeNibbles(toneCodeHiLow, toneCodeLoLow);
    string label;
    return TxCodeToTone.TryGetValue(code, out label) ? label : "Err";
}

// Given a label, returns the four low nibbles to write back (HK0, HK1, EE.low, EF.low).
public static bool TryEncodeTransmitNibbles(
    string label,
    out byte housekeeping0Low,
    out byte housekeeping1Low,
    out byte toneCodeHiLow,
    out byte toneCodeLoLow)
{
    housekeeping0Low = housekeeping1Low = toneCodeHiLow = toneCodeLoLow = 0;
    TxNibblePattern p;
    if (!ToneToTxPattern.TryGetValue(label, out p)) return false;
    housekeeping0Low = p.Housekeeping0Low;
    housekeeping1Low = p.Housekeeping1Low;
    toneCodeHiLow    = p.ToneCodeHiLow;
    toneCodeLoLow    = p.ToneCodeLoLow;
    return true;
}

// For diagnostics: compute code+label from the four nibbles you just read
public static (byte Code, string Label) InspectTransmitFromNibbles(
    byte housekeeping0Low,
    byte housekeeping1Low,
    byte toneCodeHiLow,
    byte toneCodeLoLow)
{
    byte code = BuildTransmitCodeFromToneCodeNibbles(toneCodeHiLow, toneCodeLoLow);
    string label;
    if (!TxCodeToTone.TryGetValue(code, out label)) label = "Err";
    return (code, label);
}
