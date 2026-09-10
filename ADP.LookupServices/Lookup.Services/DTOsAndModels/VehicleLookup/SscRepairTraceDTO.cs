using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Diagnostic trace of the repair check for one Special Service Campaign (SSC) on one vehicle: every piece of
/// evidence the evaluator looked at and what it made of it. Present on <see cref="SscDTO.Trace"/> only when the
/// request asked for it via <see cref="VehicleLookupRequestOptions.TraceSSCEvaluation"/>; it lists claim and
/// invoice details, so hosts should gate that option behind a permission check.
/// <para>The verdict is decided in this order, and the first source that holds wins: the SSC record's own repair
/// date; the most recently completed warranty claim that qualifies by status and references the campaign; the
/// most recent invoiced service-history labor line carrying one of the campaign's labor codes.</para>
/// </summary>
[TypeScriptModel]
[Docable]
public class SscRepairTraceDTO
{
    /// <summary>The labor operation codes the campaign itself lists, as fed by the manufacturer (trimmed).</summary>
    public List<string> CampaignLaborCodes { get; set; } = new List<string>();
    /// <summary>
    /// Additional codes accepted as equivalent to a campaign code through
    /// <c>LookupOptions.SSCInterchangeableLaborCodeGroups</c>, each with the campaign code it stands for. Empty
    /// when no configured group contains any of the campaign's codes.
    /// </summary>
    public List<SscRepairTraceLaborCodeDTO> InterchangeableLaborCodes { get; set; } = new List<SscRepairTraceLaborCodeDTO>();
    /// <summary>The repair date carried by the SSC record itself, when the source feed provides one.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? RecordRepairDate { get; set; }
    /// <summary>Every warranty claim on the vehicle, most recently completed first, with how each was judged.</summary>
    public List<SscRepairTraceWarrantyClaimDTO> WarrantyClaims { get; set; } = new List<SscRepairTraceWarrantyClaimDTO>();
    /// <summary>How many service-history labor lines the vehicle has in total (all codes, all statuses).</summary>
    public int ServiceHistoryLaborLinesExamined { get; set; }
    /// <summary>The service-history labor lines whose code matched a campaign code (directly or through an interchangeable group), most recent first.</summary>
    public List<SscRepairTraceLaborLineDTO> ServiceHistoryLaborLines { get; set; } = new List<SscRepairTraceLaborLineDTO>();
    /// <summary>Which source decided the verdict.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SscRepairSource RepairSource { get; set; }
}
