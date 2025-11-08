using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class PaidServiceInvoiceLineModel : IIntegrationProps
{
    public string ServiceItemID { get; set; }
    public decimal Cost { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string PackageCode { get; set; }
    public ServiceItemModel ServiceItem { get; set; }
    public string IntegrationID { get; set; }
}