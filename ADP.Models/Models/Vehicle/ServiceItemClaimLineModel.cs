using System;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class ServiceItemClaimLineModel: ICompanyProps, IBranchProps
{
    public string id { get; set; }
    public long Id { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public DateTime? ClaimDate { get; set; }
    public string ServiceItemID { get; set; }
    public int? RedeemType { get; set; }
    public string ItemType => "ServiceItemClaimLine";
    public virtual ServiceItemClaimModel ServiceItemClaim { get; set; }
}