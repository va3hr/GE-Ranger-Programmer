
// UiTracerBootstrapper.cs - ensure the log file is created and note startup.
using System;

public static class UiTracerBootstrapper
{
    public static void Boot()
    {
        LogSink.Write($"[{DateTime.Now:HH:mm:ss.fff}] UI tracer booted. Log path: {LogSink.ResolvePath()}");
    }
}
