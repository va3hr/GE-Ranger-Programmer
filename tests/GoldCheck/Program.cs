using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static bool LooksAsciiHex(string s)
    {
        int hex = 0;
        foreach (char ch in s)
        {
            if (char.IsWhiteSpace(ch)) continue;
            if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))
                hex++;
            else
                return false;
        }
        return hex >= 2 && (hex % 2) == 0;
    }

    static byte[] DecodeRgrBytes(byte[] fileBytes, out string mode)
    {
        try
        {
            string text = Encoding.UTF8.GetString(fileBytes);
            if (LooksAsciiHex(text))
            {
                string compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
                int n = compact.Length / 2;
                byte[] bytes = new byte[n];
                for (int i = 0; i < n; i++)
                    bytes[i] = byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                mode = "ASCII-hex";
                return bytes;
            }
        }
        catch { }
        mode = "binary";
        return fileBytes;
    }

    static void Main()
    {
        // Locate repo root by walking up until we find Docs/ or docs/
        string current = Directory.GetCurrentDirectory();
        string docsDir = string.Empty;
        for (int i = 0; i < 6; i++)
        {
            string d1 = Path.Combine(current, "Docs");
            string d2 = Path.Combine(current, "docs");
            if (Directory.Exists(d1)) { docsDir = d1; break; }
            if (Directory.Exists(d2)) { docsDir = d2; break; }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        if (string.IsNullOrEmpty(docsDir))
        {
            Console.Error.WriteLine("Could not find a Docs/ folder up to 6 levels above the test directory.");
            Environment.Exit(1);
            return;
        }

        string rgrPath = Path.Combine(docsDir, "RANGR6M.RGR");
        if (!File.Exists(rgrPath))
        {
            Console.Error.WriteLine("Gold RGR not found at: " + rgrPath);
            Environment.Exit(1);
            return;
        }

        byte[] logical = DecodeRgrBytes(File.ReadAllBytes(rgrPath), out _);
        if (logical.Length < 128)
        {
            Console.Error.WriteLine("Logical bytes < 128 in RGR.");
            Environment.Exit(1);
            return;
        }
        logical = logical.Take(128).ToArray();

        // Check hash
        using var sha = SHA256.Create();
        string hash = BitConverter.ToString(sha.ComputeHash(logical)).Replace("-", "").ToLowerInvariant();
        if (!string.Equals(hash, ToneAndFreq.GOLD_RGR_SHA256, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Gold RGR hash mismatch.");
            Environment.Exit(1);
            return;
        }

        // Compare computed vs embedded gold
        int failures = 0;
        for (int ch = 0; ch < 16; ch++)
        {
            int i = ch * 8;
            byte A0 = logical[i + 0];
            byte A1 = logical[i + 1];
            byte A2 = logical[i + 2];
            byte B0 = logical[i + 4];
            byte B1 = logical[i + 5];
            byte B2 = logical[i + 6];

            double tx = ToneAndFreq.TxMHz(A0, A1, A2);
            double rx = ToneAndFreq.RxMHz(B0, B1, B2, tx);

            double expTx = ToneAndFreq.GoldTxMHz[ch];
            double expRx = ToneAndFreq.GoldRxMHz[ch];

            if (Math.Abs(tx - expTx) > 0.0005 || Math.Abs(rx - expRx) > 0.0005)
            {
                failures++;
                Console.Error.WriteLine($"CH {ch+1:00}: expected Tx/Rx {expTx:0.000}/{expRx:0.000}, got {tx:0.000}/{rx:0.000}");
            }
        }

        if (failures > 0)
        {
            Console.Error.WriteLine($"GOLD CHECK FAILED: {failures} mismatches.");
            Environment.Exit(1);
        }
        else
        {
            Console.WriteLine("GOLD CHECK OK: all 16 channels match embedded gold.");
        }
    }
}
