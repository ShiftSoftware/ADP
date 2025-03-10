using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ServiceItemClaimLineModel : IPartitionedItem, ICompanyProps, IBranchProps
{
    public string id { get; set; }
    public long ID { get; set; }
    public string VIN { get; set; } = default!;
    public string CompanyID { get; set; }
    public string BranchID { get; set; }
    public DateTime? ClaimDate { get; set; }
    public string ServiceItemID { get; set; }
    public decimal Cost { get; set; }
    public virtual ServiceItemClaimModel ServiceItemClaim { get; set; }
    public string ItemType => ModelTypes.ServiceItemClaimLine;
}