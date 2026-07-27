using ShiftSoftware.ADP.Menus.Generation;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Covers the detail a <see cref="GeneratedMenuLine"/> carries ALONGSIDE its generated codes.
///
/// Consumers live outside this package — the report exporter is in the host application, the vehicle
/// lookup is in ADP.LookupServices — so they must be able to render or re-compose a line without
/// re-reading the menus database or being handed the mapping dictionaries separately. These tests
/// assert that promise holds: every component that went into a code is recoverable from the result,
/// and recombining the components reproduces the code exactly.
/// </summary>
public class GeneratedMenuLineDetailTests
{
    private static List<GeneratedMenuLine> Generate(
        long countryId = 2,
        string language = "en",
        decimal transferRate = 1.0m,
        long brandId = MenuGraphFixture.BrandId,
        bool includeBrandMapping = true,
        bool includePartCost = false) =>
        MenuCodeGenerator.Generate(
            MenuGenerationRequestFixture.Build(brandId, includeBrandMapping),
            new MenuGenerationConfig
            {
                CountryID = countryId,
                Language = language,
                TransferRate = transferRate,
                IncludePartCost = includePartCost,
            }).ToList();

    /// <summary>
    /// The menu code is exactly its components recombined. Note the segment and the model code SWAP
    /// places between periodic and standalone lines, which is why a consumer needs
    /// <see cref="GeneratedMenuLine.LineType"/> to reconstruct.
    /// </summary>
    [Theory]
    [InlineData("en")]
    [InlineData("ar")]
    public void MenuCodeComponents_RecomposeTheCode(string language)
    {
        var lines = Generate(language: language);
        Assert.NotEmpty(lines);

        foreach (var line in lines)
        {
            var recomposed = line.LineType == MenuLineType.Periodic
                ? $"{line.MenuCodePrefix} {line.BasicModelCode} {line.MenuCodeSegment} {line.MenuCodePostfix}".Trim()
                : $"{line.MenuCodePrefix} {line.MenuCodeSegment} {line.BasicModelCode} {line.MenuCodePostfix}".Trim();

            Assert.Equal(line.Code, recomposed);
        }
    }

    /// <summary>The labour code is exactly its four components concatenated.</summary>
    [Fact]
    public void LabourCodeComponents_RecomposeTheCode()
    {
        var lines = Generate();
        Assert.NotEmpty(lines);

        foreach (var line in lines)
        {
            var recomposed =
                $"{line.LabourOperationCode}{line.AllowedTimeText}{line.LabourRateCode}{line.BrandAbbreviation}".Trim();

            Assert.Equal(line.LabourCode, recomposed);
        }
    }

    /// <summary>
    /// The labour-rate code and brand abbreviation are surfaced, so a consumer never needs the mapping
    /// dictionaries the export currently receives as separate context.
    /// </summary>
    [Fact]
    public void LabourRateCodeAndBrandAbbreviation_AreSurfaced()
    {
        var lines = Generate();

        Assert.All(lines, line =>
        {
            Assert.Equal("LR1", line.LabourRateCode);
            Assert.Equal("A", line.BrandAbbreviation);
        });
    }

    /// <summary>
    /// The brand mapping's COMPANY CODE is carried too. It feeds no generated code — a DMS export
    /// writes it as its own column — which is exactly why it would otherwise force the consumer to
    /// keep a brand-mapping dictionary around.
    /// </summary>
    [Fact]
    public void BrandCode_IsCarried_AndIsNullWhenBrandIsUnmapped()
    {
        Assert.All(Generate(), line => Assert.Equal("BC1", line.BrandCode));

        var unmapped = Generate(brandId: MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false);

        Assert.All(unmapped, line =>
        {
            Assert.Null(line.BrandCode);            // no mapping row, and no fallback for this one
            Assert.Equal("Z", line.BrandAbbreviation);   // ...whereas the abbreviation does fall back
        });
    }

    /// <summary>
    /// <see cref="MenuLineType"/> distinguishes the two standalone shapes, which the older
    /// <c>IsStandalone</c> flag collapsed together.
    /// </summary>
    [Fact]
    public void LineType_DistinguishesTheThreeShapes()
    {
        var lines = Generate();

        Assert.Equal(
            [MenuLineType.Periodic, MenuLineType.Periodic, MenuLineType.StandaloneUngrouped, MenuLineType.StandaloneGrouped],
            lines.Select(line => line.LineType));

        Assert.All(lines, line => Assert.Equal(line.LineType != MenuLineType.Periodic, line.IsStandalone));
    }

    /// <summary>Each line points back at the source rows that produced it.</summary>
    [Fact]
    public void SourceRowIdentifiers_ArePopulatedPerLineType()
    {
        var lines = Generate();

        var periodic = lines.Where(line => line.LineType == MenuLineType.Periodic).ToList();
        Assert.Equal([501L, 502L], periodic.Select(line => line.ServiceIntervalID));
        Assert.All(periodic, line =>
        {
            Assert.Equal(10L, line.ServiceIntervalGroupID);   // both intervals resolve via group 10
            Assert.Null(line.MenuItemID);
            Assert.Null(line.StandaloneGroupID);
        });

        var ungrouped = lines.Single(line => line.LineType == MenuLineType.StandaloneUngrouped);
        Assert.Equal(900L, ungrouped.MenuItemID);
        Assert.Null(ungrouped.StandaloneGroupID);
        Assert.Null(ungrouped.ServiceIntervalID);

        var grouped = lines.Single(line => line.LineType == MenuLineType.StandaloneGrouped);
        Assert.Equal(800L, grouped.StandaloneGroupID);
        Assert.Null(grouped.MenuItemID);
        Assert.Null(grouped.ServiceIntervalID);
    }

    /// <summary>
    /// A grouped line folds parts from several menu items, so each part records which item it came
    /// from — otherwise that association is lost in the fold.
    /// </summary>
    [Fact]
    public void Parts_RecordTheirOriginatingMenuItem()
    {
        var grouped = Generate().Single(line => line.LineType == MenuLineType.StandaloneGrouped);

        Assert.Equal(
            [(901L, "PN-0011"), (902L, "PN-0021")],
            grouped.Parts.Select(part => (part.MenuItemID, part.PartNumber)));
    }

    /// <summary>
    /// The part's authored sort order is carried, so a consumer can present parts in the order they
    /// were authored. The generator itself does not sort by it — output order follows input order.
    /// </summary>
    [Fact]
    public void Parts_CarryTheirAuthoredSortOrder()
    {
        var ungrouped = Generate().Single(line => line.LineType == MenuLineType.StandaloneUngrouped);

        Assert.Equal(
            [("PN-0001", 0), ("PN-0002", 1), ("PN-0004", 3)],   // PN-0003 (sort order 2) is soft-deleted
            ungrouped.Parts.Select(part => (part.PartNumber, part.SortOrder)));
    }

    /// <summary>
    /// Distinguishes "priced at zero" from "no price row for this country" — both render as 0 money,
    /// and a consumer showing prices needs to tell them apart.
    /// </summary>
    [Fact]
    public void Parts_ReportWhetherACountryPriceWasFound()
    {
        Assert.All(Generate(countryId: 2).SelectMany(line => line.Parts), part =>
        {
            Assert.True(part.HasCountryPrice);
            Assert.True(part.Price > 0);
        });

        Assert.All(Generate(countryId: 0).SelectMany(line => line.Parts), part =>
        {
            Assert.False(part.HasCountryPrice);
            Assert.Equal(0, part.Price);
        });
    }

    /// <summary>
    /// Dealer cost is OMITTED unless asked for. This is the safe default: the lookup and the public web
    /// components must never see dealer cost, and making exclusion the default means a consumer leaks it
    /// only by explicitly requesting it — never by forgetting to strip it.
    /// </summary>
    [Fact]
    public void PartCost_IsExcludedByDefault()
    {
        var parts = Generate().SelectMany(line => line.Parts).ToList();

        Assert.NotEmpty(parts);
        Assert.All(parts, part =>
        {
            Assert.Null(part.Cost);
            Assert.Null(part.TotalCost);      // null propagates through the derived total
            Assert.True(part.Price > 0);      // ...while retail price is unaffected
            Assert.True(part.TotalPrice > 0);
        });
    }

    /// <summary>The DMS export opts in, and then gets the same figures the legacy export produced.</summary>
    [Fact]
    public void PartCost_IsIncludedWhenRequested()
    {
        var part = Generate(includePartCost: true)
            .SelectMany(line => line.Parts)
            .First(x => x.PartNumber == "PN-0001");

        Assert.Equal(5.500m, part.Cost);
        Assert.Equal(11.000m, part.TotalCost);   // 5.500 × 2
    }

    /// <summary>
    /// When cost IS requested but the part has no country price row, it falls back to 0 — matching the
    /// export. So null always means "not requested", never "not priced"; <see cref="GeneratedMenuPart.HasCountryPrice"/>
    /// is what distinguishes the latter.
    /// </summary>
    [Fact]
    public void PartCost_FallsBackToZeroWhenRequestedButUnpriced()
    {
        Assert.All(Generate(countryId: 0, includePartCost: true).SelectMany(line => line.Parts), part =>
        {
            Assert.False(part.HasCountryPrice);
            Assert.Equal(0m, part.Cost);        // requested → 0, not null
            Assert.Equal(0m, part.TotalCost);
        });
    }

    /// <summary>
    /// The unscaled consumable and the transfer rate are both carried, so a consumer can re-derive or
    /// re-scale rather than being stuck with the one figure this run produced.
    /// </summary>
    [Fact]
    public void RawConsumableAndTransferRate_AllowRescaling()
    {
        var line = Generate(transferRate: 2.5m).First(x => x.LineType == MenuLineType.Periodic);

        Assert.Equal(4.00m, line.RawConsumable);
        Assert.Equal(2.5m, line.TransferRate);
        Assert.Equal(10.00m, line.Consumable);
        Assert.Equal(line.Consumable, Math.Round(line.RawConsumable * line.TransferRate, 2));

        // Standalone lines have no consumable at all, scaled or raw.
        Assert.All(Generate(transferRate: 2.5m).Where(x => x.IsStandalone), standalone =>
        {
            Assert.Equal(0, standalone.Consumable);
            Assert.Equal(0, standalone.RawConsumable);
        });
    }

    /// <summary>
    /// The primary labour rate is kept alongside the resolved one: it is the labour-code lookup key,
    /// so it stays fixed while the rate ON the line follows the country.
    /// </summary>
    [Fact]
    public void PrimaryLabourRate_IsCarriedSeparatelyFromTheResolvedRate()
    {
        var line = Generate(countryId: 2).First();

        Assert.Equal(20.00m, line.LabourRate);                                  // country 2's rate
        Assert.Equal(MenuGraphFixture.PrimaryLabourRate, line.PrimaryLabourRate);   // the mapping key

        var otherCountry = Generate(countryId: 3).First();
        Assert.Equal(30.00m, otherCountry.LabourRate);
        Assert.Equal(MenuGraphFixture.PrimaryLabourRate, otherCountry.PrimaryLabourRate);

        // ...and because the labour code keys on the primary rate, it is identical across countries.
        Assert.Equal(line.LabourCode, otherCountry.LabourCode);
    }

    /// <summary>
    /// Each line echoes the configuration it was generated under, so results merged from several runs
    /// (multiple languages or countries) stay self-describing.
    /// </summary>
    [Fact]
    public void EachLine_EchoesTheConfigurationItWasGeneratedUnder()
    {
        Assert.All(Generate(countryId: 3, language: "ar", transferRate: 1.5m), line =>
        {
            Assert.Equal(3, line.CountryID);
            Assert.Equal("ar", line.Language);
            Assert.Equal(1.5m, line.TransferRate);
        });
    }
}
