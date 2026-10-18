using System;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>A host-resolved classification. Only Code is eligibility evidence; Label is display text.</summary>
[TypeScriptModel]
[Docable]
public sealed class VehicleClassificationDTO
{
    /// <summary>The host's stable code. Item conditions and reports match on this value.</summary>
    public string Code { get; set; }

    /// <summary>Display text in the request's language. Never a rule key.</summary>
    public string Label { get; set; }

    /// <summary>When the evidence behind the classification was measured, if the host states it.</summary>
    public DateTimeOffset? AsOf { get; set; }
}
