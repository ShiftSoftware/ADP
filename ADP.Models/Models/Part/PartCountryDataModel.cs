using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;


/// <summary>
/// Represents the country-specific information for a part.
/// </summary>
public class PartCountryDataModel : ICountryProps
{
    [DocIgnore]
    public string CountryID { get; set; }

    /// <summary>
    /// The Identity Hash ID of the country.
    /// </summary>
    public string CountryHashID { get; set; }

    /// <summary>
    /// Country-specific Harmonized System (HS) code for the part.
    /// </summary>
    public string HSCode { get; set; }


    /// <summary>
    /// <see href="https://adp.shift.software/generated/part/RegionPriceModel.html">Region</see>-specific price information for the part.
    /// </summary>
    public IEnumerable<RegionPriceModel> RegionPrices { get; set; }
}