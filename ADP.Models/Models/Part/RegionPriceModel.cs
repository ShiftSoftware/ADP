namespace ShiftSoftware.ADP.Models.Part;

public class RegionPriceModel : IRegionProps
{
    public string RegionIntegrationID { get; set; }
    public decimal? Price { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? WarrantyPrice { get; set; }
}