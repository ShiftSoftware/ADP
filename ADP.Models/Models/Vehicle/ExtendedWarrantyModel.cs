using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents an extended warranty contract purchased for a vehicle beyond the standard manufacturer warranty.
/// Extended warranties go through an approval process and can be revoked after issuing.
/// </summary>
[Docable]
public class ExtendedWarrantyModel : IPartitionedItem, ICompanyProps, IBranchProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) that this extended warranty applies to.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The start date of the extended warranty coverage period.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end date of the extended warranty coverage period.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Extended Warranty Entries may go through a long approval process until considered active. Or they may be revoked after issuing.
    /// The status is calculated outside and it's simply passed to ADP as a boolean.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indicates whether this extended warranty record has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.ExtendedWarranty;

    [DocIgnore]
    public long? BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }
}