using System;

public static class ToneIndexing
{
    // 0 = no tone. Valid tone indices are 1..33 (inclusive).
    // This list intentionally EXCLUDES 114.1.
    public static readonly string[] CanonicalLabels = new[]
    {
        "0",
        "67.0","71.9","74.4","77.0","79.7","82.5","85.4",
        "88.5","91.5","94.8","97.4","100.0","103.5","107.2",
        "107.7","110.0","110.9","114.8","118.8","123.0",
        "127.3","131.8","136.5","141.3","146.2","151.4",
        "156.7","162.2","167.9","173.8","179.9","186.2",
        "192.8","203.5","210.7"
    };
}
