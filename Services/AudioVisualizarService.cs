using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Numerics;
using System.Windows;

namespace ControlSensors.Services;

public class AudioVisualizerService : IDisposable {
    private WasapiLoopbackCapture? _capture;
    private readonly int _fftLength = 1024;
    private readonly int _logLength = 10;

    public event Action<float[]>? OnBandsUpdated;

    public void Start() {
        if (_capture != null) return;
        try {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += ProcessAudio;
            _capture.RecordingStopped += (s, e) => { _capture?.Dispose(); _capture = null; };
            _capture.StartRecording();
        } catch { }
    }

    public void Stop() {
        _capture?.StopRecording();
    }

    private void ProcessAudio(object? sender, WaveInEventArgs e) {
        if (e.BytesRecorded == 0) return;

        byte[] buffer = e.Buffer;
        int bytesPerSample = 4;
        int channels = _capture!.WaveFormat.Channels;
        int samples = e.BytesRecorded / bytesPerSample;
        int frames = samples / channels;

        NAudio.Dsp.Complex[] fftBuffer = new NAudio.Dsp.Complex[_fftLength];
        for (int i = 0; i < _fftLength; i++) {
            if (i < frames) {
                float sample = BitConverter.ToSingle(buffer, i * channels * bytesPerSample);
                fftBuffer[i].X = (float)(sample * FastFourierTransform.HammingWindow(i, _fftLength));
            } else {
                fftBuffer[i].X = 0;
            }
            fftBuffer[i].Y = 0;
        }

        FastFourierTransform.FFT(true, _logLength, fftBuffer);

        int bandCount = 16;
        float[] bands = new float[bandCount];

        int usefulBins = (_fftLength / 2) / 2;
        int binsPerBand = usefulBins / bandCount;

        for (int i = 0; i < bandCount; i++) {
            float sum = 0;
            for (int j = 0; j < binsPerBand; j++) {
                int index = (i * binsPerBand) + j + 1;
                double magnitude = Math.Sqrt(fftBuffer[index].X * fftBuffer[index].X + fftBuffer[index].Y * fftBuffer[index].Y);
                sum += (float)magnitude * 5000f;
            }

            float avg = sum / binsPerBand;
            avg *= (1 + (i * 0.8f));

            bands[i] = avg;
        }

        Application.Current.Dispatcher.InvokeAsync(() => {
            OnBandsUpdated?.Invoke(bands);
        });
    }

    public void Dispose() {
        Stop();
    }
}