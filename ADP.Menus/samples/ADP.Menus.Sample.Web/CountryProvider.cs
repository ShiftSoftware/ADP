using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.Menus.Sample.Web;

public class CountryProvider : IMenuCountryProvider
{
    private static readonly IReadOnlyList<CountryInfo> Countries =
    [
        new() { Id = 3, Name = "Uzbekistan" },
        new() { Id = 4, Name = "Turkmenistan" },
        new() { Id = 5, Name = "Tajikistan" },
    ];

    public ValueTask<IReadOnlyList<CountryInfo>> GetSupportedCountriesAsync()
    {
        return new ValueTask<IReadOnlyList<CountryInfo>>(Countries);
    }
}
