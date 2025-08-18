using System;
using System.IO;
using System.Text.Json;

/// <summary>
/// Simple persisted settings. Lives at %APPDATA%\X2212\settings.json
/// </summary>
public sealed class AppSettings
{
    public string LastRgrFolder { get; set; } = "";
    public string LptBaseText { get; set; } = "0xA800";
    public int WindowX { get; set; } = 0;
    public int WindowY { get; set; } = 0;
    public int WindowW { get; set; } = 0;
    public int WindowH { get; set; } = 0;

    private static string SettingsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "X2212");

    private static string SettingsPath => Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch { /* ignore & fall back to defaults */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* ignore */ }
    }
}