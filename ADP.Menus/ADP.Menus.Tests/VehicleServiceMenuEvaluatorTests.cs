using System.Reflection;
using System.Text.Json;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Menus.Generation;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 6 — the vehicle lookup's service-menu section.
///
/// <para>Everything below the section is already covered: the menu codes by
/// <see cref="CosmosToGenerationAggregatorTests"/>, the money by <see cref="ServiceMenuEvaluatorTests"/>.
/// What is new here is a JOIN on a derived key and a promise that the join cannot fail a VIN lookup — so
/// that is what these tests are about.</para>
///
/// <para>The whole read path runs for real: the fixture documents go through the production aggregator,
/// the shared generator and the production pricing. Only the Cosmos read itself is replaced. A fake one
/// layer higher would test the flattening and skip the codes, which are the part worth testing.</para>
/// </summary>
public class VehicleServiceMenuEvaluatorTests
{
    /// <summary>Well-known local emulator endpoint/key. Never connected to — construction is lazy.</summary>
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>
    /// The real <see cref="ServiceMenuCosmosService"/> with only its single Cosmos read overridden — the
    /// seam the production class exposes for exactly this.
    /// </summary>
    private sealed class StubCosmosService : ServiceMenuCosmosService
    {
        private readonly Func<string, ServiceMenuDocuments> read;

        internal StubCosmosService(Func<string, ServiceMenuDocuments> read)
            : base(new LookUpCosmosClient(new CosmosClient(ConnectionString)))
        {
            this.read = read;
        }

        public override Task<ServiceMenuDocuments> GetMenuDocumentsAsync(string basicModelCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(read(basicModelCode));
        }
    }

    private static VehicleServiceMenuEvaluator Evaluator(
        Func<string, ServiceMenuDocuments> read,
        ServiceMenuLookupOptions? options = null) =>
        new(new ServiceMenuLookupService(
            new StubCosmosService(read),
            new ServiceMenuGenerationEvaluator(Options.Create(options ?? new ServiceMenuLookupOptions()))));

    /// <summary>The catalogue as replication would write it — only the fixture model code exists.</summary>
    private static ServiceMenuDocuments TheOnlyModel(string basicModelCode) =>
        basicModelCode == MenuGraphFixture.BasicModelCode
            ? MenuCosmosDocumentFixture.Build()
            : new ServiceMenuDocuments { BasicModelCode = basicModelCode };

    /// <summary>The same catalogue, but the fixture model has a free variant beside its paid one.</summary>
    private static ServiceMenuDocuments TheOnlyModel_WithAFreeVariant(string basicModelCode) =>
        basicModelCode == MenuGraphFixture.BasicModelCode
            ? MenuCosmosDocumentFixture.WithFreeAndPaidVariants()
            : new ServiceMenuDocuments { BasicModelCode = basicModelCode };

    private static VehicleLookupRequestOptions Request(
        string language = "en",
        long? countryId = 2,
        decimal? transferRate = null,
        ServiceMenuFreeFilter freeFilter = ServiceMenuFreeFilter.All) =>
        new()
        {
            LanguageCode = language,
            ServiceMenuOptions = new VehicleServiceMenuRequestOptions
            {
                Include = true,
                CountryID = countryId,
                TransferRate = transferRate,
                FreeFilter = freeFilter,
            },
        };

    // ---- the join ------------------------------------------------------------------------------------

    [Fact]
    public async Task AMatchingModelCode_Finds_AndCarriesTheGeneratedCodes()
    {
        var section = await Evaluator(TheOnlyModel).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request());

        Assert.Equal(VehicleServiceMenuStatus.Found, section.Status);
        Assert.Equal(MenuGraphFixture.BasicModelCode, section.BasicModelCode);
        Assert.Equal(2, section.CountryID);
        Assert.Equal("en", section.Language);
        Assert.Equal(1m, section.TransferRate);
        Assert.NotEmpty(section.Services);

        // The codes are the menu lookup's codes, which are the export's codes. Asserting them here as well
        // would restate a test that already exists; asserting they SURVIVE the flattening is the new claim.
        var nested = await new ServiceMenuLookupService(
                new StubCosmosService(TheOnlyModel),
                new ServiceMenuGenerationEvaluator(Options.Create(new ServiceMenuLookupOptions())))
            .GetMenuAsync(new ServiceMenuLookupRequest
            {
                BasicModelCode = MenuGraphFixture.BasicModelCode,
                CountryID = 2,
                Language = "en",
            });

        var expected = nested.Variants
            .SelectMany(variant => variant.PeriodicServices.Concat(variant.StandaloneServices))
            .ToList();

        Assert.Equal(expected.Select(line => line.Code), section.Services.Select(line => line.Code));
        Assert.Equal(expected.Select(line => line.LabourCode), section.Services.Select(line => line.LabourCode));
        Assert.Equal(expected.Select(line => line.LineKey), section.Services.Select(line => line.LineKey));
        Assert.Equal(expected.Select(line => line.TotalPrice), section.Services.Select(line => line.TotalPrice));
    }

    /// <summary>
    /// The flat shape must preserve the nested one's order: per variant, scheduled services in distance
    /// order, then standalone. A UI that renders the list straight through depends on it, and a
    /// <c>SelectMany</c> is exactly the kind of thing a later refactor reorders without noticing.
    /// </summary>
    [Fact]
    public async Task Flattening_PreservesVariantThenScheduleOrder()
    {
        var section = await Evaluator(TheOnlyModel).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request());

        var periodic = section.Services.Where(line => !line.IsStandalone).ToList();

        Assert.Equal([10000, 20000], periodic.Select(line => line.ServiceIntervalValueInMeter));

        // Standalone lines come after every scheduled line of the same variant.
        var firstStandalone = section.Services.FindIndex(line => line.IsStandalone);
        Assert.Equal(periodic.Count, firstStandalone);
        Assert.All(section.Services.Skip(firstStandalone), line => Assert.True(line.IsStandalone));

        // The variant travels on the line — that is what makes the flat shape usable.
        Assert.All(section.Services, line =>
        {
            Assert.Equal(4471, line.VariantID);
            Assert.Equal("Variant A", line.VariantName);
        });
    }

    /// <summary>
    /// A code with no documents is a MISS, not an absence — the derived Katashiki simply does not match an
    /// authored menu code. Counting these against <c>Found</c> is how a deployment measures open item O3.
    /// </summary>
    [Fact]
    public async Task AModelCodeWithNoDocuments_IsNotFound_AndStillEchoesTheKeyItTried()
    {
        var section = await Evaluator(TheOnlyModel).EvaluateAsync("NOPE9", Request());

        Assert.Equal(VehicleServiceMenuStatus.NotFound, section.Status);
        Assert.Equal("NOPE9", section.BasicModelCode);
        Assert.Empty(section.Services);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AVehicleWithNoKatashiki_HasNoKeyToJoinOn(string? basicModelCode)
    {
        var section = await Evaluator(_ => throw new Xunit.Sdk.XunitException("must not read Cosmos with no key"))
            .EvaluateAsync(basicModelCode!, Request());

        Assert.Equal(VehicleServiceMenuStatus.NoBasicModelCode, section.Status);
        Assert.Null(section.BasicModelCode);
        Assert.Empty(section.Services);
    }

    /// <summary>The key is trimmed before it is used, matching what the menu lookup itself does.</summary>
    [Fact]
    public async Task TheJoinKey_IsTrimmed()
    {
        var section = await Evaluator(TheOnlyModel).EvaluateAsync($"  {MenuGraphFixture.BasicModelCode}  ", Request());

        Assert.Equal(VehicleServiceMenuStatus.Found, section.Status);
        Assert.Equal(MenuGraphFixture.BasicModelCode, section.BasicModelCode);
    }

    // ---- containment: a menu fault never fails a VIN lookup -------------------------------------------

    public static TheoryData<Exception> ContainedFaults() =>
    [
        new ServiceMenuContainerNotFoundException("Services", "ServiceMenus", new Exception()),
        new ServiceMenuGenerationException(MenuGraphFixture.BasicModelCode, new KeyNotFoundException()),
        new CosmosException("throttled", System.Net.HttpStatusCode.TooManyRequests, 429, "activity", 1),
    ];

    /// <summary>
    /// The Phase-5 write-up left this open: "an unprovisioned menu container should not be able to fail a
    /// whole VIN lookup". It cannot. Each enumerated menu fault becomes a status the response carries —
    /// contained, and visible, rather than swallowed.
    /// </summary>
    [Theory]
    [MemberData(nameof(ContainedFaults))]
    public async Task AMenuFault_BecomesAStatus_NotAThrow(Exception fault)
    {
        var section = await Evaluator(_ => throw fault)
            .EvaluateAsync(MenuGraphFixture.BasicModelCode, Request());

        Assert.Equal(VehicleServiceMenuStatus.Unavailable, section.Status);
        Assert.Equal(MenuGraphFixture.BasicModelCode, section.BasicModelCode);
        Assert.Empty(section.Services);
    }

    /// <summary>
    /// Containment stops at the menu subsystem's own faults. A bug in this assembly — or in a host's
    /// <c>CountrySettingsResolver</c>, which runs inside the VIN lookup — must surface as a failure rather
    /// than as a section that is quietly "unavailable" forever.
    /// </summary>
    [Fact]
    public async Task AnUnexpectedFault_IsNotContained()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Evaluator(_ => throw new InvalidOperationException("a bug, not a menu fault"))
                .EvaluateAsync(MenuGraphFixture.BasicModelCode, Request()));
    }

    /// <summary>Cancellation is not a fault and is never dressed up as one.</summary>
    [Fact]
    public async Task Cancellation_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Evaluator(TheOnlyModel).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(), cancellation.Token));
    }

    /// <summary>
    /// A host that never registered the menu lookup gets a status naming that, not a null reference and not
    /// the provisioning story — the two have different fixes.
    /// </summary>
    [Fact]
    public async Task AnUnregisteredMenuLookup_ReportsItself()
    {
        var section = await new VehicleServiceMenuEvaluator(null!)
            .EvaluateAsync(MenuGraphFixture.BasicModelCode, Request());

        Assert.Equal(VehicleServiceMenuStatus.NotRegistered, section.Status);
        Assert.Empty(section.Services);
    }

    // ---- config --------------------------------------------------------------------------------------

    /// <summary>
    /// The section is priced for the request's country and generated in the request's language. There is no
    /// separate menu language: a vehicle lookup rendering in one language with menu codes in another would
    /// be a bug, so the request's language is simply used.
    /// </summary>
    [Fact]
    public async Task CountryAndLanguage_ComeFromTheRequest()
    {
        var arabic = await Evaluator(TheOnlyModel).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request("ar", countryId: 3));
        var english = await Evaluator(TheOnlyModel).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request("en", countryId: 3));

        Assert.Equal(3, arabic.CountryID);
        Assert.Equal("ar", arabic.Language);

        // Same services, different language: the codes move, the line keys do not.
        Assert.Equal(english.Services.Select(line => line.LineKey), arabic.Services.Select(line => line.LineKey));
        Assert.NotEqual(english.Services.Select(line => line.Code), arabic.Services.Select(line => line.Code));
    }

    /// <summary>
    /// With no country on the request, the menu lookup's own default applies — the section does not invent
    /// one, and does not reach into <c>LookupOptions</c> for it.
    /// </summary>
    [Fact]
    public async Task NoCountryOnTheRequest_FallsBackToTheMenuOptions()
    {
        var section = await Evaluator(TheOnlyModel, new ServiceMenuLookupOptions { DefaultCountryID = 3 })
            .EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(countryId: null));

        Assert.Equal(3, section.CountryID);
    }

    /// <summary>
    /// Asking for the section and nothing else is the common call: the language comes from the request, the
    /// country from the menu options' default, and the transfer rate is 1. Every other member is optional.
    /// </summary>
    [Fact]
    public async Task IncludeAlone_UsesTheDefaults()
    {
        var section = await Evaluator(TheOnlyModel, new ServiceMenuLookupOptions { DefaultCountryID = 2 })
            .EvaluateAsync(
                MenuGraphFixture.BasicModelCode,
                new VehicleLookupRequestOptions
                {
                    LanguageCode = "en",
                    ServiceMenuOptions = new VehicleServiceMenuRequestOptions { Include = true },
                });

        Assert.Equal(VehicleServiceMenuStatus.Found, section!.Status);
        Assert.Equal(2, section.CountryID);
        Assert.Equal(1m, section.TransferRate);
        Assert.NotEmpty(section.Services);
    }

    // ---- the opt-in ----------------------------------------------------------------------------------

    /// <summary>
    /// No section unless it was asked for, and no Cosmos read either. A null options object and an un-set
    /// <see cref="VehicleServiceMenuRequestOptions.Include"/> mean the same thing.
    ///
    /// <para>The third case is the one that matters: options SUPPLIED but <c>Include</c> false must stay
    /// off. "Options were provided, so they must want it" is the obvious simplification of this gate and it
    /// would silently turn the section on — and with it an extra partition read per vehicle — for every
    /// caller that set a country without asking for a menu.</para>
    /// </summary>
    [Fact]
    public async Task NoSection_UnlessTheRequestAsksForOne()
    {
        var evaluator = Evaluator(_ => throw new Xunit.Sdk.XunitException("must not read Cosmos when no menu was asked for"));

        Assert.Null(await evaluator.EvaluateAsync(MenuGraphFixture.BasicModelCode, new VehicleLookupRequestOptions()));

        Assert.Null(await evaluator.EvaluateAsync(
            MenuGraphFixture.BasicModelCode,
            new VehicleLookupRequestOptions { ServiceMenuOptions = new VehicleServiceMenuRequestOptions() }));

        Assert.Null(await evaluator.EvaluateAsync(
            MenuGraphFixture.BasicModelCode,
            new VehicleLookupRequestOptions
            {
                ServiceMenuOptions = new VehicleServiceMenuRequestOptions { CountryID = 2, TransferRate = 2.5m },
            }));

        Assert.Null(await evaluator.EvaluateAsync(MenuGraphFixture.BasicModelCode, null!));
    }

    /// <summary>
    /// The caller's transfer rate reaches the generator and actually scales the consumable — the point of
    /// exposing it. Asserting it on the LINE rather than only on the echoed
    /// <see cref="VehicleServiceMenuDTO.TransferRate"/> is what makes this a real test: echoing a number the
    /// fold never saw would pass the weaker one.
    /// </summary>
    [Fact]
    public async Task TheRequestsTransferRate_ScalesTheConsumable()
    {
        var unscaled = await Evaluator(TheOnlyModel).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(transferRate: 1m));
        var scaled = await Evaluator(TheOnlyModel).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(transferRate: 2.5m));

        Assert.Equal(1m, unscaled.TransferRate);
        Assert.Equal(2.5m, scaled.TransferRate);

        var unscaledConsumables = unscaled.Services.Where(line => !line.IsStandalone).Select(line => line.Consumable).ToList();
        var scaledConsumables = scaled.Services.Where(line => !line.IsStandalone).Select(line => line.Consumable).ToList();

        Assert.NotEmpty(unscaledConsumables);
        Assert.All(unscaledConsumables, consumable => Assert.True(consumable > 0));
        Assert.Equal(unscaledConsumables.Select(consumable => consumable * 2.5m), scaledConsumables);

        // It moves money and nothing else: the labour-rate mapping is keyed by the variant's PRIMARY rate,
        // so no menu or labour code may shift with it.
        Assert.Equal(unscaled.Services.Select(line => line.Code), scaled.Services.Select(line => line.Code));
        Assert.Equal(unscaled.Services.Select(line => line.LabourCode), scaled.Services.Select(line => line.LabourCode));
    }

    /// <summary>
    /// A transfer rate the caller supplied wins over the host's resolver. The alternative — accepting the
    /// value and quietly generating with a different one — would make the field look wired while doing
    /// nothing, visible only as money that does not add up.
    /// </summary>
    [Fact]
    public async Task TheRequestsTransferRate_WinsOverTheHostsResolver()
    {
        var options = new ServiceMenuLookupOptions
        {
            CountrySettingsResolver = _ =>
                new ValueTask<ServiceMenuCountrySettings>(new ServiceMenuCountrySettings { TransferRate = 1m, UsePrimaryLabourRate = true }),
        };

        var resolverOnly = await Evaluator(TheOnlyModel, options).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request());
        var overridden = await Evaluator(TheOnlyModel, options).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(transferRate: 2.5m));

        Assert.Equal(1m, resolverOnly.TransferRate);
        Assert.Equal(2.5m, overridden.TransferRate);
    }

    // ---- the free-of-charge flag and its filter -------------------------------------------------------

    /// <summary>
    /// The flat shape has no variant to hang a variant-level fact on, so the flag rides every line the
    /// variant produced — exactly as its name does. A caller grouping by <c>VariantID</c> gets it either
    /// way; one rendering the flat list straight through needs it here.
    /// </summary>
    [Fact]
    public async Task TheFreeFlag_TravelsOnEveryLine()
    {
        var section = await Evaluator(TheOnlyModel_WithAFreeVariant).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request());

        Assert.Equal(VehicleServiceMenuStatus.Found, section.Status);
        Assert.All(section.Services, line =>
            Assert.Equal(line.VariantID == MenuCosmosDocumentFixture.FreeVariantID, line.IsFree));

        Assert.Contains(section.Services, line => line.IsFree);
        Assert.Contains(section.Services, line => !line.IsFree);
    }

    /// <summary>The request's filter is the menu lookup's filter — the option is not quietly dropped.</summary>
    [Theory]
    [InlineData(ServiceMenuFreeFilter.All, true, true)]
    [InlineData(ServiceMenuFreeFilter.FreeOnly, true, false)]
    [InlineData(ServiceMenuFreeFilter.PaidOnly, false, true)]
    public async Task TheFreeFilter_ReachesTheMenuLookup(ServiceMenuFreeFilter filter, bool expectFree, bool expectPaid)
    {
        var section = await Evaluator(TheOnlyModel_WithAFreeVariant)
            .EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(freeFilter: filter));

        Assert.Equal(VehicleServiceMenuStatus.Found, section.Status);
        Assert.Equal(expectFree, section.Services.Any(line => line.VariantID == MenuCosmosDocumentFixture.FreeVariantID));
        Assert.Equal(expectPaid, section.Services.Any(line => line.VariantID == MenuCosmosDocumentFixture.PaidVariantID));

        // Whatever came back, every line agrees with the filter that asked for it.
        if (filter != ServiceMenuFreeFilter.All)
            Assert.All(section.Services, line => Assert.Equal(filter == ServiceMenuFreeFilter.FreeOnly, line.IsFree));
    }

    /// <summary>
    /// A filter that excludes every variant is <see cref="VehicleServiceMenuStatus.Found"/> with nothing in
    /// it — NOT <see cref="VehicleServiceMenuStatus.NotFound"/>. The model HAS a menu; this request asked
    /// for a part of it that is empty. Conflating the two would corrupt the O3 miss rate, which counts
    /// NotFound as "the derived key matched no authored menu".
    /// </summary>
    [Fact]
    public async Task AFilterThatExcludesEveryVariant_IsFoundWithNoServices()
    {
        // The single-variant fixture is not free, so FreeOnly leaves nothing.
        var section = await Evaluator(TheOnlyModel)
            .EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(freeFilter: ServiceMenuFreeFilter.FreeOnly));

        Assert.Equal(VehicleServiceMenuStatus.Found, section.Status);
        Assert.Empty(section.Services);

        // The menu lookup underneath says the same thing its own way: the partition exists.
        var nested = await new ServiceMenuLookupService(
                new StubCosmosService(TheOnlyModel),
                new ServiceMenuGenerationEvaluator(Options.Create(new ServiceMenuLookupOptions())))
            .GetMenuAsync(new ServiceMenuLookupRequest
            {
                BasicModelCode = MenuGraphFixture.BasicModelCode,
                CountryID = 2,
                Language = "en",
                FreeFilter = ServiceMenuFreeFilter.FreeOnly,
            });

        Assert.False(nested.NotFound);
        Assert.Empty(nested.Variants);
    }

    /// <summary>
    /// Filtering changes which variants come back and nothing else. A line served under a filter is the
    /// same line, code and price, that comes back unfiltered.
    /// </summary>
    [Fact]
    public async Task Filtering_ChangesNothingAboutTheLinesItKeeps()
    {
        var all = await Evaluator(TheOnlyModel_WithAFreeVariant).EvaluateAsync(MenuGraphFixture.BasicModelCode, Request());
        var freeOnly = await Evaluator(TheOnlyModel_WithAFreeVariant)
            .EvaluateAsync(MenuGraphFixture.BasicModelCode, Request(freeFilter: ServiceMenuFreeFilter.FreeOnly));

        var expected = all.Services.Where(line => line.IsFree).ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected.Select(line => line.LineKey), freeOnly.Services.Select(line => line.LineKey));
        Assert.Equal(expected.Select(line => line.Code), freeOnly.Services.Select(line => line.Code));
        Assert.Equal(expected.Select(line => line.TotalPrice), freeOnly.Services.Select(line => line.TotalPrice));
    }

    /// <summary>
    /// The default is "everything". A caller that never sets the filter — and every call written before it
    /// existed — sees what it always saw.
    /// </summary>
    [Fact]
    public void TheFreeFilter_DefaultsToAll()
    {
        Assert.Equal(ServiceMenuFreeFilter.All, new VehicleServiceMenuRequestOptions().FreeFilter);
        Assert.Equal(ServiceMenuFreeFilter.All, default(ServiceMenuFreeFilter));
    }

    // ---- the contract the web components see ---------------------------------------------------------

    /// <summary>
    /// The flat line must carry every field the nested line carries (plus the variant), and the flat part
    /// every field the nested part carries. Otherwise a field added to the menu lookup silently never
    /// reaches the vehicle lookup or the web component — which is a data loss nothing else would catch.
    /// </summary>
    [Fact]
    public void TheFlatShape_CarriesEveryFieldOfTheNestedOne()
    {
        static HashSet<string> Names(Type type) =>
            type.GetProperties().Select(property => property.Name).ToHashSet();

        Assert.Empty(Names(typeof(ServiceMenuLineDTO)).Except(Names(typeof(VehicleServiceMenuLineDTO))));
        Assert.Empty(Names(typeof(ServiceMenuPartDTO)).Except(Names(typeof(VehicleServiceMenuPartDTO))));

        // What the flat line adds is exactly the variant-level facts, which it carries ON the line because
        // it has no variant object to hang them on.
        var variantLevel = Names(typeof(VehicleServiceMenuLineDTO)).Except(Names(typeof(ServiceMenuLineDTO))).ToHashSet();

        Assert.Equal(["IsFree", "VariantID", "VariantName"], variantLevel.OrderBy(name => name));

        // ...and each of them is a field the nested shape's VARIANT really has, so this list cannot become
        // a place where the flat shape invents fields the menu lookup never returns.
        Assert.Empty(variantLevel.Except(Names(typeof(ServiceMenuVariantDTO))));

        Assert.Empty(Names(typeof(VehicleServiceMenuPartDTO)).Except(Names(typeof(ServiceMenuPartDTO))));
    }

    /// <summary>
    /// Dealer cost must not reach a public web component. The generator is never asked for it, so there is
    /// nothing to strip — but this is the type someone would copy a margin field onto, so the absence is
    /// asserted rather than assumed. Same guard the menu lookup's own DTOs carry.
    /// </summary>
    [Fact]
    public void DealerCost_NeverReachesTheVehicleSection()
    {
        foreach (var type in new[] { typeof(VehicleServiceMenuDTO), typeof(VehicleServiceMenuLineDTO), typeof(VehicleServiceMenuPartDTO) })
            Assert.DoesNotContain(
                type.GetProperties(),
                property =>
                    property.Name.Contains("Cost", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("Profit", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("Margin", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Both enums reach the wire as strings. The generated TypeScript types them as string unions, so a
    /// numeric enum here would make the generated model quietly wrong — the kind of mismatch that shows up
    /// as a switch statement that never matches.
    /// </summary>
    [Fact]
    public void TheEnumsSerializeAsStrings_MatchingTheGeneratedTypeScript()
    {
        var json = JsonSerializer.Serialize(new VehicleServiceMenuDTO
        {
            Status = VehicleServiceMenuStatus.Found,
            Services = [new VehicleServiceMenuLineDTO { LineType = ServiceMenuLineType.StandaloneGrouped }],
        });

        Assert.Contains("\"Found\"", json);
        Assert.Contains("\"StandaloneGrouped\"", json);

        // The menu lookup's own line uses the same enum and must agree on the wire form.
        Assert.Contains(
            "\"Periodic\"",
            JsonSerializer.Serialize(new ServiceMenuLineDTO { LineType = ServiceMenuLineType.Periodic }));
    }

    /// <summary>
    /// Zero must not mean "found". A section deserialized from a payload that predates the status field, or
    /// default-constructed by a mapper, would otherwise claim a menu it never looked up.
    /// </summary>
    [Fact]
    public void TheDefaultStatus_DoesNotClaimAMenu()
    {
        Assert.NotEqual(VehicleServiceMenuStatus.Found, default(VehicleServiceMenuStatus));
        Assert.NotEqual(VehicleServiceMenuStatus.Found, new VehicleServiceMenuDTO().Status);
    }

    /// <summary>
    /// The section is opt-in. An extra single-partition read and a fold PER VEHICLE is not something a bulk
    /// lookup should start paying because a package was upgraded.
    /// </summary>
    [Fact]
    public void TheSection_IsOffByDefault()
    {
        Assert.Null(new VehicleLookupRequestOptions().ServiceMenuOptions);
        Assert.Null(new VehicleLookupDTO().ServiceMenu);

        // The switch lives beside the settings it governs, and everything else stays optional — so opting in
        // is one flag and nothing else, and there is no second flag on another object to forget.
        Assert.False(new VehicleServiceMenuRequestOptions().Include);
        Assert.Null(new VehicleServiceMenuRequestOptions().CountryID);
        Assert.Null(new VehicleServiceMenuRequestOptions().TransferRate);

        Assert.DoesNotContain(
            typeof(VehicleLookupRequestOptions).GetProperties(),
            property => property.Name.Contains("ServiceMenu", StringComparison.OrdinalIgnoreCase)
                     && property.PropertyType != typeof(VehicleServiceMenuRequestOptions));
    }

    /// <summary>
    /// The join key is read off the DTO on the lookup path, so it must not throw for a vehicle whose
    /// identifiers never resolved. A null Katashiki is a miss; a null identifiers block is the same miss,
    /// not a 500.
    /// </summary>
    [Fact]
    public void TheJoinKey_IsNullSafe()
    {
        Assert.Null(new VehicleLookupDTO().BasicModelCode);
        Assert.Null(new VehicleLookupDTO { Identifiers = new VehicleIdentifiersDTO() }.BasicModelCode);

        Assert.Equal(
            "ABC12",
            new VehicleLookupDTO { Identifiers = new VehicleIdentifiersDTO { Katashiki = "ABC12-XYZ" } }.BasicModelCode);
    }

    /// <summary>
    /// The generated TypeScript emits same-directory imports, so every type reachable from
    /// <see cref="VehicleLookupDTO"/> has to live in the vehicle-lookup DTO folder. This is why the flat
    /// types exist at all instead of reusing the menu lookup's — and it fails silently, at runtime, in the
    /// browser, if someone "simplifies" it later.
    /// </summary>
    [Fact]
    public void EveryTypeScriptTypeReachableFromTheVehicleLookup_LivesBesideIt()
    {
        var vehicleLookupNamespace = typeof(VehicleLookupDTO).Namespace;

        foreach (var type in new[] { typeof(VehicleServiceMenuDTO), typeof(VehicleServiceMenuLineDTO), typeof(VehicleServiceMenuPartDTO) })
        {
            Assert.Equal(vehicleLookupNamespace, type.Namespace);
            Assert.NotNull(type.GetCustomAttributes().SingleOrDefault(a => a.GetType().Name == "TypeScriptModelAttribute"));
        }

        // Enums are inlined as string unions rather than imported, which is the one thing that may live
        // elsewhere — ServiceMenuLineType does.
        Assert.True(typeof(ServiceMenuLineType).IsEnum);
        Assert.NotEqual(vehicleLookupNamespace, typeof(ServiceMenuLineType).Namespace);
    }
}
