namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class RegionPrice
{
    public string RegionIntegrationID { get; set; }
    public decimal? WarrantyPrice { get; set; }
    public decimal? FOB { get; set; }
    public decimal? Price { get; set; }
}
