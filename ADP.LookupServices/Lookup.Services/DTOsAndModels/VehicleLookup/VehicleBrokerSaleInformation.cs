using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Contains broker sale information when a vehicle was sold through a third-party broker (TBP).
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleBrokerSaleInformation
{
    /// <summary>The ID of the broker that sold the vehicle.</summary>
    public long BrokerID { get; set; }
    /// <summary>The name of the broker.</summary>
    public string BrokerName { get; set; }
    /// <summary>The broker's invoice number for the sale.</summary>
    public long? InvoiceNumber { get; set; }
    /// <summary>The date of the broker invoice.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }
}