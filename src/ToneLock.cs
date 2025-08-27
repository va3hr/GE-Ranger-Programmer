
public static class ToneLock
{
    public static readonly string[] ToneMenuTx = ToneIndexing.CanonicalLabels;
    public static readonly string[] ToneMenuRx = ToneIndexing.CanonicalLabels;

    public static (string tx, string rx) DecodeChannel(
        byte A3, byte A2, byte A1, byte A0,
        byte B3, byte B2, byte B1, byte B0)
    {
        string tx = ToneIndexing.TryDecodeTx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        string rx = ToneIndexing.TryDecodeRx(A0, A1, A2, A3, B0, B1, B2, B3) ?? "0";
        return (tx, rx);
    }
}
