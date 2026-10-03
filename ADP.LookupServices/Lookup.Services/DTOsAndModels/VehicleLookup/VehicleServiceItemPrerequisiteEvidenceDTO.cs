using ShiftSoftware.ADP.Models;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// The recorded labor line that satisfied an expected service milestone. Missing source values stay
/// null. InvoiceDate is the line's invoice date, not an inferred visit date or a benefit claim date.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceItemPrerequisiteEvidenceDTO
{
    public DateTime? InvoiceDate { get; set; }
    public int? Odometer { get; set; }
    public string? PackageCode { get; set; }
    public string? ServiceCode { get; set; }
    public string? LaborCode { get; set; }
    public string? ServiceDescription { get; set; }
    public string? JobDescription { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? JobNumber { get; set; }
    public string? LineID { get; set; }
    public long? CompanyID { get; set; }
    public long? BranchID { get; set; }
}
