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

        private Task _scheduleTask;

        public ToneScheduler(int intervalMs, int toneDurationMs, int toneFrequencyHz, double toneGain)
        {
            IntervalMs = intervalMs;
            DurationMs = toneDurationMs;
            FrequencyHz = toneFrequencyHz;
            Gain = toneGain;

            _scheduleTask = Task.Run(Schedule);
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

                var wavelengthMs = 1000d / FrequencyHz;
                var durationRoundedToCompleteWavelengthsMs = wavelengthMs * Math.Floor((DurationMs / wavelengthMs));

                var signal = new SignalGenerator()
                {
                    Gain = Gain,
                    Frequency = FrequencyHz,
                    Type = SignalGeneratorType.Sin
                }
                .Take(TimeSpan.FromMilliseconds(durationRoundedToCompleteWavelengthsMs));

                using (var waveout = new WaveOutEvent())
                {
                    waveout.Init(signal);
                    waveout.Volume = 0f;
                    waveout.Play();

                    while (waveout.PlaybackState == PlaybackState.Playing)
                    {
                        var posMs = waveout.GetPositionTimeSpan().TotalMilliseconds;
                        var middlePos = (float)durationRoundedToCompleteWavelengthsMs / 2;
                        var distanceFromMiddle = Math.Abs((float)posMs - middlePos);
                        var normalisedDistanceFromMiddle = distanceFromMiddle / middlePos;
                        waveout.Volume = Math.Clamp(1.0f - normalisedDistanceFromMiddle, 0, 1);
                        await Task.Delay(10);
                    }
                }
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
