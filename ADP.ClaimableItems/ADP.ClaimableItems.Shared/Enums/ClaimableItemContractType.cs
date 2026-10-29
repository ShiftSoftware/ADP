using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

/// <summary>
/// The commercial kind of a service contract. Values are stored, so they are frozen.
/// </summary>
public enum ClaimableItemContractType
{
    [Description("Purchased Service Contract")]
    PurchasedServiceContract = 0,

    [Description("Leasing Contract")]
    LeasingContract = 1,
}
