namespace ShiftSoftware.ADP.Models.Part;

/// <summary>
/// Refers to a Name in a Stock (Warehouse)
/// </summary>
[Docable]
public class StockPartModel: 
    IPartitionedItem,
    ICompanyProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The Unique Name Number
    /// </summary>
    public string PartNumber { get; set; }

    /// <summary>
    /// The Warehouse/Location Identifier where the part is stored.
    /// </summary>
    public string Location { get; set; } = default!;

    /// <summary>
    /// The current on-hand Quantity of the part in the stock.
    /// </summary>
    public decimal Quantity { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.StockPart;


    [DocIgnore]
    public string CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }
}