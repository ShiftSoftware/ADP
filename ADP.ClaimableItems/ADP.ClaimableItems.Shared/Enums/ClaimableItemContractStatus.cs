using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

/// <summary>
/// A contract's state as shown to a user. Not stored: it is derived from <c>IsDraft</c> and
/// <c>IsCancelled</c>.
/// </summary>
public enum ClaimableItemContractStatus
{
    [Description("Draft")]
    Draft = 0,

    [Description("Issued")]
    Issued = 1,

    [Description("Cancelled")]
    Cancelled = 2,
}
