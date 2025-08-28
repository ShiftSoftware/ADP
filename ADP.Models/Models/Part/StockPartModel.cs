using System;

namespace ShiftSoftware.ADP.Models.Part;

/// <summary>
/// Refers to a Part in a Stock (Warehouse)
/// </summary>
[Docable]
public class StockPartModel: 
    IPartitionedItem,
    ICompanyProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The Unique Part Number
    /// </summary>
    public string PartNumber { get; set; }

    /// <summary>
    /// The Warehouse/Location Identifier where the part is stored.
    /// </summary>
    public string Location { get; set; } = default!;

    /// <summary>
    /// The current AvailableQuantity of the part in the stock.
    /// </summary>
    public decimal AvailableQuantity { get; set; }

    /// <summary>
    /// The current on-hand quantity of the part in the stock
    /// </summary>
    public decimal OnHandQuantity { get; set; }

    public decimal OnOrderQauntity { get; set; }

    public DateTimeOffset? InventoryDate { get; set; }

    public DateTimeOffset? LastSoldDate { get; set; }

    public DateTimeOffset? LastArrivedDate { get; set; }

    public DateTimeOffset? LastPurchasedDate { get; set; }

    public DateTimeOffset? FirstReceivedDate { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.StockPart;


    [DocIgnore]
    public string CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    public string IntegrationID { get; set; }
}