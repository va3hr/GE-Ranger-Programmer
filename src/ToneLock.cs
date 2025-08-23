#nullable enable
using System;
using System.Text;

namespace RangrApp.Locked
{
    // Facade preserved for RgrCodec/MainForm. Delegates to TxToneLock/RxToneLock.
    public static class ToneLock
    {
        public static readonly string[] ToneMenuTx = TxToneLock.ToneMenuTx;
        public static readonly string[] ToneMenuRx = RxToneLock.ToneMenuRx;
        public static readonly string[] ToneMenuAll = ToneMenuTx; // UI binds to this

        // ---- Decode wrappers ----
        public static (string Tx, string Rx) DecodeChannel(
            byte A3, byte A2, byte A1, byte A0,
            byte B3, byte B2, byte B1, byte B0)
        {
            var tx = TxToneLock.TxToneFromBytes(B0, B2, B3);
            var rx = RxToneLock.RxToneFromBytes(A3, B3, tx);
            return (tx, rx);
        }

        public static string TxToneFromBytes(byte B0, byte B2, byte B3) => TxToneLock.TxToneFromBytes(B0,B2,B3);
        public static string TxToneFromBytes(byte A1, byte B1) => TxToneLock.TxToneFromBytes(A1,B1);
        public static string RxToneFromBytes(byte A3, byte A2, byte B3) => RxToneLock.RxToneFromBytes(A3,A2,B3);
        public static string RxToneFromBytes(byte A3, byte B3, string txTone) => RxToneLock.RxToneFromBytes(A3,B3,txTone);

        // ---- Encode wrappers ----
        public static bool TrySetTxTone(ref byte B0, ref byte B2, ref byte B3, string? display)
            => TxToneLock.TrySetTxTone(ref B0, ref B2, ref B3, display);
        public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte B3, string? display)
            => RxToneLock.TrySetRxTone(ref A3, ref A2, ref B3, display);

        // ---- Utilities some call sites reference ----
        public static void SetLastChannel(int ch) { /* no-op status shim */ }
        public static string ToAsciiHex256(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length*3);
            for (int i=0;i<bytes.Length;i++) { if (i>0) sb.Append(' '); sb.Append(bytes[i].ToString("X2")); }
            return sb.ToString();
        }
        public static byte[] ToX2212Nibbles(byte[] image128)
        {
            var outbuf = new byte[image128.Length*2];
            for (int i=0,j=0;i<image128.Length;i++,j+=2) { byte b=image128[i]; outbuf[j]=(byte)((b>>4)&0xF); outbuf[j+1]=(byte)(b&0xF);}
            return outbuf;
        }

        // Display helpers
        public static string TxIndexToDisplay(int i) => (i>=0 && i<ToneMenuTx.Length) ? ToneMenuTx[i] : "?";
        public static string RxIndexToDisplay(int i) => (i>=0 && i<ToneMenuRx.Length) ? ToneMenuRx[i] : "?";
    }
}
