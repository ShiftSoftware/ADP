using System;

namespace ShiftSoftware.ADP.Models.TBP;

public class BrokerInitialVehicleModel : IPartitionedItem
{
    public string id { get; set; } = default!;
    public long Id { get; set; }
    public long BrokerID { get; set; }
    public string VIN { get; set; } = default!;
    public bool Deleted { get; set; }
    public PartitionedItemType ItemType => ModelTypes.BrokerInitialVehicle;
}