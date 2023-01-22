using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Spake
{
    internal class ToneScheduler
    {
        public event EventHandler<EventArgs>? ToneStarted;
        public event EventHandler<EventArgs>? ToneEnded;

        private int _intervalMs;
        public int IntervalMs
        {
            get
            {
                return _intervalMs;
            }
            set
            {
                _intervalMs = value;
                _scheduleLoopCancellationTokenSource?.Cancel();
            }
        }
        public int DurationMs { get; set; }
        public int FrequencyHz { get; set; }
        public double Gain { get; set; }

        private Dictionary<string, TonePlayer> TargetDevices { get; init; } = new Dictionary<string, TonePlayer>();

        private Task _scheduleTask;
        private CancellationTokenSource _scheduleLoopCancellationTokenSource;

        public ToneScheduler(int intervalMs, int toneDurationMs, int toneFrequencyHz, double toneGain)
        {
            IntervalMs = intervalMs;
            DurationMs = toneDurationMs;
            FrequencyHz = toneFrequencyHz;
            Gain = toneGain;

            _scheduleLoopCancellationTokenSource = new CancellationTokenSource();
            _scheduleTask = Task.Run(Schedule);
        }

        public void SetTargetDevices(IList<string> deviceUniqueIds)
        {
            foreach (var deviceToRemove in TargetDevices.Where(td => !deviceUniqueIds.Contains(td.Key)).ToList())
            {
                TargetDevices.Remove(deviceToRemove.Key);
            }

            foreach (var deviceUniqueIdToAdd in deviceUniqueIds.Where(duid => !TargetDevices.ContainsKey(duid)).ToList())
            {
                TargetDevices.Add(deviceUniqueIdToAdd, new TonePlayer(deviceUniqueIdToAdd));
            }
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

                try
                {
                    await Task.Delay(IntervalMs, _scheduleLoopCancellationTokenSource.Token);
                }
                catch (TaskCanceledException) { }

                _scheduleLoopCancellationTokenSource = new CancellationTokenSource();
            };
        }

        public async Task Play()
        {
            try
            {
                OnToneStarted();

                var playToneTasks = TargetDevices.Select(async targetDevice => await targetDevice.Value.PlayTone(FrequencyHz, Gain, DurationMs));
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

        protected virtual void OnToneStarted()
        {
            if (ToneStarted != null)
            {
                ToneStarted(this, EventArgs.Empty);
            }
        }

        protected virtual void OnToneEnded()
        {
            if (ToneEnded != null)
            {
                ToneEnded(this, EventArgs.Empty);
            }
        }
    }
}
