using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace UltraDictate.Windows.Core;

public class AudioCaptureService : IDisposable
{
    private WasapiCapture? _capture;
    private readonly List<float> _rawMonoSamples = new();
    private readonly object _lock = new();
    private bool _isRecording = false;
    private int _sourceSampleRate = 48000;

    public const int TargetSampleRate = 16000;
    public event Action<float>? AudioLevelChanged;

    public bool IsRecording => _isRecording;

    public void StartRecording()
    {
        lock (_lock)
        {
            if (_isRecording) return;
            _rawMonoSamples.Clear();
            _isRecording = true;

            try
            {
                _capture = new WasapiCapture();
                _sourceSampleRate = _capture.WaveFormat.SampleRate;
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

            var raw = _rawMonoSamples.ToArray();
            _rawMonoSamples.Clear();

            if (raw.Length == 0) return Array.Empty<float>();

            // Resample from device native sample rate to TargetSampleRate (16 kHz)
            return Resample(raw, _sourceSampleRate, TargetSampleRate);
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!_isRecording || _capture == null || e.BytesRecorded == 0) return;

        var format = _capture.WaveFormat;
        int bytesRecorded = e.BytesRecorded;
        byte[] buffer = e.Buffer;

        int channels = Math.Max(1, format.Channels);
        int bitsPerSample = format.BitsPerSample;
        int bytesPerSample = bitsPerSample / 8;
        if (bytesPerSample <= 0) return;

        int totalSamples = bytesRecorded / bytesPerSample;
        int frameCount = totalSamples / channels;
        if (frameCount <= 0) return;

        var monoFrame = new float[frameCount];
        float sumSquares = 0f;

        bool isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat ||
                       (format is WaveFormatExtensible ext && ext.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71")) ||
                       (format.BitsPerSample == 32 && format.Encoding != WaveFormatEncoding.Pcm);

        for (int f = 0; f < frameCount; f++)
        {
            float monoSample = 0f;
            for (int ch = 0; ch < channels; ch++)
            {
                int byteOffset = (f * channels + ch) * bytesPerSample;
                if (byteOffset + bytesPerSample > bytesRecorded) break;

                float sampleVal = 0f;
                if (isFloat && bytesPerSample == 4)
                {
                    sampleVal = BitConverter.ToSingle(buffer, byteOffset);
                }
                else if (bitsPerSample == 16)
                {
                    short s = BitConverter.ToInt16(buffer, byteOffset);
                    sampleVal = s / 32768.0f;
                }
                else if (bitsPerSample == 24)
                {
                    int s = (buffer[byteOffset] << 8) | (buffer[byteOffset + 1] << 16) | ((sbyte)buffer[byteOffset + 2] << 24);
                    sampleVal = s / 2147483648.0f;
                }
                else if (bitsPerSample == 32 && !isFloat)
                {
                    int s = BitConverter.ToInt32(buffer, byteOffset);
                    sampleVal = s / 2147483648.0f;
                }

                monoSample += sampleVal;
            }

            monoSample /= channels;
            monoFrame[f] = monoSample;
            sumSquares += monoSample * monoSample;
        }

        lock (_lock)
        {
            _rawMonoSamples.AddRange(monoFrame);
        }

        float rms = MathF.Sqrt(sumSquares / frameCount);
        // Perceptual dynamic curve: normal speech is ~0.005 to 0.08 RMS
        float visualLevel = Math.Clamp(MathF.Pow(rms * 12.0f, 0.65f), 0f, 1f);
        AudioLevelChanged?.Invoke(visualLevel);
    }

    public static float[] Resample(float[] input, int sourceRate, int targetRate)
    {
        if (sourceRate == targetRate || input.Length == 0) return input;
        double ratio = (double)targetRate / sourceRate;
        int outputLength = (int)(input.Length * ratio);
        if (outputLength <= 0) return Array.Empty<float>();

        float[] output = new float[outputLength];
        for (int i = 0; i < outputLength; i++)
        {
            double srcIndex = i / ratio;
            int index1 = (int)srcIndex;
            int index2 = Math.Min(index1 + 1, input.Length - 1);
            float fraction = (float)(srcIndex - index1);
            output[i] = input[index1] * (1.0f - fraction) + input[index2] * fraction;
        }
        return output;
    }

    public void Dispose()
    {
        _capture?.Dispose();
        _capture = null;
        GC.SuppressFinalize(this);
    }
}
