using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.DataServices;

/// <summary>
/// One country, so the menu screens never ask for one and the transfer rate is 1. The ID is the country's ID on the
/// identity server, from <c>Sample:Country</c>. With no country configured, menus are priced under country 0.
/// </summary>
public class CountryProvider : IMenuCountryProvider
{
    private readonly IReadOnlyList<CountryInfo> countries;

    public CountryProvider(IConfiguration configuration)
    {
        var country = configuration.GetSection("Sample:Country").Get<CountryInfo>();

        countries = country is null ? [] : [country];
    }

    public ValueTask<IReadOnlyList<CountryInfo>> GetSupportedCountriesAsync() => ValueTask.FromResult(countries);
}
