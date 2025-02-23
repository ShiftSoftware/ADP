using System;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class BrokerInvoiceModel
{
    public string id { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public string ItemType => "BrokerInvoice";
    public DateTime InvoiceDate { get; set; }
    public long? BrokerCustomerID { get; set; }
    public bool IsDeleted { get; set; }
    public long Id { get; set; }
    public long InvoiceNumber { get; set; }
    public long? NonOfficialBrokerCustomerID { get; set; }
}
