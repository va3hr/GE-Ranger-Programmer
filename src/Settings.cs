using System;
using System.IO;
using System.Text.Json;

public class AppSettings
{
    public string LastRgrFolder { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    // Keep text form so we preserve user-entered hex/decimal faithfully
    public string LptBaseText { get; set; } = "0xA800";

    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowW { get; set; } = -1;
    public int WindowH { get; set; } = -1;

    public static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "X2212");

    public static string ConfigPath => Path.Combine(ConfigDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch { /* fall through to defaults */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Non-fatal; ignore save errors
        }
    }
}