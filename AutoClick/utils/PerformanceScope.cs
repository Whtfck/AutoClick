using System;
using System.Diagnostics;

namespace AutoClick.utils;

public readonly struct PerformanceScope : IDisposable
{
    private readonly string _name;
    private readonly Stopwatch _stopwatch;
    private readonly double _thresholdMs;

    private PerformanceScope(string name, double thresholdMs)
    {
        _name = name;
        _thresholdMs = thresholdMs;
        _stopwatch = Stopwatch.StartNew();
    }

    public static PerformanceScope Track(string name, double thresholdMs = 16)
    {
        return new PerformanceScope(name, thresholdMs);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var elapsed = _stopwatch.Elapsed.TotalMilliseconds;
        if (elapsed >= _thresholdMs)
        {
            LogUtil.Performance($"{_name} took {elapsed:F2} ms");
        }
    }
}
