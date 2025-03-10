namespace ShiftSoftware.ADP.Models.Part;

public class RegionPriceModel : IRegionProps
{
    public string RegionID { get; set; }
    public decimal? RetailPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? WarrantyPrice { get; set; }
}