using System;
using System.Collections.Generic;
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
    public string CurrentModelPath { get; private set; } = string.Empty;
    public string CurrentModelName => string.IsNullOrEmpty(CurrentModelPath) ? "None" : Path.GetFileName(CurrentModelPath);

    public event Action<string>? StatusChanged;
    public event Action<int>? DownloadProgressChanged;

    public static readonly string DefaultModelsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UltraDictate",
        "models"
    );

    public static readonly string DefaultModelFile = Path.Combine(DefaultModelsDir, "ggml-base.bin");
    public static readonly string SmallModelFile = Path.Combine(DefaultModelsDir, "ggml-small.bin");

    public OnnxSpeechEngine()
    {
        Task.Run(InitializeModelAsync);
    }

    public async Task InitializeModelAsync()
    {
        try
        {
            Directory.CreateDirectory(DefaultModelsDir);

            // 1. Check if model exists (Small prioritized if present, then Base)
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

    public string? FindExistingModel(string? preferredType = null)
    {
        // 1. If Small is preferred or if no preference, check small first (superior quality)
        if (preferredType == "Small" || string.IsNullOrEmpty(preferredType))
        {
            var smallCandidates = new[]
            {
                SmallModelFile,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-small.bin"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ggml-small.bin")
            };

            foreach (var path in smallCandidates)
            {
                if (File.Exists(path) && new FileInfo(path).Length > 100_000_000)
                {
                    return path;
                }
            }
        }

        // 2. Base candidates
        var baseCandidates = new[]
        {
            DefaultModelFile,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-base.bin"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ggml-base.bin"),
            Path.Combine(DefaultModelsDir, "ggml-tiny.bin"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-tiny.bin"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ggml-tiny.bin")
        };

        foreach (var path in baseCandidates)
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

    public void SwitchModel(string modelType)
    {
        string? model = FindExistingModel(modelType);
        if (model != null && model != CurrentModelPath)
        {
            LoadModel(model);
        }
    }

    public async Task DownloadModelAsync(string destinationPath)
    {
        if (_isDownloading) return;

        lock (_lock)
        {
            _isDownloading = true;
        }

        string filename = Path.GetFileName(destinationPath);
        UpdateStatus($"Downloading {filename}...");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            string url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{filename}";
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? (filename.Contains("small") ? 487_000_000 : 147_951_456);
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
            UpdateStatus($"Download failed: {ex.Message}. Check connection or place model in models/.");
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
                CurrentModelPath = modelPath;
                UpdateStatus($"Model Ready ({Path.GetFileName(modelPath)})");
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
                return "[UltraDictate: Загрузка модели Whisper... Пожалуйста, подождите]";
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

                // Optimal multi-threading
                builder.WithThreads(Math.Max(1, Environment.ProcessorCount - 1));

                // Context prompt conditioning to boost Russian vocabulary and eliminate case misrecognitions
                if (lang == "ru" || lang == "auto")
                {
                    builder.WithPrompt("Здравствуйте. Это грамотная русская речь, чёткая диктовка с соблюдением правил грамматики и пунктуации.");
                }
                else if (lang == "en")
                {
                    builder.WithPrompt("Hello. This is clear speech dictation in English with proper grammar and punctuation.");
                }

                // Deterministic greedy decoding & no-speech filtering to eliminate looping hallucinations
                builder.WithTemperature(0.0f);
                builder.WithNoSpeechThreshold(0.6f);

                using var processor = builder.Build();
                var seenSegments = new List<string>();

                await foreach (var segment in processor.ProcessAsync(wavStream))
                {
                    string text = segment.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    // Clean up repetitions and loop hallucinations
                    if (seenSegments.Count > 0)
                    {
                        string prev = seenSegments[^1];
                        if (string.Equals(prev, text, StringComparison.OrdinalIgnoreCase)) continue;
                        if (prev.Contains(text, StringComparison.OrdinalIgnoreCase) && text.Length > 8) continue;
                        if (text.Contains(prev, StringComparison.OrdinalIgnoreCase) && prev.Length > 8)
                        {
                            seenSegments[^1] = text;
                            continue;
                        }
                    }

                    seenSegments.Add(text);
                }

                return string.Join(" ", seenSegments).Trim();
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
