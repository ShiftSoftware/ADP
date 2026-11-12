using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.Menus.Sample.MultiCountry.Web;

/// <summary>
/// Several countries, so the menu screens ask which country's prices apply and for the transfer rate. The IDs are
/// the countries' IDs on the identity server, from <c>Sample:Countries</c>.
/// </summary>
public class CountryProvider : IMenuCountryProvider
{
    private readonly IReadOnlyList<CountryInfo> countries;

    public CountryProvider(IConfiguration configuration)
    {
        countries = configuration.GetSection("Sample:Countries").Get<List<CountryInfo>>() ?? [];
    }

    public ValueTask<IReadOnlyList<CountryInfo>> GetSupportedCountriesAsync() => ValueTask.FromResult(countries);
}
