using System;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class BrokerInitialVehicleModel
{
    public string id { get; set; } = default!;
    public long Id { get; set; }
    public long BrokerID { get; set; }
    public string VIN { get; set; } = default!;
    public bool Deleted { get; set; }
    public DateTime SaveDate { get; set; }
    public string ItemType => "BrokerInitialVehicle";
}
