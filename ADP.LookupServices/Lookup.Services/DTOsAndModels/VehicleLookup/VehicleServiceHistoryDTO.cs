using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a service history entry for a vehicle — a past service invoice with its labor and part lines.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceHistoryDTO
{
    /// <summary>The type of service performed (e.g., Warranty, Paid, Internal).</summary>
    public string ServiceType { get; set; } = default!;
    /// <summary>The date the service was performed.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? ServiceDate { get; set; }
    /// <summary>The vehicle's odometer reading at the time of service.</summary>
    public int? Mileage { get; set; }
    /// <summary>The localized name of the company that performed the service.</summary>
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string CompanyName { get; set; }
    /// <summary>The localized name of the branch that performed the service.</summary>
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string BranchName { get; set; }
    /// <summary>The dealer account number.</summary>
    public string AccountNumber { get; set; } = default!;
    /// <summary>The service invoice number.</summary>
    public string InvoiceNumber { get; set; }
    /// <summary>The parent invoice number (in case of credit/debit notes).</summary>
    public string ParentInvoiceNumber { get; set; }
    /// <summary>The job/work order number.</summary>
    public string JobNumber { get; set; }
    /// <summary>The <see cref="VehicleLaborDTO">labor lines</see> on this service invoice.</summary>
    public IEnumerable<VehicleLaborDTO> LaborLines { get; set; }
    /// <summary>The <see cref="VehiclePartDTO">part lines</see> on this service invoice.</summary>
    public IEnumerable<VehiclePartDTO> PartLines { get; set; }
}