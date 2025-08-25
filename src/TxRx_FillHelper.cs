
// TxRx_FillHelper.cs - one-call helper to get display strings for a channel.
// Call from your UI code right before you write to the grid/combos.
public static class TxRx_FillHelper
{
    // image128: 128-byte RGR image
    // screenCh1to16: channel number in the DOS/UI screen order (1..16)
    public static (string Tx, string Rx) GetDisplayTones(byte[] image128, int screenCh1to16)
    {
        // Screen->File permutation (0-based)
        int[] screenToFile = new int[] { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };
        int fileIdx = screenToFile[screenCh1to16 - 1];
        int off = fileIdx * 8;

        byte A3 = image128[off + 0], A2 = image128[off + 1], A1 = image128[off + 2], A0 = image128[off + 3];
        byte B3 = image128[off + 4], B2 = image128[off + 5], B1 = image128[off + 6], B0 = image128[off + 7];

        var (tx, rx) = ToneLock.DecodeChannel(A3, A2, A1, A0, B3, B2, B1, B0);
        return (tx, rx);
    }

    // DEBUG: hard-code TX for screen channel 1 to "100.0". Remove after UI path is verified.
    public static (string Tx, string Rx) GetDisplayTones_DebugTxCh1_1000(byte[] image128, int screenCh1to16)
    {
        var (tx, rx) = GetDisplayTones(image128, screenCh1to16);
        if (screenCh1to16 == 1) tx = ToneLock.Cg[12]; // "100.0"
        return (tx, rx);
    }
}
