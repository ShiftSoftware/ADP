using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

public class ClaimableItemContractCalculationsTests
{
    private record Line(int Km, int? Duration, ClaimableItemContractDurationUnit? Unit);

    private static List<(Line Line, DateTime? ExpireDate)> Roll(DateTime start, params Line[] lines) =>
        ClaimableItemContractCalculations.RollPeriodicExpiry(start, lines, x => x.Km, x => x.Duration, x => x.Unit);

    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(100, 0.05, 105)]
    [InlineData(33.33, 0.07, 35.66)]   // 35.6631, to the cent
    [InlineData(0, 0.10, 0)]
    public void Price_with_increment_is_cost_plus_increment_to_the_cent(decimal cost, decimal increment, decimal expected)
    {
        Assert.Equal(expected, ClaimableItemContractCalculations.PriceWithIncrement(cost, increment));
    }

    [Fact]
    public void Periodic_lines_roll_in_interval_order_each_from_the_previous_expiry()
    {
        var start = new DateTime(2026, 6, 1);

        var result = Roll(start,
            new Line(20000, 6, ClaimableItemContractDurationUnit.Months),
            new Line(10000, 3, ClaimableItemContractDurationUnit.Months),
            new Line(30000, 1, ClaimableItemContractDurationUnit.Years));

        Assert.Equal([10000, 20000, 30000], result.Select(x => x.Line.Km));
        Assert.Equal(new DateTime(2026, 9, 1), result[0].ExpireDate);
        Assert.Equal(new DateTime(2027, 3, 1), result[1].ExpireDate);
        Assert.Equal(new DateTime(2028, 3, 1), result[2].ExpireDate);
    }

    [Fact]
    public void A_line_without_a_duration_breaks_the_chain_from_there_on()
    {
        var result = Roll(new DateTime(2026, 6, 1),
            new Line(10000, 3, ClaimableItemContractDurationUnit.Months),
            new Line(20000, null, ClaimableItemContractDurationUnit.Months),
            new Line(30000, 3, ClaimableItemContractDurationUnit.Months));

        Assert.Equal(new DateTime(2026, 9, 1), result[0].ExpireDate);
        Assert.Null(result[1].ExpireDate);
        Assert.Null(result[2].ExpireDate);
    }

    [Fact]
    public void A_line_without_a_unit_breaks_the_chain_too()
    {
        var result = Roll(new DateTime(2026, 6, 1), new Line(10000, 3, null));

        Assert.Null(Assert.Single(result).ExpireDate);
    }

    [Fact]
    public void No_periodic_lines_gives_an_empty_chain()
    {
        Assert.Empty(Roll(new DateTime(2026, 6, 1)));
    }
}
