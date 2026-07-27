using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Generation;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 1 — proves the shared <see cref="MenuCodeGenerator"/> is a faithful port of the DMS export's
/// <c>MenuExportService.GenerateMenuLines</c>.
///
/// These are DIFFERENTIAL tests: the same logical menu data is fed to both implementations — as EF
/// entities to the original, as a hand-built <see cref="MenuGenerationRequest"/> to the port — and the
/// outputs are compared field by field. Two independently-authored fixtures are used deliberately; a
/// shared adapter would let a bug cancel itself out on both sides.
///
/// This is what makes "the lookup's codes are identical to the export's" a property of the build
/// rather than a promise. Once Phase 2 repoints the export at the generator these tests become
/// tautological — keep them anyway as the regression guard on the generator itself.
/// </summary>
public class MenuCodeGeneratorPortTests
{
    private static string Legacy(long countryId, decimal transferRate, string? language, bool usePrimaryLabourRate, long brandId = MenuGraphFixture.BrandId, bool includeBrandMapping = true)
    {
        var fixture = MenuGraphFixture.Build(brandId, includeBrandMapping);
        return MenuLineFormatter.FormatCore(MenuExportService.GenerateMenuLines(
            fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings,
            countryId, transferRate, language, usePrimaryLabourRate));
    }

    private static string Ported(long countryId, decimal transferRate, string? language, bool usePrimaryLabourRate, long brandId = MenuGraphFixture.BrandId, bool includeBrandMapping = true)
    {
        var request = MenuGenerationRequestFixture.Build(brandId, includeBrandMapping);
        return MenuLineFormatter.FormatCore(MenuCodeGenerator.Generate(request, new MenuGenerationConfig
        {
            CountryID = countryId,
            TransferRate = transferRate,
            Language = language,
            UsePrimaryLabourRate = usePrimaryLabourRate,

            // The export path — the only one that gets dealer cost, and the one these tests compare
            // against. The lookup leaves this off; see PartCost_IsExcludedByDefault.
            IncludePartCost = true,
        }));
    }

    [Theory]
    // country, transferRate, language, usePrimaryLabourRate
    [InlineData(2, 1.0, "en", false)]     // baseline: country prices, country labour rate
    [InlineData(0, 2.5, "ar", true)]      // no country rows, scaled consumable, primary rate, Arabic
    [InlineData(2, 1.0, "ar", false)]     // language only
    [InlineData(3, 1.0, "en", false)]     // the other country's prices and labour rate
    [InlineData(2, 1.0, null, false)]     // null language → English
    [InlineData(2, 1.0, "fr", false)]     // unknown language → English fallback
    [InlineData(2, 3.3333, "en", false)]  // rounding of the scaled consumable
    [InlineData(2, 1.0, "en", true)]      // primary labour rate with country prices present
    [InlineData(9, 1.0, "en", false)]     // country with no price and no labour rate rows
    public void PortedGenerator_MatchesLegacyExport(double countryId, double transferRate, string? language, bool usePrimaryLabourRate)
    {
        var expected = Legacy((long)countryId, (decimal)transferRate, language, usePrimaryLabourRate);
        var actual = Ported((long)countryId, (decimal)transferRate, language, usePrimaryLabourRate);

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    /// <summary>The "Z" brand-abbreviation fallback must survive the port too.</summary>
    [Fact]
    public void PortedGenerator_MatchesLegacyExport_ForUnmappedBrand()
    {
        var expected = Legacy(2, 1.0m, "en", false, MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false);
        var actual = Ported(2, 1.0m, "en", false, MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false);

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
        Assert.Contains("LR1Z", actual);
    }

    /// <summary>
    /// The port preserves the export's fail-loud behaviour on a missing (brand, primary rate) mapping
    /// (open item O1 — left as-is by decision). <see cref="KeyNotFoundException"/> in both, from the
    /// dictionary indexer.
    /// </summary>
    [Fact]
    public void PortedGenerator_ThrowsLikeLegacy_WhenLabourRateMappingMissing()
    {
        var request = MenuGenerationRequestFixture.Build(includeLabourRateMapping: false);

        Assert.Throws<KeyNotFoundException>(() =>
            MenuCodeGenerator.Generate(request, new MenuGenerationConfig { CountryID = 2, Language = "en" }).ToList());
    }

    /// <summary>
    /// A variant flagged as having standalone items but with none that qualify must NOT throw on a
    /// missing labour-rate mapping — the export only performs that lookup once it has a line to emit.
    /// Regression guard: hoisting the lookup out of the loops (an obvious "optimisation") breaks this.
    /// </summary>
    [Fact]
    public void PortedGenerator_DoesNotResolveLabourRate_WhenNoLinesAreProduced()
    {
        var request = MenuGenerationRequestFixture.Build(includeLabourRateMapping: false);
        var variant = request.Variants[0];

        variant.Periods.Clear();                 // no periodic lines
        foreach (var item in variant.Items)      // no standalone-qualifying parts
            item.Parts.Clear();

        var lines = MenuCodeGenerator.Generate(request, new MenuGenerationConfig { CountryID = 2, Language = "en" }).ToList();

        Assert.Empty(lines);
    }

    /// <summary>
    /// <see cref="GeneratedMenuLine.LineKey"/> identifies a line independently of language, so the same
    /// line can be correlated across language runs. Codes cannot be used for that — they differ.
    /// </summary>
    [Fact]
    public void LineKeys_AreUniqueAndLanguageInvariant()
    {
        var request = MenuGenerationRequestFixture.Build();

        var english = MenuCodeGenerator.Generate(request, new MenuGenerationConfig { CountryID = 2, Language = "en" }).ToList();
        var arabic = MenuCodeGenerator.Generate(request, new MenuGenerationConfig { CountryID = 2, Language = "ar" }).ToList();

        Assert.Equal(english.Select(x => x.LineKey), arabic.Select(x => x.LineKey));
        Assert.Equal(english.Count, english.Select(x => x.LineKey).Distinct().Count());

        Assert.Equal(
            ["P|4471|501", "P|4471|502", "S|4471|900", "G|4471|800"],
            english.Select(x => x.LineKey));

        // ...while the codes really do vary by language, which is why LineKey exists.
        Assert.NotEqual(english.Select(x => x.Code), arabic.Select(x => x.Code));
    }

    /// <summary>
    /// The labour-rate mapping is keyed by decimal VALUE, so 12.5 and 12.50 are the same key. Pinned
    /// because a string-formatted key (an obvious simplification) would silently miss the mapping and
    /// produce a different labour code from the export's.
    /// </summary>
    [Fact]
    public void LabourRateKey_IgnoresDecimalScale()
    {
        Assert.Equal(new MenuGenerationLabourRateKey(101, 12.5m), new MenuGenerationLabourRateKey(101, 12.50m));
        Assert.Equal(new MenuGenerationLabourRateKey(101, 12.5m).GetHashCode(), new MenuGenerationLabourRateKey(101, 12.50m).GetHashCode());

        var request = MenuGenerationRequestFixture.Build();
        request.Reference.LabourRateCodes = new Dictionary<MenuGenerationLabourRateKey, string>
        {
            // Authored with a different scale than the variant's 12.50 — must still resolve.
            [new MenuGenerationLabourRateKey(MenuGraphFixture.BrandId, 12.5m)] = "LR1",
        };

        var lines = MenuCodeGenerator.Generate(request, new MenuGenerationConfig { CountryID = 2, Language = "en" }).ToList();

        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Contains("LR1", line.LabourCode));
    }

    /// <summary>Service-interval metadata is carried through for the lookup's benefit.</summary>
    [Fact]
    public void PeriodicLines_CarryServiceIntervalMetadata()
    {
        var lines = MenuCodeGenerator.Generate(
            MenuGenerationRequestFixture.Build(),
            new MenuGenerationConfig { CountryID = 2, Language = "en" }).ToList();

        var periodic = lines.Where(x => !x.IsStandalone).ToList();
        Assert.Equal(["S01", "S02"], periodic.Select(x => x.ServiceIntervalCode));
        Assert.Equal([10000, 20000], periodic.Select(x => x.ServiceIntervalValueInMeter));

        Assert.All(lines.Where(x => x.IsStandalone), line =>
        {
            Assert.Null(line.ServiceIntervalCode);
            Assert.Null(line.ServiceIntervalValueInMeter);
        });
    }

    [Fact]
    public void Generate_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => MenuCodeGenerator.Generate(null!, new MenuGenerationConfig()).ToList());
        Assert.Throws<ArgumentNullException>(() => MenuCodeGenerator.Generate(new MenuGenerationRequest(), null!).ToList());
    }
}
