using System;
using System.Collections.Generic;
using System.Text;
using ShiftSoftware.ADP.Models.DealerData;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class TLPCancelledServiceItemCosmosModel
{
    public string id { get; set; }
    public string VIN { get; set; }
    public string ItemType => "TLPCancelledServiceItem";

    public long ID { get; set; }

    public long ToyotaLoyaltyProgramRedeemableItemID { get; set; }

    public long? TLPPackageInvoiceTLPItemID { get; set; }

    public VehcileServiceItemTypes Type { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? ModificationDate { get; set; }

    public DateTime? LastReplicationDate { get; set; }
}
