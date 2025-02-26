using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

public class CountryPriceModel : ICountryProps
{
    public string CountryIntegrationID { get; set; }
    public IEnumerable<RegionPriceModel> RegionPrices { get; set; }
}