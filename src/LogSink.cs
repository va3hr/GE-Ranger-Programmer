
// LogSink.cs - simple file logger for the tracer.
using System;
using System.IO;

public static class LogSink
{
    private static readonly object _lock = new object();
    public static string LogPath = Path.Combine(AppContext.BaseDirectory, "ToneUiOverwrite.log");

    public static void Write(string message)
    {
        lock (_lock)
        {
            File.AppendAllText(LogPath, message + Environment.NewLine);
        }
    }
}
