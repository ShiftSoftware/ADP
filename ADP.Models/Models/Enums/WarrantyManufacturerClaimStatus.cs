using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The manufacturer-side processing status of a warranty claim.
/// </summary>
[Docable]
public enum WarrantyManufacturerClaimStatus
{
    /// <summary>
    /// No manufacturer status available (not applicable or not yet submitted).
    /// </summary>
    [Description("N/A")]
    NA = 0,

    /// <summary>
    /// The claim has been exported to the manufacturer's system.
    /// </summary>
    [Description("Exported")]
    Exported = 1,

    /// <summary>
    /// The manufacturer has downloaded and acknowledged the claim.
    /// </summary>
    [Description("Downloaded")]
    Downloaded = 2,

    /// <summary>
    /// The manufacturer has approved and paid the claim.
    /// </summary>
    [Description("Paid")]
    Paid = 3,

    /// <summary>
    /// The manufacturer has rejected the claim.
    /// </summary>
    [Description("Rejected")]
    Rejected = 4,

    /// <summary>
    /// The claim is on hold at the manufacturer's side, pending further review.
    /// </summary>
    [Description("On Hold")]
    OnHold = 5
}