using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using UltraDictate.Windows.Core;

namespace UltraDictate.Windows.UI;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly AppSettings _settings;
    private readonly GlobalHotkeyListener _hotkeyListener;
    private readonly AudioCaptureService _audioCapture;
    private readonly OnnxSpeechEngine _speechEngine;
    private readonly RecordingHUD _recordingHUD;
    private bool _isBusy = false;

    public TrayApplicationContext(AppSettings settings)
    {
        _settings = settings;

        _recordingHUD = new RecordingHUD();
        _audioCapture = new AudioCaptureService();
        _speechEngine = new OnnxSpeechEngine();
        _hotkeyListener = new GlobalHotkeyListener();

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

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = contextMenu,
            Text = "UltraDictate — Hold Right Ctrl to dictate",
            Visible = true
        };

        _trayIcon.ShowBalloonTip(3000, "UltraDictate", "Ready! Hold Right Ctrl to dictate.", ToolTipIcon.Info);
    }

    private void OnHotkeyDown()
    {
        if (_isBusy) return;

        try
        {
            _audioCapture.StartRecording();
            _recordingHUD.ShowAtCursor();
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(3000, "UltraDictate Error", ex.Message, ToolTipIcon.Error);
        }
    }

    private async void OnHotkeyUp()
    {
        if (!_audioCapture.IsRecording) return;

        _recordingHUD.HideHUD();
        _isBusy = true;

        try
        {
            var pcm = _audioCapture.StopRecording();
            if (pcm.Length > 0)
            {
                string rawText = await _speechEngine.TranscribeAsync(pcm, _settings.Language);
                string finalText = await TextPostProcessor.ProcessAsync(rawText, _settings);

                if (!string.IsNullOrWhiteSpace(finalText))
                {
                    TextInputService.InsertText(finalText);
                }
            }
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(3000, "UltraDictate Error", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings, updated =>
        {
            SettingsManager.SaveSettings(updated);
            _trayIcon.Text = $"UltraDictate — {updated.Hotkey}";
        });
        form.ShowDialog();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "UltraDictate v1.0.0\n" +
            "Fast, private, local push-to-talk speech dictation for macOS & Windows.\n\n" +
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
