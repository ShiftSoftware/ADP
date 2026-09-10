// The trace is genuinely optional (omitted unless requested), and the annotation is what makes the
// generated TypeScript declare it as `trace?:` rather than a required field.
#nullable enable annotations
using Newtonsoft.Json;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    /// <summary>
    /// Which evidence decided <see cref="Repaired"/>: the SSC record's own repair date, a qualifying warranty
    /// claim, or an invoiced service-history labor line — or <c>None</c> when the campaign is still open.
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public SscRepairSource RepairSource { get; set; }
    /// <summary>The <see cref="SSCPartDTO">parts</see> required for the recall repair.</summary>
    public IEnumerable<SSCPartDTO> Parts { get; set; }
    /// <summary>
    /// Diagnostic trace of how <see cref="Repaired"/> was decided. Null unless the request asked for it via
    /// <see cref="VehicleLookupRequestOptions.TraceSSCEvaluation"/>, and omitted from the JSON when null.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SscRepairTraceDTO? Trace { get; set; }

    public SscDTO()
    {
        Parts = new List<SSCPartDTO>();
    }
}