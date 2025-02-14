using System;

namespace ShiftSoftware.ADP.Models.DTOs.TBP;

public class BrokerVehicleStockDTO
{
    public string VIN { get; set; }
    public string AccountNumber { get; set; }
    public string Model { get; set; }
    public string ModelYear { get; set; }
    public string Engine { get; set; }
    public string Variant_Desc { get; set; }
    public string Variant_Code { get; set; }
    public string Katashiki { get; set; }
    public string Color { get; set; }
    public string ColorDesc { get; set; }
    public string Trim { get; set; }
    public string TrimDesc { get; set; }
    public string Cylinders { get; set; }
    public string PlateNumber { get; set; }
    public string PlateNumberRegion { get; set; }
    public decimal? RSP { get; set; }
    public decimal? MSP { get; set; }
    public string TransferedFrom { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string VSRegion { get; set; }
    public string BrokerName { get; set; }
}
