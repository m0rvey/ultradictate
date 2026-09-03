using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace UltraDictate.Windows.Core;

public class AppSettings
{
    public string Hotkey { get; set; } = "RightControl";
    public string TriggerMode { get; set; } = "HoldToDictate"; // "HoldToDictate" or "PressToToggle"
    public string ModelName { get; set; } = "openai/whisper-base";
    public string Language { get; set; } = "auto";
    public bool RemoveTrailingPeriod { get; set; } = false;
    public bool EnableAICleanup { get; set; } = false;
    public string AIBaseUrl { get; set; } = "http://localhost:11434/v1"; // Ollama by default
    public string AIModel { get; set; } = "llama3.2";
    public string AIApiKey { get; set; } = "";
    public Dictionary<string, string> CustomCorrections { get; set; } = new();
}

public static class SettingsManager
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UltraDictate");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "config.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch { }

        var defaultSettings = new AppSettings();
        Save(defaultSettings);
        return defaultSettings;
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }

    public static void SaveSettings(AppSettings settings) => Save(settings);
}
