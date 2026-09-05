using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using UltraDictate.Windows.Core;

namespace UltraDictate.Windows.UI;

public class TrayApplicationContext : ApplicationContext
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private readonly NotifyIcon _trayIcon;
    private readonly AppSettings _settings;
    private readonly GlobalHotkeyListener _hotkeyListener;
    private readonly AudioCaptureService _audioCapture;
    private readonly OnnxSpeechEngine _speechEngine;
    private readonly RecordingHUD _recordingHUD;
    private IntPtr _targetWindow = IntPtr.Zero;
    private bool _isBusy = false;

    public TrayApplicationContext(AppSettings settings)
    {
        _settings = settings;

        _recordingHUD = new RecordingHUD();
        _audioCapture = new AudioCaptureService();
        _speechEngine = new OnnxSpeechEngine();
        _hotkeyListener = new GlobalHotkeyListener();

        ApplyHotkeySettings();

        _audioCapture.AudioLevelChanged += level => _recordingHUD.UpdateAudioLevel(level);

        _hotkeyListener.HotkeyDown += OnHotkeyDown;
        _hotkeyListener.HotkeyUp += OnHotkeyUp;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("UltraDictate (Active)", null, null).Enabled = false;
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Settings...", null, (s, e) => ShowSettings());
        contextMenu.Items.Add("About UltraDictate", null, (s, e) => ShowAbout());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApp());

        Icon trayIcon = SystemIcons.Application;
        try
        {
            var resIcon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(resIcon))
            {
                trayIcon = new Icon(resIcon);
            }
        }
        catch { }

        _trayIcon = new NotifyIcon
        {
            Icon = trayIcon,
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _speechEngine.StatusChanged += status =>
        {
            UpdateTrayTooltip($"UltraDictate — {status}");
        };

        _speechEngine.DownloadProgressChanged += percent =>
        {
            if (percent % 25 == 0)
            {
                _trayIcon.ShowBalloonTip(2000, "UltraDictate", $"Downloading AI Speech Model: {percent}%", ToolTipIcon.Info);
            }
        };

        UpdateTrayTooltip($"UltraDictate — {_settings.Hotkey}");
        _trayIcon.ShowBalloonTip(3000, "UltraDictate", $"Ready! {_settings.Hotkey} to dictate.", ToolTipIcon.Info);
    }

    private void ApplyHotkeySettings()
    {
        _hotkeyListener.TargetVkCode = _settings.Hotkey switch
        {
            "RightAlt" => 0xA5,
            "CapsLock" => 0x14,
            "F8" => 0x77,
            _ => 0xA3 // Right Control
        };
    }

    private void UpdateTrayTooltip(string text)
    {
        try
        {
            if (text.Length > 63) text = text.Substring(0, 60) + "...";
            _trayIcon.Text = text;
        }
        catch { }
    }

    private void OnHotkeyDown()
    {
        if (_isBusy) return;

        if (_settings.TriggerMode == "PressToToggle")
        {
            if (_audioCapture.IsRecording)
            {
                _ = StopAndTranscribeAsync();
            }
            else
            {
                StartRecording();
            }
        }
        else
        {
            StartRecording();
        }
    }

    private void OnHotkeyUp()
    {
        if (_settings.TriggerMode == "HoldToDictate")
        {
            _ = StopAndTranscribeAsync();
        }
    }

    private void StartRecording()
    {
        if (_isBusy || _audioCapture.IsRecording) return;

        try
        {
            // Capture currently focused window before HUD displays
            _targetWindow = GetForegroundWindow();
            _audioCapture.StartRecording();
            _recordingHUD.ShowAtCursor();
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(3000, "UltraDictate Error", ex.Message, ToolTipIcon.Error);
        }
    }

    private async Task StopAndTranscribeAsync()
    {
        if (!_audioCapture.IsRecording || _isBusy) return;

        _isBusy = true;
        _recordingHUD.SetTranscribing();

        try
        {
            var pcm = _audioCapture.StopRecording();
            if (pcm.Length > 0)
            {
                string rawText = await _speechEngine.TranscribeAsync(pcm, _settings.Language);
                string finalText = await TextPostProcessor.ProcessAsync(rawText, _settings);

                if (!string.IsNullOrWhiteSpace(finalText))
                {
                    TextInputService.InsertText(finalText, _settings.InsertionMode, _targetWindow);
                }
            }
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(3000, "UltraDictate Error", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            _recordingHUD.HideHUD();
            _isBusy = false;
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings, updated =>
        {
            SettingsManager.SaveSettings(updated);
            ApplyHotkeySettings();
            UpdateTrayTooltip($"UltraDictate — {updated.Hotkey}");
        });
        form.ShowDialog();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "UltraDictate v1.0.1\n" +
            "Fast, private, local speech dictation for macOS & Windows.\n\n" +
            "Speech Engine: Local Whisper AI (Whisper.net / ONNX)\n" +
            "Hardware Acceleration: DirectML (GPU / NPU) & Apple Silicon (ANE)\n" +
            "AI Post-Processing: Local Ollama & LM Studio ready\n\n" +
            "Author: m0rvey (github.com/m0rvey/ultradictate)\n" +
            "Licensed under the MIT License.",
            "About UltraDictate",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ExitApp()
    {
        _trayIcon.Visible = false;
        _hotkeyListener.Dispose();
        _audioCapture.Dispose();
        _speechEngine.Dispose();
        _recordingHUD.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _hotkeyListener.Dispose();
            _audioCapture.Dispose();
            _speechEngine.Dispose();
            _recordingHUD.Dispose();
        }
        base.Dispose(disposing);
    }
}
