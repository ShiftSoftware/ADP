using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.Enums;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// What the report rows carry beyond the generated line — currently the service interval's three
/// per-menu-type descriptions, handed to <see cref="IMenuReportExporter"/> so a client's report can use
/// one other than the line's own <see cref="MenuLineDTO.Description"/>.
/// </summary>
public class MenuExportServiceTests
{
    private static List<MenuLineDTO> Generate(MenuType menuType)
    {
        var fixture = MenuGraphFixture.Build();
        fixture.Variants.Single().MenuType = menuType;

        return MenuExportService.GenerateMenuLines(
            fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings, countryId: 2, transferRate: 1m, language: "en")
            .ToList();
    }

    /// <summary>
    /// Every scheduled row carries all three descriptions of its interval whatever the variant's type,
    /// while <see cref="MenuLineDTO.Description"/> stays the one for that type.
    /// </summary>
    [Theory]
    [InlineData(MenuType.Paid, "Plain description")]
    [InlineData(MenuType.Free, "Plain description (free)")]
    [InlineData(MenuType.Contract, "Plain description (contract)")]
    public void AScheduledRow_CarriesAllThreeDescriptions(MenuType menuType, string expectedDescription)
    {
        var row = Assert.Single(Generate(menuType), line => !line.IsStandalone && line.PaidDescription == "Plain description");

        Assert.Equal(expectedDescription, row.Description);
        Assert.Equal("Plain description (free)", row.FreeDescription);
        Assert.Equal("Plain description (contract)", row.ContractDescription);
    }

    /// <summary>A standalone row has no interval, so it has none of the three.</summary>
    [Fact]
    public void AStandaloneRow_HasNoIntervalDescriptions()
    {
        var standalone = Generate(MenuType.Paid).Where(line => line.IsStandalone).ToList();

        Assert.NotEmpty(standalone);
        Assert.All(standalone, line =>
        {
            Assert.Null(line.PaidDescription);
            Assert.Null(line.FreeDescription);
            Assert.Null(line.ContractDescription);
        });
    }
}
