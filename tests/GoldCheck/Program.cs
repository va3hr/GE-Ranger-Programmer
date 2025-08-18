using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

    static string FindRepoDocsDir()
    {
        string current = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6; i++)
        {
            string d1 = Path.Combine(current, "Docs");
            string d2 = Path.Combine(current, "docs");
            if (Directory.Exists(d1)) return d1;
            if (Directory.Exists(d2)) return d2;
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return "";
    }

    static void Main()
    {
        string docsDir = FindRepoDocsDir();
        if (string.IsNullOrEmpty(docsDir))
        {
            Console.Error.WriteLine("Could not find a Docs/ folder up to 6 levels above the test directory.");
            Environment.Exit(1);
            return;
        }

        string rgrPath = Path.Combine(docsDir, "RANGR6M.RGR");
        string csvPath = Path.Combine(docsDir, "RANGR6M_cal.csv");

        if (!File.Exists(rgrPath))
        {
            Console.Error.WriteLine("Gold RGR not found at: " + rgrPath);
            Environment.Exit(1);
            return;
        }
        if (!File.Exists(csvPath))
        {
            Console.Error.WriteLine("Gold CSV not found at: " + csvPath);
            Environment.Exit(1);
            return;
        }

        // Load logical 128 bytes from RGR
        byte[] logical = DecodeRgrBytes(File.ReadAllBytes(rgrPath), out _);
        if (logical.Length < 128)
        {
            Console.Error.WriteLine("Logical bytes < 128 in RGR.");
            Environment.Exit(1);
            return;
        }
        logical = logical.Take(128).ToArray();

        // Load expected Tx/Rx from CSV (columns: CH, DOS Tx Freq (enter), DOS Rx Freq (enter))
        var lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            Console.Error.WriteLine("CSV seems empty: " + csvPath);
            Environment.Exit(1);
            return;
        }
        string[] headers = SplitCsvLine(lines[0]);
        int idxCh = Array.FindIndex(headers, h => h.Trim().Equals("CH", StringComparison.OrdinalIgnoreCase));
        int idxTx = Array.FindIndex(headers, h => h.Contains("DOS Tx Freq", StringComparison.OrdinalIgnoreCase));
        int idxRx = Array.FindIndex(headers, h => h.Contains("DOS Rx Freq", StringComparison.OrdinalIgnoreCase));
        if (idxCh < 0 || idxTx < 0 || idxRx < 0)
        {
            Console.Error.WriteLine("CSV headers not found (need CH, DOS Tx Freq (enter), DOS Rx Freq (enter)).");
            Environment.Exit(1);
            return;
        }

        double[] expTx = new double[16];
        double[] expRx = new double[16];
        for (int ln = 1; ln < lines.Length; ln++)
        {
            if (string.IsNullOrWhiteSpace(lines[ln])) continue;
            var cols = SplitCsvLine(lines[ln]);
            if (cols.Length <= Math.Max(idxRx, Math.Max(idxTx, idxCh))) continue;
            if (!int.TryParse(cols[idxCh], out int ch)) continue;
            if (ch < 1 || ch > 16) continue;

            if (double.TryParse(cols[idxTx], NumberStyles.Float, CultureInfo.InvariantCulture, out double txv) &&
                double.TryParse(cols[idxRx], NumberStyles.Float, CultureInfo.InvariantCulture, out double rxv))
            {
                expTx[ch - 1] = txv;
                expRx[ch - 1] = rxv;
            }
        }

        // Compute via production code
        int fails = 0;
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

            if (Math.Abs(tx - expTx[ch]) > 0.0005 || Math.Abs(rx - expRx[ch]) > 0.0005)
            {
                fails++;
                Console.Error.WriteLine($"CH {ch+1:00}: expected Tx/Rx {expTx[ch]:0.000}/{expRx[ch]:0.000}, got {tx:0.000}/{rx:0.000}");
            }
        }

        if (fails > 0)
        {
            Console.Error.WriteLine($"GOLD CHECK FAILED: {fails} mismatches vs CSV.");
            Environment.Exit(1);
        }
        else
        {
            Console.WriteLine("GOLD CHECK OK: all 16 channels match CSV.");
        }
    }

    // Simple CSV splitter that respects quotes
    static string[] SplitCsvLine(string line)
    {
        var list = new System.Collections.Generic.List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"'); i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                list.Add(sb.ToString()); sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        list.Add(sb.ToString());
        return list.ToArray();
    }
}