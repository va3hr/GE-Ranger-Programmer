using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    // Detect ASCII-hex file and decode to bytes; otherwise return raw bytes
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
                {
                    bytes[i] = byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                mode = "ASCII-hex";
                return bytes;
            }
        }
        catch { }
        mode = "binary";
        return fileBytes;
    }

    static (double[] tx, double[] rx) LoadExpectedCsv(string csvPath)
    {
        if (!File.Exists(csvPath))
            throw new FileNotFoundException("Expected CSV not found: " + csvPath);

        var lines = File.ReadAllLines(csvPath);
        var rx = new double[16];
        var tx = new double[16];
        int count = 0;

        // Regex to find numbers like 52.525, 103.5, etc.
        var re = new Regex(r"[-+]?\d+\.\d+");

        foreach (var line in lines)
        {
            var m = re.Matches(line);
            if (m.Count >= 2)
            {
                // First two decimal numbers on the line are taken as Tx and Rx MHz
                if (!double.TryParse(m[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double txv)) continue;
                if !double.TryParse(m[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double rxv) continue;
                tx[count] = txv;
                rx[count] = rxv;
                count++;
                if (count == 16) break;
            }
        }

        if (count != 16)
            throw new InvalidOperationException($"Expected 16 channels in CSV, found {count}. Please ensure the CSV lists Tx and Rx per row.");

        return (tx, rx);
    }

    static void Main()
    {
        string repoRoot = Directory.GetCurrentDirectory();
        // Walk up until we find Docs/ or src/ (works when run from tests/GoldCheck directory)
        for (int i = 0; i < 6 && !Directory.Exists(Path.Combine(repoRoot, "Docs")) && !Directory.Exists(Path.Combine(repoRoot, "docs")); i++)
            repoRoot = Path.GetDirectoryName(repoRoot) ?? repoRoot;

        string docs = Directory.Exists(Path.Combine(repoRoot, "Docs")) ? Path.Combine(repoRoot, "Docs") : Path.Combine(repoRoot, "docs");
        string csvPath = Path.Combine(docs, "RANGR6M_cal.csv");
        string rgrPath = Path.Combine(docs, "RANGR6M.RGR");

        if (!File.Exists(rgrPath))
            throw new FileNotFoundException("Gold RGR not found: " + rgrPath);

        var expected = LoadExpectedCsv(csvPath);

        byte[] fileBytes = File.ReadAllBytes(rgrPath);
        string mode;
        byte[] logical = DecodeRgrBytes(fileBytes, out mode);
        if (logical.Length < 128) throw new InvalidOperationException("Gold RGR logical bytes < 128");

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

            double expTx = expected.tx[ch];
            double expRx = expected.rx[ch];

            bool okTx = Math.Abs(tx - expTx) < 0.0005;
            bool okRx = Math.Abs(rx - expRx) < 0.0005;

            if (!okTx || !okRx)
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
            Console.WriteLine("GOLD CHECK OK: all 16 channels match expected frequencies.");
        }
    }
}
