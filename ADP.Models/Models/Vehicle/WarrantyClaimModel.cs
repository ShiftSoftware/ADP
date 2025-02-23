using System;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class WarrantyClaimModel
{
    public string id { get; set; } = default!;
    public long Id { get; set; }
    public string VIN { get; set; } = default!;
    public string ItemType => "WarrantyClaim";
    public bool IsDeleted { get; set; }
    public DateTime? RepairDate { get; set; }
    public int? ClaimStatus { get; set; }
    public string DistComment1 { get; set; }
    public string LaborOperationNoMain { get; set; }
}