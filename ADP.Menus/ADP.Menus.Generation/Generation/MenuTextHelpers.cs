using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>
/// The two text helpers that feed generated codes, ported VERBATIM from ADP.Menus so the export and
/// the lookup produce byte-identical codes.
///
/// "Verbatim" is the requirement, not an oversight: these functions have already produced codes that
/// live in a DMS. Quirks are preserved deliberately and are covered by the Phase 0 golden tests —
/// changing any of them changes codes that have already been issued.
/// </summary>
public static class MenuTextHelpers
{
    /// <summary>
    /// Renders an allowed time for embedding in a labour code.
    ///
    /// Preserved quirks (all pinned by tests):
    ///  • the trailing-zero trim makes 1 and 10 (and 2 and 20) collide onto the same text;
    ///  • the decimal is formatted with the AMBIENT culture, so a culture whose decimal separator is
    ///    not '.' yields a different string. Left as-is by an explicit product decision: the affected
    ///    deployments do not hit it, and pinning the culture here would change existing codes.
    /// </summary>
    public static string GetAllowedTimeText(decimal allowedTime)
    {
        if (allowedTime < 0)
            throw new ArgumentException("Allowed time cannot be negative");

        var result = allowedTime.ToString().Trim('0');
        result = result.Replace(".", string.Empty);

        if (allowedTime < 1)
            return "0" + result;

        if (result.Length > 1)
            return result;

        return result + "0";
    }

    /// <summary>
    /// Resolves a possibly-multi-language string into a single language value. Multi-language values
    /// are stored as a JSON object keyed by 2-letter ISO language codes; plain values pass through
    /// unchanged, as does anything that fails to parse.
    /// </summary>
    public static string Resolve(string raw, string language)
    {
        if (string.IsNullOrEmpty(raw)) return raw ?? string.Empty;
        if (raw[0] != '{') return raw;

        try
        {
            var valuesByLanguage = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            if (valuesByLanguage is null || valuesByLanguage.Count == 0) return raw;

            var languageKey = "en";
            if (!string.IsNullOrEmpty(language))
            {
                try { languageKey = new CultureInfo(language).TwoLetterISOLanguageName; }
                catch { languageKey = language; }
            }

            if (valuesByLanguage.TryGetValue(languageKey, out var requested)) return requested;
            if (valuesByLanguage.TryGetValue("en", out var english)) return english;
            return valuesByLanguage.Values.FirstOrDefault() ?? raw;
        }
        catch
        {
            return raw;
        }
    }
}
