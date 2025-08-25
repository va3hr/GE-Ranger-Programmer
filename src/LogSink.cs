
// LogSink.cs - robust file logger for the tracer (writes to %TEMP% by default).
using System;
using System.IO;

public static class LogSink
{
    private static readonly object _lock = new object();

    public static string ResolvePath()
    {
        // Allow override via environment variable
        var env = Environment.GetEnvironmentVariable("TONE_UI_TRACE_PATH");
        string path = !string.IsNullOrWhiteSpace(env) ? env : Path.Combine(Path.GetTempPath(), "ToneUiOverwrite.log");
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        catch { /* ignore */ }
        return path;
    }

    public static void Write(string message)
    {
        try
        {
            var path = ResolvePath();
            lock (_lock)
            {
                File.AppendAllText(path, message + Environment.NewLine);
            }
        }
        catch
        {
            // As a last resort, swallow logging errors to avoid crashing the app.
        }
    }
}
