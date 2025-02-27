using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleBrokerSaleInformation
{
    public long BrokerID { get; set; }
    public string BrokerName { get; set; }
    public long? CustomerID { get; set; }
    public long? InvoiceNumber { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }
}