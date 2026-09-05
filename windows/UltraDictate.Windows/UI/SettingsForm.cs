using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    private CheckBox _trailingPeriodCheck = null!;

    private CheckBox _aiCleanupCheck = null!;
    private TextBox _aiBaseUrlText = null!;
    private TextBox _aiModelText = null!;
    private TextBox _aiKeyText = null!;

    public SettingsForm(AppSettings settings, Action<AppSettings> onSave)
    {
        _settings = settings;
        _onSave = onSave;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "UltraDictate — Settings";
        Size = new Size(620, 560);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 17, 23); // #0D1117 Deep Dark
        ForeColor = Color.FromArgb(240, 246, 252);
        Font = new Font("Segoe UI", 9.5f);

        // Header Panel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
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
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White
        };

        var subtitleLabel = new Label
        {
            Text = "DirectML & ONNX Speech Recognition • Local & Private",
            Location = new Point(24, 38),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(139, 148, 158)
        };

        var badgeLabel = new Label
        {
            Text = "DirectML Ready",
            Location = new Point(Width - 160, 22),
            Size = new Size(115, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(26, 44, 76),
            ForeColor = Color.FromArgb(88, 166, 255)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(badgeLabel);

        // Tab Control with custom dark owner-drawn tabs
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(20, 10),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(160, 36),
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

        var generalTab = new TabPage("General") { BackColor = Color.FromArgb(13, 17, 23) };
        var aiTab = new TabPage("AI Post-Processing") { BackColor = Color.FromArgb(13, 17, 23) };

        SetupGeneralTab(generalTab);
        SetupAITab(aiTab);

        tabControl.TabPages.Add(generalTab);
        tabControl.TabPages.Add(aiTab);

        // Bottom Action Panel
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
            Location = new Point(Width - 170, 14),
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
            Location = new Point(Width - 280, 14),
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
        int top = 25;

        // Card Panel for Settings
        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(560, 310),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 20;

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

        innerTop += 68;
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

        innerTop += 68;
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

        innerTop += 72;
        _trailingPeriodCheck = new CheckBox
        {
            Text = "Automatically remove trailing period from dictated text",
            Location = new Point(24, innerTop),
            Width = 460,
            Checked = _settings.RemoveTrailingPeriod,
            ForeColor = Color.FromArgb(201, 209, 217),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_trailingPeriodCheck);

        tab.Controls.Add(card);
    }

    private void SetupAITab(TabPage tab)
    {
        int top = 25;

        var card = new Panel
        {
            Location = new Point(20, top),
            Size = new Size(560, 310),
            BackColor = Color.FromArgb(22, 27, 34)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        int innerTop = 20;

        _aiCleanupCheck = new CheckBox
        {
            Text = "Enable AI Post-Processing Cleanup (Local Ollama / LM Studio)",
            Location = new Point(24, innerTop),
            Width = 510,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Checked = _settings.EnableAICleanup,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_aiCleanupCheck);

        innerTop += 45;
        card.Controls.Add(CreateLabel("API Base URL (e.g. Ollama http://localhost:11434/v1):", 24, innerTop));
        _aiBaseUrlText = new TextBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 500,
            Text = _settings.AIBaseUrl,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiBaseUrlText);

        innerTop += 62;
        card.Controls.Add(CreateLabel("Model Name (e.g. llama3.2, mistral, gpt-4o-mini):", 24, innerTop));
        _aiModelText = new TextBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 500,
            Text = _settings.AIModel,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiModelText);

        innerTop += 62;
        card.Controls.Add(CreateLabel("API Key (leave empty for local Ollama / LM Studio):", 24, innerTop));
        _aiKeyText = new TextBox
        {
            Location = new Point(24, innerTop + 24),
            Width = 500,
            Text = _settings.AIApiKey,
            UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_aiKeyText);

        innerTop += 55;
        var infoLabel = new Label
        {
            Text = "🔒 Privacy: Local Ollama runs 100% on your PC without transmitting speech or text online.",
            Location = new Point(24, innerTop),
            Width = 500,
            Height = 30,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(139, 148, 158)
        };
        card.Controls.Add(infoLabel);

        tab.Controls.Add(card);
    }

    private void SaveAndClose()
    {
        _settings.Hotkey = _hotkeyCombo.SelectedIndex == 1 ? "RightAlt" : "RightControl";
        _settings.TriggerMode = _triggerModeCombo.SelectedIndex == 1 ? "PressToToggle" : "HoldToDictate";
        _settings.Language = _languageCombo.SelectedIndex switch
        {
            1 => "ru",
            2 => "en",
            _ => "auto"
        };
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
