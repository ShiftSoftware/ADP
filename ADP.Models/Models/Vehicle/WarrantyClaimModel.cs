using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class WarrantyClaimModel : IPartitionedItem
{
    public string id { get; set; } = default!;
    public long ID { get; set; }
    public string VIN { get; set; } = default!;
    public bool IsDeleted { get; set; }
    public DateTime? RepairDate { get; set; }
    public int? ClaimStatus { get; set; }
    public string DistributorComment { get; set; }
    public string LaborOperationNoMain { get; set; }
    public string ItemType => ModelTypes.WarrantyClaim;
}