namespace ShiftSoftware.ADP.Menus.Sample.API.Data;

public static class LabourRateCountries
{
    public const long Uzbekistan = 3;
    public const long Turkmenistan = 4;
    public const long Tajikistan = 5;

    public static readonly IReadOnlyList<long> SupportedCountryIds =
    [
        Uzbekistan,
        Turkmenistan,
        Tajikistan
    ];
}
