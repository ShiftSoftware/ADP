using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Request DTO for initiating a part lookup against the manufacturer's system.
/// </summary>
[TypeScriptModel]
[Docable]
public class ManufacturerPartLookupRequestDTO
{
    /// <summary>The part number to look up.</summary>
    public string PartNumber { get; set; }
    /// <summary>The quantity requested.</summary>
    public decimal Quantity { get; set; }
    /// <summary>The order type (e.g., Sea or Airplane shipping).</summary>
    public ManufacturerOrderType OrderType { get; set; }
    /// <summary>An optional log identifier for tracing.</summary>
    public string? LogId { get; set; }
}