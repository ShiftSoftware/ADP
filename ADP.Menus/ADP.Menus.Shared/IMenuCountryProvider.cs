namespace ShiftSoftware.ADP.Menus.Shared;

public interface IMenuCountryProvider
{
    ValueTask<IReadOnlyList<CountryInfo>> GetSupportedCountriesAsync();
}

public class CountryInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
}
