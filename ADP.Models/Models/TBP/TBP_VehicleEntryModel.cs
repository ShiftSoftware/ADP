using System;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_VehicleEntryModel
{
    public DateTime? InvoiceDate { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string Status { get; set; }
    public string LineStatus { get; set; }
    public string Location { get; set; }
    public long? CompanyID { get; set; }
    public string? Region { get; set; }
}
