using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Spake
{
    internal class TonePlayer
    {
        private readonly string OutputDeviceId;

        public TonePlayer(string deviceUniqueId)
        {
            OutputDeviceId = deviceUniqueId;
        }

        public async Task PlayTone(int frequencyHz, double gain, int durationMs)
        {
            WasapiOut? outputDevice = null;

            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).SingleOrDefault(_ => _.ID == OutputDeviceId);
                if (device == null)
                    return;
                outputDevice = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: false, latency: 0);

                var toneGenerator = new SignalGenerator()
                {
                    Gain = 0,
                    Frequency = 0,
                    Type = SignalGeneratorType.Sin
                };
                var fader = new FadeInOutSampleProvider(toneGenerator, initiallySilent: true);

                outputDevice.Init(fader);

                toneGenerator.Frequency = frequencyHz;
                toneGenerator.Gain = gain;
                outputDevice.Play();

                fader.BeginFadeIn(durationMs / 2);
                await Task.Delay(durationMs / 2);

                fader.BeginFadeOut(durationMs / 2);
                await Task.Delay(250 + durationMs / 2);
            }
            finally
            {
                if (outputDevice != null)
                {
                    outputDevice.Stop();
                    outputDevice.Dispose();
                }
            }
        }
    }
}
