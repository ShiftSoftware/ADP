using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// One service-history labor line whose code is one of the SSC campaign's labor codes (or interchangeable with
/// one), as the repair check saw it. Lines with unrelated codes are not listed; only their count is reported.
/// </summary>
[TypeScriptModel]
[Docable]
public class SscRepairTraceLaborLineDTO
{
    /// <summary>The invoice the labor line belongs to.</summary>
    public string InvoiceNumber { get; set; }
    /// <summary>The invoice date. This becomes the SSC repair date when the line is selected.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }
    /// <summary>The invoice status as recorded by the dealer system.</summary>
    public string InvoiceStatus { get; set; }
    /// <summary>The labor operation code on the line.</summary>
    public string LaborCode { get; set; }
    /// <summary>The campaign's own labor code the line's code stands for (equal to <see cref="LaborCode"/> on a direct match).</summary>
    public string CampaignLaborCode { get; set; }
    /// <summary>Whether the invoice status counts as repair evidence (X or C).</summary>
    public bool StatusQualifies { get; set; }
    /// <summary>Whether this is the line that decided the verdict: the most recent qualifying line, consulted only when no warranty claim was selected.</summary>
    public bool Selected { get; set; }
}
