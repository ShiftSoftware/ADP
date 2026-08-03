// CS8714: Dictionary<long?, BrandMapping> mirrors the production aggregator signature, whose nullable
// BrandID key matches the brand-mapping lookup. Not ours to change here.
#pragma warning disable CS8714

using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 2 — the DMS export's EF → generic adapter.
///
/// After Phase 2 the export no longer folds anything itself: it aggregates its EF graph into a
/// <see cref="MenuGenerationRequest"/> and hands that to the shared <see cref="MenuCodeGenerator"/>.
/// The Phase 0 golden snapshots already run through this adapter, so they prove the export's output did
/// not change. What they cannot show is WHY, or which parts of the EF graph the adapter is allowed to
/// read — that is what these tests pin.
///
/// The headline test is <see cref="AggregatedEfGraph_GeneratesIdenticallyTo_HandBuiltRequest"/>: the
/// adapter's output must generate exactly what the independently hand-authored
/// <see cref="MenuGenerationRequestFixture"/> generates. Both fixtures describe the same logical menu,
/// so a disagreement means the adapter mis-reads the EF graph.
/// </summary>
public class EfToGenerationAggregatorTests
{
    private static MenuGenerationRequest Aggregate(MenuGraphFixture.Fixture fixture) =>
        EfToGenerationAggregator.Build(fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings);

    private static MenuGenerationConfig Config(long countryId = 2, decimal transferRate = 1.0m, string? language = "en", bool usePrimaryLabourRate = false) =>
        new()
        {
            CountryID = countryId,
            TransferRate = transferRate,
            Language = language,
            UsePrimaryLabourRate = usePrimaryLabourRate,
            IncludePartCost = true,
        };

    /// <summary>
    /// The adapter and the hand-built generic fixture must be interchangeable inputs to the generator,
    /// across the same configuration matrix the Phase 1 differential tests use.
    /// </summary>
    [Theory]
    // country, transferRate, language, usePrimaryLabourRate
    [InlineData(2, 1.0, "en", false)]
    [InlineData(0, 2.5, "ar", true)]
    [InlineData(3, 1.0, "en", false)]
    [InlineData(2, 1.0, null, false)]
    [InlineData(2, 3.3333, "en", false)]
    [InlineData(2, 1.0, "en", true)]
    [InlineData(9, 1.0, "en", false)]
    public void AggregatedEfGraph_GeneratesIdenticallyTo_HandBuiltRequest(double countryId, double transferRate, string? language, bool usePrimaryLabourRate)
    {
        var config = Config((long)countryId, (decimal)transferRate, language, usePrimaryLabourRate);

        var fromEntities = MenuLineFormatter.FormatCore(
            MenuCodeGenerator.Generate(Aggregate(MenuGraphFixture.Build()), config));

        var fromHandBuilt = MenuLineFormatter.FormatCore(
            MenuCodeGenerator.Generate(MenuGenerationRequestFixture.Build(), config));

        Assert.Equal(fromHandBuilt, fromEntities, ignoreLineEndingDifferences: true);
    }

    /// <summary>The "Z" brand-abbreviation fallback survives the adapter too.</summary>
    [Fact]
    public void AggregatedEfGraph_GeneratesIdenticallyTo_HandBuiltRequest_ForUnmappedBrand()
    {
        var fromEntities = MenuLineFormatter.FormatCore(MenuCodeGenerator.Generate(
            Aggregate(MenuGraphFixture.Build(MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false)), Config()));

        var fromHandBuilt = MenuLineFormatter.FormatCore(MenuCodeGenerator.Generate(
            MenuGenerationRequestFixture.Build(MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false), Config()));

        Assert.Equal(fromHandBuilt, fromEntities, ignoreLineEndingDifferences: true);
        Assert.Contains("LR1Z", fromEntities);
    }

    /// <summary>
    /// A group's interval membership comes ONLY from <see cref="ServiceIntervalGroup.ServiceIntervals"/>
    /// — never inferred from <see cref="ServiceInterval.ServiceIntervalGroupID"/>.
    ///
    /// That navigation is the only thing the original fold consulted
    /// (<c>labourDetail.ServiceIntervalGroup.ServiceIntervals.Any(...)</c>), so the adapter must not be
    /// more generous. The graph below is deliberately inconsistent: interval 502 points AT group 10 by
    /// foreign key while being absent from the group's collection. The export emitted no line for it, so
    /// neither may the adapter — inferring membership from the foreign key would resurrect that line and
    /// quietly issue a menu code the DMS never received.
    ///
    /// This needs its own minimal graph rather than <see cref="MenuGraphFixture"/>: group 10 there is
    /// also reachable through a replacement item, and that second <c>AddGroup</c> would overwrite (and
    /// so mask) any foreign-key inference, leaving the test unable to fail.
    /// </summary>
    [Fact]
    public void GroupMembership_ComesFromTheServiceIntervalsNavigation_NotTheForeignKey()
    {
        var fixture = ForeignKeyOnlyMembershipGraph();

        var request = Aggregate(fixture);

        // The reference data itself must not know about 502...
        Assert.Equal([501], request.Reference.Groups[10].ServiceIntervalIDs);

        // ...so no line is generated for it, even though the variant is periodically available for it.
        var lines = MenuCodeGenerator.Generate(request, Config()).ToList();
        Assert.Equal(["S01"], lines.Select(line => line.ServiceIntervalCode));
    }

    /// <summary>
    /// One variant, two periodic availabilities, one interval group whose <c>ServiceIntervals</c>
    /// contains only the FIRST of them — and no replacement items, so the group is written to the
    /// reference data exactly once. See the test above for why that matters.
    /// </summary>
    private static MenuGraphFixture.Fixture ForeignKeyOnlyMembershipGraph()
    {
        var group = new ServiceIntervalGroup(10) { Name = "Group 10", LabourCode = "GRPA", LabourDescription = "Group 10 labour" };

        var member = new ServiceInterval(501)
        {
            Code = "S01",
            FullName = "Interval 501",
            ValueInMeter = 10000,
            Description = "Member of the group",
            ServiceIntervalGroupID = group.ID,
            ServiceIntervalGroup = group,
        };

        // Same foreign key, same navigation — but deliberately NOT in group.ServiceIntervals.
        var nonMember = new ServiceInterval(502)
        {
            Code = "S02",
            FullName = "Interval 502",
            ValueInMeter = 20000,
            Description = "Points at the group by FK only",
            ServiceIntervalGroupID = group.ID,
            ServiceIntervalGroup = group,
        };

        group.ServiceIntervals = new List<ServiceInterval> { member };

        var vehicleModel = new VehicleModel { Name = "Model One", BrandID = MenuGraphFixture.BrandId, LabourRate = MenuGraphFixture.PrimaryLabourRate };
        vehicleModel.ID = 300;

        var menu = new Data.Entities.Menu
        {
            BasicModelCode = MenuGraphFixture.BasicModelCode,
            BrandID = MenuGraphFixture.BrandId,
            VehicleModelID = vehicleModel.ID,
            VehicleModel = vehicleModel,
        };
        menu.ID = 200;

        var variant = new MenuVariant
        {
            MenuID = menu.ID,
            Menu = menu,
            Name = "Variant A",
            MenuPrefix = "MEN",
            MenuPostfix = "PX",
            LabourRate = MenuGraphFixture.PrimaryLabourRate,
            HasStandaloneItems = false,
        };
        variant.ID = 4471;

        variant.LabourDetails = new List<MenuLabourDetails>
        {
            new()
            {
                MenuVariantID = variant.ID,
                MenuVariant = variant,
                ServiceIntervalGroupID = group.ID,
                ServiceIntervalGroup = group,
                AllowedTime = 0.50m,
                Consumable = 4.00m,
            },
        };
        variant.LabourDetails.First().ID = 44721;

        variant.PeriodicAvailabilities = new List<MenuPeriodicAvailability>
        {
            Availability(44731, variant, member),
            Availability(44732, variant, nonMember),
        };

        variant.Items = new List<MenuItem>();
        menu.Variants = new List<MenuVariant> { variant };

        return new MenuGraphFixture.Fixture
        {
            Variants = [variant],
            LabourRateMappings = new Dictionary<CompositeKey<long?, decimal>, LabourRateMapping>
            {
                [new CompositeKey<long?, decimal>(MenuGraphFixture.BrandId, MenuGraphFixture.PrimaryLabourRate)] =
                    new LabourRateMapping(1) { BrandID = MenuGraphFixture.BrandId, LabourRate = MenuGraphFixture.PrimaryLabourRate, Code = "LR1" },
            },
            BrandMappings = new Dictionary<long?, BrandMapping>
            {
                [MenuGraphFixture.BrandId] = new BrandMapping(1) { BrandID = MenuGraphFixture.BrandId, Code = "BC1", BrandAbbreviation = "A" },
            },
        };
    }

    private static MenuPeriodicAvailability Availability(long id, MenuVariant variant, ServiceInterval interval)
    {
        var availability = new MenuPeriodicAvailability
        {
            MenuVariantID = variant.ID,
            MenuVariant = variant,
            ServiceIntervalID = interval.ID,
            ServiceInterval = interval,
        };
        availability.ID = id;
        return availability;
    }

    /// <summary>
    /// Soft-deleted rows are filtered out BY THE ADAPTER, so the generation request holds live rows only
    /// and the generator never reasons about deletion (see <see cref="MenuGenerationRequest"/>).
    ///
    /// The rows must therefore be ABSENT from the request itself — not merely absent from the generated
    /// lines, which a generator-side filter would also achieve. Asserting on the request is what pins
    /// WHERE the rule lives.
    /// </summary>
    [Fact]
    public void SoftDeletedRows_AreFilteredOutByTheAdapter_NotLeftToTheGenerator()
    {
        var request = Aggregate(MenuGraphFixture.Build());
        var variant = request.Variants.Single();

        // 903 soft-deleted, 904 behind a soft-deleted link, 905 has no replacement item at all.
        Assert.Equal([900, 901, 902], variant.Items.Select(item => item.MenuItemID));

        Assert.Equal([2, 3], variant.CountryLabourRates.Select(rate => rate.CountryID));

        var parts = variant.Items.SelectMany(item => item.Parts).ToList();
        Assert.DoesNotContain(parts, part => part.PartNumber == "PN-0003");        // soft-deleted part
        Assert.Equal(2, parts.Single(part => part.PartNumber == "PN-0001").CountryPrices.Count);   // one price soft-deleted

        var partNumbers = MenuCodeGenerator.Generate(request, Config())
            .SelectMany(line => line.Parts)
            .Select(part => part.PartNumber)
            .ToList();

        Assert.DoesNotContain("PN-0003", partNumbers);   // soft-deleted part
        Assert.DoesNotContain("PN-0031", partNumbers);   // part of a soft-deleted menu item
        Assert.DoesNotContain("PN-0041", partNumbers);   // part behind a soft-deleted replacement-item link
        Assert.DoesNotContain("PN-0051", partNumbers);   // part of an item with no replacement-item link
    }

    /// <summary>
    /// Collection order is preserved end to end. It is observable behaviour: the periodic pass takes the
    /// FIRST matching labour detail and a grouped standalone line takes its allowed time from the FIRST
    /// item in the group, so the order the adapter emits decides which row wins (open item O8).
    /// </summary>
    [Fact]
    public void CollectionOrder_IsPreserved()
    {
        var variant = Aggregate(MenuGraphFixture.Build()).Variants.Single();

        Assert.Equal([501, 502, 503], variant.Periods.Select(x => x.ServiceIntervalID));
        Assert.Equal([900, 901, 902], variant.Items.Select(x => x.MenuItemID));
        Assert.Equal(
            ["PN-0001", "PN-0002", "PN-0004"],
            variant.Items.Single(x => x.MenuItemID == 900).Parts.Select(x => x.PartNumber));
    }

    /// <summary>
    /// The labour-rate mapping's composite key survives the hop into the generic request with its
    /// decimal-value semantics intact (12.5 and 12.50 are the same key) — see
    /// <c>MenuCodeGeneratorPortTests.LabourRateKey_IgnoresDecimalScale</c> for why a string key would not do.
    /// </summary>
    [Fact]
    public void LabourRateMappingKeys_SurviveTheHop()
    {
        var request = EfToGenerationAggregator.Build(
            [],
            new Dictionary<CompositeKey<long?, decimal>, LabourRateMapping>
            {
                [new CompositeKey<long?, decimal>(101, 12.5m)] = new LabourRateMapping(1) { BrandID = 101, LabourRate = 12.5m, Code = "LR1" },
            },
            new Dictionary<long?, BrandMapping>
            {
                [101] = new BrandMapping(1) { BrandID = 101, Code = "BC1", BrandAbbreviation = "A" },
            });

        Assert.Equal("LR1", request.Reference.LabourRateCodes[new MenuGenerationLabourRateKey(101, 12.50m)]);
        Assert.Equal("BC1", request.Reference.BrandMappings[101].Code);
        Assert.Equal("A", request.Reference.BrandMappings[101].Abbreviation);
    }

    /// <summary>
    /// The export's adapter carries the variant's free-of-charge flag too. It has no use for it — the DMS
    /// export neither filters nor prices on it — but the two adapters feed ONE request type, and a field
    /// only one of them fills is a field that disagrees the moment anything reads it from both.
    /// </summary>
    [Fact]
    public void TheFreeFlag_ReachesTheGenerationRequest()
    {
        var fixture = MenuGraphFixture.Build();
        var variant = fixture.Variants.Single();
        variant.IsFree = true;

        var request = EfToGenerationAggregator.Build(fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings);

        Assert.True(request.Variants.Single().IsFree);

        variant.IsFree = false;
        Assert.False(EfToGenerationAggregator.Build(fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings)
            .Variants.Single().IsFree);
    }

    [Fact]
    public void Build_RejectsNullArguments()
    {
        var labourRates = new Dictionary<CompositeKey<long?, decimal>, LabourRateMapping>();
        var brands = new Dictionary<long?, BrandMapping>();

        Assert.Throws<ArgumentNullException>(() => EfToGenerationAggregator.Build(null!, labourRates, brands));
        Assert.Throws<ArgumentNullException>(() => EfToGenerationAggregator.Build([], null!, brands));
        Assert.Throws<ArgumentNullException>(() => EfToGenerationAggregator.Build([], labourRates, null!));
    }
}
