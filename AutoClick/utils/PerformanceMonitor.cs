using System;
using System.Diagnostics;
using System.Threading;

namespace AutoClick.utils;

public sealed class PerformanceMonitor : IDisposable
{
    private readonly Process _targetProcess;
    private readonly Timer _timer;
    private readonly int _intervalMs;
    private TimeSpan _lastCpuTime;
    private DateTime _lastCheckTime;
    private readonly string _label;
    private bool _disposed;

    private PerformanceMonitor(Process process, string label, int intervalMs)
    {
        _targetProcess = process;
        _label = label;
        _intervalMs = intervalMs;
        _lastCpuTime = process.TotalProcessorTime;
        _lastCheckTime = DateTime.UtcNow;
        _timer = new Timer(CollectMetrics, null, intervalMs, intervalMs);
    }

    public static PerformanceMonitor Start(Process process, string label, int intervalMs = 1000)
    {
        return new PerformanceMonitor(process, label, Math.Max(250, intervalMs));
    }

    private void CollectMetrics(object? state)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _targetProcess.Refresh();
            var now = DateTime.UtcNow;
            var cpu = _targetProcess.TotalProcessorTime;
            var cpuDelta = cpu - _lastCpuTime;
            var timeDelta = now - _lastCheckTime;

            double cpuUsage = 0;
            if (timeDelta.TotalMilliseconds > 1)
            {
                cpuUsage = (cpuDelta.TotalMilliseconds / (Environment.ProcessorCount * timeDelta.TotalMilliseconds)) * 100.0;
            }

            var workingSetMb = _targetProcess.WorkingSet64 / (1024.0 * 1024.0);
            var threads = _targetProcess.Threads.Count;

            LogUtil.Performance($"[{_label}] CPU: {cpuUsage:F2}% | RAM: {workingSetMb:F2} MB | Threads: {threads}");

            _lastCpuTime = cpu;
            _lastCheckTime = now;
        }
        catch (Exception ex)
        {
            LogUtil.Error($"Performance monitoring failed for {_label}", ex);
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
        try
        {
            _targetProcess.Dispose();
        }
        catch
        {
            // ignore
        }
    }
}
