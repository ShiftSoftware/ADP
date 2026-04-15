using System.Globalization;
using System.Text.Json;

namespace ShiftSoftware.ADP.Menus.Data.DataServices;

/// <summary>
/// Resolves a possibly-multi-language string into a single language value.
/// Multi-language values are stored as a JSON object keyed by 2-letter ISO language codes,
/// e.g. {"en":"OIL-CHG","ru":"Ð—ÐÐœ-ÐœÐÐ¡Ð›"}. Plain (non-JSON) values are returned as-is.
/// </summary>
public static class LocalizedText
{
    public static string Resolve(string? raw, string? language)
    {
        if (string.IsNullOrEmpty(raw)) return raw ?? string.Empty;
        if (raw[0] != '{') return raw;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            if (dict is null || dict.Count == 0) return raw;

            var key = "en";
            if (!string.IsNullOrEmpty(language))
            {
                try { key = new CultureInfo(language).TwoLetterISOLanguageName; }
                catch { key = language; }
            }

            if (dict.TryGetValue(key, out var v)) return v;
            if (dict.TryGetValue("en", out var en)) return en;
            return dict.Values.FirstOrDefault() ?? raw;
        }
        catch
        {
            return raw;
        }
    }
}
