using System;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>A host-resolved classification. Only Code is eligibility evidence; Label is display text.</summary>
[TypeScriptModel]
[Docable]
public sealed class VehicleClassificationDTO
{
    public string Code { get; set; }
    public string Label { get; set; }
    public DateTimeOffset? AsOf { get; set; }
}
