using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;


/// <summary>
/// Represents the country-specific information for a part.
/// </summary>
[Docable]
public class PartCountryDataModel : ICountryProps
{
    [DocIgnore]
    public long? CountryID { get; set; }

    /// <summary>
    /// The Country Hash ID from the Identity System.
    /// </summary>
    public string CountryHashID { get; set; }

    /// <summary>
    /// Country-specific Harmonized System (HS) code for the part.
    /// </summary>
    public string HSCode { get; set; }


    /// <summary>
    /// <see cref="RegionPriceModel">Region</see>-specific price information for the part.
    /// </summary>
    public IEnumerable<RegionPriceModel> RegionPrices { get; set; }
}