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
        Size = new Size(580, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 17, 23); // Dark theme #0D1117
        ForeColor = Color.FromArgb(240, 246, 252);
        Font = new Font("Segoe UI", 9.5f);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Top,
            Height = 410,
            Padding = new Point(16, 8)
        };

        var generalTab = new TabPage("General") { BackColor = Color.FromArgb(22, 27, 34) };
        var aiTab = new TabPage("AI Cleanup (Ollama & Cloud)") { BackColor = Color.FromArgb(22, 27, 34) };

        SetupGeneralTab(generalTab);
        SetupAITab(aiTab);

        tabControl.TabPages.Add(generalTab);
        tabControl.TabPages.Add(aiTab);

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(13, 17, 23)
        };

        var saveButton = new Button
        {
            Text = "Save Changes",
            DialogResult = DialogResult.OK,
            Size = new Size(130, 36),
            Location = new Point(Width - 165, 12),
            BackColor = Color.FromArgb(35, 134, 54),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += (s, e) => SaveAndClose();

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(100, 36),
            Location = new Point(Width - 275, 12),
            BackColor = Color.FromArgb(33, 38, 45),
            ForeColor = Color.FromArgb(201, 209, 217),
            FlatStyle = FlatStyle.Flat
        };
        cancelButton.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
        cancelButton.Click += (s, e) => Close();

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);

        Controls.Add(tabControl);
        Controls.Add(buttonPanel);
    }

    private void SetupGeneralTab(TabPage tab)
    {
        int top = 20;

        tab.Controls.Add(CreateLabel("Push-to-Talk Hotkey:", 25, top));
        _hotkeyCombo = new ComboBox
        {
            Location = new Point(25, top + 25),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White
        };
        _hotkeyCombo.Items.AddRange(new object[] { "Right Control", "Right Alt", "Caps Lock", "F8" });
        _hotkeyCombo.SelectedItem = _settings.Hotkey == "RightAlt" ? "Right Alt" : "Right Control";
        tab.Controls.Add(_hotkeyCombo);

        top += 70;
        tab.Controls.Add(CreateLabel("Trigger Mode:", 25, top));
        _triggerModeCombo = new ComboBox
        {
            Location = new Point(25, top + 25),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White
        };
        _triggerModeCombo.Items.AddRange(new object[] { "Hold to Dictate (Push-to-Talk)", "Press to Toggle (Start / Stop)" });
        _triggerModeCombo.SelectedIndex = _settings.TriggerMode == "PressToToggle" ? 1 : 0;
        tab.Controls.Add(_triggerModeCombo);

        top += 70;
        tab.Controls.Add(CreateLabel("Recognition Language:", 25, top));
        _languageCombo = new ComboBox
        {
            Location = new Point(25, top + 25),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White
        };
        _languageCombo.Items.AddRange(new object[] { "Auto-detect (Recommended)", "Russian (ru)", "English (en)" });
        _languageCombo.SelectedIndex = _settings.Language switch
        {
            "ru" => 1,
            "en" => 2,
            _ => 0
        };
        tab.Controls.Add(_languageCombo);

        top += 75;
        _trailingPeriodCheck = new CheckBox
        {
            Text = "Remove trailing period from dictated text",
            Location = new Point(25, top),
            Width = 400,
            Checked = _settings.RemoveTrailingPeriod,
            ForeColor = Color.FromArgb(201, 209, 217)
        };
        tab.Controls.Add(_trailingPeriodCheck);
    }

    private void SetupAITab(TabPage tab)
    {
        int top = 20;

        _aiCleanupCheck = new CheckBox
        {
            Text = "Enable AI Post-Processing Cleanup (Local Ollama or Cloud)",
            Location = new Point(25, top),
            Width = 480,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Checked = _settings.EnableAICleanup,
            ForeColor = Color.White
        };
        tab.Controls.Add(_aiCleanupCheck);

        top += 45;
        tab.Controls.Add(CreateLabel("API Base URL (e.g. Ollama http://localhost:11434/v1):", 25, top));
        _aiBaseUrlText = new TextBox
        {
            Location = new Point(25, top + 24),
            Width = 480,
            Text = _settings.AIBaseUrl,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White
        };
        tab.Controls.Add(_aiBaseUrlText);

        top += 65;
        tab.Controls.Add(CreateLabel("Model Name (e.g. llama3.2, mistral, gpt-4o-mini):", 25, top));
        _aiModelText = new TextBox
        {
            Location = new Point(25, top + 24),
            Width = 480,
            Text = _settings.AIModel,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White
        };
        tab.Controls.Add(_aiModelText);

        top += 65;
        tab.Controls.Add(CreateLabel("API Key (leave empty for local Ollama / LM Studio):", 25, top));
        _aiKeyText = new TextBox
        {
            Location = new Point(25, top + 24),
            Width = 480,
            Text = _settings.AIApiKey,
            UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(13, 17, 23),
            ForeColor = Color.White
        };
        tab.Controls.Add(_aiKeyText);

        top += 65;
        var infoLabel = new Label
        {
            Text = "🔒 Privacy: Local Ollama runs 100% on your machine without sending any data over the internet.",
            Location = new Point(25, top),
            Width = 480,
            Height = 40,
            ForeColor = Color.FromArgb(139, 148, 158)
        };
        tab.Controls.Add(infoLabel);
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
