﻿using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class SSCPartDTO
{
    public string PartNumber { get; set; } = default!;
    public string PartDescription { get; set; }
    public bool IsAvailable { get; set; }
}