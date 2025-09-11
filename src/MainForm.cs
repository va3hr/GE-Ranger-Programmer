// Replace the existing HexGrid_CellEndEdit method in MainForm.Events.cs with this version:

private void HexGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
{
    if (hexGrid == null) return;
    
    // CRITICAL FIX: Proper bounds checking to prevent crashes
    if (e.RowIndex < 0 || e.RowIndex >= hexGrid.Rows.Count) return;
    if (e.ColumnIndex < 0 || e.ColumnIndex >= 8) return; // Only 8 hex columns
    
    try
    {
        SaveUndoState();
        
        var cell = hexGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (cell?.Value == null) 
        {
            // If cell is null, restore original value
            UpdateHexDisplay();
            return;
        }
        
        string input = cell.Value.ToString() ?? "";
        
        // Parse the hex value
        if (byte.TryParse(input, NumberStyles.HexNumber, null, out byte val))
        {
            // CRITICAL: Use the channel mapping to find the correct data offset
            int channel = e.RowIndex + 1;  // Channel 1-16
            int startAddress = ChannelToAddress[channel];
            
            // Calculate the offset in our 128-byte array using the mapping function
            int dataOffset = GetDataOffsetForAddress(startAddress);
            int finalOffset = dataOffset + e.ColumnIndex;
            
            // Ensure we stay within bounds
            if (finalOffset >= 128) finalOffset = finalOffset % 128;
            
            if (finalOffset >= 0 && finalOffset < _currentData.Length)
            {
                if (_currentData[finalOffset] != val)
                {
                    _currentData[finalOffset] = val;
                    _dataModified = true;
                    LogMessage($"Modified Ch{channel} byte {e.ColumnIndex} (address {(startAddress + e.ColumnIndex):X2}): {val:X2}");
                }
                cell.Value = $"{val:X2}";
                
                // Defer the ASCII update to avoid reentrant calls
                BeginInvoke(new Action(() => UpdateAsciiForRowFixed(e.RowIndex)));
            }
        }
        else
        {
            // Revert to original value by refreshing display
            UpdateHexDisplay();
            LogMessage("Invalid hex value - reverted");
        }
    }
    catch (Exception ex)
    {
        LogMessage($"Cell edit error: {ex.Message}");
        // In case of any error, refresh the display
        try
        {
            BeginInvoke(new Action(() => UpdateHexDisplay()));
        }
        catch
        {
            // Silent fail on recovery attempt
        }
    }
}

// Also replace UpdateAsciiForRow with this version that uses the channel mapping:
private void UpdateAsciiForRowFixed(int row)
{
    if (hexGrid?.Rows == null || row < 0 || row >= hexGrid.Rows.Count) return;
    
    try
    {
        int channel = row + 1;  // Channel 1-16
        int startAddress = ChannelToAddress[channel];
        int dataOffset = GetDataOffsetForAddress(startAddress);
        
        StringBuilder ascii = new StringBuilder(8);
        for (int col = 0; col < 8; col++)
        {
            int finalOffset = dataOffset + col;
            if (finalOffset >= 128) finalOffset = finalOffset % 128;
            
            byte val = _currentData[finalOffset];
            char c = (val >= 32 && val <= 126) ? (char)val : '.';
            ascii.Append(c);
        }
        
        var targetRow = hexGrid.Rows[row];
        if (targetRow?.Cells != null && targetRow.Cells.Count > 8)
        {
            var asciiCell = targetRow.Cells[targetRow.Cells.Count - 1];
            if (asciiCell != null)
                asciiCell.Value = ascii.ToString();
        }
    }
    catch
    {
        // Ignore ASCII update errors
    }
}
