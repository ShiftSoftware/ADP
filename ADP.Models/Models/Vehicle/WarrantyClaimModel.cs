using System;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class WarrantyClaimModel: IPartitionedItem
{
    public string id { get; set; } = default!;
    public long Id { get; set; }
    public string VIN { get; set; } = default!;
    public bool IsDeleted { get; set; }
    public DateTime? RepairDate { get; set; }
    public int? ClaimStatus { get; set; }
    public string DistComment1 { get; set; }
    public string LaborOperationNoMain { get; set; }
    public PartitionedItemType ItemType => ModelTypes.WarrantyClaim;
}