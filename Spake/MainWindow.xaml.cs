using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using Spake.Properties;
//using System.Windows.Forms;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;

namespace Spake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ToneScheduler _toneScheduler;
        private TaskbarIcon _taskbarIcon;
        private bool _startMinimised = false;

        public string IconPath { get; set; }

        public int ToneSchedulerIntervalMs
        {
            get
            {
                return _toneScheduler.IntervalMs;
            }
            set
            {
                _toneScheduler.IntervalMs = value;
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
                SaveSettings();
            }
        }

        public int ToneSchedulerDurationMs
        {
            get
            {
                return _toneScheduler.DurationMs;
            }
            set
            {
                _toneScheduler.DurationMs = value;
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
                SaveSettings();
            }
        }

        private string IconIdlePath => System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath.ToString(), "Icons", "idle.ico");
        private string IconPlayingPath => System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath.ToString(), "Icons", "playing.ico");

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _taskbarIcon = (TaskbarIcon)FindResource("NotifyIcon");
            _taskbarIcon.Icon = new Icon(IconIdlePath);

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
            _startMinimised = (bool)Settings.Default["StartMinimised"];

            if (_startMinimised)
            {
                Application.Current.MainWindow.Hide();
            }
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

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            _taskbarIcon.Icon = new Icon(IconPlayingPath);
            await _toneScheduler.Test();
            _taskbarIcon.Icon = new Icon(IconIdlePath);
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

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Application.Current.MainWindow.Hide();
            }

            _startMinimised = WindowState == WindowState.Minimized;
            SaveSettings();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
    }
}
