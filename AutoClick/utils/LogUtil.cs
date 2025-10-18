using System;
using System.IO;
using System.Text;

namespace AutoClick.utils;

public static class LogUtil
{
    private static readonly object _lock = new();
    private static readonly string _logDirectory;
    private static readonly string _logFilePath;

    static LogUtil()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _logDirectory = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(_logDirectory);
            _logFilePath = Path.Combine(_logDirectory, $"AutoClick_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.AppendAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] Log file created.{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            _logDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _logFilePath = Path.Combine(_logDirectory, $"AutoClick_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [WARN] Failed to initialize log file: {ex.Message}");
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Warning(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null)
    {
        var builder = new StringBuilder(message);
        if (ex != null)
        {
            builder.AppendLine();
            builder.Append(ex.ToString());
        }
        Write("ERROR", builder.ToString());
    }

    public static void Performance(string message) => Write("PERF", message);

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // ignore logging failures to avoid recursive exceptions
            }
        }

        Console.WriteLine(line);
    }
}
