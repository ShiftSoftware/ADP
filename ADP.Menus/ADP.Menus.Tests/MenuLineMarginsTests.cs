using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 2 — the derived margin / profit arithmetic after its move off <see cref="MenuLineDTO"/> and
/// into the report layer (<see cref="MenuLineMargins"/>).
///
/// The Phase 0 golden snapshots render every one of these figures, so they already prove the move did
/// not change any value. These tests add the parts a snapshot cannot reach: the divide-by-zero guards,
/// the rounding scale, and the null-<c>Parts</c> case — the edges most at risk if someone later "tidies"
/// a formula.
/// </summary>
public class MenuLineMarginsTests
{
    private static MenuLineDTO Line() => new()
    {
        AllowedTime = 0.50m,
        LabourRate = 20.00m,
        Consumable = 4.00m,
        DiscountPercentage = 10m,
        Parts =
        [
            new MenuLinePartDTO { PartNumber = "PN-0001", Quantity = 2m, Cost = 5.500m, Price = 7.250m },
            new MenuLinePartDTO { PartNumber = "PN-0011", Quantity = 1m, Cost = 10.000m, Price = 12.500m },
        ],
    };

    /// <summary>
    /// The same line as golden snapshot #1, asserted figure by figure — decimal SCALE included, because
    /// the snapshots pin unformatted decimals and a change of scale is a change of snapshot.
    /// </summary>
    [Fact]
    public void EveryFigure_MatchesTheGoldenSnapshotLine()
    {
        var line = Line();

        Assert.Equal(5.00m, line.LabourCost);
        Assert.Equal(10.0000m, line.LabourPrice);
        Assert.Equal(14.0000m, line.LabourTotalPrice);
        Assert.Equal(5.0000m, line.LabourProfit);

        Assert.Equal(21.000m, line.PartsCost);
        Assert.Equal(27.000m, line.PartsPrice);
        Assert.Equal(6.000m, line.PartsProfit);
        Assert.Equal(28.57m, line.PartsProfitPercentage);

        Assert.Equal(11.0000m, line.GrossProfit);
        Assert.Equal(29.73m, line.GrossProfitPercentage);
        Assert.Equal(15.0000m, line.MenuProfit);
        Assert.Equal(36.90000m, line.MenuTotalPrice);
    }

    /// <summary>Labour cost is a flat 10 per allowed hour — NOT the dealer's labour rate.</summary>
    [Fact]
    public void LabourCost_IsTenPerAllowedHour_RegardlessOfLabourRate()
    {
        var line = Line();
        line.LabourRate = 999m;

        Assert.Equal(5.00m, line.LabourCost);
    }

    /// <summary>A zero parts cost yields 0%, not a divide-by-zero.</summary>
    [Fact]
    public void PartsProfitPercentage_IsZero_WhenPartsCostIsZero()
    {
        var line = Line();
        line.Parts = [new MenuLinePartDTO { PartNumber = "PN-0001", Quantity = 2m, Cost = 0m, Price = 7.250m }];

        Assert.Equal(0m, line.PartsCost);
        Assert.Equal(0m, line.PartsProfitPercentage);
    }

    /// <summary>Zero revenue yields 0%, not a divide-by-zero.</summary>
    [Fact]
    public void GrossProfitPercentage_IsZero_WhenRevenueIsZero()
    {
        var line = new MenuLineDTO { AllowedTime = 0m, LabourRate = 0m, Consumable = 0m, Parts = [] };

        Assert.Equal(0m, line.GrossProfitPercentage);
    }

    /// <summary>Percentages are rounded to two decimals — the scale the snapshots pin.</summary>
    [Fact]
    public void Percentages_AreRoundedToTwoDecimals()
    {
        var line = Line();

        Assert.Equal(2, line.PartsProfitPercentage.Scale);
        Assert.Equal(2, line.GrossProfitPercentage.Scale);
    }

    /// <summary>A null <c>Parts</c> collection contributes 0 rather than throwing.</summary>
    [Fact]
    public void NullParts_ContributeZero()
    {
        var line = new MenuLineDTO { AllowedTime = 0.50m, LabourRate = 20.00m, Consumable = 4.00m, Parts = null };

        Assert.Equal(0m, line.PartsCost);
        Assert.Equal(0m, line.PartsPrice);
        Assert.Equal(0m, line.PartsProfit);
        Assert.Equal(14.0000m, line.MenuTotalPrice);
    }

    /// <summary>A null discount is treated as no discount.</summary>
    [Fact]
    public void NullDiscount_LeavesTheTotalUndiscounted()
    {
        var line = Line();
        line.DiscountPercentage = null;

        Assert.Equal(line.LabourTotalPrice + line.PartsPrice, line.MenuTotalPrice);
    }
}
