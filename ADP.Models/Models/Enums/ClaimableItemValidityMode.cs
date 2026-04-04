using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Determines how the validity period of a claimable item is calculated.
/// </summary>
[Docable]
public enum ClaimableItemValidityMode
{
    /// <summary>
    /// Validity is calculated relative to the activation date (e.g., valid for X days after activation).
    /// </summary>
    [Description("Relative to activation date (e.g., valid for X days after activation)")]
    RelativeToActivation = 1,

    /// <summary>
    /// Validity uses fixed calendar dates regardless of when the item was activated (e.g., valid from date X to date Y).
    /// </summary>
    [Description("Fixed calendar dates (e.g., valid from X to Y regardless of activation)")]
    FixedDateRange = 2
}