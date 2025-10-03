using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class PartPriceDTO
{
    [ShiftSoftware.ShiftEntity.Model.HashIds.CountryHashIdConverter]
    public string CountryID { get; set; }
    public string CountryName { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.RegionHashIdConverter]
    public string RegionID { get; set; }
    public string RegionName { get; set; }
    public PriceDTO RetailPrice { get; set; }
    public PriceDTO PurchasePrice { get; set; }
    public PriceDTO WarrantyPrice { get; set; }
}