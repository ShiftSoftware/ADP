using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

namespace ShiftSoftware.ADP.ClaimableItems.Shared;

public static class ClaimableItemContractDurations
{
    /// <summary>
    /// Adds a contract or line duration to a date. Unlike the legacy helper this replaces, years are
    /// supported rather than silently ignored.
    /// </summary>
    public static DateTime AddTo(DateTime start, int duration, ClaimableItemContractDurationUnit unit) => unit switch
    {
        ClaimableItemContractDurationUnit.Days => start.AddDays(duration),
        ClaimableItemContractDurationUnit.Months => start.AddMonths(duration),
        ClaimableItemContractDurationUnit.Years => start.AddYears(duration),
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown duration unit."),
    };
}
