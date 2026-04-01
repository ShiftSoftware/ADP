using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// DTO for updating the status of a manufacturer part lookup request.
/// </summary>
[Docable]
public class UpdateManufacturerPartLookupStatusDTO
{
    [DocIgnore]
    public string id { get; set; }
    /// <summary>The part number that was looked up.</summary>
    public string PartNumber { get; set; }
    /// <summary>The new status to set (Pending, Resolved, UnResolved).</summary>
    public ManufacturerPartLookupStatus Status { get; set; }
    /// <summary>The manufacturer result key-value pairs, if resolved.</summary>
    public IEnumerable<KeyValuePairDTO>? ManufacturerResult { get; set; }
}