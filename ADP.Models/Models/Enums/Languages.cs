using System.Collections.Generic;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum Languages
{
    [Description("Unspecified")]
    Unspecified = 0,

    [Description("English")]
    English = 1,

    [Description("Arabic")]
    Arabic = 2,

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
