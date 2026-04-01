namespace ShiftSoftware.ADP.Models;

/// <summary>
/// Defines country-level properties for models that are associated with a specific country.
/// </summary>
public interface ICountryProps
{
    /// <summary>
    /// The internal country ID.
    /// </summary>
    public long? CountryID { get; set; }

    /// <summary>
    /// The Country Hash ID from the Identity System.
    /// </summary>
    public string CountryHashID { get; set; }
}