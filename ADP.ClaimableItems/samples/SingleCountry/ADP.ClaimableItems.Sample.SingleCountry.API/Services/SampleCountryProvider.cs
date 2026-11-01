using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.API.Services;

/// <summary>
/// One country, so the server picks it for every contract and the transfer rate is 1: the contract form shows
/// neither field. The ID is the country's ID on the identity server, from <c>Sample:Country</c>. With no country
/// configured the menu lookup's default country applies.
/// </summary>
public class SampleCountryProvider : IMenuCountryProvider
{
    private readonly IReadOnlyList<CountryInfo> countries;

    public SampleCountryProvider(IConfiguration configuration)
    {
        var country = configuration.GetSection("Sample:Country").Get<CountryInfo>();

        countries = country is null ? [] : [country];
    }

    public ValueTask<IReadOnlyList<CountryInfo>> GetSupportedCountriesAsync() => ValueTask.FromResult(countries);
}
