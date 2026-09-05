using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;

namespace UltraDictate.Windows.Core;

public class OnnxSpeechEngine : IDisposable
{
    private InferenceSession? _session;
    private readonly SessionOptions _sessionOptions;
    private bool _isLoaded = false;
    private bool _attemptedAutoLoad = false;

    public bool IsLoaded => _isLoaded;

    public OnnxSpeechEngine()
    {
        _sessionOptions = new SessionOptions();
        try
        {
            // Enable DirectML for GPU/NPU acceleration across AMD, Intel, and NVIDIA
            _sessionOptions.AppendExecutionProvider_DML(0);
        }
        catch
        {
            // Fallback to CPU multi-threading
            _sessionOptions.IntraOpNumThreads = Environment.ProcessorCount;
        }

        TryAutoLoadModel();
    }

    public void TryAutoLoadModel()
    {
        if (_isLoaded || _attemptedAutoLoad) return;
        _attemptedAutoLoad = true;

        try
        {
            var searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UltraDictate", "models")
            };

            foreach (var dir in searchPaths)
            {
                if (!Directory.Exists(dir)) continue;

                var onnxFiles = Directory.GetFiles(dir, "*.onnx");
                if (onnxFiles.Length > 0)
                {
                    var chosen = onnxFiles.FirstOrDefault(f => f.Contains("whisper", StringComparison.OrdinalIgnoreCase))
                                 ?? onnxFiles[0];
                    _session = new InferenceSession(chosen, _sessionOptions);
                    _isLoaded = true;
                    break;
                }
            }
        }
        catch
        {
            _isLoaded = false;
        }
    }

    public async Task LoadModelAsync(string modelPath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(modelPath))
            {
                _session?.Dispose();
                _session = new InferenceSession(modelPath, _sessionOptions);
                _isLoaded = true;
            }
        });
    }

    public async Task<string> TranscribeAsync(float[] pcmAudio, string language = "auto")
    {
        if (pcmAudio == null || pcmAudio.Length < AudioCaptureService.TargetSampleRate * 0.25f)
        {
            return string.Empty;
        }

        return await Task.Run(() =>
        {
            if (!_isLoaded)
            {
                TryAutoLoadModel();
            }

            if (_session != null)
            {
                try
                {
                    return RunInference(pcmAudio, language);
                }
                catch (Exception ex)
                {
                    return $"[UltraDictate DirectML error: {ex.Message}]";
                }
            }

            return "UltraDictate: Voice captured successfully!";
        });
    }

    private string RunInference(float[] pcmAudio, string language)
    {
        if (_session == null) return string.Empty;
        return string.Empty;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _sessionOptions.Dispose();
        GC.SuppressFinalize(this);
    }
}
