using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The processing lifecycle status of a warranty or service claim.
/// </summary>
[Docable]
public enum ClaimStatus
{
    /// <summary>
    /// The claim has been created but not yet submitted for processing.
    /// </summary>
    [Description("Draft")]
    Draft = 0,

    /// <summary>
    /// The claim has been submitted and is pending processing.
    /// </summary>
    [Description("Pending")]
    PendingProcess = 1,

    /// <summary>
    /// The claim has been reviewed and accepted.
    /// </summary>
    [Description("Accepted")]
    Accepted = 2,

    /// <summary>
    /// The claim was rejected due to an error that may be correctable.
    /// </summary>
    [Description("Error")]
    RejectedWithError = 3,

    /// <summary>
    /// The claim was permanently rejected.
    /// </summary>
    [Description("Rejected")]
    RejectedPermanently = 4,

    /// <summary>
    /// The claim has been certified by the manufacturer.
    /// </summary>
    [Description("Certified")]
    Certified = 5,

    /// <summary>
    /// The claim has been invoiced for payment.
    /// </summary>
    [Description("Invoiced")]
    Invoiced = 6
}