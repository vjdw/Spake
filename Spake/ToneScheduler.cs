using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Spake
{
    internal class ToneScheduler
    {
        public event EventHandler<EventArgs> ToneStarted;
        public event EventHandler<EventArgs> ToneEnded;

        public int IntervalMs { get; set; }
        public int DurationMs { get; set; }
        public int FrequencyHz { get; set; }
        public double Gain { get; set; }

        private HashSet<string> TargetDevices { get; set; } = new HashSet<string>();

        private Task _scheduleTask;

        public ToneScheduler(int intervalMs, int toneDurationMs, int toneFrequencyHz, double toneGain)
        {
            IntervalMs = intervalMs;
            DurationMs = toneDurationMs;
            FrequencyHz = toneFrequencyHz;
            Gain = toneGain;

            _scheduleTask = Task.Run(Schedule);
        }

        public void SetTargetDevices(IEnumerable<string> deviceIds)
        {
            TargetDevices = deviceIds.ToHashSet();
        }

        private async Task Schedule()
        {
            while (true)
            {
                try
                {
                    await Play();
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("Spake ToneScheduler Schedule", ex.Message, EventLogEntryType.Error);
                }

                await Task.Delay(IntervalMs);
            };
        }

        public async Task Play()
        {
            try
            {
                OnToneStarted();

                var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

                var playToneTasks = TargetDevices.Select(async targetDeviceId =>
                {
                    var targetDevice = devices.SingleOrDefault(_ => _.ID == targetDeviceId);
                    if (targetDevice != null)
                    {
                        using var outputDevice = new WasapiOut(targetDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);

                        if (!await IsDevicePlayingAudio(targetDevice))
                        {
                            await PlayToneOnDevice(outputDevice);
                        }
                    }
                });
                await Task.WhenAll(playToneTasks);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Spake ToneScheduler Play", ex.Message, EventLogEntryType.Error);
            }
            finally
            {
                OnToneEnded();
            }
        }

        private async Task PlayToneOnDevice(WasapiOut outputDevice)
        {
            // Prevents popping.
            const int LeadOutMs = 1000;

            var signalProvider = new SignalGenerator()
            {
                Gain = Gain,
                Frequency = FrequencyHz,
                Type = SignalGeneratorType.Sin
            }
            .Take(TimeSpan.FromMilliseconds(DurationMs + LeadOutMs));

            FadeInOutSampleProvider faderProvider = new FadeInOutSampleProvider(signalProvider, initiallySilent: true);
            faderProvider.BeginFadeIn(DurationMs / 2);

            outputDevice.Init(faderProvider);
            outputDevice.Volume = 1f;
            outputDevice.Play();

            var fadeOutTriggered = false;
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                if (!fadeOutTriggered && outputDevice.GetPositionTimeSpan().TotalMilliseconds >= DurationMs / 2)
                {
                    faderProvider.BeginFadeOut(DurationMs / 2);
                    fadeOutTriggered = true;
                }
                await Task.Delay(10);
            }
        }

        private static async Task<bool> IsDevicePlayingAudio(MMDevice device)
        {
            using var recordingDevice = new WasapiLoopbackCapture(device);
            var bytesRecorded = 0;
            recordingDevice.DataAvailable += (object? sender, WaveInEventArgs e) => { bytesRecorded = e.BytesRecorded; };
            recordingDevice.StartRecording();
            await Task.Delay(500);
            recordingDevice.StopRecording();
            var deviceAlreadyPlayingAudio = bytesRecorded > 0;
            return deviceAlreadyPlayingAudio;
        }

        private void RecordingDevice_DataAvailable(object? sender, WaveInEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnToneStarted()
        {
            EventHandler<EventArgs> handler = ToneStarted;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnToneEnded()
        {
            EventHandler<EventArgs> handler = ToneEnded;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
