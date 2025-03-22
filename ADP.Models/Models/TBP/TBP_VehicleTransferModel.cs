using System;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_VehicleTransferModel
{
    public long ID { get; set; }
    public bool IsDeleted { get; set; }
    public long SellerBrokerID { get; set; }
    public long BuyerBrokerID { get; set; }
    public DateTimeOffset TransferDate { get; set; }
}
