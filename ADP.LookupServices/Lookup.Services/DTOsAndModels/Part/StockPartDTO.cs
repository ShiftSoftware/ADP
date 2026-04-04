using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Represents a part's stock availability at a specific location/warehouse.
/// </summary>
[TypeScriptModel]
[Docable]
public class StockPartDTO
{
    /// <summary>The result of the stock quantity lookup (e.g., Available, NotAvailable, PartiallyAvailable).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuantityLookUpResults QuantityLookUpResult { get; set; }
    /// <summary>The location/warehouse identifier.</summary>
    public string LocationID { get; set; }
    /// <summary>The resolved location/warehouse name.</summary>
    public string LocationName { get; set; }
    /// <summary>The available quantity at this location (only shown if configured).</summary>
    public decimal? AvailableQuantity { get; set; }
}
