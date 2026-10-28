using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

/// <summary>
/// What produced a contract line on the service menu. Same values as the lookup's
/// <c>ServiceMenuLineType</c>, which this assembly does not reference.
/// </summary>
public enum ClaimableItemContractLineType
{
    /// <summary>A scheduled service at one interval. May not be repeated on a VIN's live contracts.</summary>
    [Description("Periodic")]
    Periodic = 0,

    [Description("Standalone")]
    Standalone = 1,

    [Description("Standalone Group")]
    StandaloneGroup = 2,
}
