using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
    // 定义 Windows API 函数
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private ProcessInfo? _processInfo = null;

    private bool _isRunning;

    private JObject? _config;

    private double _matchValue;

    public MainWindow()
    {
        InitializeComponent();
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                // 其他线程更新此控件
                Dispatcher.Invoke(() => { IsRunningCheckBox.IsChecked = _isRunning; });
                Console.WriteLine($"switch isRunning to:{_isRunning}");
            }
        }
    }


    private void PidBtn_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessList processList = new ProcessList();
        processList.ShowDialog();

        if (processList.SelectedProcess != null)
        {
            _processInfo = processList.SelectedProcess;
            var pid = processList.SelectedProcess.Pid;
            var pName = processList.SelectedProcess.ProcessName;
            PidTextBox.Text = $"{pName}({pid})";
        }
    }

    private void ConfigBtn_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
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
            CheckConfig(fileName);
            ConfigTextBox.Text = fileName;
        }
    }

    private void StartBtn_OnClick(object sender, RoutedEventArgs e)
    {
        string filepath = ConfigTextBox.Text;
        JObject? config = CheckConfig(filepath);
        if (config != null)
        {
            IsRunning = true;
            // 启动监听
            // WatchWindow();
            // 启动自动点击任务
            config["curDir"] = Path.GetDirectoryName(filepath);
            _config = config;
            _matchValue = ToDouble(config["MatchValue"]);
            StartTask(config);
        }
    }

    private void StopBtn_OnClick(object sender, RoutedEventArgs e)
    {
        IsRunning = false;
    }

    private JObject? CheckConfig(String filepath)
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
                    return taskObj;
                }
            }

            MessageBox.Show($"Not Found Config:{_processInfo.ProcessName} In File:{filepath}", "Info");
            return null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading the file: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        return null;
    }

    private void WatchWindow()
    {
        Thread monitoringThread = new Thread(_ =>
        {
            while (IsRunning)
            {
                try
                {
                    // 获取当前活动窗口的句柄
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        // 获取窗口所属的进程 ID
                        GetWindowThreadProcessId(hwnd, out var processId);

                        // 根据进程 ID 获取进程信息
                        Process process = Process.GetProcessById((int)processId);

                        // 判断当前活动窗口是否属于目标应用程序
                        IsRunning = process.ProcessName.Equals(_processInfo.ProcessName,
                            StringComparison.OrdinalIgnoreCase);

                        // Console.WriteLine($"Front is: {process.ProcessName}, isPause: {isPause}");
                    }

                    // 暂停一段时间，避免频繁检查
                    Thread.Sleep(200);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    IsRunning = false;
                }
            }
        });
        monitoringThread.IsBackground = true; // 设置为后台线程
        monitoringThread.Start();
    }

    private void StartTask(JObject? config)
    {
        if (config == null)
        {
            throw new Exception("Config is null");
        }

        Thread t = new Thread(_ =>
        {
            try
            {
                Console.WriteLine($"Starting task:{_processInfo.ProcessName}");

                string curDir = ObjToString(config["curDir"]);
                string resDir = ObjToString(config["ResourcePath"]);
                JArray tasks = ToJArray(config["Tasks"]);

                Dictionary<String, Mat> matMap = new Dictionary<String, Mat>();

                IntPtr handle = CaptureUtil.GetWindowHandle(_processInfo.ProcessName);
                CaptureUtil.ShowWindow(handle, CaptureUtil.SW_RESTORE);
                CaptureUtil.SetForegroundWindow(handle);
                while (IsRunning)
                {
                    foreach (var t in tasks)
                    {
                        JObject task = ToJObject(t);
                        JArray iconGroups = ToJArray(task["IconGroups"]);
                        
                        int targetIndex = ToInt(task["TargetIndex"]);
                        JArray actions = ToJArray(task["Actions"]);

                        // 不同组集合
                        foreach (var igs in iconGroups)
                        {
                            Bitmap bitmap = CaptureUtil.CaptureWindow(handle);
                            CaptureUtil.GetWindowRect(handle, out RECT rect);
                            Mat src = CaptureUtil.BitmapToEmguMat(bitmap);

                            // 组内图片集合
                            JArray iconGroup = ToJArray(igs);
                            // 组内是否全部匹配
                            bool allMatched = false;
                            Dictionary<int, MatchResult> results = new Dictionary<int, MatchResult>();
                            for (int i = 0; i < iconGroup.Count; i++)
                            {
                                string icon = $"{curDir}/{resDir}/{ObjToString(iconGroup[i])}";

                                var matchResult = matMap.TryGetValue(icon, out var iconMat)
                                    ? CaptureUtil.Match(src, iconMat)
                                    : CaptureUtil.Match(src, icon);

                                matMap[icon] = matchResult.Dst;

                                results.Add(i, matchResult);
                                if (IsMatched(matchResult))
                                {
                                    Console.WriteLine($"Matched: {icon}");
                                    if (i == iconGroup.Count - 1)
                                    {
                                        allMatched = true;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (allMatched)
                            {
                                
                                MatchResult targetResult = results[targetIndex];
                                
                                ActionUtil.DoActions(rect, actions, targetResult);
                            }
                        }

                        // 任务延迟
                        Thread.Sleep(ToInt(task["Delay"]));
                    }
                }
            }
            catch (Exception e)
            {
                IsRunning = false;
                Console.Error.WriteLine(e);
                MessageBox.Show(e.Message, "Error");
            }
        });

        t.IsBackground = true;
        t.Start();
        IsRunning = true;
    }


    private bool IsMatched(MatchResult matchResult)
    {
        if (_matchValue > 0)
        {
            return matchResult.ResultValue >= _matchValue;
        }

        return false;
    }
}