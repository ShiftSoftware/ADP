using System.Text.Json;
using System.Text.Json.Serialization;
using ADP.TestData.Generator;
using ADP.TestData.Generator.Anonymisation;
using NSubstitute;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;

// Resolve paths relative to the repo root. Two dev-time overrides exist so an environment can be
// tried without touching the committed fixtures: --environments=<dir> reads the environment JSON
// from there, --out=<dir> writes everything (fixtures and index.json) to that one directory instead
// of the consumers' source trees. The post-build run passes neither.
//
// Two further modes share the argument parsing and run instead of the generation:
//   --anonymise=<raw environment> --seed=<text>|--seed-file=<path> --keys=<path> [--vocabulary=<path>]
//                [--name=<env>] [--to=<dir>]   — a real estate's environment → a public one (AnonymiserCommand)
//   --verify=<forbidden list> [--scan=<path>;<path>…]   — fail on any listed string in environment / fixture
//                JSON; scans the environments and the fixture output trees when --scan is omitted.
var arguments = args
    .Select(a => a.Split('=', 2))
    .Where(a => a.Length == 2 && a[0].StartsWith("--"))
    .ToDictionary(a => a[0], a => a[1], StringComparer.Ordinal);

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var environmentsDir = arguments.TryGetValue("--environments", out var environmentsOverride)
    ? Path.GetFullPath(environmentsOverride)
    : Path.Combine(repoRoot, "ADP.TestData", "environments");
var webComponentsOutputDir = Path.Combine(repoRoot, "ADP.WebComponents", "adp-web-components", "src", "features", "mocks", "data", "generated");
var webComponentsDevDir = Path.Combine(repoRoot, "ADP.WebComponents", "adp-web-components", "www", "mocks", "generated");
var docsOutputDir = Path.Combine(repoRoot, "ADP.Docs", "Docs", "docs", "web-components", "demo-data");
// The neutral demo images the fixtures' URLs point at (DemoAssets); copied to dist/mocks/assets by the build.
var assetsDir = Path.Combine(repoRoot, "ADP.WebComponents", "adp-web-components", "src", "features", "mocks", "data", "assets");

if (arguments.ContainsKey("--anonymise") || arguments.ContainsKey("--anonymize"))
    return AnonymiserCommand.Run(arguments, repoRoot);

if (arguments.TryGetValue("--verify", out var forbiddenList))
    return ForbiddenListVerifier.Run(
        Path.GetFullPath(forbiddenList),
        arguments.TryGetValue("--scan", out var scan)
            ? scan.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Path.GetFullPath)
            : new[] { environmentsDir, webComponentsOutputDir, docsOutputDir });

Console.WriteLine($"Repo root: {repoRoot}");
Console.WriteLine($"Environments: {environmentsDir}");

var deserializeOptions = new JsonSerializerOptions();
deserializeOptions.Converters.Add(new NullableLongDictionaryConverter());
deserializeOptions.Converters.Add(new JsonStringEnumConverter());

var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
};
serializeOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

// Output directories: always the two source trees; also the dev server's www/ cache when it exists.
var outputDirs = new List<string> { webComponentsOutputDir, docsOutputDir };
if (Directory.Exists(Path.Combine(repoRoot, "ADP.WebComponents", "adp-web-components", "www")))
    outputDirs.Add(webComponentsDevDir);
if (arguments.TryGetValue("--out", out var outputOverride))
    outputDirs = new List<string> { Path.GetFullPath(outputOverride) };

// index.json: the environment list, each environment's fixture keys per file and its clock anchor,
// so the dev harness can list environments and fixtures without hand-listing either, and pin its
// "today" to the date the statuses were computed against.
var index = new GeneratedIndex();

foreach (var envFile in Directory.GetFiles(environmentsDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
{
    var envName = Path.GetFileNameWithoutExtension(envFile);
    Console.WriteLine($"\n=== Processing environment: {envName} ===");

    var json = File.ReadAllText(envFile);
    var env = JsonSerializer.Deserialize<GeneratorEnvironment>(json, deserializeOptions)
        ?? throw new InvalidOperationException($"Failed to deserialize '{envFile}'");

    // Build name lookup dictionaries from Companies/Countries/Regions
    var companyNames = env.Companies.ToDictionary(c => c.CompanyId, c => c.CompanyName);
    var branchNames = env.Companies.SelectMany(c => c.Branches).ToDictionary(b => b.BranchId, b => b.BranchName);
    var countryNames = env.Countries.ToDictionary(c => c.CountryId, c => c.CountryName);
    var regionNames = env.Regions.ToDictionary(r => r.RegionId, r => r.RegionName);

    var lookupOptions = env.LookupOptions.ToLookupOptions(companyNames, branchNames, countryNames, regionNames);
    lookupOptions.TimeProvider = FixedTimeProvider.Anchor;
    var requestOptions = env.RequestOptions.ToVehicleLookupRequestOptions();
    IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();

    // Mock IVehicleLookupStorageService
    var storageService = Substitute.For<IVehicleLookupStorageService>();

    // Wire up vehicle model lookups — by variant + brand (the default) and by katashiki (the
    // path VehicleSpecificationEvaluator takes when the request says UseKatashikiLookup).
    storageService.GetVehicleModelsAsync(Arg.Any<string>(), Arg.Any<long?>())
        .Returns(callInfo =>
        {
            var variant = callInfo.ArgAt<string>(0);
            var brand = callInfo.ArgAt<long?>(1);
            var match = env.VehicleModels.FirstOrDefault(m =>
                m.VariantCode == variant && m.BrandID == brand);
            return Task.FromResult(match);
        });

    storageService.GetVehicleModelsByKatashikiAsync(Arg.Any<string>())
        .Returns(callInfo =>
        {
            var katashiki = callInfo.ArgAt<string>(0);
            return Task.FromResult(env.VehicleModels.Where(m => m.Katashiki == katashiki).AsEnumerable());
        });

    // Colour tables: a point-read on code + brand, as the storage service does; no code or no
    // brand finds nothing. Read by the specification and by the paint-thickness certificate.
    storageService.GetExteriorColorsAsync(Arg.Any<string>(), Arg.Any<long?>())
        .Returns(callInfo => Task.FromResult(
            FindColor(env.ExteriorColors, callInfo.ArgAt<string>(0), callInfo.ArgAt<long?>(1))));

    storageService.GetInteriorColorsAsync(Arg.Any<string>(), Arg.Any<long?>())
        .Returns(callInfo => Task.FromResult(
            FindColor(env.InteriorColors, callInfo.ArgAt<string>(0), callInfo.ArgAt<long?>(1))));

    // The end customer, matched on the dealer's customer id within the owning company — the same
    // query the storage service runs. Only reached when the request asks to look the customer up.
    storageService.GetCustomerAsync(Arg.Any<string>(), Arg.Any<long?>())
        .Returns(callInfo =>
        {
            var customerID = callInfo.ArgAt<string>(0);
            var companyID = callInfo.ArgAt<long?>(1);
            var match = string.IsNullOrWhiteSpace(customerID)
                ? null
                : env.Customers.FirstOrDefault(c => c.CustomerID == customerID && c.CompanyID == companyID);
            return Task.FromResult(match!);
        });

    // Brokers, both overloads, from the environment's Brokers table. No evaluator reads these today
    // (the sale information names the broker from the stock row's embedded Broker), so this is here
    // for the day one does, not because a fixture depends on it.
    storageService.GetBrokerAsync(Arg.Any<string>(), Arg.Any<long?>())
        .Returns(callInfo =>
        {
            var accountNumber = callInfo.ArgAt<string>(0);
            var companyID = callInfo.ArgAt<long?>(1);
            var match = env.Brokers.FirstOrDefault(b => !b.IsDeleted
                && b.CompanyID == companyID
                && (b.AccountNumbers?.Contains(accountNumber) ?? false));
            return Task.FromResult(match!);
        });

    storageService.GetBrokerAsync(Arg.Any<long>())
        .Returns(callInfo =>
        {
            var id = callInfo.ArgAt<long>(0);
            var match = env.Brokers.FirstOrDefault(b => !b.IsDeleted && (b.ID == id || b.id == id.ToString()));
            return Task.FromResult(match!);
        });

    // Wire up service items
    storageService.GetServiceItemsAsync(Arg.Any<bool>())
        .Returns(Task.FromResult<IEnumerable<ServiceItemModel>>(env.ServiceItems));

    // Wire up broker stock (looks up from environment data by VIN, falls back to empty)
    storageService.GetBrokerStockAsync(Arg.Any<long?>(), Arg.Any<string>())
        .Returns(callInfo =>
        {
            var vin = callInfo.ArgAt<string>(1);
            if (vin is not null && env.BrokerStocks.TryGetValue(vin, out var stocks))
                return Task.FromResult(stocks.AsEnumerable());
            return Task.FromResult(Enumerable.Empty<ShiftSoftware.ADP.Models.TBP.TBP_StockModel>());
        });

    // === Generate Vehicle Lookup Output ===
    var vehicleLookupOutput = new Dictionary<string, VehicleLookupDTO>();

    foreach (var (vin, aggregate) in env.Vehicles)
    {
        Console.WriteLine($"  Vehicle: {vin}");
        aggregate.VIN = vin;
        aggregate.BrokerInitialVehicles.AddRange(env.BrokerInitialVehicles);
        aggregate.BrokerInvoices.AddRange(env.BrokerInvoices);

        var vehicleLookup = await GenerateVehicleLookup(
            vin, aggregate, env, lookupOptions, requestOptions, storageService, serviceProvider);
        vehicleLookupOutput[vin] = vehicleLookup;
    }

    // === Generate Paint Thickness Certificate Output ===
    // Only VINs whose data satisfies the strict anchor (an invoiced VehicleEntry of
    // LookupOptions.DistributorCompanyID + a PDI inspection strictly before it) appear.
    var certificateOutput = new Dictionary<string, PaintThicknessCertificateModel>();

    foreach (var (vin, aggregate) in env.Vehicles)
    {
        var certificate = await new PaintThicknessCertificateEvaluator(aggregate, lookupOptions, serviceProvider, storageService)
            .Evaluate(requestOptions.LanguageCode);

        if (certificate is not null)
        {
            Console.WriteLine($"  Certificate: {vin}");
            certificateOutput[vin] = certificate;
        }
    }

    // === Generate Part Lookup Output ===
    var partLookupOutput = new Dictionary<string, PartLookupDTO>();

    foreach (var (partNumber, partAggregate) in env.Parts)
    {
        Console.WriteLine($"  Part: {partNumber}");

        // Initialize null collections
        partAggregate.CatalogParts ??= [];
        partAggregate.StockParts ??= [];
        partAggregate.CompanyDeadStockParts ??= [];

        var partLookup = await GeneratePartLookup(
            partNumber, partAggregate, lookupOptions, serviceProvider,
            env.RequestOptions.DistributorStockLookupQuantityFor(partNumber), requestOptions.LanguageCode);
        if (partLookup is not null)
            partLookupOutput[partNumber] = partLookup;
    }

    index.Environments.Add(new GeneratedIndexEnvironment
    {
        Name = envName,
        Anchor = FixedTimeProvider.Anchor.GetUtcNow().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        Files = new Dictionary<string, List<string>>
        {
            ["vehicle-lookup"] = vehicleLookupOutput.Keys.ToList(),
            ["part-lookup"] = partLookupOutput.Keys.ToList(),
            ["paint-thickness-certificate"] = certificateOutput.Keys.ToList(),
        },
    });

    // === Write Output Files ===
    var vehicleJson = JsonSerializer.Serialize(vehicleLookupOutput, serializeOptions);
    var partJson = JsonSerializer.Serialize(partLookupOutput, serializeOptions);
    var certificateJson = JsonSerializer.Serialize(certificateOutput, serializeOptions);

    // Every bundled-asset URL the fixtures carry must name a file that ships in the package — the
    // resolvers only ever emit files from the known set, so this catches a hand-typed ImageUrl in an
    // environment (the claim-document pictures) or a drawing deleted from under a fixture. Fail the
    // build rather than publish a fixture with a broken picture.
    VerifyBundledAssets(assetsDir, envName, vehicleJson, partJson, certificateJson);

    foreach (var baseDir in outputDirs)
    {
        var envOutputDir = Path.Combine(baseDir, envName);
        Directory.CreateDirectory(envOutputDir);

        File.WriteAllText(Path.Combine(envOutputDir, "vehicle-lookup.json"), vehicleJson);
        File.WriteAllText(Path.Combine(envOutputDir, "part-lookup.json"), partJson);
        File.WriteAllText(Path.Combine(envOutputDir, "paint-thickness-certificate.json"), certificateJson);

        Console.WriteLine($"  Output written to: {envOutputDir}");
    }
}

foreach (var baseDir in outputDirs)
{
    Directory.CreateDirectory(baseDir);
    File.WriteAllText(Path.Combine(baseDir, "index.json"), JsonSerializer.Serialize(index, serializeOptions));
    Console.WriteLine($"Index written to: {Path.Combine(baseDir, "index.json")}");
}

Console.WriteLine("\nDone.");
return 0;

// === Helper Methods ===

// Mirrors VehicleLookupService.LookupFromAggregateAsync step for step, with the environment
// standing in for storage and the host: keep the two in the same order when either changes.
static async Task<VehicleLookupDTO> GenerateVehicleLookup(
    string vin,
    ShiftSoftware.ADP.Lookup.Services.Aggregate.CompanyDataAggregateModel aggregate,
    GeneratorEnvironment env,
    LookupOptions options,
    VehicleLookupRequestOptions requestOptions,
    IVehicleLookupStorageService storageService,
    IServiceProvider serviceProvider)
{
    var language = requestOptions.LanguageCode;
    var vehicle = new VehicleEntryEvaluator(aggregate, options).Evaluate();
    var ownership = new VehicleOwnershipEvaluator(aggregate).Evaluate(vehicle);

    var data = new VehicleLookupDTO
    {
        VIN = vin,
        IsAuthorized = new VehicleAuthorizationEvaluator(aggregate).Evaluate(),
        PaintThicknessInspections = await new VehiclePaintThicknessEvaluator(aggregate, options, serviceProvider)
            .Evaluate(language),
        PaintThicknessCertificateAvailable = new PaintThicknessCertificateEvaluator(aggregate, options, serviceProvider, storageService)
            .EvaluateAvailability(),
        Identifiers = new VehicleIdentifierEvaluator(aggregate).Evaluate(vehicle),
        VehicleSpecification = await new VehicleSpecificationEvaluator(storageService).Evaluate(vehicle, requestOptions),
        ServiceHistory = await new VehicleServiceHistoryEvaluator(aggregate, options, serviceProvider)
            .Evaluate(language, requestOptions.VehicleServiceHistoryConsistencyLevel),
        SSC = new VehicleSSCEvaluator(aggregate, options).Evaluate(requestOptions.TraceSSCEvaluation),
        NextServiceDate = aggregate.LaborLines?.Max(x => x.NextServiceDate),
        Accessories = await new VehicleAccessoriesEvaluator(aggregate, options, serviceProvider)
            .Evaluate(language),
        SaleInformation = await new VehicleSaleInformationEvaluator(aggregate, options, serviceProvider, storageService)
            .Evaluate(vehicle, ownership, requestOptions),
    };

    await ApplySscPartAvailability(data.SSC, vin, env, options, requestOptions, serviceProvider);

    // The signed certificate URLs (one per print language) ride the lookup when the certificate is
    // available, the request opted in and a resolver is wired — the same three-way gate as production.
    if (data.PaintThicknessCertificateAvailable
        && requestOptions.GeneratePaintThicknessCertificateUrls
        && options.PaintThicknessCertificateUrlsResolver is not null)
    {
        var certificateUrls = await options.PaintThicknessCertificateUrlsResolver(
            new LookupOptionResolverModel<string>(vin, language, serviceProvider));

        if (certificateUrls is not null && certificateUrls.Count > 0)
            data.PaintThicknessCertificateUrls = certificateUrls;
    }

    data.Warranty = await new WarrantyAndFreeServiceDateEvaluator(aggregate, options)
        .EvaluateAsync(
            vehicle,
            data.SaleInformation,
            requestOptions.IgnoreBrokerStock,
            language,
            serviceProvider);

    var serviceItemsResult = await new VehicleServiceItemEvaluator(
        storageService, aggregate, options, serviceProvider
    ).Evaluate(
        vehicle,
        ownership,
        data.Warranty?.FreeServiceStartDate,
        language,
        data.SaleInformation?.Broker
    );

    data.ServiceItems = serviceItemsResult.serviceItems;

    if (data.Warranty is not null)
    {
        if (data.Warranty.WarrantyStartDate is not null)
            data.Warranty.ActivationIsRequired = serviceItemsResult.activationRequired;

        // Company-scoped: with the allocation guard off this is Required / NotRequired as before; with it
        // on, the environment's RequestingCompanyID decides between Required and BlockedNotAllocated.
        data.Warranty.ActivationStatus = new ActivationStatusEvaluator(aggregate, options)
            .Evaluate(serviceItemsResult.activationRequired, requestOptions.RequestingCompanyID);
    }

    return data;
}

// SSC part availability, gated like production on LookupOptions.EnableSSCPartAvailability: off leaves every
// part "not checked" (null). On, with a declared stock scope, the enricher's own rule runs against the
// environment's Parts as the stock container (the production enricher differs only in reading Cosmos);
// on without a scope, availability is seeded demonstratively — each open recall's parts alternate
// in-stock / out-of-stock so the mocks show all three chips — repaired recalls staying null either way.
static async Task ApplySscPartAvailability(
    IEnumerable<SscDTO>? sscs,
    string vin,
    GeneratorEnvironment env,
    LookupOptions options,
    VehicleLookupRequestOptions requestOptions,
    IServiceProvider serviceProvider)
{
    if (!options.EnableSSCPartAvailability || sscs is null)
        return;

    var openRecallParts = sscs
        .Where(s => s is not null && !s.Repaired)
        .SelectMany(s => s.Parts ?? [])
        .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.PartNumber))
        .Select(p => p.PartNumber)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (openRecallParts.Count == 0)
        return;

    if (options.SSCPartStockScopeResolver is { } scopeResolver)
    {
        var scope = await scopeResolver(new LookupOptionResolverModel<SSCPartAvailabilityScopeRequest>(
            new SSCPartAvailabilityScopeRequest(vin, openRecallParts), requestOptions.LanguageCode, serviceProvider));

        if (scope is { Count: > 0 })
        {
            var stockRows = env.Parts.Values.SelectMany(p => p.StockParts ?? []).ToList();
            SSCPartAvailabilityEnricher.ApplyAvailability(sscs, stockRows, scope, options.PartNumberStorageKeyResolver);
            return;
        }
    }

    var sscInStock = true;
    foreach (var sscRecall in sscs)
    {
        if (sscRecall is null || sscRecall.Repaired || sscRecall.Parts is null)
            continue;

        foreach (var sscPart in sscRecall.Parts)
        {
            if (sscPart is null)
                continue;

            sscPart.IsAvailable = sscInStock;
            sscInStock = !sscInStock;
        }
    }
}

static ColorModel? FindColor(IEnumerable<ColorModel> colors, string? code, long? brand)
{
    if (string.IsNullOrWhiteSpace(code) || brand is null)
        return null;

    return colors.FirstOrDefault(c => c.Code == code && c.BrandID == brand);
}

static async Task<PartLookupDTO?> GeneratePartLookup(
    string partNumber,
    PartAggregateCosmosModel partAggregate,
    LookupOptions options,
    IServiceProvider serviceProvider,
    int? distributorStockLookupQuantity,
    string language)
{
    var cosmosPartCatalog = partAggregate.CatalogParts?.FirstOrDefault();

    var priceEvaluation = await new PartPriceEvaluator(partAggregate, options, serviceProvider)
        .Evaluate(source: null, language: language);

    return new PartLookupDTO
    {
        PartNumber = partNumber,
        PartDescription = cosmosPartCatalog?.PartName,
        LocalDescription = cosmosPartCatalog?.LocalDescription,
        ProductGroup = cosmosPartCatalog?.ProductGroup,
        BinType = cosmosPartCatalog?.BinType,
        CubicMeasure = cosmosPartCatalog?.CubicMeasure,
        Length = cosmosPartCatalog?.Length is not null && cosmosPartCatalog?.Length > 0
            ? cosmosPartCatalog?.Length : cosmosPartCatalog?.Dimension1,
        Width = cosmosPartCatalog?.Width is not null && cosmosPartCatalog?.Width > 0
            ? cosmosPartCatalog?.Width : cosmosPartCatalog?.Dimension2,
        Height = cosmosPartCatalog?.Height is not null && cosmosPartCatalog?.Height > 0
            ? cosmosPartCatalog?.Height : cosmosPartCatalog?.Dimension3,
        GrossWeight = cosmosPartCatalog?.GrossWeight,
        HSCode = cosmosPartCatalog?.HSCode,
        NetWeight = cosmosPartCatalog?.NetWeight,
        Origin = cosmosPartCatalog?.Origin,
        PNC = cosmosPartCatalog?.PNC,
        SupersededTo = cosmosPartCatalog?.SupersededTo?.Select(x => x.PartNumber),
        SupersededFrom = cosmosPartCatalog?.SupersededFrom?.Select(x => x.PartNumber),
        DistributorPurchasePrice = priceEvaluation.distributorPurchasePrice,
        Prices = priceEvaluation.prices,
        DeadStock = await new PartDeadStockEvaluator(partAggregate, options, serviceProvider).Evaluate(language),
        StockParts = await new PartStockEvaluator(partAggregate, options, serviceProvider)
            .Evaluate(distributorStockLookupQuantity, language),
    };
}

static void VerifyBundledAssets(string assetsDir, string envName, params string[] serializedFixtures)
{
    var urlPattern = new System.Text.RegularExpressions.Regex(
        System.Text.RegularExpressions.Regex.Escape(DemoAssets.CdnBaseUrl) + "([^\"\\\\]+)");

    var missing = serializedFixtures
        .SelectMany(json => urlPattern.Matches(json).Select(m => m.Groups[1].Value))
        .Distinct(StringComparer.Ordinal)
        .Where(relative => !File.Exists(Path.Combine(assetsDir, relative.Replace('/', Path.DirectorySeparatorChar))))
        .ToList();

    if (missing.Count > 0)
        throw new InvalidOperationException(
            $"Environment '{envName}' references bundled demo assets that do not exist under '{assetsDir}': "
            + string.Join(", ", missing));
}

static string FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
            File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Could not find repo root (no .git directory found).");
}

/// <summary>
/// Fixed clock used so generated sample data (signatures, expiries) stays byte-stable
/// across runs. Update <see cref="Anchor"/> only when intentionally re-baselining samples.
/// </summary>
sealed class FixedTimeProvider : TimeProvider
{
    public static readonly FixedTimeProvider Anchor = new(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));

    private readonly DateTimeOffset _utcNow;
    public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
    public override DateTimeOffset GetUtcNow() => _utcNow;
}

/// <summary>
/// Shape of <c>mocks/generated/index.json</c>: which environments exist, which fixture keys each file
/// holds, and the calendar date (<c>yyyy-MM-dd</c>, UTC) every status in that environment was computed
/// against — the value a consumer passes as <c>today</c> so its clock agrees with the fixture.
/// </summary>
sealed class GeneratedIndex
{
    public List<GeneratedIndexEnvironment> Environments { get; set; } = new();
}

sealed class GeneratedIndexEnvironment
{
    public string Name { get; set; } = "";
    public string Anchor { get; set; } = "";
    /// <summary>Fixture keys per file stem (<c>vehicle-lookup</c>, <c>part-lookup</c>, <c>paint-thickness-certificate</c>).</summary>
    public Dictionary<string, List<string>> Files { get; set; } = new();
}

/// <summary>
/// JSON converter for Dictionary&lt;long?, int&gt; keys serialized as strings.
/// </summary>
class NullableLongDictionaryConverter : JsonConverter<Dictionary<long?, int>>
{
    public override Dictionary<long?, int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dict = new Dictionary<long?, int>();
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return dict;
            var key = reader.GetString();
            reader.Read();
            var value = reader.GetInt32();
            long? parsedKey = long.TryParse(key, out var k) ? k : null;
            dict[parsedKey] = value;
        }
        throw new JsonException("Unexpected end of JSON.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<long?, int> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key?.ToString() ?? "null");
            writer.WriteNumberValue(kvp.Value);
        }
        writer.WriteEndObject();
    }
}
