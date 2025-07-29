using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum ClaimStatus
{
    [Description("Draft")]
    Draft = 0,

    [Description("Pending")]
    PendingProcess = 1,

    [Description("Accepted")]
    Accepted = 2,

    [Description("Error")]
    RejectedWithError = 3,

    [Description("Rejected")]
    RejectedPermanently = 4,

    [Description("Certified")]
    Certified = 5,

    [Description("Invoiced")]
    Invoiced = 6
}