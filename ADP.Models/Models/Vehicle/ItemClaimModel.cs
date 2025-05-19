using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ItemClaimModel : IPartitionedItem, ICompanyProps, IBranchProps
{
    public string id { get; set; }
    public long ID { get; set; }
    public string VIN { get; set; } = default!;
    public string CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public string BranchID { get; set; }
    public string BranchHashID { get; set; }
    public DateTimeOffset ClaimDate { get; set; }
    public bool IsDeleted { get; set; }
    public string ServiceItemID { get; set; }
    public string VehicleInspectionID { get; set; }
    public decimal Cost { get; set; }
    public string PackageCode { get; set; }
    public string JobNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public string QRCode { get; set; }
    public string ItemType => ModelTypes.ItemClaim;
}