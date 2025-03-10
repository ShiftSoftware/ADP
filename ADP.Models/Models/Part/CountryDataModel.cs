using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

public class CountryDataModel : ICountryProps
{
    public string CountryID { get; set; }
    public string HSCode { get; set; }
    public IEnumerable<RegionPriceModel> RegionPrices { get; set; }
}