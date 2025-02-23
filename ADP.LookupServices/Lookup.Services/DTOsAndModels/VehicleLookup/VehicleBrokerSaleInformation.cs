using System;
using System.Collections.Generic;
using System.Text;
using ShiftSoftware.ADP.Models.JsonConverters;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class VehicleBrokerSaleInformation
{
    public long BrokerId { get; set; }
    public string BrokerName { get; set; }
    public long? CustomerID { get; set; }
    public long? InvoiceNumber { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }
}
