using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class ManufacturerPartLookupDTO
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public IEnumerable<ManufacturerDataDTO>? ManufacturerResult { get; set; }
}