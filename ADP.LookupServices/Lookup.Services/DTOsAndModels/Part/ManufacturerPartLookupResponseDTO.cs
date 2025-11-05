using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class ManufacturerPartLookupResponseDTO
{
    public string id { get; set; }
    public string PartNumber { get; set; }
    public ManufacturerOrderType OrderType { get; set; }
    public ManufacturerPartLookupStatus Status { get; set; }
    public IEnumerable<KeyValuePairDTO>? ManufacturerResult { get; set; }
    public string? Message { get; set; }
}
