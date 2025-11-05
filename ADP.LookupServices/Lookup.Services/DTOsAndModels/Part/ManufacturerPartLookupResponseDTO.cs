using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class ManufacturerPartLookupResponseDTO
{
    public string id { get; set; }
    public string PartNumber { get; set; }
    public ManufacturerOrderType OrderType { get; set; }
    public ManufacturerPartLookupStatus Status { get; set; }
    public Dictionary<string, string>? ManufacturerResult { get; set; }
}
