using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

namespace ShiftSoftware.ADP.ClaimableItems.Shared;

/// <summary>
/// The contract arithmetic, shared by the repository (which stores the results) and the form (which previews
/// them), so what the form shows is what the save stores.
/// </summary>
public static class ClaimableItemContractCalculations
{
    /// <summary>The customer price of a line: its cost plus the contract's increment, to the cent.</summary>
    public static decimal PriceWithIncrement(decimal cost, decimal priceIncrement) =>
        decimal.Round(cost * (1 + priceIncrement), 2);

    /// <summary>
    /// The rolling expiry of the periodic lines. Ordered by the km they are due at, the first expires at
    /// <paramref name="start"/> + its duration and each next one at the previous expiry + its own duration.
    /// A line without a duration breaks the chain: it and every line after it get null.
    /// </summary>
    public static List<(T Line, DateTime? ExpireDate)> RollPeriodicExpiry<T>(
        DateTime start,
        IEnumerable<T> periodicLines,
        Func<T, int?> intervalKm,
        Func<T, int?> duration,
        Func<T, ClaimableItemContractDurationUnit?> durationUnit)
    {
        var result = new List<(T, DateTime?)>();
        DateTime? previous = start;

        foreach (var line in periodicLines.OrderBy(x => intervalKm(x) ?? 0))
        {
            if (previous is not null && duration(line) is > 0 && durationUnit(line) is { } unit)
                previous = ClaimableItemContractDurations.AddTo(previous.Value, duration(line)!.Value, unit);
            else
                previous = null;

            result.Add((line, previous));
        }

        return result;
    }
}
