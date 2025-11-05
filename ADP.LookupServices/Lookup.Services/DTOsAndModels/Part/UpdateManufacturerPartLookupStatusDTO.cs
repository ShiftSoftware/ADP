using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class UpdateManufacturerPartLookupStatusDTO
{
    public string id { get; set; }
    public string PartNumber { get; set; }
    public ManufacturerPartLookupStatus Status { get; set; }
    public IEnumerable<KeyValuePairDTO>? ManufacturerResult { get; set; }
}