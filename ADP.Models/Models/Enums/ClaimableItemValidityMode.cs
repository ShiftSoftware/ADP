using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum ClaimableItemValidityMode
{
    [Description("Relative to activation date (e.g., valid for X days after activation)")]
    RelativeToActivation = 1,

    [Description("Fixed calendar dates (e.g., valid from X to Y regardless of activation)")]
    FixedDateRange = 2
}