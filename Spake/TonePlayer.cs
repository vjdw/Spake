using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Threading.Tasks;

namespace Spake
{
    internal class TonePlayer : IDisposable
    {
        private WasapiOut _outputDevice;
        private SignalGenerator _toneGenerator;
        private FadeInOutSampleProvider _fader;
        private bool disposedValue;

        public TonePlayer(WasapiOut outputDevice)
        {
            _outputDevice = outputDevice;
            _toneGenerator = new SignalGenerator()
            {
                Gain = 0,
                Frequency = 0,
                Type = SignalGeneratorType.Sin
            };
            _fader = new FadeInOutSampleProvider(_toneGenerator, initiallySilent: true);

            _outputDevice.Init(_fader);
        }

        public async Task PlayTone(int frequencyHz, double gain, int durationMs)
        {
            try
            {
                _toneGenerator.Frequency = frequencyHz;
                _toneGenerator.Gain = gain;
                _outputDevice.Play();

                _fader.BeginFadeIn(durationMs / 2);
                await Task.Delay(durationMs / 2);

                _fader.BeginFadeOut(durationMs / 2);
                await Task.Delay(250 + durationMs / 2);
            }
            finally
            {
                _outputDevice.Stop();
                _toneGenerator.Frequency = 0;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _outputDevice.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
