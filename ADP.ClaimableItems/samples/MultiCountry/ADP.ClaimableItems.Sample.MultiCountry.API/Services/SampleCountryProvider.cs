using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.ClaimableItems.Sample.MultiCountry.API.Services;

/// <summary>
/// Several countries, so the contract form asks the user for the country whose prices apply and for the transfer
/// rate. The IDs are the countries' IDs on the identity server, from <c>Sample:Countries</c>.
/// </summary>
public class SampleCountryProvider : IMenuCountryProvider
{
    private readonly IReadOnlyList<CountryInfo> countries;

    public SampleCountryProvider(IConfiguration configuration)
    {
        countries = configuration.GetSection("Sample:Countries").Get<List<CountryInfo>>() ?? [];
    }

    public ValueTask<IReadOnlyList<CountryInfo>> GetSupportedCountriesAsync() => ValueTask.FromResult(countries);
}
