using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

using AutoClick.ui;
using AutoClick.utils;
using Emgu.CV;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using static AutoClick.utils.SafeConvertUtil;
using Exception = System.Exception;
using Window = System.Windows.Window;

namespace AutoClick;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private ProcessInfo? _processInfo;
    private volatile bool _isRunning;
    private double _matchValue;
    private Thread? _workerThread;
    private PerformanceMonitor? _targetPerformanceMonitor;
    private PerformanceMonitor? _appPerformanceMonitor;

    public MainWindow()
    {
        InitializeComponent();
        ConfigTextBox.Text = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "./resource/config.json");
        InitializeAppPerformanceMonitor();
        LogUtil.Info("AutoClick UI initialized");
    }

    public bool IsRunning => _isRunning;

    private void InitializeAppPerformanceMonitor()
    {
        try
        {
            _appPerformanceMonitor = PerformanceMonitor.Start(Process.GetCurrentProcess(), "AutoClick");
        }
        catch (Exception ex)
        {
            LogUtil.Warning($"启动 AutoClick 性能监控失败: {ex.Message}");
        }
    }

    private void SetRunningState(bool value)
    {
        if (_isRunning == value)
        {
            return;
        }

        _isRunning = value;
        Dispatcher.Invoke(() => { IsRunningCheckBox.IsChecked = _isRunning; });
        LogUtil.Info($"switch isRunning to:{_isRunning}");

        if (!_isRunning)
        {
            StopPerformanceMonitoring();
        }
    }

    private void PidBtn_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessList processList = new();
        processList.ShowDialog();

        if (processList.SelectedProcess != null)
        {
            _processInfo = processList.SelectedProcess;
            var pid = processList.SelectedProcess.Pid;
            var pName = processList.SelectedProcess.ProcessName;
            PidTextBox.Text = $"{pName}({pid})";
            LogUtil.Info($"Selected process -> {pName} ({pid})");
        }
    }

    private void ConfigBtn_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new()
        {
            Title = "Select a JSON file",
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
            Filter = "JSON File (*.json)|*.json|All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false
        };

        bool? result = openFileDialog.ShowDialog();

        if (result == true)
        {
            var fileName = openFileDialog.FileName;
            if (CheckConfig(fileName) != null)
            {
                ConfigTextBox.Text = fileName;
            }
        }
    }

    private void StartBtn_OnClick(object sender, RoutedEventArgs e)
    {
        if (IsRunning)
        {
            MessageBox.Show("Config is already running");
            return;
        }

        string filepath = ConfigTextBox.Text;
        JObject? config = CheckConfig(filepath);
        if (config != null)
        {
            config["curDir"] = Path.GetDirectoryName(filepath);
            _matchValue = ToDouble(config["MatchValue"]);
            StartTask(config);
        }
    }

    private void StopBtn_OnClick(object sender, RoutedEventArgs e)
    {
        StopAutomation();
    }

    private void StopAutomation()
    {
        if (!IsRunning)
        {
            return;
        }

        SetRunningState(false);
        Thread? worker = _workerThread;
        if (worker != null && worker.IsAlive)
        {
            if (!worker.Join(TimeSpan.FromSeconds(2)))
            {
                try
                {
                    worker.Interrupt();
                }
                catch (Exception ex)
                {
                    LogUtil.Warning($"中断工作线程失败: {ex.Message}");
                }

                if (!worker.Join(TimeSpan.FromSeconds(1)) && worker.IsAlive)
                {
                    LogUtil.Warning("工作线程在中断后仍未退出，将在后台继续停止过程。");
                }
            }
        }

        _workerThread = null;
        LogUtil.Info("Automation stopped by user");
    }

    private JObject? CheckConfig(string filepath)
    {
        if (IsRunning)
        {
            MessageBox.Show("Config is already running");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filepath))
        {
            MessageBox.Show("Please select a valid JSON file");
            return null;
        }

        if (!File.Exists(filepath))
        {
            MessageBox.Show("File does not exist");
            return null;
        }

        if (_processInfo == null)
        {
            MessageBox.Show("Please select a process first");
            return null;
        }

        try
        {
            string fileContent = File.ReadAllText(filepath);
            JObject jsonObj = JObject.Parse(fileContent);
            JArray processList = ToJArray(jsonObj["ProcessList"]);

            foreach (var ps in processList)
            {
                JObject taskObj = ToJObject(ps);
                if (taskObj["ProcessName"] != null && ObjToString(taskObj["ProcessName"]) == _processInfo.ProcessName)
                {
                    LogUtil.Info($"Loaded config for process {_processInfo.ProcessName}");
                    return taskObj;
                }
            }

            MessageBox.Show($"Not Found Config:{_processInfo.ProcessName} In File:{filepath}", "Info");
            return null;
        }
        catch (Exception ex)
        {
            LogUtil.Error("读取配置失败", ex);
            MessageBox.Show($"Error reading the file: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        return null;
    }

    private void StartTask(JObject config)
    {
        _workerThread = new Thread(() =>
        {
            Dictionary<string, Mat> matCache = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                string? processName = _processInfo?.ProcessName;
                if (string.IsNullOrWhiteSpace(processName))
                {
                    throw new InvalidOperationException("未选择有效的进程");
                }

                string curDir = ObjToString(config["curDir"]);
                string resDir = ObjToString(config["ResourcePath"]);
                JArray tasks = ToJArray(config["Tasks"]);

                int pollIntervalMs = NormalizeInterval(ToInt(config["PollIntervalMs"]), 80);
                int noMatchDelayMs = NormalizeInterval(ToInt(config["NoMatchDelayMs"]), pollIntervalMs / 2, 0);

                LogUtil.Info($"Starting automation for {processName}, pollInterval={pollIntervalMs}ms, noMatchDelay={noMatchDelayMs}ms");

                CaptureUtil.ResetDiagnostics();
                IntPtr handle = CaptureUtil.GetWindowHandle(processName);
                CaptureUtil.ShowWindow(handle, CaptureUtil.SW_RESTORE);
                CaptureUtil.SetForegroundWindow(handle);
                Thread.Sleep(200);

                StartPerformanceMonitoring();
                SetRunningState(true);

                Stopwatch loopStopwatch = Stopwatch.StartNew();

                while (IsRunning)
                {
                    bool hasMatchedAction = false;

                    foreach (var taskToken in tasks)
                    {
                        if (!IsRunning)
                        {
                            break;
                        }

                        JObject task = ToJObject(taskToken);
                        JArray iconGroups = ToJArray(task["IconGroups"]);
                        int targetIndex = ToInt(task["TargetIndex"]);
                        JArray actions = ToJArray(task["Actions"]);
                        int taskDelay = Math.Max(0, ToInt(task["Delay"]));

                        foreach (var igs in iconGroups)
                        {
                            if (!IsRunning)
                            {
                                break;
                            }

                            using Bitmap bitmap = CaptureUtil.CaptureWindow(handle);
                            CaptureUtil.GetWindowRect(handle, out RECT rect);
                            using Mat src = CaptureUtil.BitmapToEmguMat(bitmap);

                            JArray iconGroup = ToJArray(igs);
                            bool allMatched = true;
                            Dictionary<int, MatchResult> results = new();

                            for (int i = 0; i < iconGroup.Count; i++)
                            {
                                if (!IsRunning)
                                {
                                    allMatched = false;
                                    break;
                                }

                                string iconPath = Path.Combine(curDir, resDir, ObjToString(iconGroup[i]));

                                MatchResult matchResult = matCache.TryGetValue(iconPath, out Mat? iconMat)
                                    ? CaptureUtil.Match(src, iconMat)
                                    : CaptureUtil.Match(src, iconPath);

                                if (!matCache.ContainsKey(iconPath))
                                {
                                    matCache[iconPath] = matchResult.Dst;
                                }

                                results[i] = matchResult;
                                if (!IsMatched(matchResult))
                                {
                                    allMatched = false;
                                    break;
                                }
                            }

                            if (allMatched)
                            {
                                hasMatchedAction = true;
                                if (results.TryGetValue(targetIndex, out MatchResult targetResult))
                                {
                                    LogUtil.Info($"Matched icon group -> executing {actions.Count} action(s)");
                                    ActionUtil.DoActions(rect, actions, targetResult);
                                }
                                else
                                {
                                    LogUtil.Warning($"匹配结果中找不到目标索引 {targetIndex}");
                                }
                            }
                            else if (noMatchDelayMs > 0)
                            {
                                Thread.Sleep(noMatchDelayMs);
                            }
                        }

                        if (taskDelay > 0)
                        {
                            Thread.Sleep(taskDelay);
                        }
                    }

                    if (!IsRunning)
                    {
                        break;
                    }

                    if (!hasMatchedAction)
                    {
                        int elapsed = (int)loopStopwatch.ElapsedMilliseconds;
                        int sleepMs = pollIntervalMs - elapsed;
                        if (sleepMs > 0)
                        {
                            Thread.Sleep(sleepMs);
                        }
                    }

                    loopStopwatch.Restart();
                }
            }
            catch (ThreadInterruptedException)
            {
                LogUtil.Warning("工作线程被中断");
            }
            catch (Exception e)
            {
                LogUtil.Error("自动化任务发生异常", e);
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Automation task error: {e.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                foreach (var mat in matCache.Values)
                {
                    mat.Dispose();
                }

                matCache.Clear();
                if (ReferenceEquals(_workerThread, Thread.CurrentThread))
                {
                    _workerThread = null;
                }
                SetRunningState(false);
            }
        })
        {
            IsBackground = true,
            Name = "AutoClick-Worker"
        };

        _workerThread.Start();
    }

    private static int NormalizeInterval(int value, int fallback, int minValue = 16)
    {
        if (value <= 0)
        {
            value = fallback;
        }

        return Math.Clamp(value, minValue, 5000);
    }

    private void StartPerformanceMonitoring()
    {
        StopPerformanceMonitoring();

        try
        {
            if (_processInfo != null)
            {
                var process = Process.GetProcessById(_processInfo.Pid);
                _targetPerformanceMonitor = PerformanceMonitor.Start(process, _processInfo.ProcessName);
            }
        }
        catch (Exception ex)
        {
            LogUtil.Warning($"启动 {_processInfo?.ProcessName} 性能监控失败: {ex.Message}");
        }
    }

    private void StopPerformanceMonitoring()
    {
        _targetPerformanceMonitor?.Dispose();
        _targetPerformanceMonitor = null;
    }

    private void WatchWindow()
    {
        Thread monitoringThread = new(_ =>
        {
            while (IsRunning)
            {
                try
                {
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        GetWindowThreadProcessId(hwnd, out var processId);
                        Process process = Process.GetProcessById((int)processId);
                        bool shouldRun = process.ProcessName.Equals(_processInfo?.ProcessName,
                            StringComparison.OrdinalIgnoreCase);
                        SetRunningState(shouldRun);
                    }

                    Thread.Sleep(200);
                }
                catch (Exception e)
                {
                    LogUtil.Error("窗口监控异常", e);
                    SetRunningState(false);
                }
            }
        })
        {
            IsBackground = true
        };
        monitoringThread.Start();
    }

    private bool IsMatched(MatchResult matchResult)
    {
        if (_matchValue > 0)
        {
            return matchResult.ResultValue >= _matchValue;
        }

        return false;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopAutomation();
        _appPerformanceMonitor?.Dispose();
        base.OnClosed(e);
    }
}
