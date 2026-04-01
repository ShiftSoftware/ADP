using System.Collections.Generic;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Supported languages for multilingual content across the platform.
/// </summary>
[Docable]
public enum Languages
{
    /// <summary>
    /// Language not specified.
    /// </summary>
    [Description("Unspecified")]
    Unspecified = 0,

    /// <summary>
    /// English (ISO code: en).
    /// </summary>
    [Description("English")]
    English = 1,

    /// <summary>
    /// Arabic (ISO code: ar).
    /// </summary>
    [Description("Arabic")]
    Arabic = 2,

    /// <summary>
    /// Kurdish (ISO code: ku).
    /// </summary>
    [Description("Kurdish")]
    Kurdish = 3,
}

public static class LanguagesExtensions
{
    public static Dictionary<Languages, string> LanguaeIsoCodes = new Dictionary<Languages, string>
    {
        [Languages.Unspecified] = null,
        [Languages.English] = "en",
        [Languages.Arabic] = "ar",
        [Languages.Kurdish] = "ku",
    };
    public static string GetIsoCode(this Languages value)
    {
        if (LanguaeIsoCodes.ContainsKey(value))
            return LanguaeIsoCodes[value];

        return null;
    }
}
