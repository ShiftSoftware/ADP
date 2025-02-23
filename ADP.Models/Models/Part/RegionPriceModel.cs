namespace ShiftSoftware.ADP.Models.Part;

public class RegionPriceModel
{
    public string RegionIntegrationID { get; set; }
    public decimal? WarrantyPrice { get; set; }
    public decimal? FOB { get; set; }
    public decimal? Price { get; set; }
}