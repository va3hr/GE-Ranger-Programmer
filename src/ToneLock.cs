using System;
using System.Collections.Generic;

public static class ToneLock
{
    // Known-good low-5-bit → tone mappings seen in your new DOS file/photo.
    // We only assert tones we’re sure about; anything else becomes "?".
    private static readonly Dictionary<int, string> Low5ToTone = new()
    {
        { 3, "162.2" },  // CH8
        { 7, "107.2" },  // CH9
        { 5, "127.3" },  // CH16
    };

    /// <summary>
    /// Compute the Rx tone string for one channel.
    /// Rules:
    ///  - If B2.low5 == 0 → treat as "no explicit RX index".
    ///      In that case, if TX has a non-zero tone, mirror TX (this yields 131.8 on CH1).
    ///      If TX is zero → return "0".
    ///  - If B2.low5 != 0 and we recognize the index → return mapped tone.
    ///  - Otherwise → return "?" (unknown/unsupported).
    /// This avoids ever showing a *wrong* tone.
    /// </summary>
    public static string RxTone(byte A0, byte A1, byte A2, byte A3,
                                byte B0, byte B1, byte B2, byte B3,
                                string txTone /* already "0", "?", or a real tone */)
    {
        int idx = B2 & 0x1F;

        if (idx == 0)
        {
            // No explicit RX index stored. If TX has a real tone, mirror it (CH1 case).
            // Otherwise, this is truly "no tone".
            return (txTone != null && txTone != "0" && txTone != "?") ? txTone : "0";
        }

        if (Low5ToTone.TryGetValue(idx, out var tone))
            return tone;

        // Index present but not one we trust → unknown.
        return "?";
    }

    /// <summary>
    /// Ensure the chosen tone exists in the grid’s combo menu.
    /// If not, return "?" so the cell stays valid and never pops an error dialog.
    /// </summary>
    public static string CoerceToMenu(string tone, string[] menu)
    {
        if (string.IsNullOrWhiteSpace(tone)) return "0";
        foreach (var item in menu)
            if (item == tone) return tone;
        return "?";
    }
}
2) Use it in MainForm.cs (only change the Rx-tone assignment)
In your existing PopulateGridFromLogical loop, keep everything the same except replace the Rx-tone lines with this block:

csharp
Copy
Edit
// TX tone (existing logic)
string txTone = SafeTxTone(A2, B3);
_grid.Rows[ch].Cells[3].Value = txTone;

// RX tone via ToneLock (mirrors TX when B2.low5==0, else mapped, else "?")
var rxMenu = ((DataGridViewComboBoxColumn)_grid.Columns[4]).DataSource as string[] ?? Array.Empty<string>();
string rxToneRaw = ToneLock.RxTone(A0, A1, A2, A3, B0, B1, B2, B3, txTone);
string rxTone = ToneLock.CoerceToMenu(rxToneRaw, rxMenu);
_grid.Rows[ch].Cells[4].Value = rxTone;
