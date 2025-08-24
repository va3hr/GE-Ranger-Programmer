
// RgrInspector.cs — tiny console to dump TX/RX from a .RGR ascii-hex file
// Build: dotnet new console -n RgrInspector && replace Program.cs with this file
// Usage: RgrInspector <path-to-.RGR>
using System;
using System.IO;
using System.Linq;

class Program
{
    static readonly string[] Cg = new string[]
    {
        "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
        "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
        "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
        "179.9","186.2","192.8","203.5","210.7"
    };

    static void Main(string[] args)
    {
        if (args.Length != 1) { Console.WriteLine("Usage: RgrInspector <file.RGR>"); return; }
        string hex = File.ReadAllText(args[0]).Trim().Replace(" ", "").Replace("\r","").Replace("\n","");
        if (hex.Length != 256) { Console.WriteLine("Expected 256 ASCII-hex chars, got " + hex.Length); return; }

        byte[] bytes = new byte[128];
        for (int i=0;i<128;i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i*2,2), 16);
        }

        Console.WriteLine("CH, A3,A2,A1,A0,B3,B2,B1,B0, TX, RX");
        for (int ch=0; ch<16; ch++)
        {
            int off = ch*8;
            byte A3=bytes[off+0], A2=bytes[off+1], A1=bytes[off+2], A0=bytes[off+3];
            byte B3=bytes[off+4], B2=bytes[off+5], B1=bytes[off+6], B0=bytes[off+7];

            string tx = TxFromA1B1(A1,B1);
            string rx = RxFromA3B3(A3,B3, tx);
            Console.WriteLine($"{ch+1:00}, {A3:X2},{A2:X2},{A1:X2},{A0:X2},{B3:X2},{B2:X2},{B1:X2},{B0:X2}, {tx}, {rx}");
        }
    }

    static string TxFromA1B1(byte A1, byte B1)
    {
        int idx = 0;
        if (A1 == 0xEF || A1 == 0xEC) idx = 20;
        else if (A1 == 0xAE) idx = 16;
        else if (A1 == 0x6D) idx = 13;
        else if (A1 == 0x2D) idx = 25;
        else if (A1 == 0xAC) idx = 11;
        else if (A1 == 0x68 || A1 == 0x98) idx = 26;
        else if (A1 == 0xA5) idx = 19;
        else if (A1 == 0x63) idx = 16;
        else if (A1 == 0xE3 || A1 == 0xE2) idx = 13;
        else if (A1 == 0x28) idx = ((B1 & 0x80)!=0) ? 14 : 15;
        else idx = 0;
        return Cg[idx];
    }

    static string RxFromA3B3(byte A3, byte B3, string txDisplay)
    {
        // idx[5..0] = [A3.6, A3.7, A3.0, A3.1, A3.2, A3.3] → LSB positions {1,0,7,6,5,4}
        int b5 = (A3 >> 1) & 1;
        int b4 = (A3 >> 0) & 1;
        int b3 = (A3 >> 7) & 1;
        int b2 = (A3 >> 6) & 1;
        int b1 = (A3 >> 5) & 1;
        int b0 = (A3 >> 4) & 1;
        int idx = (b5<<5)|(b4<<4)|(b3<<3)|(b2<<2)|(b1<<1)|b0;

        bool follow = (B3 & 0x01) != 0;
        if (idx == 0 && follow) return txDisplay;
        return Cg[Math.Min(idx, Cg.Length-1)];
    }
}
