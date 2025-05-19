namespace ShiftSoftware.ADP.Models.Part;


/// <summary>
/// Used to define the price of a part in a specific region.
/// </summary>
[Docable]
public class RegionPriceModel : IRegionProps
{

    [DocIgnore]
    public string RegionID { get; set; }


    /// <summary>
    /// The Identity Hash ID of the region.
    /// </summary>
    public string RegionHashID { get; set; }

    /// <summary>
    /// The Retail Price of the part in the region.
    /// </summary>
    public decimal? RetailPrice { get; set; }

    /// <summary>
    /// The retailer purchase price of the part in the region. (Alos known as the distributor sell price)
    /// </summary>
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// The warranty price of the part in the region. (As reimbursed by the distributor)
    /// </summary>
    public decimal? WarrantyPrice { get; set; }
}