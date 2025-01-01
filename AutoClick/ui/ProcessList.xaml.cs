using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AutoClick.ui
{
    /// <summary>
    /// ProcessList.xaml 的交互逻辑
    /// </summary>
    public partial class ProcessList : Window
    {
        private ObservableCollection<ProcessInfo> _processes;
        private CollectionViewSource _collectionViewSource;

        public ProcessInfo? SelectedProcess { get; set; }

        public ProcessList()
        {
            InitializeComponent();

            _processes = new ObservableCollection<ProcessInfo>();
            _collectionViewSource = new CollectionViewSource { Source = _processes };
            ProcessListBox.ItemsSource = _collectionViewSource.View;

            LoadProcess();
        }

        private void LoadProcess()
        {
            var processInfos = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                .Select(p => new ProcessInfo
                {
                    ProcessName = p.ProcessName,
                    Pid = p.Id,
                });

            _processes.Clear();
            foreach (var p in processInfos)
            {
                _processes.Add(p);
            }
        }

        private void RefreshBtn_OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            RefreshProcessListBox();
        }

        private void OkBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var processInfo = ProcessListBox.SelectedItem as ProcessInfo;
            if (processInfo != null)
            {
                SelectedProcess = processInfo;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please Select A Process", "Info");
            }
        }

        private void ProcTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            RefreshProcessListBox();
        }

        private void RefreshProcessListBox()
        {
            LoadProcess();
            if (_collectionViewSource.View != null)
            {
                _collectionViewSource.View.Filter = item =>
                {
                    var processInfo = item as ProcessInfo;
                    if (processInfo == null) return false;

                    string search = ProcTextBox.Text.ToLower();
                    return processInfo.Pid.ToString().Contains(search) ||
                           processInfo.ProcessName.ToLower().Contains(search);
                };
            }
        }
    }

    public class ProcessInfo
    {
        public int Pid { get; init; }
        public required string ProcessName { get; init; }

        public override string ToString()
        {
            return $"PID: {Pid},\t\tName: {ProcessName}";
        }
    }
}