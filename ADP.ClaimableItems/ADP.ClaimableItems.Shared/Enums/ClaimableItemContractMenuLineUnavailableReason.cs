using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

/// <summary>Why a contract menu line cannot be put on a contract.</summary>
public enum ClaimableItemContractMenuLineUnavailableReason
{
    /// <summary>At least one part has no price for the country, so the price is understated.</summary>
    [Description("Has parts without a price")]
    UnpricedParts = 1,

    /// <summary>A periodic service due at or below the vehicle's mileage.</summary>
    [Description("At or below the vehicle's mileage")]
    AtOrBelowMileage = 2,

    /// <summary>
    /// The same periodic menu is on another issued contract of the SAME VIN that is neither cancelled nor
    /// expired. Other VINs never block it.
    /// </summary>
    [Description("Already on another contract")]
    OnAnotherContract = 3,
}
