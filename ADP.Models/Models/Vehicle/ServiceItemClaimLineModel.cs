using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ServiceItemClaimLineModel : IPartitionedItem, ICompanyProps, IBranchProps
{
    public string id { get; set; }
    public long Id { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public DateTime? ClaimDate { get; set; }
    public string ServiceItemID { get; set; }
    public decimal Cost { get; set; }
    public virtual ServiceItemClaimModel ServiceItemClaim { get; set; }
    public PartitionedItemType ItemType => ModelTypes.ServiceItemClaimLine;
}