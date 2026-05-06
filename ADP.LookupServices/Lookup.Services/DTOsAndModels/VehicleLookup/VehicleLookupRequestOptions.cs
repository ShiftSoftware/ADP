using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Options passed to the vehicle lookup service to control lookup behavior, language, logging, and consistency level.
/// </summary>
[Docable]
public class VehicleLookupRequestOptions
{
    /// <summary>The language code for localized content (defaults to "en").</summary>
    public string LanguageCode { get; set; } = "en";
    /// <summary>Whether to skip broker stock lookup for this request.</summary>
    public bool IgnoreBrokerStock { get; set; }
    /// <summary>Whether to insert an SSC lookup audit log entry.</summary>
    public bool InsertSSCLog { get; set; }
    /// <summary>The SSC log info to record if InsertSSCLog is true.</summary>
    public SSCLogInfo SSCLogInfo { get; set; }
    /// <summary>Whether to insert a customer vehicle lookup audit log entry.</summary>
    public bool InsertCustomerVehcileLookupLog { get; set; }
    /// <summary>The customer vehicle lookup log info to record.</summary>
    public CustomerVehicleLookupLogInfo CustomerVehicleLookupLogInfo { get; set; }
    /// <summary>The consistency level for service history queries (defaults to Strong).</summary>
    public ConsistencyLevels VehicleServiceHistoryConsistencyLevel { get; set; } = ConsistencyLevels.Strong;
    /// <summary>Whether to look up end customer information for the vehicle.</summary>
    public bool LookupEndCustomer { get; set; }
    /// <summary>Whether to include the legacy paint thickness format in the response.</summary>
    public bool LegacyPaintThickness { get; set; }
    /// <summary>Whether to use Katashiki instead of VariantCode for vehicle model lookup.</summary>
    public bool UseKatashikiLookup { get; set; }

    /// <summary>
    /// When true, the service item evaluator records a structured
    /// <see cref="Diagnostics.ServiceItemTrace"/> of every decision (eligibility, expansion,
    /// status, post-processing) and attaches it to <see cref="VehicleLookupDTO.ServiceItemTrace"/>.
    /// Off by default; opt in per request only when debugging. Adds an O(items) walk and
    /// per-item allocations; do not leave on in production hot paths.
    /// </summary>
    public bool TraceServiceItemEvaluation { get; set; }
}