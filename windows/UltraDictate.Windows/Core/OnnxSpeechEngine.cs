using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace UltraDictate.Windows.Core;

public class OnnxSpeechEngine : IDisposable
{
    private WhisperFactory? _whisperFactory;
    private bool _isLoaded = false;
    private bool _isDownloading = false;
    private string _statusMessage = "Initializing...";
    private readonly object _lock = new();

    public bool IsLoaded => _isLoaded;
    public bool IsDownloading => _isDownloading;
    public string StatusMessage => _statusMessage;

    public event Action<string>? StatusChanged;
    public event Action<int>? DownloadProgressChanged;

    public static readonly string DefaultModelsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UltraDictate",
        "models"
    );

    public static readonly string DefaultModelFile = Path.Combine(DefaultModelsDir, "ggml-base.bin");

    public OnnxSpeechEngine()
    {
        Task.Run(InitializeModelAsync);
    }

    public async Task InitializeModelAsync()
    {
        try
        {
            Directory.CreateDirectory(DefaultModelsDir);

            // 1. Check if model exists in models directory or app directory
            string? foundModel = FindExistingModel();
            if (foundModel != null && File.Exists(foundModel))
            {
                LoadModel(foundModel);
                return;
            }

            // 2. Auto-download ggml-base.bin if missing
            await DownloadModelAsync(DefaultModelFile);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Model init error: {ex.Message}");
        }
    }

    private string? FindExistingModel()
    {
        var candidatePaths = new[]
        {
            DefaultModelFile,
            Path.Combine(DefaultModelsDir, "ggml-tiny.bin"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-base.bin"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-tiny.bin"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ggml-base.bin"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ggml-tiny.bin")
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 10_000_000)
            {
                return path;
            }
        }

        if (Directory.Exists(DefaultModelsDir))
        {
            var binFiles = Directory.GetFiles(DefaultModelsDir, "*.bin");
            if (binFiles.Length > 0)
            {
                return binFiles[0];
            }
        }

        return null;
    }

    public async Task DownloadModelAsync(string destinationPath)
    {
        if (_isDownloading) return;

        lock (_lock)
        {
            _isDownloading = true;
        }

        UpdateStatus("Downloading Whisper AI model (~140MB)...");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            string url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin";
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 147_951_456;
            using var contentStream = await response.Content.ReadAsStreamAsync();

            string tempFile = destinationPath + ".tmp";
            using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        int percent = (int)((totalRead * 100) / totalBytes);
                        DownloadProgressChanged?.Invoke(percent);
                    }
                }
            }

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            File.Move(tempFile, destinationPath);

            LoadModel(destinationPath);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Download failed: {ex.Message}. Check internet or place ggml-base.bin in models/.");
        }
        finally
        {
            lock (_lock)
            {
                _isDownloading = false;
            }
        }
    }

    public void LoadModel(string modelPath)
    {
        lock (_lock)
        {
            try
            {
                _whisperFactory?.Dispose();
                _whisperFactory = WhisperFactory.FromPath(modelPath);
                _isLoaded = true;
                UpdateStatus("Whisper Model Ready");
            }
            catch (Exception ex)
            {
                _isLoaded = false;
                UpdateStatus($"Failed to load Whisper: {ex.Message}");
            }
        }
    }

    public async Task<string> TranscribeAsync(float[] pcmAudio, string language = "auto")
    {
        if (pcmAudio == null || pcmAudio.Length < AudioCaptureService.TargetSampleRate * 0.25f)
        {
            return string.Empty;
        }

        if (!_isLoaded && _isDownloading)
        {
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(250);
                if (_isLoaded) break;
            }

            if (!_isLoaded)
            {
                return "[UltraDictate: Загрузка модели Whisper... Пожалуйста, подождите завершения]";
            }
        }

        if (!_isLoaded || _whisperFactory == null)
        {
            string? found = FindExistingModel();
            if (found != null)
            {
                LoadModel(found);
            }
            else
            {
                _ = InitializeModelAsync();
                return "[UltraDictate: Модель Whisper загружается. Повторите через минуту]";
            }
        }

        return await Task.Run(async () =>
        {
            try
            {
                using var wavStream = new MemoryStream();
                using (var writer = new BinaryWriter(wavStream, Encoding.UTF8, leaveOpen: true))
                {
                    int dataSize = pcmAudio.Length * 2;
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(dataSize + 36);
                    writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                    writer.Write(Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16); // Subchunk1Size
                    writer.Write((short)1); // AudioFormat PCM
                    writer.Write((short)1); // NumChannels Mono
                    writer.Write(AudioCaptureService.TargetSampleRate); // SampleRate 16000
                    writer.Write(AudioCaptureService.TargetSampleRate * 2); // ByteRate
                    writer.Write((short)2); // BlockAlign
                    writer.Write((short)16); // BitsPerSample
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write(dataSize);

                    for (int i = 0; i < pcmAudio.Length; i++)
                    {
                        short sample = (short)Math.Clamp((int)(pcmAudio[i] * 32767f), -32768, 32767);
                        writer.Write(sample);
                    }
                }
                wavStream.Position = 0;

                string lang = string.IsNullOrWhiteSpace(language) ? "auto" : language.ToLowerInvariant();
                var factory = _whisperFactory;
                if (factory == null) return string.Empty;
                var builder = factory.CreateBuilder();
                if (lang != "auto")
                {
                    builder.WithLanguage(lang);
                }
                else
                {
                    builder.WithLanguageDetection();
                }

                using var processor = builder.Build();
                var textBuilder = new StringBuilder();

                await foreach (var segment in processor.ProcessAsync(wavStream))
                {
                    if (!string.IsNullOrWhiteSpace(segment.Text))
                    {
                        textBuilder.Append(segment.Text.Trim()).Append(' ');
                    }
                }

                return textBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                return $"[UltraDictate ASR error: {ex.Message}]";
            }
        });
    }

    private void UpdateStatus(string status)
    {
        _statusMessage = status;
        StatusChanged?.Invoke(status);
    }

    public void Dispose()
    {
        _whisperFactory?.Dispose();
        _whisperFactory = null;
        GC.SuppressFinalize(this);
    }
}
