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

    private ComboBox _whisperModelCombo = null!;
    private Label _smallStatusLabel = null!;
    private Label _baseStatusLabel = null!;

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
        Text = "UltraDictate — Настройки";
        Size = new Size(680, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 17, 23); // #0D1117 Deep Obsidian
        ForeColor = Color.FromArgb(240, 246, 252);
        Font = new Font("Segoe UI", 9.5f);

        // 1. Top Header Banner
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = Color.FromArgb(22, 27, 34)
        };
        headerPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
        };

        var titleLabel = new Label
        {
            Text = "⚙️ Настройки UltraDictate",
            Location = new Point(24, 14),
            AutoSize = true,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White
        };

        var subtitleLabel = new Label
        {
            Text = "Локальное распознавание Whisper AI • 100% автономно и конфиденциально",
            Location = new Point(26, 42),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.8f),
            ForeColor = Color.FromArgb(139, 148, 158)
        };

        string activeModelName = _settings.WhisperModelType == "Base" ? "Whisper Base" : "Whisper Small";
        var badgeLabel = new Label
        {
            Text = $"● {activeModelName}",
            Location = new Point(Width - 195, 24),
            Size = new Size(155, 26),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(26, 44, 76),
            ForeColor = Color.FromArgb(88, 166, 255)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(badgeLabel);

        // 2. Modern Tab Navigation
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(22, 10),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(190, 40),
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

        var generalTab = new TabPage("Диктовка и ввод") { BackColor = Color.FromArgb(13, 17, 23) };
        var modelTab = new TabPage("Модель Whisper") { BackColor = Color.FromArgb(13, 17, 23) };
        var aiTab = new TabPage("AI Постобработка") { BackColor = Color.FromArgb(13, 17, 23) };

        SetupGeneralTab(generalTab);
        SetupModelTab(modelTab);
        SetupAITab(aiTab);

        tabControl.TabPages.Add(generalTab);
        tabControl.TabPages.Add(modelTab);
        tabControl.TabPages.Add(aiTab);

        // 3. Bottom Action Bar
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            BackColor = Color.FromArgb(22, 27, 34)
        };
        buttonPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawLine(pen, 0, 0, buttonPanel.Width, 0);
        };

        var saveButton = new Button
        {
            Text = "Сохранить настройки",
            DialogResult = DialogResult.OK,
            Size = new Size(165, 38),
            Location = new Point(Width - 200, 14),
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
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
            Size = new Size(95, 38),
            Location = new Point(Width - 310, 14),
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
        int top = 16;

        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(625, 380),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 18;

        card.Controls.Add(CreateLabel("Горячая клавиша диктовки (Push-to-Talk):", 24, innerTop));
        _hotkeyCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _hotkeyCombo.Items.AddRange(new object[] { "Правый Ctrl (Right Control) — По умолчанию", "Правый Alt (Right Alt)", "Caps Lock", "F8" });
        _hotkeyCombo.SelectedIndex = _settings.Hotkey switch
        {
            "RightAlt" => 1,
            "CapsLock" => 2,
            "F8" => 3,
            _ => 0
        };
        card.Controls.Add(_hotkeyCombo);

        innerTop += 66;
        card.Controls.Add(CreateLabel("Режим срабатывания клавиши:", 24, innerTop));
        _triggerModeCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _triggerModeCombo.Items.AddRange(new object[] { "Удерживать для записи (Hold to Dictate)", "Нажать для переключения (Start / Stop Toggle)" });
        _triggerModeCombo.SelectedIndex = _settings.TriggerMode == "PressToToggle" ? 1 : 0;
        card.Controls.Add(_triggerModeCombo);

        innerTop += 66;
        card.Controls.Add(CreateLabel("Язык распознавания речи:", 24, innerTop));
        _languageCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _languageCombo.Items.AddRange(new object[] { "Автоопределение (Рекомендуется)", "Русский (ru)", "English (en)" });
        _languageCombo.SelectedIndex = _settings.Language switch
        {
            "ru" => 1,
            "en" => 2,
            _ => 0
        };
        card.Controls.Add(_languageCombo);

        innerTop += 66;
        card.Controls.Add(CreateLabel("Способ вставки текста в приложения:", 24, innerTop));
        _insertionModeCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _insertionModeCombo.Items.AddRange(new object[] { "Быстрая вставка буфером обмена (Рекомендуется)", "Прямой ввод символов (SendInput Unicode)" });
        _insertionModeCombo.SelectedIndex = _settings.InsertionMode == "DirectTyping" ? 1 : 0;
        card.Controls.Add(_insertionModeCombo);

        innerTop += 68;
        _trailingPeriodCheck = new CheckBox
        {
            Text = "Автоматически удалять точку на конце надиктованной фразы",
            Location = new Point(24, innerTop),
            Width = 530,
            Checked = _settings.RemoveTrailingPeriod,
            ForeColor = Color.FromArgb(201, 209, 217),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_trailingPeriodCheck);

        tab.Controls.Add(card);
    }

    private void SetupModelTab(TabPage tab)
    {
        int top = 16;

        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(625, 380),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 20;

        var modelTitle = new Label
        {
            Text = "🧠 Локальные модели речи Whisper",
            Location = new Point(24, innerTop),
            AutoSize = true,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White
        };
        card.Controls.Add(modelTitle);

        innerTop += 34;
        card.Controls.Add(CreateLabel("Активный профиль модели:", 24, innerTop));
        _whisperModelCombo = new ComboBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 420,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _whisperModelCombo.Items.AddRange(new object[]
        {
            "Whisper Small (~465 МБ) — Высокая точность (Качество Mac) [Рекомендуется]",
            "Whisper Base (~140 МБ) — Быстрая и легкая для слабых ПК"
        });
        _whisperModelCombo.SelectedIndex = _settings.WhisperModelType == "Base" ? 1 : 0;
        card.Controls.Add(_whisperModelCombo);

        innerTop += 66;
        string smallFile = OnnxSpeechEngine.SmallModelFile;
        string baseFile = OnnxSpeechEngine.DefaultModelFile;
        bool smallExists = File.Exists(smallFile) && new FileInfo(smallFile).Length > 100_000_000;
        bool baseExists = File.Exists(baseFile) && new FileInfo(baseFile).Length > 10_000_000;

        _smallStatusLabel = new Label
        {
            Text = smallExists
                ? $"✓ Whisper Small: Установлена ({new FileInfo(smallFile).Length / (1024 * 1024)} МБ) • Отличное распознавание русской речи"
                : "⏳ Whisper Small: Не загружена (нажмите кнопку ниже для загрузки)",
            Location = new Point(24, innerTop),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = smallExists ? Color.FromArgb(63, 185, 80) : Color.FromArgb(240, 136, 62)
        };
        card.Controls.Add(_smallStatusLabel);

        innerTop += 24;
        _baseStatusLabel = new Label
        {
            Text = baseExists
                ? $"✓ Whisper Base: Установлена ({new FileInfo(baseFile).Length / (1024 * 1024)} МБ) • Мгновенный отклик"
                : "⏳ Whisper Base: Не загружена",
            Location = new Point(24, innerTop),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = baseExists ? Color.FromArgb(63, 185, 80) : Color.FromArgb(139, 148, 158)
        };
        card.Controls.Add(_baseStatusLabel);

        innerTop += 38;
        var descLabel = new Label
        {
            Text = "• Whisper Small: Рекомендуется для повседневного набора. Точно слышит окончания,\n" +
                   "  пунктуацию, деловую и техническую терминологию без повторов и галлюцинаций.\n" +
                   "• Whisper Base: Компактный вариант для ультрабыстрых коротких реплик.\n" +
                   "Все вычисления выполняются оффлайн на вашем компьютере через AVX2/DirectML.",
            Location = new Point(24, innerTop),
            Size = new Size(570, 70),
            ForeColor = Color.FromArgb(139, 148, 158)
        };
        card.Controls.Add(descLabel);

        innerTop += 80;
        var openFolderButton = new Button
        {
            Text = "📁 Открыть папку моделей",
            Location = new Point(24, innerTop),
            Size = new Size(200, 36),
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
        int top = 16;

        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(625, 380),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 16;

        _aiCleanupCheck = new CheckBox
        {
            Text = "Включить AI Постобработку (исправление грамматики и пунктуации через LLM)",
            Location = new Point(24, innerTop),
            Width = 570,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Checked = _settings.EnableAICleanup,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_aiCleanupCheck);

        innerTop += 34;
        card.Controls.Add(CreateLabel("Быстрые пресеты провайдеров:", 24, innerTop));
        innerTop += 22;

        var presetOllama = CreatePresetButton("🦙 Локальный Ollama", 24, innerTop, 160, () =>
        {
            _aiBaseUrlText.Text = "http://localhost:11434/v1";
            _aiModelText.Text = "llama3.2";
            _aiKeyText.Text = "";
        });

        var presetLMStudio = CreatePresetButton("🧪 Local LM Studio", 192, innerTop, 160, () =>
        {
            _aiBaseUrlText.Text = "http://localhost:1234/v1";
            _aiModelText.Text = "local-model";
            _aiKeyText.Text = "";
        });

        var presetOpenAI = CreatePresetButton("⚡ OpenAI / Cloud", 360, innerTop, 150, () =>
        {
            _aiBaseUrlText.Text = "https://api.openai.com/v1";
            _aiModelText.Text = "gpt-4o-mini";
        });

        card.Controls.Add(presetOllama);
        card.Controls.Add(presetLMStudio);
        card.Controls.Add(presetOpenAI);

        innerTop += 40;
        card.Controls.Add(CreateLabel("Адрес API (Base URL):", 24, innerTop));
        _aiBaseUrlText = new TextBox
        {
            Location = new Point(24, innerTop + 22),
            Width = 550,
            Text = _settings.AIBaseUrl,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiBaseUrlText);

        innerTop += 54;
        card.Controls.Add(CreateLabel("Название модели (Model):", 24, innerTop));
        _aiModelText = new TextBox
        {
            Location = new Point(24, innerTop + 22),
            Width = 550,
            Text = _settings.AIModel,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiModelText);

        innerTop += 54;
        card.Controls.Add(CreateLabel("API Ключ (не требуется для локальных Ollama / LM Studio):", 24, innerTop));
        _aiKeyText = new TextBox
        {
            Location = new Point(24, innerTop + 22),
            Width = 550,
            Text = _settings.AIApiKey,
            UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiKeyText);

        innerTop += 52;
        var testButton = new Button
        {
            Text = "⚡ Проверить подключение",
            Location = new Point(24, innerTop),
            Size = new Size(185, 32),
            BackColor = Color.FromArgb(33, 38, 45),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        testButton.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
        testButton.Click += async (s, e) =>
        {
            testButton.Enabled = false;
            _aiStatusLabel.Text = "⏳ Проверка подключения...";
            _aiStatusLabel.ForeColor = Color.FromArgb(201, 209, 217);

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                var testUrl = _aiBaseUrlText.Text.Trim().TrimEnd('/') + "/models";
                var res = await client.GetAsync(testUrl);
                if (res.IsSuccessStatusCode)
                {
                    _aiStatusLabel.Text = "✓ Подключение успешно!";
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
                _aiStatusLabel.Text = $"✗ Недоступно: {ex.Message}";
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
            Location = new Point(220, innerTop + 6),
            AutoSize = true,
            ForeColor = Color.FromArgb(139, 148, 158)
        };
        card.Controls.Add(_aiStatusLabel);

        tab.Controls.Add(card);
    }

    private Button CreatePresetButton(string text, int x, int y, int width, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 28),
            BackColor = Color.FromArgb(30, 36, 45),
            ForeColor = Color.FromArgb(201, 209, 217),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.2f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
        btn.Click += (s, e) => onClick();
        return btn;
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
        _settings.WhisperModelType = _whisperModelCombo.SelectedIndex == 1 ? "Base" : "Small";
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
