using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class PaidServiceInvoiceLineModel
{
    public long Id { get; set; }
    public long PaidServiceInvoiceID { get; set; }
    public long ServiceItemID { get; set; }
    public decimal Cost { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string MenuCode { get; set; }
    public ServiceItemModel ServiceItem { get; set; }
}