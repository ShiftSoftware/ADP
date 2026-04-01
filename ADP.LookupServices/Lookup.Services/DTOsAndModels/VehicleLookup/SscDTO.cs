using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a Special Service Campaign (SSC) / safety recall affecting a vehicle.
/// Includes the recall code, description, required labor and parts, and whether the repair has been completed.
/// </summary>
[TypeScriptModel]
[Docable]
public class SscDTO
{
    /// <summary>The unique code identifying the recall campaign.</summary>
    public string SSCCode { get; set; } = default!;
    /// <summary>A description of the recall and the issue being addressed.</summary>
    public string Description { get; set; } = default!;
    /// <summary>The <see cref="SSCLaborDTO">labor operations</see> required for the recall repair.</summary>
    public IEnumerable<SSCLaborDTO> Labors { get; set; } = default!;
    /// <summary>Whether the recall repair has been completed for this vehicle.</summary>
    public bool Repaired { get; set; }
    /// <summary>The date the recall repair was completed. Null if not yet repaired.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? RepairDate { get; set; }
    /// <summary>The <see cref="SSCPartDTO">parts</see> required for the recall repair.</summary>
    public IEnumerable<SSCPartDTO> Parts { get; set; }

    public SscDTO()
    {
        Parts = new List<SSCPartDTO>();
    }
}