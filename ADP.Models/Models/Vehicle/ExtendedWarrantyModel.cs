using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ExtendedWarrantyModel : IPartitionedItem, ICompanyProps, IBranchProps
{
    public string id { get; set; }
    public string VIN { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Extended Warranty Entries may go through a long approval process until considered active. Or they may be revoked after issuing.
    /// The status is calculated outside and it's simply passed to ADP as a boolean
    /// </summary>
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public string ItemType => ModelTypes.ExtendedWarranty;

    public long? BranchID { get; set; }
    public string BranchHashID { get; set; }
}