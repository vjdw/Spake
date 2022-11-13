﻿using NAudio.Wave.SampleProviders;
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

namespace Spake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ToneScheduler _toneScheduler;

        public string IconPath { get; set; }

        public int ToneSchedulerIntervalMs
        {
            get { return _toneScheduler.IntervalMs; }
            set { _toneScheduler.IntervalMs = value; }
        }

        public int ToneSchedulerFrequencyHz
        {
            get { return _toneScheduler.FrequencyHz ; }
            set { _toneScheduler.FrequencyHz = value; }
        }

        public int ToneSchedulerDurationMs
        {
            get { return _toneScheduler.DurationMs; }
            set { _toneScheduler.DurationMs = value; }
        }

        public double ToneSchedulerGain
        {
            get { return _toneScheduler.Gain; }
            set { _toneScheduler.Gain = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _toneScheduler = new ToneScheduler(
                (int)Settings.Default["IntervalMs"],
                (int)Settings.Default["DurationMs"],
                (int)Settings.Default["FrequencyHz"],
                (double)Settings.Default["Gain"]);

            _toneScheduler.ToneStarted += ToneScheduler_ToneStarted;
            _toneScheduler.ToneEnded += ToneScheduler_ToneEnded;

            taskbarIcon.Icon = new Icon("./Icons/idle.ico");
        }

        private void ToneScheduler_ToneStarted(object? sender, EventArgs e)
        {
            taskbarIcon.Icon = new Icon("./Icons/playing.ico");
        }

        private void ToneScheduler_ToneEnded(object? sender, EventArgs e)
        {
            taskbarIcon.Icon = new Icon("./Icons/idle.ico");
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            Settings.Default["IntervalMs"] = _toneScheduler.IntervalMs;
            Settings.Default["DurationMs"] = _toneScheduler.DurationMs;
            Settings.Default["Gain"] = _toneScheduler.Gain;
            Settings.Default["FrequencyHz"] = _toneScheduler.FrequencyHz;
            Settings.Default.Save();
        }
    }
}