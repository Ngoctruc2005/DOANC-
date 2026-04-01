using System.Text.Json;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace TourismApp.Services;

public static class TTSHelper
{
    private static readonly HttpClient _httpClient;

    static TTSHelper()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }

    public static async Task SpeakDescriptionAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var lang = Preferences.Get("language", "vi");
        string textToSpeak = text;

        if (lang != "vi")
        {
            textToSpeak = await TranslateTextAsync(text, lang);
        }

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        
        // Find suitable locale (e.g., lang = "en", then look for en-US)
        var speechLocale = locales.FirstOrDefault(l => l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
                           ?? locales.FirstOrDefault(); // fallback

        var options = new SpeechOptions()
        {
            Locale = speechLocale
        };

        try
        {
            await TextToSpeech.Default.SpeakAsync(textToSpeak, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lỗi Thuyết Minh]: {ex.Message}");
        }
    }

    public static async Task<string> TranslateTextAsync(string text, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        try
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";
            var response = await _httpClient.GetStringAsync(url);

            var doc = JsonDocument.Parse(response);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && 
                doc.RootElement[0].ValueKind == JsonValueKind.Array)
            {
                string translated = "";
                foreach (var item in doc.RootElement[0].EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
                    {
                        translated += item[0].GetString() + " ";
                    }
                }
                return translated.Trim();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lỗi Dịch Thuật]: {ex.Message}");
        }
        return text; 
    }
}