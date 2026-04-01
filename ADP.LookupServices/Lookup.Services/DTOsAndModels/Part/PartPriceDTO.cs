using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Contains the pricing information for a part in a specific country and region.
/// </summary>
[TypeScriptModel]
[Docable]
public class PartPriceDTO
{
    /// <summary>The Country Hash ID.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CountryHashIdConverter]
    public string CountryID { get; set; }
    /// <summary>The resolved country name.</summary>
    public string CountryName { get; set; }
    /// <summary>The Region Hash ID.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.RegionHashIdConverter]
    public string RegionID { get; set; }
    /// <summary>The resolved region name.</summary>
    public string RegionName { get; set; }
    /// <summary>The retail price for this part in this region.</summary>
    public PriceDTO RetailPrice { get; set; }
    /// <summary>The retailer purchase price (distributor sell price) for this part.</summary>
    public PriceDTO PurchasePrice { get; set; }
    /// <summary>The warranty reimbursement price for this part.</summary>
    public PriceDTO WarrantyPrice { get; set; }
}