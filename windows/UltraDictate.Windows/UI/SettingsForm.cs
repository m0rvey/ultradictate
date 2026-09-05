using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using UltraDictate.Windows.Core;

namespace UltraDictate.Windows.UI;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _onSave;

    private ComboBox _hotkeyCombo = null!;
    private ComboBox _triggerModeCombo = null!;
    private ComboBox _languageCombo = null!;
    private ComboBox _insertionModeCombo = null!;
    private CheckBox _trailingPeriodCheck = null!;

    private CheckBox _aiCleanupCheck = null!;
    private TextBox _aiBaseUrlText = null!;
    private TextBox _aiModelText = null!;
    private TextBox _aiKeyText = null!;
    private Label _aiStatusLabel = null!;

    public SettingsForm(AppSettings settings, Action<AppSettings> onSave)
    {
        _settings = settings;
        _onSave = onSave;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "UltraDictate — Settings";
        Size = new Size(640, 600);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 17, 23); // #0D1117 Deep Obsidian
        ForeColor = Color.FromArgb(240, 246, 252);
        Font = new Font("Segoe UI", 9.5f);

        // Top Header Banner
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(22, 27, 34)
        };
        headerPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
        };

        var titleLabel = new Label
        {
            Text = "UltraDictate Preferences",
            Location = new Point(24, 14),
            AutoSize = true,
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Color.White
        };

        var subtitleLabel = new Label
        {
            Text = "Local Whisper AI Speech Recognition • 100% On-Device & Private",
            Location = new Point(24, 40),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(139, 148, 158)
        };

        var badgeLabel = new Label
        {
            Text = "Whisper Ready",
            Location = new Point(Width - 165, 22),
            Size = new Size(120, 26),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(26, 44, 76),
            ForeColor = Color.FromArgb(88, 166, 255)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(badgeLabel);

        // Modern Tab Navigation
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(20, 10),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(170, 38),
            SizeMode = TabSizeMode.Fixed
        };

        tabControl.DrawItem += (s, e) =>
        {
            var g = e.Graphics;
            var rect = tabControl.GetTabRect(e.Index);
            bool isSelected = tabControl.SelectedIndex == e.Index;

            var bg = isSelected ? Color.FromArgb(30, 38, 50) : Color.FromArgb(22, 27, 34);
            using var brush = new SolidBrush(bg);
            g.FillRectangle(brush, rect);

            if (isSelected)
            {
                using var activeBar = new SolidBrush(Color.FromArgb(88, 166, 255));
                g.FillRectangle(activeBar, rect.X, rect.Bottom - 3, rect.Width, 3);
            }

            var text = tabControl.TabPages[e.Index].Text;
            var textColor = isSelected ? Color.White : Color.FromArgb(139, 148, 158);
            using var textBrush = new SolidBrush(textColor);
            using var font = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular);

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(text, font, textBrush, rect, sf);
        };

        var generalTab = new TabPage("Dictation & Input") { BackColor = Color.FromArgb(13, 17, 23) };
        var aiTab = new TabPage("AI Post-Processing") { BackColor = Color.FromArgb(13, 17, 23) };
        var modelTab = new TabPage("Speech Model") { BackColor = Color.FromArgb(13, 17, 23) };

        SetupGeneralTab(generalTab);
        SetupAITab(aiTab);
        SetupModelTab(modelTab);

        tabControl.TabPages.Add(generalTab);
        tabControl.TabPages.Add(modelTab);
        tabControl.TabPages.Add(aiTab);

        // Bottom Action Bar
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = Color.FromArgb(22, 27, 34)
        };
        buttonPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawLine(pen, 0, 0, buttonPanel.Width, 0);
        };

        var saveButton = new Button
        {
            Text = "Save Changes",
            DialogResult = DialogResult.OK,
            Size = new Size(130, 36),
            Location = new Point(Width - 175, 14),
            BackColor = Color.FromArgb(35, 134, 54), // GitHub green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += (s, e) => SaveAndClose();

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(95, 36),
            Location = new Point(Width - 285, 14),
            BackColor = Color.FromArgb(33, 38, 45),
            ForeColor = Color.FromArgb(201, 209, 217),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        cancelButton.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
        cancelButton.Click += (s, e) => Close();

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);

        Controls.Add(tabControl);
        Controls.Add(buttonPanel);
        Controls.Add(headerPanel);
    }

    private void SetupGeneralTab(TabPage tab)
    {
        int top = 20;

        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(580, 370),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 18;

        card.Controls.Add(CreateLabel("Push-to-Talk Hotkey:", 24, innerTop));
        _hotkeyCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _hotkeyCombo.Items.AddRange(new object[] { "Right Control", "Right Alt", "Caps Lock", "F8" });
        _hotkeyCombo.SelectedItem = _settings.Hotkey == "RightAlt" ? "Right Alt" : "Right Control";
        card.Controls.Add(_hotkeyCombo);

        innerTop += 66;
        card.Controls.Add(CreateLabel("Trigger Mode:", 24, innerTop));
        _triggerModeCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _triggerModeCombo.Items.AddRange(new object[] { "Hold to Dictate (Push-to-Talk)", "Press to Toggle (Start / Stop)" });
        _triggerModeCombo.SelectedIndex = _settings.TriggerMode == "PressToToggle" ? 1 : 0;
        card.Controls.Add(_triggerModeCombo);

        innerTop += 66;
        card.Controls.Add(CreateLabel("Recognition Language:", 24, innerTop));
        _languageCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _languageCombo.Items.AddRange(new object[] { "Auto-detect (Recommended)", "Russian (ru)", "English (en)" });
        _languageCombo.SelectedIndex = _settings.Language switch
        {
            "ru" => 1,
            "en" => 2,
            _ => 0
        };
        card.Controls.Add(_languageCombo);

        innerTop += 66;
        card.Controls.Add(CreateLabel("Text Insertion Method:", 24, innerTop));
        _insertionModeCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _insertionModeCombo.Items.AddRange(new object[] { "Clipboard Paste (Instant & Universal)", "Direct Keystrokes (SendInput Unicode)" });
        _insertionModeCombo.SelectedIndex = _settings.InsertionMode == "DirectTyping" ? 1 : 0;
        card.Controls.Add(_insertionModeCombo);

        innerTop += 68;
        _trailingPeriodCheck = new CheckBox
        {
            Text = "Automatically remove trailing period from dictated phrases",
            Location = new Point(24, innerTop),
            Width = 500,
            Checked = _settings.RemoveTrailingPeriod,
            ForeColor = Color.FromArgb(201, 209, 217),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_trailingPeriodCheck);

        tab.Controls.Add(card);
    }

    private void SetupModelTab(TabPage tab)
    {
        int top = 20;

        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(580, 370),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 24;

        var modelTitle = new Label
        {
            Text = "Offline Whisper Engine",
            Location = new Point(24, innerTop),
            AutoSize = true,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White
        };
        card.Controls.Add(modelTitle);

        innerTop += 34;
        string modelFile = OnnxSpeechEngine.DefaultModelFile;
        bool exists = File.Exists(modelFile);

        var statusDesc = new Label
        {
            Text = exists
                ? $"✓ Model status: Ready on disk (ggml-base.bin, {new FileInfo(modelFile).Length / (1024 * 1024)} MB)"
                : "⏳ Model status: Will auto-download on first launch (~140 MB)",
            Location = new Point(24, innerTop),
            AutoSize = true,
            ForeColor = exists ? Color.FromArgb(63, 185, 80) : Color.FromArgb(240, 136, 62)
        };
        card.Controls.Add(statusDesc);

        innerTop += 45;
        var descLabel = new Label
        {
            Text = "Whisper runs 100% locally on your machine with high accuracy for Russian and English.\n" +
                   "Audio is processed in-memory and never sent to any server.",
            Location = new Point(24, innerTop),
            Size = new Size(520, 40),
            ForeColor = Color.FromArgb(139, 148, 158)
        };
        card.Controls.Add(descLabel);

        innerTop += 65;
        var openFolderButton = new Button
        {
            Text = "📁 Open Models Folder",
            Location = new Point(24, innerTop),
            Size = new Size(180, 36),
            BackColor = Color.FromArgb(33, 38, 45),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        openFolderButton.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
        openFolderButton.Click += (s, e) =>
        {
            try
            {
                Directory.CreateDirectory(OnnxSpeechEngine.DefaultModelsDir);
                Process.Start(new ProcessStartInfo
                {
                    FileName = OnnxSpeechEngine.DefaultModelsDir,
                    UseShellExecute = true
                });
            }
            catch { }
        };
        card.Controls.Add(openFolderButton);

        tab.Controls.Add(card);
    }

    private void SetupAITab(TabPage tab)
    {
        int top = 20;

        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(580, 370),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 18;

        _aiCleanupCheck = new CheckBox
        {
            Text = "Enable AI Post-Processing Cleanup (Local Ollama / LM Studio)",
            Location = new Point(24, innerTop),
            Width = 520,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Checked = _settings.EnableAICleanup,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_aiCleanupCheck);

        innerTop += 42;
        card.Controls.Add(CreateLabel("API Base URL (e.g. Ollama http://localhost:11434/v1):", 24, innerTop));
        _aiBaseUrlText = new TextBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 520,
            Text = _settings.AIBaseUrl,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiBaseUrlText);

        innerTop += 60;
        card.Controls.Add(CreateLabel("Model Name (e.g. llama3.2, mistral, gpt-4o-mini):", 24, innerTop));
        _aiModelText = new TextBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 520,
            Text = _settings.AIModel,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiModelText);

        innerTop += 60;
        card.Controls.Add(CreateLabel("API Key (leave empty for local Ollama / LM Studio):", 24, innerTop));
        _aiKeyText = new TextBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 520,
            Text = _settings.AIApiKey,
            UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiKeyText);

        innerTop += 58;
        var testButton = new Button
        {
            Text = "⚡ Test Connection",
            Location = new Point(24, innerTop),
            Size = new Size(150, 32),
            BackColor = Color.FromArgb(33, 38, 45),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        testButton.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
        testButton.Click += async (s, e) =>
        {
            testButton.Enabled = false;
            _aiStatusLabel.Text = "Testing connection...";
            _aiStatusLabel.ForeColor = Color.FromArgb(201, 209, 217);

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                var testUrl = _aiBaseUrlText.Text.Trim().TrimEnd('/') + "/models";
                var res = await client.GetAsync(testUrl);
                if (res.IsSuccessStatusCode)
                {
                    _aiStatusLabel.Text = "✓ Connection successful!";
                    _aiStatusLabel.ForeColor = Color.FromArgb(63, 185, 80);
                }
                else
                {
                    _aiStatusLabel.Text = $"HTTP {(int)res.StatusCode}: {res.ReasonPhrase}";
                    _aiStatusLabel.ForeColor = Color.FromArgb(240, 136, 62);
                }
            }
            catch (Exception ex)
            {
                _aiStatusLabel.Text = $"✗ Offline: {ex.Message}";
                _aiStatusLabel.ForeColor = Color.FromArgb(248, 81, 73);
            }
            finally
            {
                testButton.Enabled = true;
            }
        };
        card.Controls.Add(testButton);

        _aiStatusLabel = new Label
        {
            Text = "",
            Location = new Point(185, innerTop + 6),
            AutoSize = true,
            ForeColor = Color.FromArgb(139, 148, 158)
        };
        card.Controls.Add(_aiStatusLabel);

        tab.Controls.Add(card);
    }

    private void SaveAndClose()
    {
        _settings.Hotkey = _hotkeyCombo.SelectedIndex switch
        {
            1 => "RightAlt",
            2 => "CapsLock",
            3 => "F8",
            _ => "RightControl"
        };
        _settings.TriggerMode = _triggerModeCombo.SelectedIndex == 1 ? "PressToToggle" : "HoldToDictate";
        _settings.Language = _languageCombo.SelectedIndex switch
        {
            1 => "ru",
            2 => "en",
            _ => "auto"
        };
        _settings.InsertionMode = _insertionModeCombo.SelectedIndex == 1 ? "DirectTyping" : "ClipboardPaste";
        _settings.RemoveTrailingPeriod = _trailingPeriodCheck.Checked;

        _settings.EnableAICleanup = _aiCleanupCheck.Checked;
        _settings.AIBaseUrl = _aiBaseUrlText.Text.Trim();
        _settings.AIModel = _aiModelText.Text.Trim();
        _settings.AIApiKey = _aiKeyText.Text.Trim();

        _onSave(_settings);
        Close();
    }

    private static Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(201, 209, 217)
        };
    }
}
