using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Spake.Properties;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Spake
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static ToneScheduler _toneScheduler = default!;
        private TaskbarIcon _taskbarIcon;
        private bool _startMinimised = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string IconPath { get; set; } = default!;

        public int ToneSchedulerIntervalMinutes
        {
            get
            {
                return _toneScheduler.IntervalMs / 60000;
            }
            set
            {
                _toneScheduler.IntervalMs = value * 60000;
                OnPropertyChanged("LabelContentForInterval");
                SaveSettings();
            }
        }

        public int ToneSchedulerFrequencyHz
        {
            get
            {
                return _toneScheduler.FrequencyHz;
            }
            set
            {
                _toneScheduler.FrequencyHz = value;
                OnPropertyChanged("LabelContentForFrequency");
                SaveSettings();
            }
        }

        public int ToneSchedulerDurationSeconds
        {
            get
            {
                return _toneScheduler.DurationMs / 1000;
            }
            set
            {
                _toneScheduler.DurationMs = value * 1000;
                OnPropertyChanged("LabelContentForDuration");
                SaveSettings();
            }
        }

        public double ToneSchedulerGain
        {
            get
            {
                return _toneScheduler.Gain;
            }
            set
            {
                _toneScheduler.Gain = value;
                OnPropertyChanged("LabelContentForGain");
                SaveSettings();
            }
        }

        public string LabelContentForInterval => $"Play tone every {ToneSchedulerIntervalMinutes} minute{(ToneSchedulerIntervalMinutes == 1 ? "" : "s")}";
        public string LabelContentForDuration => $"Duration: {ToneSchedulerDurationSeconds} second{(ToneSchedulerDurationSeconds == 1 ? "" : "s")}";
        public string LabelContentForFrequency => $"Frequency: {ToneSchedulerFrequencyHz} Hz";
        public string LabelContentForGain => $"Gain: {ToneSchedulerGain:N2}";

        private string IconIdlePath => System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath.ToString(), "Icons", "idle.ico");
        private string IconPlayingPath => System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath.ToString(), "Icons", "playing.ico");

        public MainWindow()
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            InitializeComponent();
            DataContext = this;

            _taskbarIcon = (TaskbarIcon)FindResource("NotifyIcon");
            _taskbarIcon.Icon = new Icon(IconIdlePath);

            _startMinimised = (bool)Settings.Default["StartMinimised"];

            if (_toneScheduler == null)
            {
                _toneScheduler = new ToneScheduler(
                    (int)Settings.Default["IntervalMs"],
                    (int)Settings.Default["DurationMs"],
                    (int)Settings.Default["FrequencyHz"],
                    (double)Settings.Default["Gain"]);

                _toneScheduler.ToneStarted += ToneScheduler_ToneStarted;
                _toneScheduler.ToneEnded += ToneScheduler_ToneEnded;
            }
            
            chkStartAtLogin.IsChecked = (bool)Settings.Default["StartAtLogin"];
            chkStartMinimised.IsChecked = _startMinimised;
            if (_startMinimised)
            {
                Application.Current.MainWindow.Hide();
            }

            AddCheckboxesForActiveDevices();
            LoadSelectedDevicesIntoToneScheduler();
        }

        private void AddCheckboxesForActiveDevices()
        {
            var selectedDevicesDocument = ReadSelectedDevicesFromUserSettings();

            var activeDevices = new MMDeviceEnumerator()
                .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .Select(_ => new SelectedDevice(_.ID, _.FriendlyName));

            panelActiveDevices.Children.Clear();
            foreach (var activeDevice in activeDevices)
            {
                var checkbox = new CheckBox();
                checkbox.Content = activeDevice.FriendlyName;
                checkbox.Margin = new Thickness(3);
                checkbox.IsChecked = selectedDevicesDocument.SelectedDevices.Any(_ => _.Id == activeDevice.Id);
                checkbox.Checked += DeviceCheckbox_CheckedChanged;
                checkbox.Unchecked += DeviceCheckbox_CheckedChanged;
                checkbox.Tag = activeDevice;
                panelActiveDevices.Children.Add(checkbox);
            }
        }

        private void LoadSelectedDevicesIntoToneScheduler()
        {
            var selectedDevicesDocument = ReadSelectedDevicesFromUserSettings();
            _toneScheduler.SetTargetDevices(selectedDevicesDocument.SelectedDevices.Select(_ => _.Id).ToList());
        }

        private void DeviceCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SaveSelectedDevices();
            LoadSelectedDevicesIntoToneScheduler();
        }

        private void ToneScheduler_ToneStarted(object? sender, EventArgs e)
        {
            _taskbarIcon.Icon = new Icon(IconPlayingPath);
        }

        private void ToneScheduler_ToneEnded(object? sender, EventArgs e)
        {
            _taskbarIcon.Icon = new Icon(IconIdlePath);
        }

        private void SaveSettings()
        {
            Settings.Default["IntervalMs"] = _toneScheduler.IntervalMs;
            Settings.Default["DurationMs"] = _toneScheduler.DurationMs;
            Settings.Default["Gain"] = _toneScheduler.Gain;
            Settings.Default["FrequencyHz"] = _toneScheduler.FrequencyHz;
            Settings.Default["StartAtLogin"] = chkStartAtLogin.IsChecked;
            Settings.Default["StartMinimised"] = _startMinimised;
            Settings.Default.Save();
        }

        private void SaveSelectedDevices()
        {
            var activeDeviceCheckboxes = panelActiveDevices.Children.OfType<UIElement>().Where(_ => _ is CheckBox).Select(_ => _ as CheckBox);

            var previousSelectedDevicesDocument = ReadSelectedDevicesFromUserSettings();
            var previouslySelectedDevicesThatAreNoLongerActive = previousSelectedDevicesDocument.SelectedDevices.Where(sd => !activeDeviceCheckboxes.Any(adc => (adc!.Tag as SelectedDevice)!.Id == sd.Id)).ToList();

            var newSelectedDevicesDocument = new SelectedDevicesDocument();
            foreach (var checkbox in activeDeviceCheckboxes)
            {
                if (checkbox!.IsChecked.GetValueOrDefault())
                {
                    var selectedDevice = checkbox.Tag as SelectedDevice;
                    newSelectedDevicesDocument.SelectedDevices.Add(new SelectedDevice(selectedDevice!.Id, selectedDevice.FriendlyName));
                }
            }

            // Preserve devices that are currently not shown in the UI, but were selected in the UI before.
            newSelectedDevicesDocument.SelectedDevices = newSelectedDevicesDocument.SelectedDevices.Concat(previouslySelectedDevicesThatAreNoLongerActive).ToList();

            Settings.Default["SelectedDevices"] = JsonSerializer.Serialize(newSelectedDevicesDocument);
            Settings.Default.Save();
        }

        private SelectedDevicesDocument ReadSelectedDevicesFromUserSettings()
        {
            try
            {
                var selectedDevices = (string)Settings.Default["SelectedDevices"];
                selectedDevices = string.IsNullOrEmpty(selectedDevices) ? "{}" : selectedDevices;
                return JsonSerializer.Deserialize<SelectedDevicesDocument>(selectedDevices)!;
            }
            catch
            {
                return new SelectedDevicesDocument();
            }
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            await _toneScheduler.Play();
        }

        private void btnRefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            AddCheckboxesForActiveDevices();
        }

        private void chkStartAtLogin_Checked(object sender, RoutedEventArgs e)
        {
            var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true)!;
            key.SetValue("Spake", System.Windows.Forms.Application.ExecutablePath.ToString());

            SaveSettings();
        }

        private void chkStartAtLogin_Unchecked(object sender, RoutedEventArgs e)
        {
            var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true)!;
            key.DeleteValue("Spake", false);

            SaveSettings();
        }

        private void chkStartMinimised_Checked(object sender, RoutedEventArgs e)
        {
            _startMinimised = true;

            SaveSettings();
        }

        private void chkStartMinimised_Unchecked(object sender, RoutedEventArgs e)
        {
            _startMinimised = false;

            SaveSettings();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Application.Current.MainWindow.Hide();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }

        async void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            // After waking from suspend we can't play audio until the user unlocks the session.
            if (new[] { SessionSwitchReason.SessionLogon, SessionSwitchReason.SessionUnlock }.Contains(e.Reason))
            {
                await _toneScheduler.Play();
            }
        }
    }
}
