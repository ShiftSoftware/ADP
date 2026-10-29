using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

public enum ClaimableItemContractDurationUnit
{
    [Description("Days")]
    Days = 1,

    [Description("Months")]
    Months = 2,

    [Description("Years")]
    Years = 3,
}
