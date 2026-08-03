using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 5 — the vehicle lookup's own evaluators: which variants participate, how a line is generated,
/// how it is priced, and how the result is shaped for a reader.
///
/// The generation-parity rules are NOT retested here (they belong to
/// <see cref="CosmosToGenerationAggregatorTests"/>). What is tested here is everything the lookup adds
/// on top — and the two things it must never do: charge different money than the export, or let dealer
/// cost reach the result.
/// </summary>
public class ServiceMenuEvaluatorTests
{
    private static ServiceMenuDocuments Documents() => MenuCosmosDocumentFixture.Build();

    /// <summary>
    /// The evaluator under its own options, exactly as DI builds it — no service provider, no general
    /// <c>LookupOptions</c>.
    /// </summary>
    private static ServiceMenuGenerationEvaluator Evaluator(ServiceMenuLookupOptions options = null) =>
        new(Options.Create(options ?? new ServiceMenuLookupOptions()));

    private static List<GeneratedMenuLine> Lines(bool includePartCost = false, long countryId = 2, decimal transferRate = 1m) =>
        MenuCodeGenerator.Generate(
            CosmosToGenerationAggregator.Build(Documents()),
            new MenuGenerationConfig
            {
                CountryID = countryId,
                TransferRate = transferRate,
                Language = "en",
                IncludePartCost = includePartCost,
            })
        .ToList();

    // ---- pricing ------------------------------------------------------------------------------------

    /// <summary>
    /// The money on a lookup line must be the money on the export's line. Both are computed from the
    /// same generated line, by two implementations that live in different assemblies — so this is the
    /// test that stops them drifting when someone "tidies" one of them.
    /// </summary>
    [Theory]
    [InlineData(2, 1.0)]
    [InlineData(3, 1.0)]
    [InlineData(0, 2.5)]
    [InlineData(9, 1.0)]     // country with no price rows at all
    public void Pricing_MatchesTheExportsOwnArithmetic(double countryId, double transferRate)
    {
        foreach (var line in Lines(includePartCost: true, (long)countryId, (decimal)transferRate))
        {
            var priced = ServiceMenuPricingEvaluator.Evaluate(line);

            // The export's figures, through the report layer's extension members.
            var reportLine = new MenuLineDTO
            {
                LabourRate = line.LabourRate,
                AllowedTime = line.AllowedTime,
                Consumable = line.Consumable,
                DiscountPercentage = line.DiscountPercentage,
                Parts = line.Parts
                    .Select(part => new MenuLinePartDTO
                    {
                        PartNumber = part.PartNumber,
                        Quantity = part.Quantity,
                        Cost = part.Cost.GetValueOrDefault(),
                        Price = part.Price,
                    })
                    .ToList(),
            };

            Assert.Equal(reportLine.LabourPrice, priced.LabourPrice);
            Assert.Equal(reportLine.LabourTotalPrice, priced.LabourTotalPrice);
            Assert.Equal(reportLine.PartsPrice, priced.PartsTotalPrice);
            Assert.Equal(reportLine.MenuTotalPrice, priced.TotalPrice);
        }
    }

    /// <summary>The discount amount is derived back out of the total, so the two always reconcile.</summary>
    [Fact]
    public void DiscountAmount_ReconcilesWithTheTotal()
    {
        foreach (var priced in ServiceMenuPricingEvaluator.Evaluate(Lines()))
        {
            Assert.Equal(
                priced.LabourTotalPrice + priced.PartsTotalPrice,
                priced.TotalPrice + priced.DiscountAmount);
        }
    }

    [Fact]
    public void NoDiscount_LeavesTheTotalAlone()
    {
        var priced = ServiceMenuPricingEvaluator.Evaluate(new GeneratedMenuLine
        {
            LabourRate = 20m,
            AllowedTime = 0.5m,
            Consumable = 4m,
            DiscountPercentage = null,
            Parts = [new GeneratedMenuPart { PartNumber = "PN", Quantity = 2m, Price = 7.25m, HasCountryPrice = true }],
        });

        Assert.Equal(10m, priced.LabourPrice);
        Assert.Equal(14m, priced.LabourTotalPrice);
        Assert.Equal(14.50m, priced.PartsTotalPrice);
        Assert.Equal(0m, priced.DiscountAmount);
        Assert.Equal(28.50m, priced.TotalPrice);
        Assert.False(priced.HasUnpricedParts);
    }

    /// <summary>
    /// A part with no price row for the country is priced 0 by fallback rather than skipped, so the
    /// total is understated. That has to be visible, or a caller quotes a customer a price that is
    /// missing a part.
    /// </summary>
    [Fact]
    public void UnpricedParts_AreFlagged_NotHidden()
    {
        // Country 9 has no price rows anywhere in the fixture.
        var priced = ServiceMenuPricingEvaluator.Evaluate(Lines(countryId: 9));

        Assert.NotEmpty(priced);
        Assert.All(priced.Where(line => line.Parts.Count > 0), line =>
        {
            Assert.True(line.HasUnpricedParts);
            Assert.All(line.Parts, part =>
            {
                Assert.False(part.HasCountryPrice);
                Assert.Equal(0m, part.UnitPrice);
            });
        });
    }

    [Fact]
    public void PartTotals_AreUnitPriceTimesQuantity()
    {
        foreach (var part in ServiceMenuPricingEvaluator.Evaluate(Lines()).SelectMany(line => line.Parts))
            Assert.Equal(part.UnitPrice * part.Quantity, part.TotalPrice);
    }

    // ---- dealer cost --------------------------------------------------------------------------------

    /// <summary>
    /// Dealer cost must not reach the lookup. It is held off at the generator, not stripped afterwards,
    /// so there is nothing on the DTO to leak in the first place — this pins both halves: the evaluator
    /// never asks for cost, and no lookup DTO type has anywhere to put it.
    /// </summary>
    [Fact]
    public async Task DealerCost_NeverReachesTheLookup()
    {
        var evaluator = Evaluator();
        var config = await evaluator.ResolveConfigAsync(new ServiceMenuLookupRequest { BasicModelCode = MenuGraphFixture.BasicModelCode, CountryID = 2 });

        Assert.False(config.IncludePartCost);

        var lines = evaluator.Evaluate(Documents(), config);

        Assert.NotEmpty(lines);
        Assert.All(lines.SelectMany(line => line.Parts), part =>
        {
            Assert.Null(part.Cost);
            Assert.Null(part.TotalCost);
        });

        // ...and the DTO has no home for it even if a future change did request it.
        var partProperties = typeof(ServiceMenuPartDTO).GetProperties().Select(x => x.Name).ToList();
        Assert.DoesNotContain(partProperties, name => name.Contains("Cost", StringComparison.OrdinalIgnoreCase));

        var lineProperties = typeof(ServiceMenuLineDTO).GetProperties().Select(x => x.Name).ToList();
        Assert.DoesNotContain(lineProperties, name =>
            name.Contains("Cost", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Profit", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Margin", StringComparison.OrdinalIgnoreCase));
    }

    // ---- config resolution (open item O6) ------------------------------------------------------------

    [Fact]
    public async Task Config_FallsBackFromRequestToOptionsToZero()
    {
        var withRequest = await Evaluator(new ServiceMenuLookupOptions { DefaultCountryID = 5 })
            .ResolveConfigAsync(new ServiceMenuLookupRequest { CountryID = 7 });
        Assert.Equal(7, withRequest.CountryID);

        var withOptions = await Evaluator(new ServiceMenuLookupOptions { DefaultCountryID = 5 })
            .ResolveConfigAsync(new ServiceMenuLookupRequest());
        Assert.Equal(5, withOptions.CountryID);

        var withNeither = await Evaluator()
            .ResolveConfigAsync(new ServiceMenuLookupRequest());
        Assert.Equal(0, withNeither.CountryID);
    }

    /// <summary>
    /// The resolver supplies the deployment's transfer rate and owns the labour-rate mode outright — which
    /// is how a single-country deployment reproduces its own export's "primary labour rate, transfer rate 1"
    /// normalisation. A request that says nothing gets exactly that.
    /// </summary>
    [Fact]
    public async Task CountrySettingsResolver_SuppliesTheDefaults()
    {
        var withoutResolver = await Evaluator()
            .ResolveConfigAsync(new ServiceMenuLookupRequest());

        Assert.Equal(1m, withoutResolver.TransferRate);
        Assert.False(withoutResolver.UsePrimaryLabourRate);

        var withResolver = await Evaluator(SingleCountryDeployment())
            .ResolveConfigAsync(new ServiceMenuLookupRequest());

        Assert.Equal(3m, withResolver.TransferRate);
        Assert.True(withResolver.UsePrimaryLabourRate);
    }

    /// <summary>
    /// An explicitly supplied transfer rate wins over the resolver. The alternative — the caller sets a
    /// value and quietly gets a different one — is the worse failure: it surfaces only as money that does
    /// not add up. A host that wants the resolver to be the sole authority does not expose the field.
    ///
    /// <para><see cref="MenuGenerationConfig.UsePrimaryLabourRate"/> is NOT overridable and stays the
    /// resolver's, because the request has no way to express it — asserted here so the two halves are not
    /// quietly merged into one rule later.</para>
    /// </summary>
    [Fact]
    public async Task AnExplicitTransferRate_WinsOverTheResolver()
    {
        var config = await Evaluator(SingleCountryDeployment())
            .ResolveConfigAsync(new ServiceMenuLookupRequest { TransferRate = 2.5m });

        Assert.Equal(2.5m, config.TransferRate);
        Assert.True(config.UsePrimaryLabourRate);
    }

    private static ServiceMenuLookupOptions SingleCountryDeployment() => new()
    {
        CountrySettingsResolver = _ =>
            new ValueTask<ServiceMenuCountrySettings>(new ServiceMenuCountrySettings { TransferRate = 3m, UsePrimaryLabourRate = true }),
    };

    [Fact]
    public async Task DefaultTransferRate_IsOne()
    {
        var config = await Evaluator()
            .ResolveConfigAsync(new ServiceMenuLookupRequest());

        Assert.Equal(1m, config.TransferRate);
    }

    // ---- generation evaluator ------------------------------------------------------------------------

    /// <summary>
    /// The generator's <see cref="KeyNotFoundException"/> on missing reference data is deliberate
    /// (open item O1) and is kept — but wrapped, so the failure names the model instead of arriving as
    /// a bare dictionary miss from inside a fold.
    /// </summary>
    [Fact]
    public void MissingReferenceData_ThrowsANamedException()
    {
        var documents = MenuCosmosDocumentFixture.Build(includeLabourRateMapping: false);
        var evaluator = Evaluator();

        var exception = Assert.Throws<ServiceMenuGenerationException>(() =>
            evaluator.Evaluate(documents, new MenuGenerationConfig { CountryID = 2, Language = "en" }));

        Assert.Equal(MenuGraphFixture.BasicModelCode, exception.BasicModelCode);
        Assert.IsType<KeyNotFoundException>(exception.InnerException);
    }

    [Fact]
    public void EmptyDocuments_GenerateNothing()
    {
        var evaluator = Evaluator();

        Assert.Empty(evaluator.Evaluate(new ServiceMenuDocuments(), new MenuGenerationConfig()));
        Assert.Empty(evaluator.Evaluate(null!, new MenuGenerationConfig()));
    }

    /// <summary>
    /// Every live variant of the model is returned — there is no variant filter BY ID, because nothing
    /// outside the menus database holds a variant id. Pinned so one is not quietly introduced as a
    /// convenience: it would be a parameter no caller could populate.
    ///
    /// <para><see cref="ServiceMenuLookupRequest.FreeFilter"/> is not that, and is deliberately allowed:
    /// it is a RULE a caller can express about which variants it wants, which is what the request's own
    /// remarks reserve room for. The assertion below still holds — it bans "Variant" in a property name,
    /// i.e. the id list, not the rule.</para>
    /// </summary>
    [Fact]
    public void EveryVariantOfTheModel_IsReturned()
    {
        var evaluator = Evaluator();
        var documents = Documents();

        var variantIDs = evaluator.Evaluate(documents, new MenuGenerationConfig { CountryID = 2, Language = "en" })
            .Select(line => line.VariantID)
            .Distinct()
            .ToList();

        Assert.Equal(
            documents.Variants.Select(variant => variant.VariantID).OrderBy(id => id),
            variantIDs.OrderBy(id => id));

        Assert.DoesNotContain(
            typeof(ServiceMenuLookupRequest).GetProperties(),
            property => property.Name.Contains("Variant", StringComparison.OrdinalIgnoreCase));
    }

    // ---- the free-of-charge flag and its filter -------------------------------------------------------

    private static List<long> VariantIDs(ServiceMenuFreeFilter filter) =>
        Evaluator()
            .Evaluate(MenuCosmosDocumentFixture.WithFreeAndPaidVariants(), new MenuGenerationConfig { CountryID = 2, Language = "en" }, filter)
            .Select(line => line.VariantID)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

    /// <summary>
    /// The filter selects VARIANTS, and a variant it excludes contributes nothing — not its scheduled
    /// services and not its standalone ones.
    /// </summary>
    [Theory]
    [InlineData(ServiceMenuFreeFilter.All, new[] { MenuCosmosDocumentFixture.PaidVariantID, MenuCosmosDocumentFixture.FreeVariantID })]
    [InlineData(ServiceMenuFreeFilter.FreeOnly, new[] { MenuCosmosDocumentFixture.FreeVariantID })]
    [InlineData(ServiceMenuFreeFilter.PaidOnly, new[] { MenuCosmosDocumentFixture.PaidVariantID })]
    public void FreeFilter_SelectsVariantsByTheirFlag(ServiceMenuFreeFilter filter, long[] expected)
    {
        Assert.Equal(expected.OrderBy(id => id), VariantIDs(filter));
    }

    /// <summary>
    /// The default is <see cref="ServiceMenuFreeFilter.All"/>, so a caller that never heard of the option
    /// — and every call written before it existed — keeps returning every variant.
    /// </summary>
    [Fact]
    public void NoFilter_MeansAll()
    {
        Assert.Equal(ServiceMenuFreeFilter.All, new ServiceMenuLookupRequest().FreeFilter);

        var withoutArgument = Evaluator()
            .Evaluate(MenuCosmosDocumentFixture.WithFreeAndPaidVariants(), new MenuGenerationConfig { CountryID = 2, Language = "en" })
            .Select(line => line.VariantID)
            .Distinct()
            .OrderBy(id => id);

        Assert.Equal(VariantIDs(ServiceMenuFreeFilter.All), withoutArgument);
    }

    /// <summary>
    /// A filter that matches nothing generates nothing. That is a legitimate answer, not a fault — and
    /// the caller, holding the filter, is the one that can tell it apart from a model with no menu.
    /// </summary>
    [Fact]
    public void AFilterMatchingNoVariant_GeneratesNothing()
    {
        // The single-variant fixture is not free, so FreeOnly excludes the only variant there is.
        var lines = Evaluator().Evaluate(
            Documents(),
            new MenuGenerationConfig { CountryID = 2, Language = "en" },
            ServiceMenuFreeFilter.FreeOnly);

        Assert.Empty(lines);
    }

    /// <summary>
    /// The flag is carried, never computed on. The two fixture variants differ ONLY in their id and the
    /// flag, so every generated code and every figure must be identical between them — if the flag ever
    /// starts zeroing a total, this is what fails.
    /// </summary>
    [Fact]
    public void TheFreeFlag_ChangesNoCodeAndNoMoney()
    {
        var lines = Evaluator().Evaluate(
            MenuCosmosDocumentFixture.WithFreeAndPaidVariants(),
            new MenuGenerationConfig { CountryID = 2, Language = "en" });

        var free = ServiceMenuPricingEvaluator.Evaluate(lines.Where(line => line.VariantID == MenuCosmosDocumentFixture.FreeVariantID).ToList());
        var paid = ServiceMenuPricingEvaluator.Evaluate(lines.Where(line => line.VariantID == MenuCosmosDocumentFixture.PaidVariantID).ToList());

        Assert.NotEmpty(free);
        Assert.Equal(paid.Count, free.Count);

        Assert.Equal(paid.Select(line => line.Code), free.Select(line => line.Code));
        Assert.Equal(paid.Select(line => line.LabourCode), free.Select(line => line.LabourCode));
        Assert.Equal(paid.Select(line => line.LabourTotalPrice), free.Select(line => line.LabourTotalPrice));
        Assert.Equal(paid.Select(line => line.PartsTotalPrice), free.Select(line => line.PartsTotalPrice));
        Assert.Equal(paid.Select(line => line.DiscountAmount), free.Select(line => line.DiscountAmount));
        Assert.Equal(paid.Select(line => line.TotalPrice), free.Select(line => line.TotalPrice));
    }

    /// <summary>
    /// Filtering runs BEFORE generation, so an excluded variant is never generated — which also means it
    /// cannot fail. Here the free variant is the one with the missing labour-rate mapping: asking for the
    /// paid one succeeds, and asking for everything still throws.
    /// </summary>
    [Fact]
    public void AnExcludedVariant_IsNeverGenerated_SoItCannotFail()
    {
        var documents = MenuCosmosDocumentFixture.WithFreeAndPaidVariants();

        // Move the free variant onto a primary labour rate nothing maps, and drop the mapping it embedded.
        // Nulling the embedded mapping alone would not do it: the paid variant embeds the SAME (brand, rate)
        // row, and one live copy anywhere in the partition supplies the dictionary entry for all of them.
        foreach (var variant in documents.Variants.Where(variant => variant.VariantID == MenuCosmosDocumentFixture.FreeVariantID))
        {
            variant.LabourRate = 99.99m;
            variant.LabourRateMapping = null;
        }

        var evaluator = Evaluator();
        var config = new MenuGenerationConfig { CountryID = 2, Language = "en" };

        Assert.NotEmpty(evaluator.Evaluate(documents, config, ServiceMenuFreeFilter.PaidOnly));

        Assert.Throws<ServiceMenuGenerationException>(() => evaluator.Evaluate(documents, config, ServiceMenuFreeFilter.All));
        Assert.Throws<ServiceMenuGenerationException>(() => evaluator.Evaluate(documents, config, ServiceMenuFreeFilter.FreeOnly));
    }

    // ---- schedule evaluator --------------------------------------------------------------------------

    [Fact]
    public void Schedule_GroupsByVariant_AndSplitsPeriodicFromStandalone()
    {
        var variants = ServiceMenuScheduleEvaluator.Evaluate(Lines());

        var variant = Assert.Single(variants);
        Assert.Equal(4471, variant.VariantID);
        Assert.Equal("Variant A", variant.VariantName);
        Assert.Equal("BC1", variant.BrandCode);
        Assert.Equal(10m, variant.DiscountPercentage);

        // The menus catalog's own vehicle-model name is not echoed back: the caller looked the menu up
        // BY the model, and the two names are authored in different places.
        Assert.DoesNotContain(
            typeof(ServiceMenuVariantDTO).GetProperties(),
            property => property.Name == "Model");

        Assert.Equal(2, variant.PeriodicServices.Count);
        Assert.All(variant.PeriodicServices, line => Assert.False(line.IsStandalone));

        Assert.Equal(2, variant.StandaloneServices.Count);
        Assert.All(variant.StandaloneServices, line => Assert.True(line.IsStandalone));
    }

    /// <summary>
    /// The variant's free-of-charge flag reaches the shape a caller reads, on the variant — where the
    /// nested shape keeps variant-level facts. The prices beside it are untouched by it.
    /// </summary>
    [Fact]
    public void Schedule_CarriesTheFreeFlagOntoTheVariant()
    {
        var lines = MenuCodeGenerator.Generate(
            CosmosToGenerationAggregator.Build(MenuCosmosDocumentFixture.WithFreeAndPaidVariants()),
            new MenuGenerationConfig { CountryID = 2, Language = "en" }).ToList();

        var variants = ServiceMenuScheduleEvaluator.Evaluate(lines);

        Assert.True(Assert.Single(variants, variant => variant.VariantID == MenuCosmosDocumentFixture.FreeVariantID).IsFree);
        Assert.False(Assert.Single(variants, variant => variant.VariantID == MenuCosmosDocumentFixture.PaidVariantID).IsFree);
    }

    /// <summary>Scheduled services are read along the distance axis, so that is what they sort by.</summary>
    [Fact]
    public void Schedule_OrdersPeriodicServicesByDistance()
    {
        var variant = Assert.Single(ServiceMenuScheduleEvaluator.Evaluate(Lines()));

        Assert.Equal([10000, 20000], variant.PeriodicServices.Select(line => line.ServiceIntervalValueInMeter));
        Assert.Equal(["S01", "S02"], variant.PeriodicServices.Select(line => line.ServiceIntervalCode));
    }

    /// <summary>Ungrouped standalone items before groups — the generator's own emission order.</summary>
    [Fact]
    public void Schedule_OrdersUngroupedStandaloneServicesBeforeGroups()
    {
        var variant = Assert.Single(ServiceMenuScheduleEvaluator.Evaluate(Lines()));

        Assert.Equal(
            [ServiceMenuLineType.StandaloneUngrouped, ServiceMenuLineType.StandaloneGrouped],
            variant.StandaloneServices.Select(line => line.LineType));
    }

    /// <summary>
    /// <see cref="ServiceMenuLineDTO.LineKey"/> is the language-invariant identity, so a caller can
    /// correlate the same service across two language requests. Codes cannot be used for that.
    /// </summary>
    [Fact]
    public void Schedule_CarriesLanguageInvariantLineKeys()
    {
        static List<string> KeysFor(string language)
        {
            var lines = MenuCodeGenerator.Generate(
                CosmosToGenerationAggregator.Build(Documents()),
                new MenuGenerationConfig { CountryID = 2, Language = language }).ToList();

            return ServiceMenuScheduleEvaluator.Evaluate(lines)
                .SelectMany(variant => variant.PeriodicServices.Concat(variant.StandaloneServices))
                .Select(line => line.LineKey)
                .ToList();
        }

        Assert.Equal(KeysFor("en"), KeysFor("ar"));
    }

    [Fact]
    public void Schedule_OfNoLines_IsEmpty()
    {
        Assert.Empty(ServiceMenuScheduleEvaluator.Evaluate([]));
        Assert.Empty(ServiceMenuScheduleEvaluator.Evaluate(null!));
    }
}
