using System.Text.Json;
using System.Text.Json.Serialization;
using ADP.TestData.Generator;
using NSubstitute;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Vehicle;

// Resolve paths relative to the repo root
var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var environmentsDir = Path.Combine(repoRoot, "ADP.TestData", "environments");
var webComponentsOutputDir = Path.Combine(repoRoot, "ADP.WebComponents", "adp-web-components", "src", "features", "mocks", "data", "generated");
var webComponentsDevDir = Path.Combine(repoRoot, "ADP.WebComponents", "adp-web-components", "www", "mocks", "generated");
var docsOutputDir = Path.Combine(repoRoot, "ADP.Docs", "Docs", "docs", "web-components", "demo-data");

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

foreach (var envFile in Directory.GetFiles(environmentsDir, "*.json"))
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
    IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();

    // Mock IVehicleLoockupStorageService
    var storageService = Substitute.For<IVehicleLoockupStorageService>();

    // Wire up vehicle model lookups
    storageService.GetVehicleModelsAsync(Arg.Any<string>(), Arg.Any<long?>())
        .Returns(callInfo =>
        {
            var variant = callInfo.ArgAt<string>(0);
            var brand = callInfo.ArgAt<long?>(1);
            var match = env.VehicleModels.FirstOrDefault(m =>
                m.VariantCode == variant && m.BrandID == brand);
            return Task.FromResult(match);
        });

    // Wire up service items
    storageService.GetServiceItemsAsync(Arg.Any<bool>())
        .Returns(Task.FromResult<IEnumerable<ServiceItemModel>>(env.ServiceItems));

    // Wire up broker stock (returns empty for standard dealer)
    storageService.GetBrokerStockAsync(Arg.Any<long?>(), Arg.Any<string>())
        .Returns(Task.FromResult(Enumerable.Empty<ShiftSoftware.ADP.Models.TBP.TBP_StockModel>()));

    // === Generate Vehicle Lookup Output ===
    var vehicleLookupOutput = new Dictionary<string, VehicleLookupDTO>();

    foreach (var (vin, aggregate) in env.Vehicles)
    {
        Console.WriteLine($"  Vehicle: {vin}");
        aggregate.VIN = vin;
        aggregate.BrokerInitialVehicles.AddRange(env.BrokerInitialVehicles);
        aggregate.BrokerInvoices.AddRange(env.BrokerInvoices);

        var vehicleLookup = await GenerateVehicleLookup(
            vin, aggregate, lookupOptions, storageService, serviceProvider);
        vehicleLookupOutput[vin] = vehicleLookup;
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
            partNumber, partAggregate, lookupOptions, serviceProvider);
        if (partLookup is not null)
            partLookupOutput[partNumber] = partLookup;
    }

    // === Write Output Files ===
    // Always write to source dirs; also write to www/ if dev server is running
    var outputDirs = new List<string> { webComponentsOutputDir, docsOutputDir };
    if (Directory.Exists(Path.Combine(repoRoot, "ADP.WebComponents", "adp-web-components", "www")))
        outputDirs.Add(webComponentsDevDir);

    foreach (var baseDir in outputDirs)
    {
        var envOutputDir = Path.Combine(baseDir, envName);
        Directory.CreateDirectory(envOutputDir);

        var vehicleJson = JsonSerializer.Serialize(vehicleLookupOutput, serializeOptions);
        File.WriteAllText(Path.Combine(envOutputDir, "vehicle-lookup.json"), vehicleJson);

        var partJson = JsonSerializer.Serialize(partLookupOutput, serializeOptions);
        File.WriteAllText(Path.Combine(envOutputDir, "part-lookup.json"), partJson);

        Console.WriteLine($"  Output written to: {envOutputDir}");
    }
}

Console.WriteLine("\nDone.");

// === Helper Methods ===

static async Task<VehicleLookupDTO> GenerateVehicleLookup(
    string vin,
    ShiftSoftware.ADP.Lookup.Services.Aggregate.CompanyDataAggregateModel aggregate,
    LookupOptions options,
    IVehicleLoockupStorageService storageService,
    IServiceProvider serviceProvider)
{
    var vehicle = new VehicleEntryEvaluator(aggregate).Evaluate();

    var requestOptions = new VehicleLookupRequestOptions
    {
        LanguageCode = "en",
        LookupEndCustomer = false,
        IgnoreBrokerStock = false,
    };

    var data = new VehicleLookupDTO
    {
        VIN = vin,
        IsAuthorized = new VehicleAuthorizationEvaluator(aggregate).Evaluate(),
        PaintThicknessInspections = await new VehiclePaintThicknessEvaluator(aggregate, options, serviceProvider)
            .Evaluate("en"),
        Identifiers = new VehicleIdentifierEvaluator(aggregate).Evaluate(vehicle),
        VehicleSpecification = await new VehicleSpecificationEvaluator(storageService).Evaluate(vehicle),
        ServiceHistory = await new VehicleServiceHistoryEvaluator(aggregate, options, serviceProvider)
            .Evaluate("en", ShiftSoftware.ADP.Lookup.Services.Enums.ConsistencyLevels.Strong),
        SSC = new VehicleSSCEvaluator(aggregate).Evaluate(),
        NextServiceDate = aggregate.LaborLines?.Max(x => x.NextServiceDate),
        Accessories = await new VehicleAccessoriesEvaluator(aggregate, options, serviceProvider)
            .Evaluate("en"),
        SaleInformation = await new VehicleSaleInformationEvaluator(aggregate, options, serviceProvider, storageService)
            .Evaluate(requestOptions),
    };

    data.Warranty = new WarrantyAndFreeServiceDateEvaluator(aggregate, options)
        .Evaluate(vehicle, data.SaleInformation, requestOptions.IgnoreBrokerStock);

    var serviceItemsResult = await new VehicleServiceItemEvaluator(
        storageService, aggregate, options, serviceProvider
    ).Evaluate(
        vehicle,
        data.Warranty?.FreeServiceStartDate,
        data.SaleInformation,
        "en"
    );

    data.ServiceItems = serviceItemsResult.serviceItems;

    if (data.Warranty is not null && data.Warranty.WarrantyStartDate is not null)
        data.Warranty.ActivationIsRequired = serviceItemsResult.activationRequired;

    return data;
}

static async Task<PartLookupDTO?> GeneratePartLookup(
    string partNumber,
    PartAggregateCosmosModel partAggregate,
    LookupOptions options,
    IServiceProvider serviceProvider)
{
    var cosmosPartCatalog = partAggregate.CatalogParts?.FirstOrDefault();

    var priceEvaluation = await new PartPriceEvaluator(partAggregate, options, serviceProvider)
        .Evaluate(source: null, language: "en");

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
        DeadStock = await new PartDeadStockEvaluator(partAggregate, options, serviceProvider).Evaluate("en"),
        StockParts = await new PartStockEvaluator(partAggregate, options, serviceProvider)
            .Evaluate(distributorStockLookupQuantity: 1, language: "en"),
    };
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
