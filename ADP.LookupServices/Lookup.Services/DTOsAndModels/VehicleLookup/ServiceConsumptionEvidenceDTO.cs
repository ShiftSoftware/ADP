using System;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>A service visit that consumed an offer. This is not a financial claim.</summary>
[TypeScriptModel]
public class ServiceConsumptionEvidenceDTO
{
    public DateTime ServiceDate { get; set; }
    public string InvoiceNumber { get; set; }
    public string JobNumber { get; set; }
    public long? CompanyID { get; set; }
    public long? BranchID { get; set; }
}
