using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using UltraDictate.Windows.Core;

namespace UltraDictate.Windows.UI;

public class ModelSetupForm : Form
{
    private readonly AppSettings _settings;
    private string _selectedModel = "Small"; // "Small" or "Base"
    private Panel _cardSmall = null!;
    private Panel _cardBase = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private Button _startButton = null!;
    private bool _isDownloading = false;

    public ModelSetupForm(AppSettings settings)
    {
        _settings = settings;
        _selectedModel = string.IsNullOrEmpty(settings.WhisperModelType) ? "Small" : settings.WhisperModelType;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "UltraDictate — Выбор голосовой модели";
        ClientSize = new Size(660, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 17, 23); // #0D1117 Deep Obsidian
        ForeColor = Color.FromArgb(240, 246, 252);
        Font = new Font("Segoe UI", 9.5f);

        // Header Panel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(22, 27, 34)
        };
        headerPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
        };

        var titleLabel = new Label
        {
            Text = "🎙️ Добро пожаловать в UltraDictate",
            Location = new Point(24, 15),
            AutoSize = true,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White
        };

        var subtitleLabel = new Label
        {
            Text = "Выберите профиль модели Whisper AI для локального распознавания речи:",
            Location = new Point(26, 45),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(139, 148, 158)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);

        // Check file status on disk
        string smallFile = OnnxSpeechEngine.SmallModelFile;
        string baseFile = OnnxSpeechEngine.DefaultModelFile;
        bool smallExists = File.Exists(smallFile) && new FileInfo(smallFile).Length > 100_000_000;
        bool baseExists = File.Exists(baseFile) && new FileInfo(baseFile).Length > 10_000_000;

        int cardWidth = ClientSize.Width - 48; // 612px

        // Card 1: Whisper Small (Recommended)
        _cardSmall = CreateModelCard(
            "Small",
            "Whisper Small (Средняя модель)",
            "★ РЕКОМЕНДУЕТСЯ",
            "Максимальная точность (Mac-grade), безошибочная пунктуация и термины.\n" +
            "Размер: ~465 МБ • Отклик: ~1 сек • RAM: ~1 ГБ • Рекомендуется для всех ПК",
            smallExists ? $"● Установлена на диске ({new FileInfo(smallFile).Length / (1024 * 1024)} МБ)" : "⏳ Будет автоматически загружена (~465 МБ)",
            smallExists,
            Color.FromArgb(88, 166, 255),
            24, 95, cardWidth, 134
        );

        // Card 2: Whisper Base
        _cardBase = CreateModelCard(
            "Base",
            "Whisper Base (Маленькая модель)",
            "БЫСТРАЯ & ЛЁГКАЯ",
            "Мгновенный отклик и минимальное потребление системных ресурсов.\n" +
            "Размер: ~140 МБ • Отклик: < 0.5 сек • RAM: ~400 МБ • Для слабых ПК и ноутбуков",
            baseExists ? $"● Установлена на диске ({new FileInfo(baseFile).Length / (1024 * 1024)} МБ)" : "⏳ Будет автоматически загружена (~140 МБ)",
            baseExists,
            Color.FromArgb(139, 148, 158),
            24, 243, cardWidth, 134
        );

        UpdateCardSelection();

        // Bottom Action Bar
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 95,
            BackColor = Color.FromArgb(22, 27, 34)
        };
        bottomPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(24, 18),
            Size = new Size(bottomPanel.ClientSize.Width - 280, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false,
            Style = ProgressBarStyle.Continuous
        };

        _statusLabel = new Label
        {
            Text = "100% локально и конфиденциально. Аудио никогда не передаётся по сети.",
            Location = new Point(24, 22),
            Size = new Size(bottomPanel.ClientSize.Width - 280, 48),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(139, 148, 158)
        };

        _startButton = new Button
        {
            Text = "Начать использование →",
            Size = new Size(220, 42),
            Location = new Point(bottomPanel.ClientSize.Width - 244, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(35, 134, 54), // GitHub green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _startButton.FlatAppearance.BorderSize = 0;
        _startButton.Click += async (s, e) => await OnStartClickedAsync();

        bottomPanel.Controls.Add(_progressBar);
        bottomPanel.Controls.Add(_statusLabel);
        bottomPanel.Controls.Add(_startButton);

        Controls.Add(_cardBase);
        Controls.Add(_cardSmall);
        Controls.Add(bottomPanel);
        Controls.Add(headerPanel);
    }

    private Panel CreateModelCard(
        string modelType,
        string title,
        string badge,
        string description,
        string statusText,
        bool isInstalled,
        Color badgeColor,
        int x, int y, int width, int height)
    {
        var card = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Color.FromArgb(22, 27, 34),
            Cursor = Cursors.Hand
        };

        Action selectCard = () =>
        {
            if (_isDownloading) return;
            _selectedModel = modelType;
            UpdateCardSelection();
        };

        card.Click += (s, e) => selectCard();

        // Title on the left (indented past the radio selection circle)
        var titleLbl = new Label
        {
            Text = title,
            Location = new Point(50, 14),
            AutoSize = true,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White
        };
        titleLbl.Click += (s, e) => selectCard();

        // Badge pill on the right
        var badgeLbl = new Label
        {
            Text = badge,
            Location = new Point(card.Width - 170, 14),
            Size = new Size(150, 22),
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = badgeColor,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        badgeLbl.Click += (s, e) => selectCard();

        // 2-line description with generous horizontal room
        var descLbl = new Label
        {
            Text = description,
            Location = new Point(50, 44),
            Size = new Size(card.Width - 70, 46),
            Font = new Font("Segoe UI", 8.8f),
            ForeColor = Color.FromArgb(201, 209, 217),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        descLbl.Click += (s, e) => selectCard();

        // Status indicator (installed vs pending download)
        var statusLbl = new Label
        {
            Text = statusText,
            Location = new Point(50, 98),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = isInstalled ? Color.FromArgb(63, 185, 80) : Color.FromArgb(240, 136, 62)
        };
        statusLbl.Click += (s, e) => selectCard();

        card.Controls.Add(titleLbl);
        card.Controls.Add(badgeLbl);
        card.Controls.Add(descLbl);
        card.Controls.Add(statusLbl);

        card.Paint += (s, e) =>
        {
            bool isSelected = _selectedModel == modelType;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Outer border
            using var pen = new Pen(isSelected ? Color.FromArgb(88, 166, 255) : Color.FromArgb(48, 54, 61), isSelected ? 2f : 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);

            // Left selection circle (Radio / Checkmark)
            var circleRect = new Rectangle(18, 15, 20, 20);
            if (isSelected)
            {
                // Selected: Accent blue background with white checkmark
                using var checkBg = new SolidBrush(Color.FromArgb(31, 111, 235));
                e.Graphics.FillEllipse(checkBg, circleRect);

                using var whiteBrush = new SolidBrush(Color.White);
                using var checkFont = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString("✓", checkFont, whiteBrush, new RectangleF(18, 14, 20, 20), sf);
            }
            else
            {
                // Unselected: Dark slate circle outline
                using var borderPen = new Pen(Color.FromArgb(72, 79, 88), 1.5f);
                e.Graphics.DrawEllipse(borderPen, circleRect);
            }
        };

        return card;
    }

    private void UpdateCardSelection()
    {
        _cardSmall.Invalidate();
        _cardBase.Invalidate();
    }

    private async Task OnStartClickedAsync()
    {
        if (_isDownloading) return;

        _settings.WhisperModelType = _selectedModel;
        string targetFile = _selectedModel == "Small" ? OnnxSpeechEngine.SmallModelFile : OnnxSpeechEngine.DefaultModelFile;

        // If model already exists on disk, finish immediately
        if (File.Exists(targetFile) && new FileInfo(targetFile).Length > 10_000_000)
        {
            _settings.FirstRunCompleted = true;
            SettingsManager.Save(_settings);
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        // Otherwise, download model with live progress
        _isDownloading = true;
        _startButton.Enabled = false;
        _progressBar.Visible = true;
        _statusLabel.Location = new Point(24, 38);
        _statusLabel.Text = $"Загрузка Whisper {_selectedModel}... Пожалуйста, подождите.";
        _statusLabel.ForeColor = Color.FromArgb(88, 166, 255);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            string filename = Path.GetFileName(targetFile);
            string url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{filename}";

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? (_selectedModel == "Small" ? 487_000_000 : 147_951_456);
            using var contentStream = await response.Content.ReadAsStreamAsync();

            string tempFile = targetFile + ".tmp";
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
                        _progressBar.Value = Math.Clamp(percent, 0, 100);
                        _statusLabel.Text = $"Загрузка Whisper {_selectedModel}: {percent}% ({totalRead / (1024 * 1024)} МБ из {totalBytes / (1024 * 1024)} МБ)";
                    }
                }
            }

            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }
            File.Move(tempFile, targetFile);

            _settings.FirstRunCompleted = true;
            SettingsManager.Save(_settings);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Ошибка загрузки: {ex.Message}. Попробуйте снова.";
            _statusLabel.ForeColor = Color.FromArgb(248, 81, 73);
            _startButton.Enabled = true;
            _isDownloading = false;
        }
    }
}
