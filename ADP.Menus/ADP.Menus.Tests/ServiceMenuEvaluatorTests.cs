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
    /// With no resolver the request's transfer rate applies and per-country labour rates are used; with
    /// one, the host is the authority — which is how a single-country deployment reproduces its own
    /// export's "primary labour rate, transfer rate 1" normalisation.
    /// </summary>
    [Fact]
    public async Task CountrySettingsResolver_OverridesTheRequest()
    {
        var withoutResolver = await Evaluator()
            .ResolveConfigAsync(new ServiceMenuLookupRequest { TransferRate = 2.5m });

        Assert.Equal(2.5m, withoutResolver.TransferRate);
        Assert.False(withoutResolver.UsePrimaryLabourRate);

        var options = new ServiceMenuLookupOptions
        {
            CountrySettingsResolver = _ =>
                new ValueTask<ServiceMenuCountrySettings>(new ServiceMenuCountrySettings { TransferRate = 1m, UsePrimaryLabourRate = true }),
        };

        var withResolver = await Evaluator(options)
            .ResolveConfigAsync(new ServiceMenuLookupRequest { TransferRate = 2.5m });

        Assert.Equal(1m, withResolver.TransferRate);
        Assert.True(withResolver.UsePrimaryLabourRate);
    }

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
    /// Every live variant of the model is returned — there is no variant filter, because nothing outside
    /// the menus database holds a variant id. Pinned so a filter is not quietly reintroduced as a
    /// convenience: it would be a parameter no caller could populate.
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
