// ======================= DO NOT EDIT =======================
// RgrCodec.cs — Canonical .RGR decode helpers (128 logical bytes).
// Purpose: centralize ASCII‑hex vs binary handling and per‑channel
// hex line formatting so MainForm stays simple and endian‑safe.
// ============================================================
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace X2212
{
    public static class RgrCodec
    {
        public static bool LooksAsciiHex(string text)
        {
            int hex = 0;
            foreach (char ch in text)
            {
                if (char.IsWhiteSpace(ch)) continue;
                if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))
                    hex++;
                else
                    return false;
            }
            return hex >= 2 && (hex % 2) == 0;
        }

        // Returns exactly 128 logical bytes (trim or pad if needed).
        public static byte[] Decode(byte[] fileBytes)
        {
            if (fileBytes == null) return new byte[128];

            try
            {
                string text = Encoding.UTF8.GetString(fileBytes);
                if (LooksAsciiHex(text))
                {
                    string compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
                    int n = Math.Min(128, compact.Length / 2);
                    byte[] logical = new byte[128];
                    for (int i = 0; i < n; i++)
                        logical[i] = byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return logical;
                }
            }
            catch { /* fall back to binary */ }

            // Binary: trim/pad to 128
            var result = new byte[128];
            int m = Math.Min(128, fileBytes.Length);
            Array.Copy(fileBytes, 0, result, 0, m);
            return result;
        }

        // Pretty hex for one channel block [A0..B3] at channel index 0..15.
        public static string HexLine(byte[] logical128, int channelIndex)
        {
            if (logical128 == null || logical128.Length < 128) return "";
            if ((uint)channelIndex > 15) return "";
            int i = channelIndex * 8;
            return $"{logical128[i+0]:X2} {logical128[i+1]:X2} {logical128[i+2]:X2} {logical128[i+3]:X2}  " +
                   $"{logical128[i+4]:X2} {logical128[i+5]:X2} {logical128[i+6]:X2} {logical128[i+7]:X2}";
        }
    }
}
