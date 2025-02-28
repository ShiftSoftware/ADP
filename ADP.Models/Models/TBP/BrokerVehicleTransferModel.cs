using System;

namespace ShiftSoftware.ADP.Models.TBP;

public class BrokerVehicleTransferModel
{
    public string id { get; set; } = default!;
    public long Id { get; set; }
    public string VIN { get; set; } = default!;
    public DateTime TransferDate { get; set; }
    public long TransferNumber { get; set; }
    public bool IsDeleted { get; set; }
    public long? FromBrokerID { get; set; }
    public long? ToBrokerID { get; set; }
    public string ItemType => ModelTypes.BrokerVehicleTransfer;
}