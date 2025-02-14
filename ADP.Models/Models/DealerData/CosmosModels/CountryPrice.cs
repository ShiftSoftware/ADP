using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class CountryPrice
{
    public string CountryIntegrationID { get; set; }
    public IEnumerable<RegionPrice> RegionPrices { get; set; }
}
