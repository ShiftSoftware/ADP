using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

public class CountryPriceModel
{
    public string CountryIntegrationID { get; set; }
    public IEnumerable<RegionPriceModel> RegionPrices { get; set; }
}
