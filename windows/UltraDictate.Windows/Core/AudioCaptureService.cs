using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace UltraDictate.Windows.Core;

public class AudioCaptureService : IDisposable
{
    private WasapiCapture? _capture;
    private readonly List<float> _recordedSamples = new();
    private readonly object _lock = new();
    private bool _isRecording = false;

    public const int TargetSampleRate = 16000;
    public event Action<float>? AudioLevelChanged;

    public bool IsRecording => _isRecording;

    public void StartRecording()
    {
        lock (_lock)
        {
            if (_isRecording) return;
            _recordedSamples.Clear();
            _isRecording = true;

            try
            {
                _capture = new WasapiCapture();
                _capture.DataAvailable += OnDataAvailable;
                _capture.StartRecording();
            }
            catch (Exception ex)
            {
                _isRecording = false;
                throw new InvalidOperationException($"Failed to initialize audio capture: {ex.Message}", ex);
            }
        }
    }

    public float[] StopRecording()
    {
        lock (_lock)
        {
            if (!_isRecording) return Array.Empty<float>();
            _isRecording = false;

            try
            {
                _capture?.StopRecording();
                _capture?.Dispose();
                _capture = null;
            }
            catch { }

            return _recordedSamples.ToArray();
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!_isRecording || _capture == null) return;

        var format = _capture.WaveFormat;
        float maxLevel = 0f;

        lock (_lock)
        {
            using var ms = new MemoryStream(e.Buffer, 0, e.BytesRecorded);
            using var reader = new WaveFileReader(ms);

            // Simple peak level calculation for HUD
            int sampleCount = e.BytesRecorded / (format.BitsPerSample / 8);
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                if (i + 4 <= e.BytesRecorded)
                {
                    float sample = BitConverter.ToSingle(e.Buffer, i);
                    _recordedSamples.Add(sample);
                    float abs = Math.Abs(sample);
                    if (abs > maxLevel) maxLevel = abs;
                }
            }
        }

        AudioLevelChanged?.Invoke(maxLevel);
    }

    public void Dispose()
    {
        _capture?.Dispose();
        _capture = null;
        GC.SuppressFinalize(this);
    }
}
