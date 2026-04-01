using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Response DTO from a manufacturer part lookup, containing the status and result data.
/// </summary>
[TypeScriptModel]
[Docable]
public class ManufacturerPartLookupResponseDTO
{
    [DocIgnore]
    public string id { get; set; }
    /// <summary>The part number that was looked up.</summary>
    public string PartNumber { get; set; }
    /// <summary>The order type used for the lookup.</summary>
    public ManufacturerOrderType OrderType { get; set; }
    /// <summary>The status of the lookup (Pending, Resolved, UnResolved).</summary>
    public ManufacturerPartLookupStatus Status { get; set; }
    /// <summary>Key-value pairs returned by the manufacturer's system.</summary>
    public IEnumerable<KeyValuePairDTO>? ManufacturerResult { get; set; }
    /// <summary>An optional message from the manufacturer or system.</summary>
    public string? Message { get; set; }
    /// <summary>The quantity that was requested.</summary>
    public decimal Quantity { get; set; }
}
