using System;
using ShiftSoftware.ADP.Models.DealerData;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class BrokerInitialVehicleCosmosModel
{
    public string id { get; set; } = default!;

    public long Id { get; set; }

    public long BrokerId { get; set; }

    public string VIN { get; set; } = default!;

    public bool Deleted { get; set; }
    public DateTime SaveDate { get; set; }

    public string ItemType => "BrokerInitialVehicle";
}
