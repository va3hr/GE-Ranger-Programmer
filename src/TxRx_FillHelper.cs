
// TxRx_FillHelper.cs - returns (Tx, Rx) from the 8 bytes of a screen channel.
public static class TxRx_FillHelper
{
    public static (string Tx, string Rx) GetDisplayTones(byte[] image128, int screenCh1to16)
    {
        int[] screenToFile = new int[] { 6, 2, 0, 3, 1, 4, 5, 7, 14, 8, 9, 11, 13, 10, 12, 15 };
        int fileIdx = screenToFile[screenCh1to16 - 1];
        int off = fileIdx * 8;

        byte A3 = image128[off + 0], A2 = image128[off + 1], A1 = image128[off + 2], A0 = image128[off + 3];
        byte B3 = image128[off + 4], B2 = image128[off + 5], B1 = image128[off + 6], B0 = image128[off + 7];

        var (tx, rx) = ToneLock.DecodeChannel(A3, A2, A1, A0, B3, B2, B1, B0);
        return (tx, rx);
    }
    public static (string Tx, string Rx) GetDisplayTones_DebugTxCh1_1000(byte[] image128, int screenCh1to16)
    {
        var (tx, rx) = GetDisplayTones(image128, screenCh1to16);
        if (screenCh1to16 == 1) tx = ToneLock.Cg[12]; // "100.0"
        return (tx, rx);
    }
}
