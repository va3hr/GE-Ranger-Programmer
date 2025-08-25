
#nullable disable
using System;
using System.Text;

public static class ToneLock
{
    public static readonly string[] ToneMenuTx = new string[] { "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7" };
    public static readonly string[] ToneMenuRx = new string[] { "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8","97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3","131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8","179.9","186.2","192.8","203.5","210.7" };

    public static string ToAsciiHex256(byte[] buf)
    {
        if (buf == null) return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < buf.Length; i++)
        {
            if (i>0) sb.Append(i % 8 == 0 ? " " : " ");
            sb.Append(buf[i].ToString("X2"));
        }
        return sb.ToString().Trim();
    }

    public static byte[] ToX2212Nibbles(byte[] image128) => image128;

    private static int DecodeTxIndex(byte a1, byte a2, byte b1, byte b0)
    {
        if (a1==0x98 && a2==0x27)
        {
            if (b1==0x94 && b0==0xD3) return 2;
            if (b1==0x94 && b0==0xC7) return 8;
            if (b1==0x90 && b0==0xC9) return 12;
            if (b1==0x90 && b0==0x9D) return 21;
            if (b1==0x90 && b0==0x7E) return 24;
            if (b1==0x94 && b0==0x81) return 33;
            return 0;
        }
        if (a1==0x9C && a2==0x27)
        {
            if (b1==0x90 && b0==0x71) return 1;
            if (b1==0x94 && b0==0x66) return 7;
            if (b1==0x90 && b0==0xC9) return 12;
            if (b1==0x90 && b0==0xCB) return 16;
            if (b1==0x94 && b0==0x80) return 30;
            return 0;
        }
        return -1;
    }

    private static string TxToneFromBytes(byte a1, byte a2, byte b1, byte b0)
    {
        int idx = DecodeTxIndex(a1,a2,b1,b0);
        if (idx < 0 || idx >= ToneMenuTx.Length) return "0";
        return ToneMenuTx[idx];
    }

    private static int RxIndexFromA3A2(byte a3, byte a2)
    {
        int hi2 = (a3 >> 6) & 0x03;
        int lo4 = (a2 >> 4) & 0x0F;
        int val = (hi2 << 4) | lo4;
        return val;
    }

    private static string RxToneFromBytes(byte a3, byte a2, byte a1, byte a0, byte b3, string txTone)
    {
        if (a1==0x98 && a2==0x27)
        {
            if (a0==0x00) return "0";
            if (a0==0x71) return ToneMenuRx[1];
            if (a0==0x91) return ToneMenuRx[33];
        }
        int idx = RxIndexFromA3A2(a3,a2);
        if (idx==0)
        {
            bool follow = (b3 & 0x01) != 0;
            return follow ? txTone : "0";
        }
        if (idx >= 0 && idx < ToneMenuRx.Length) return ToneMenuRx[idx];
        return "0";
    }

    public static void DecodeChannel(byte[] row, out string txTone, out string rxTone, out int cct, out int ste)
    {
        if (row==null || row.Length<8) { txTone="0"; rxTone="0"; cct=0; ste=0; return; }
        byte A3=row[0], A2=row[1], A1=row[2], A0=row[3], B3=row[4], B2=row[5], B1=row[6], B0=row[7];
        txTone = TxToneFromBytes(A1,A2,B1,B0);
        rxTone = RxToneFromBytes(A3,A2,A1,A0,B3, txTone);
        cct = (B3 >> 1) & 0x07;
        ste = (B2 >> 0) & 0x01;
    }

    public static void DecodeChannel(byte A3,byte A2,byte A1,byte A0,byte B3,byte B2,byte B1,byte B0,
                                     out string txTone,out string rxTone,out int cct,out int ste)
    {
        txTone = TxToneFromBytes(A1,A2,B1,B0);
        rxTone = RxToneFromBytes(A3,A2,A1,A0,B3, txTone);
        cct = (B3 >> 1) & 0x07;
        ste = (B2 >> 0) & 0x01;
    }

    public static void DecodeChannel(byte[] row, out int txIndex, out int rxIndex, out int cct, out int ste)
    {
        if (row==null || row.Length<8) { txIndex=0; rxIndex=0; cct=0; ste=0; return; }
        byte A3=row[0], A2=row[1], A1=row[2], A0=row[3], B3=row[4], B2=row[5], B1=row[6], B0=row[7];
        txIndex = DecodeTxIndex(A1,A2,B1,B0);
        if (txIndex < 0) txIndex = 0;
        int idx = RxIndexFromA3A2(A3,A2);
        if (idx==0)
        {
            bool follow = (B3 & 0x01)!=0;
            rxIndex = follow ? txIndex : 0;
        }
        else
        {
            rxIndex = (idx>=0 && idx < ToneMenuRx.Length) ? idx : 0;
        }
        cct = (B3 >> 1) & 0x07;
        ste = (B2 >> 0) & 0x01;
    }

    public static bool TrySetTxTone(ref byte a1, ref byte a2, ref byte b1, ref byte b0, string tone)
    {
        int idx = Array.IndexOf(ToneMenuTx, tone);
        if (idx < 0) return false;
        if (a1==0x98 && a2==0x27)
        {
            switch (idx)
            {
                case 2:  b1=0x94; b0=0xD3; return true;
                case 8:  b1=0x94; b0=0xC7; return true;
                case 12: b1=0x90; b0=0xC9; return true;
                case 21: b1=0x90; b0=0x9D; return true;
                case 24: b1=0x90; b0=0x7E; return true;
                case 33: b1=0x94; b0=0x81; return true;
                default: return false;
            }
        }
        if (a1==0x9C && a2==0x27)
        {
            switch (idx)
            {
                case 1:  b1=0x90; b0=0x71; return true;
                case 7:  b1=0x94; b0=0x66; return true;
                case 12: b1=0x90; b0=0xC9; return true;
                case 16: b1=0x90; b0=0xCB; return true;
                case 30: b1=0x94; b0=0x80; return true;
                default: return false;
            }
        }
        return false;
    }

    public static bool TrySetRxTone(ref byte A3, ref byte A2, ref byte A1, ref byte A0, ref byte B3, string tone, string txToneIfFollow)
    {
        if (tone == "0")
        {
            A3 = (byte)(A3 & 0x3F);
            A2 = (byte)(A2 & 0x0F);
            return true;
        }
        int idx = Array.IndexOf(ToneMenuRx, tone);
        if (idx < 0) return false;

        if (A1==0x98 && A2==0x27)
        {
            if (idx==1) { A0=0x71; return true; }
            if (idx==33) { A0=0x91; return true; }
        }

        int hi2 = (idx >> 4) & 0x03;
        int lo4 = idx & 0x0F;
        A3 = (byte)((A3 & 0x3F) | (hi2<<6));
        A2 = (byte)((A2 & 0x0F) | (lo4<<4));
        return true;
    }
}
