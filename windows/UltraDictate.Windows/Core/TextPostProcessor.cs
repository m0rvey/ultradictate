using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UltraDictate.Windows.Core;

public static class TextPostProcessor
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static async Task<string> ProcessAsync(string text, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string result = text.Trim();

        // 1. Voice commands & verbal punctuation
        result = ApplyVoiceCommands(result);

        // 2. Custom dictionary corrections
        if (settings.CustomCorrections != null)
        {
            foreach (var (pattern, replacement) in settings.CustomCorrections)
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    result = Regex.Replace(result, Regex.Escape(pattern), replacement, RegexOptions.IgnoreCase);
                }
            }
        }

        // 3. Remove trailing period if configured
        if (settings.RemoveTrailingPeriod && result.EndsWith("."))
        {
            result = result[..^1].TrimEnd();
        }

        // 4. Optional AI Cleanup (local Ollama or cloud)
        if (settings.EnableAICleanup && !string.IsNullOrEmpty(settings.AIBaseUrl))
        {
            try
            {
                result = await RequestAICleanupAsync(result, settings);
            }
            catch
            {
                // Fall back to unprocessed text on network/LLM failure
            }
        }

        return result;
    }

    private static string ApplyVoiceCommands(string text)
    {
        var commands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "новая строка", "\n" },
            { "новый абзац", "\n\n" },
            { "new line", "\n" },
            { "new paragraph", "\n\n" }
        };

        foreach (var (cmd, replacement) in commands)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(cmd)}\b", replacement, RegexOptions.IgnoreCase);
        }

        return text;
    }

    private static async Task<string> RequestAICleanupAsync(string text, AppSettings settings)
    {
        var requestUrl = settings.AIBaseUrl.TrimEnd('/') + "/chat/completions";
        var payload = new
        {
            model = settings.AIModel,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a dictation post-processor. Clean up grammar, spelling, and punctuation without altering the meaning. Output only the cleaned text."
                },
                new
                {
                    role = "user",
                    content = text
                }
            },
            temperature = 0.1
        };

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(settings.AIApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.AIApiKey);
        }

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? text;
    }
}
