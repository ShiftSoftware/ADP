using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 0 — the golden contract for menu-code generation.
///
/// These snapshots pin the EXACT output of today's <see cref="MenuExportService.GenerateMenuLines"/>
/// for a fixed synthetic graph. They are the equality target for the planned refactor:
///
///   • Phase 1 extracts the fold into the shared <c>MenuCodeGenerator</c> (netstandard2.0). The
///     generator must reproduce these lines exactly.
///   • Phase 2 repoints the DMS export at that generator. These snapshots must stay green — that is
///     what proves the export's behaviour did not change.
///   • Phase 5 runs the same generator from the Cosmos-fed lookup path, which is how "the lookup's
///     codes are identical to the export's" becomes structural rather than aspirational.
///
/// A snapshot change is therefore a BEHAVIOUR CHANGE and must be deliberate. Do not re-baseline one
/// to make a build green without confirming the new value is the intended output.
///
/// See COSMOS_REPLICATION_PLAN.md §13. The quirks these tests pin (O1, O7, O10 and the whole-hour
/// allowed-time collision) were all reviewed and DECIDED as "preserve verbatim" — they affect rare
/// cases, and changing any of them would alter menu codes a DMS has already received. These
/// assertions are therefore permanent, not temporary scaffolding.
/// </summary>
public class MenuGenerationGoldenTests
{
    private static IEnumerable<MenuLineDTO> Generate(
        MenuGraphFixture.Fixture fixture,
        long countryId,
        decimal transferRate,
        string? language,
        bool usePrimaryLabourRate) =>
        MenuExportService.GenerateMenuLines(
            fixture.Variants,
            fixture.LabourRateMappings,
            fixture.BrandMappings,
            countryId,
            transferRate,
            language,
            usePrimaryLabourRate);

    /// <summary>
    /// Baseline: a configured country with price rows, transfer rate 1, English, country labour rate.
    ///
    /// Pins, among others: the periodic code shape "{prefix} {basicModelCode} {intervalCode} {postfix}",
    /// the labour code shape "{groupLabourCode}{allowedTimeText}{labourRateCode}{brandAbbrev}",
    /// exclusion of the interval whose group has no labour details (S03 never appears), exclusion of
    /// soft-deleted items/parts/prices and of zero/null periodic quantities, and the standalone
    /// grouped line folding two items while taking its allowed time from the FIRST item in the group.
    /// </summary>
    [Fact]
    public void Golden_Country2_TransferRate1_English_CountryLabourRate()
    {
        var actual = MenuLineFormatter.Format(
            Generate(MenuGraphFixture.Build(), countryId: 2, transferRate: 1.0m, language: "en", usePrimaryLabourRate: false));

        Assert.Equal(GoldenCountry2English, actual, ignoreLineEndingDifferences: true);
    }

    /// <summary>
    /// The 0-country + non-English + transfer-rate case.
    ///
    /// Pins: every part price collapsing to 0 when no country price row matches (no throw, no skip —
    /// the lines are still emitted with zero money), the Arabic language variants of the prefixes and
    /// the standalone operation / group menu codes, <c>Consumable</c> scaled by the transfer rate and
    /// rounded to 2dp, and <c>usePrimaryLabourRate: true</c> selecting the variant's primary rate
    /// instead of a country rate.
    /// </summary>
    [Fact]
    public void Golden_Country0_TransferRate2point5_Arabic_PrimaryLabourRate()
    {
        var actual = MenuLineFormatter.Format(
            Generate(MenuGraphFixture.Build(), countryId: 0, transferRate: 2.5m, language: "ar", usePrimaryLabourRate: true));

        Assert.Equal(GoldenCountry0Arabic, actual, ignoreLineEndingDifferences: true);
    }

    /// <summary>
    /// Pins the brand-abbreviation fallback: when the brand has no <see cref="BrandMapping"/> row the
    /// labour code ends in "Z" rather than throwing. Same graph as the baseline otherwise, so the
    /// diff against <see cref="GoldenCountry2English"/> is exactly the trailing abbreviation + BrandID.
    /// </summary>
    [Fact]
    public void Golden_UnmappedBrand_FallsBackToZAbbreviation()
    {
        var fixture = MenuGraphFixture.Build(brandId: MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false);

        var actual = MenuLineFormatter.Format(
            Generate(fixture, countryId: 2, transferRate: 1.0m, language: "en", usePrimaryLabourRate: false));

        Assert.Equal(GoldenUnmappedBrand, actual, ignoreLineEndingDifferences: true);
    }

    /// <summary>
    /// A periodic availability whose interval group has no <see cref="MenuLabourDetails"/> is silently
    /// skipped (the generator <c>continue</c>s). Interval S03 is in the graph but must never produce a
    /// line — worth an explicit assertion because a missing EF <c>Include</c> in a future refactor
    /// would make lines disappear exactly this quietly.
    /// </summary>
    [Fact]
    public void PeriodicInterval_WithoutMatchingLabourDetail_IsSkipped()
    {
        var lines = Generate(MenuGraphFixture.Build(), countryId: 2, transferRate: 1.0m, language: "en", usePrimaryLabourRate: false).ToList();

        Assert.DoesNotContain(lines, line => line.Code.Contains("S03"));
        Assert.Equal(2, lines.Count(line => !line.IsStandalone));
    }

    /// <summary>Soft-deleted rows and zero/null periodic quantities never reach the output.</summary>
    [Fact]
    public void SoftDeletedAndZeroQuantityRows_AreExcluded()
    {
        var lines = Generate(MenuGraphFixture.Build(), countryId: 2, transferRate: 1.0m, language: "en", usePrimaryLabourRate: false).ToList();
        var partNumbers = lines.SelectMany(line => line.Parts).Select(part => part.PartNumber).Distinct().ToList();

        Assert.DoesNotContain("PN-0003", partNumbers);   // soft-deleted part
        Assert.DoesNotContain("PN-0031", partNumbers);   // part of a soft-deleted menu item
        Assert.DoesNotContain("PN-0041", partNumbers);   // part behind a soft-deleted replacement-item link
        Assert.DoesNotContain("PN-0051", partNumbers);   // part of an item with no replacement-item link

        // Zero/null periodic quantity: absent from periodic lines, present on the standalone one.
        var periodicParts = lines.Where(line => !line.IsStandalone).SelectMany(line => line.Parts).Select(part => part.PartNumber).ToList();
        Assert.DoesNotContain("PN-0002", periodicParts);
        Assert.DoesNotContain("PN-0004", periodicParts);

        var standaloneParts = lines.Where(line => line.IsStandalone).SelectMany(line => line.Parts).Select(part => part.PartNumber).ToList();
        Assert.Contains("PN-0002", standaloneParts);
        Assert.Contains("PN-0004", standaloneParts);
    }

    /// <summary>
    /// Open item O1 — a missing <see cref="LabourRateMapping"/> for the (brand, primary labour rate)
    /// pair makes generation THROW, because the generator indexes the dictionary directly. The DMS
    /// export tolerates this only because it pre-checks the whole catalogue and fails up front.
    ///
    /// DECIDED: keep this behaviour. Softening it to a tolerant lookup was considered and rejected —
    /// the case does not arise in practice, and failing loudly beats silently emitting a menu line
    /// with a wrong labour code. The Phase 1 port throws identically.
    /// </summary>
    [Fact]
    public void MissingLabourRateMapping_Throws_O1()
    {
        var fixture = MenuGraphFixture.Build(includeLabourRateMapping: false);

        Assert.Throws<KeyNotFoundException>(() =>
            Generate(fixture, countryId: 2, transferRate: 1.0m, language: "en", usePrimaryLabourRate: false).ToList());
    }

    // -------------------------------------------------------------------------------------------
    // Golden snapshots. Regenerate ONLY when a behaviour change is intended.
    // -------------------------------------------------------------------------------------------

    private const string GoldenCountry2English =
        """
        #1 PERIODIC
           Code="MEN ABC12 S01 PX"
           LabourCode="GRPA05LR1A"
           Description="Plain description"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=20.00 AllowedTime=0.50 Consumable=4.00 Discount=10
           LabourCost=5.00 LabourPrice=10.0000 LabourTotalPrice=14.0000 LabourProfit=5.0000
           PartsCost=21.000 PartsPrice=27.000 PartsProfit=6.000 PartsProfitPercentage=28.57
           GrossProfit=11.0000 GrossProfitPercentage=29.73 MenuProfit=15.0000 MenuTotalPrice=36.90000
           part "PN-0001" Quantity=2 Cost=5.500 Price=7.250 TotalCost=11.000 TotalPrice=14.500
           part "PN-0011" Quantity=1 Cost=10.000 Price=12.500 TotalCost=10.000 TotalPrice=12.500
        #2 PERIODIC
           Code="MEN ABC12 S02 PX"
           LabourCode="GRPA05LR1A"
           Description="{"en":"Description EN","ar":"Description AR"}"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=20.00 AllowedTime=0.50 Consumable=4.00 Discount=10
           LabourCost=5.00 LabourPrice=10.0000 LabourTotalPrice=14.0000 LabourProfit=5.0000
           PartsCost=21.000 PartsPrice=27.000 PartsProfit=6.000 PartsProfitPercentage=28.57
           GrossProfit=11.0000 GrossProfitPercentage=29.73 MenuProfit=15.0000 MenuTotalPrice=36.90000
           part "PN-0001" Quantity=2 Cost=5.500 Price=7.250 TotalCost=11.000 TotalPrice=14.500
           part "PN-0011" Quantity=1 Cost=10.000 Price=12.500 TotalCost=10.000 TotalPrice=12.500
        #3 STANDALONE
           Code="STD OPA ABC12 SPX"
           LabourCode="SLA025LR1A"
           Description="Item A Friendly"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=20.00 AllowedTime=0.25 Consumable=0 Discount=10
           LabourCost=2.50 LabourPrice=5.0000 LabourTotalPrice=5.0000 LabourProfit=2.5000
           PartsCost=14.000 PartsPrice=19.750 PartsProfit=5.750 PartsProfitPercentage=41.07
           GrossProfit=8.2500 GrossProfitPercentage=33.33 MenuProfit=8.2500 MenuTotalPrice=22.27500
           part "PN-0001" Quantity=1 Cost=5.500 Price=7.250 TotalCost=5.500 TotalPrice=7.250
           part "PN-0002" Quantity=3 Cost=2.000 Price=3.000 TotalCost=6.000 TotalPrice=9.000
           part "PN-0004" Quantity=2 Cost=1.250 Price=1.750 TotalCost=2.500 TotalPrice=3.500
        #4 STANDALONE
           Code="STD GMC ABC12 SPX"
           LabourCode="GLC075LR1A"
           Description="Standalone Group One"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=20.00 AllowedTime=0.75 Consumable=0 Discount=10
           LabourCost=7.50 LabourPrice=15.0000 LabourTotalPrice=15.0000 LabourProfit=7.5000
           PartsCost=21.000 PartsPrice=27.000 PartsProfit=6.000 PartsProfitPercentage=28.57
           GrossProfit=13.5000 GrossProfitPercentage=32.14 MenuProfit=13.5000 MenuTotalPrice=37.80000
           part "PN-0011" Quantity=2 Cost=10.000 Price=12.500 TotalCost=20.000 TotalPrice=25.000
           part "PN-0021" Quantity=1 Cost=1.000 Price=2.000 TotalCost=1.000 TotalPrice=2.000
        """;

    private const string GoldenCountry0Arabic =
        """
        #1 PERIODIC
           Code="MENA ABC12 S01 PX"
           LabourCode="GRPA05LR1A"
           Description="Plain description"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=12.50 AllowedTime=0.50 Consumable=10.00 Discount=10
           LabourCost=5.00 LabourPrice=6.2500 LabourTotalPrice=16.2500 LabourProfit=1.2500
           PartsCost=0 PartsPrice=0 PartsProfit=0 PartsProfitPercentage=0
           GrossProfit=1.2500 GrossProfitPercentage=20.0 MenuProfit=11.2500 MenuTotalPrice=14.62500
           part "PN-0001" Quantity=2 Cost=0 Price=0 TotalCost=0 TotalPrice=0
           part "PN-0011" Quantity=1 Cost=0 Price=0 TotalCost=0 TotalPrice=0
        #2 PERIODIC
           Code="MENA ABC12 S02 PX"
           LabourCode="GRPA05LR1A"
           Description="{"en":"Description EN","ar":"Description AR"}"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=12.50 AllowedTime=0.50 Consumable=10.00 Discount=10
           LabourCost=5.00 LabourPrice=6.2500 LabourTotalPrice=16.2500 LabourProfit=1.2500
           PartsCost=0 PartsPrice=0 PartsProfit=0 PartsProfitPercentage=0
           GrossProfit=1.2500 GrossProfitPercentage=20.0 MenuProfit=11.2500 MenuTotalPrice=14.62500
           part "PN-0001" Quantity=2 Cost=0 Price=0 TotalCost=0 TotalPrice=0
           part "PN-0011" Quantity=1 Cost=0 Price=0 TotalCost=0 TotalPrice=0
        #3 STANDALONE
           Code="STDA OPAA ABC12 SPX"
           LabourCode="SLA025LR1A"
           Description="Item A Friendly"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=12.50 AllowedTime=0.25 Consumable=0 Discount=10
           LabourCost=2.50 LabourPrice=3.1250 LabourTotalPrice=3.1250 LabourProfit=0.6250
           PartsCost=0 PartsPrice=0 PartsProfit=0 PartsProfitPercentage=0
           GrossProfit=0.6250 GrossProfitPercentage=20.0 MenuProfit=0.6250 MenuTotalPrice=2.81250
           part "PN-0001" Quantity=1 Cost=0 Price=0 TotalCost=0 TotalPrice=0
           part "PN-0002" Quantity=3 Cost=0 Price=0 TotalCost=0 TotalPrice=0
           part "PN-0004" Quantity=2 Cost=0 Price=0 TotalCost=0 TotalPrice=0
        #4 STANDALONE
           Code="STDA GMCA ABC12 SPX"
           LabourCode="GLC075LR1A"
           Description="Standalone Group One"
           BasicModelCode="ABC12" BrandID=101 Model="Model One"
           LabourRate=12.50 AllowedTime=0.75 Consumable=0 Discount=10
           LabourCost=7.50 LabourPrice=9.3750 LabourTotalPrice=9.3750 LabourProfit=1.8750
           PartsCost=0 PartsPrice=0 PartsProfit=0 PartsProfitPercentage=0
           GrossProfit=1.8750 GrossProfitPercentage=20.0 MenuProfit=1.8750 MenuTotalPrice=8.43750
           part "PN-0011" Quantity=2 Cost=0 Price=0 TotalCost=0 TotalPrice=0
           part "PN-0021" Quantity=1 Cost=0 Price=0 TotalCost=0 TotalPrice=0
        """;

    private const string GoldenUnmappedBrand =
        """
        #1 PERIODIC
           Code="MEN ABC12 S01 PX"
           LabourCode="GRPA05LR1Z"
           Description="Plain description"
           BasicModelCode="ABC12" BrandID=999 Model="Model One"
           LabourRate=20.00 AllowedTime=0.50 Consumable=4.00 Discount=10
           LabourCost=5.00 LabourPrice=10.0000 LabourTotalPrice=14.0000 LabourProfit=5.0000
           PartsCost=21.000 PartsPrice=27.000 PartsProfit=6.000 PartsProfitPercentage=28.57
           GrossProfit=11.0000 GrossProfitPercentage=29.73 MenuProfit=15.0000 MenuTotalPrice=36.90000
           part "PN-0001" Quantity=2 Cost=5.500 Price=7.250 TotalCost=11.000 TotalPrice=14.500
           part "PN-0011" Quantity=1 Cost=10.000 Price=12.500 TotalCost=10.000 TotalPrice=12.500
        #2 PERIODIC
           Code="MEN ABC12 S02 PX"
           LabourCode="GRPA05LR1Z"
           Description="{"en":"Description EN","ar":"Description AR"}"
           BasicModelCode="ABC12" BrandID=999 Model="Model One"
           LabourRate=20.00 AllowedTime=0.50 Consumable=4.00 Discount=10
           LabourCost=5.00 LabourPrice=10.0000 LabourTotalPrice=14.0000 LabourProfit=5.0000
           PartsCost=21.000 PartsPrice=27.000 PartsProfit=6.000 PartsProfitPercentage=28.57
           GrossProfit=11.0000 GrossProfitPercentage=29.73 MenuProfit=15.0000 MenuTotalPrice=36.90000
           part "PN-0001" Quantity=2 Cost=5.500 Price=7.250 TotalCost=11.000 TotalPrice=14.500
           part "PN-0011" Quantity=1 Cost=10.000 Price=12.500 TotalCost=10.000 TotalPrice=12.500
        #3 STANDALONE
           Code="STD OPA ABC12 SPX"
           LabourCode="SLA025LR1Z"
           Description="Item A Friendly"
           BasicModelCode="ABC12" BrandID=999 Model="Model One"
           LabourRate=20.00 AllowedTime=0.25 Consumable=0 Discount=10
           LabourCost=2.50 LabourPrice=5.0000 LabourTotalPrice=5.0000 LabourProfit=2.5000
           PartsCost=14.000 PartsPrice=19.750 PartsProfit=5.750 PartsProfitPercentage=41.07
           GrossProfit=8.2500 GrossProfitPercentage=33.33 MenuProfit=8.2500 MenuTotalPrice=22.27500
           part "PN-0001" Quantity=1 Cost=5.500 Price=7.250 TotalCost=5.500 TotalPrice=7.250
           part "PN-0002" Quantity=3 Cost=2.000 Price=3.000 TotalCost=6.000 TotalPrice=9.000
           part "PN-0004" Quantity=2 Cost=1.250 Price=1.750 TotalCost=2.500 TotalPrice=3.500
        #4 STANDALONE
           Code="STD GMC ABC12 SPX"
           LabourCode="GLC075LR1Z"
           Description="Standalone Group One"
           BasicModelCode="ABC12" BrandID=999 Model="Model One"
           LabourRate=20.00 AllowedTime=0.75 Consumable=0 Discount=10
           LabourCost=7.50 LabourPrice=15.0000 LabourTotalPrice=15.0000 LabourProfit=7.5000
           PartsCost=21.000 PartsPrice=27.000 PartsProfit=6.000 PartsProfitPercentage=28.57
           GrossProfit=13.5000 GrossProfitPercentage=32.14 MenuProfit=13.5000 MenuTotalPrice=37.80000
           part "PN-0011" Quantity=2 Cost=10.000 Price=12.500 TotalCost=20.000 TotalPrice=25.000
           part "PN-0021" Quantity=1 Cost=1.000 Price=2.000 TotalCost=1.000 TotalPrice=2.000
        """;
}
