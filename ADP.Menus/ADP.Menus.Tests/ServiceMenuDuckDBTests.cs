using System.Globalization;

using DuckDB.NET.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Extensions;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Extensions;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// The menu lookup's DuckDB READER, end to end and offline: fixture documents are seeded into a real
/// in-memory DuckDB database using the reader's own layout contract (<c>DuckDBServiceMenuSchema</c>,
/// via InternalsVisibleTo — the seeder cannot drift from the DDL the reader expects), and come back
/// through the real reader. The sync that will populate these tables in production is a separate,
/// not-yet-implemented concern; these tests pin the contract it must produce.
///
/// <para>The test that matters most is DIFFERENTIAL, like this project's export-vs-lookup golden: the
/// same documents served by the Cosmos path and by the seeded-DuckDB path must generate IDENTICAL
/// menus — codes, descriptions, money. Storage is the only thing allowed to differ.</para>
/// </summary>
public class ServiceMenuDuckDBTests
{
    /// <summary>Well-known local emulator endpoint/key. Never connected to — construction is lazy.</summary>
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    // ---- the Cosmos seam -----------------------------------------------------------------------------

    private sealed class StubCosmosService : ServiceMenuCosmosService
    {
        private readonly Func<string, ServiceMenuDocuments> read;

        internal StubCosmosService(Func<string, ServiceMenuDocuments> read)
            : base(new LookUpCosmosClient(new CosmosClient(ConnectionString)))
        {
            this.read = read;
        }

        public override Task<ServiceMenuDocuments> GetMenuDocumentsAsync(string basicModelCode, CancellationToken cancellationToken = default)
            => Task.FromResult(read(basicModelCode));
    }

    // ---- seeding: fixture documents → DuckDB tables, per the reader's own schema contract -------------

    /// <summary>
    /// An in-memory store holding the given documents, laid out exactly as the schema contract
    /// declares: tables from <c>DuckDBServiceMenuSchema.BuildCreateTableSql</c>, scalar columns native,
    /// embedded shapes as JSON — what any future menu sync must produce for the reader to work.
    /// </summary>
    private static DuckDBConnection SeededStore(params ServiceMenuDocuments[] documentSets)
    {
        var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();
        SeedInto(connection, documentSets);
        return connection;
    }

    private static void SeedInto(DuckDBConnection connection, params ServiceMenuDocuments[] documentSets)
    {
        foreach (var (tableName, modelType) in DuckDBServiceMenuSchema.Tables)
            Execute(connection, DuckDBServiceMenuSchema.BuildCreateTableSql(tableName, modelType));

        foreach (var documents in documentSets)
        {
            InsertRows(connection, ModelTypes.MenuVariant, documents.Variants);
            InsertRows(connection, ModelTypes.MenuPeriod, documents.Periods);
            InsertRows(connection, ModelTypes.MenuLabour, documents.Labours);
            InsertRows(connection, ModelTypes.MenuItem, documents.Items);
        }
    }

    private static void InsertRows<T>(DuckDBConnection connection, string tableName, List<T> models)
    {
        var columns = DuckDBServiceMenuSchema.GetColumns(typeof(T));
        var columnList = string.Join(", ", columns.Select(column => DuckDBServiceMenuSchema.QuoteIdentifier(column.Name)));

        foreach (var model in models)
        {
            var values = string.Join(", ", columns.Select(column => Literal(column.GetValue(model), column.PropertyType)));

            Execute(connection,
                $"INSERT INTO {DuckDBServiceMenuSchema.QuoteIdentifier(tableName)} ({columnList}) VALUES ({values})");
        }
    }

    private static string Literal(object? value, Type propertyType)
    {
        if (value is null)
            return "NULL";

        if (DuckDBServiceMenuSchema.IsJsonColumn(propertyType))
            return $"'{DuckDBServiceMenuSchema.EscapeLiteral(Newtonsoft.Json.JsonConvert.SerializeObject(value))}'";

        return value switch
        {
            string text => $"'{DuckDBServiceMenuSchema.EscapeLiteral(text)}'",
            bool flag => flag ? "TRUE" : "FALSE",
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()!,
        };
    }

    private static void Execute(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    // ---- fixture plumbing ----------------------------------------------------------------------------

    /// <summary>
    /// A second model's worth of documents: the fixture partition re-keyed onto another basic model
    /// code, with every document id offset so the per-type PRIMARY KEY holds.
    /// </summary>
    private static ServiceMenuDocuments RekeyedCopy(string basicModelCode, string idPrefix)
    {
        var copy = MenuCosmosDocumentFixture.Build();
        copy.BasicModelCode = basicModelCode;

        foreach (var variant in copy.Variants) { variant.BasicModelCode = basicModelCode; variant.id = idPrefix + variant.id; }
        foreach (var period in copy.Periods) { period.BasicModelCode = basicModelCode; period.id = idPrefix + period.id; }
        foreach (var labour in copy.Labours) { labour.BasicModelCode = basicModelCode; labour.id = idPrefix + labour.id; }
        foreach (var item in copy.Items) { item.BasicModelCode = basicModelCode; item.id = idPrefix + item.id; }

        return copy;
    }

    private static ServiceMenuLookupService Lookup(IServiceMenuLookupStorageService storage) =>
        new(storage, new ServiceMenuGenerationEvaluator(Options.Create(new ServiceMenuLookupOptions())));

    private static ServiceMenuLookupRequest Request(string? basicModelCode = null) => new()
    {
        BasicModelCode = basicModelCode,
        CountryID = 2,
        Language = "en",
    };

    // ---- seed + read ---------------------------------------------------------------------------------

    [Fact]
    public async Task ASeededStore_Read_RoundTripsTheDocuments()
    {
        var original = MenuCosmosDocumentFixture.Build();
        using var connection = SeededStore(original);

        var roundTripped = await new DuckDBServiceMenuLookupStorageService(connection)
            .GetMenuDocumentsAsync(MenuGraphFixture.BasicModelCode);

        Assert.Equal(original.Variants.Count, roundTripped.Variants.Count);
        Assert.Equal(original.Periods.Count, roundTripped.Periods.Count);
        Assert.Equal(original.Labours.Count, roundTripped.Labours.Count);
        Assert.Equal(original.Items.Count, roundTripped.Items.Count);

        // The embedded (JSON-columned) shapes survive: the variant keeps its mappings and country
        // rates, the item keeps its parts with their prices, the labour keeps its group's membership.
        var variant = roundTripped.Variants.Single(x => x.id == original.Variants[0].id);
        Assert.Equal(original.Variants[0].VariantName, variant.VariantName);
        Assert.Equal(original.Variants[0].LabourRate, variant.LabourRate);
        Assert.NotNull(variant.LabourRateMapping);
        Assert.Equal(original.Variants[0].LabourRateMapping.Code, variant.LabourRateMapping.Code);
        Assert.NotNull(variant.BrandMapping);
        Assert.Equal(original.Variants[0].CountryLabourRates.Count, variant.CountryLabourRates.Count);

        var item = roundTripped.Items.Single(x => x.id == original.Items[0].id);
        Assert.Equal(original.Items[0].Parts.Count, item.Parts.Count);
        Assert.Equal(
            original.Items[0].Parts.Sum(x => x.CountryPrices.Count),
            item.Parts.Sum(x => x.CountryPrices.Count));
        Assert.Equal(
            original.Items[0].ServiceIntervalGroups.Count,
            item.ServiceIntervalGroups.Count);

        var labour = roundTripped.Labours.Single(x => x.id == original.Labours[0].id);
        Assert.NotNull(labour.ServiceIntervalGroup);
        Assert.Equal(
            original.Labours[0].ServiceIntervalGroup.ServiceIntervalIDs,
            labour.ServiceIntervalGroup.ServiceIntervalIDs);
    }

    /// <summary>
    /// THE differential claim: Cosmos path and DuckDB path generate identical menus from the same
    /// documents. Decimals are compared as decimals — DuckDB's DECIMAL(38,12) changes scale, never value.
    /// </summary>
    [Fact]
    public async Task GenerationParity_CosmosPathAndDuckDBPath_ProduceIdenticalMenus()
    {
        var documents = MenuCosmosDocumentFixture.WithFreeAndPaidVariants();

        var viaCosmos = await Lookup(new StubCosmosService(_ => documents))
            .GetMenuAsync(Request(MenuGraphFixture.BasicModelCode));

        // Guard against a vacuous pass: the expected side must actually be a generated menu — two
        // variants with lines and priced parts — before the Zip-based comparison means anything.
        Assert.False(viaCosmos.NotFound);
        Assert.Equal(2, viaCosmos.Variants.Count);
        Assert.All(viaCosmos.Variants, variant => Assert.NotEmpty(variant.PeriodicServices));
        Assert.Contains(
            viaCosmos.Variants.SelectMany(variant => variant.PeriodicServices.Concat(variant.StandaloneServices)),
            line => line.Parts.Count > 0);

        using var connection = SeededStore(documents);
        var viaDuckDB = await Lookup(new DuckDBServiceMenuLookupStorageService(connection))
            .GetMenuAsync(Request(MenuGraphFixture.BasicModelCode));

        AssertMenusEqual(viaCosmos, viaDuckDB);
    }

    private static void AssertMenusEqual(ServiceMenuLookupDTO expected, ServiceMenuLookupDTO actual)
    {
        Assert.Equal(expected.NotFound, actual.NotFound);
        Assert.Equal(expected.CountryID, actual.CountryID);
        Assert.Equal(expected.TransferRate, actual.TransferRate);
        Assert.Equal(expected.Variants.Count, actual.Variants.Count);

        foreach (var (expectedVariant, actualVariant) in expected.Variants.Zip(actual.Variants))
        {
            Assert.Equal(expectedVariant.VariantID, actualVariant.VariantID);
            Assert.Equal(expectedVariant.VariantName, actualVariant.VariantName);
            Assert.Equal(expectedVariant.BrandCode, actualVariant.BrandCode);
            Assert.Equal(expectedVariant.IsFree, actualVariant.IsFree);
            Assert.Equal(expectedVariant.DiscountPercentage, actualVariant.DiscountPercentage);

            AssertLinesEqual(expectedVariant.PeriodicServices, actualVariant.PeriodicServices);
            AssertLinesEqual(expectedVariant.StandaloneServices, actualVariant.StandaloneServices);
        }
    }

    private static void AssertLinesEqual(List<ServiceMenuLineDTO> expected, List<ServiceMenuLineDTO> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var (expectedLine, actualLine) in expected.Zip(actual))
        {
            Assert.Equal(expectedLine.LineKey, actualLine.LineKey);
            Assert.Equal(expectedLine.Code, actualLine.Code);
            Assert.Equal(expectedLine.LabourCode, actualLine.LabourCode);
            Assert.Equal(expectedLine.Description, actualLine.Description);
            Assert.Equal(expectedLine.LineType, actualLine.LineType);
            Assert.Equal(expectedLine.IsStandalone, actualLine.IsStandalone);
            Assert.Equal(expectedLine.ServiceIntervalCode, actualLine.ServiceIntervalCode);
            Assert.Equal(expectedLine.ServiceIntervalValueInMeter, actualLine.ServiceIntervalValueInMeter);
            Assert.Equal(expectedLine.LabourRate, actualLine.LabourRate);
            Assert.Equal(expectedLine.AllowedTime, actualLine.AllowedTime);
            Assert.Equal(expectedLine.LabourPrice, actualLine.LabourPrice);
            Assert.Equal(expectedLine.Consumable, actualLine.Consumable);
            Assert.Equal(expectedLine.LabourTotalPrice, actualLine.LabourTotalPrice);
            Assert.Equal(expectedLine.PartsTotalPrice, actualLine.PartsTotalPrice);
            Assert.Equal(expectedLine.DiscountPercentage, actualLine.DiscountPercentage);
            Assert.Equal(expectedLine.DiscountAmount, actualLine.DiscountAmount);
            Assert.Equal(expectedLine.TotalPrice, actualLine.TotalPrice);
            Assert.Equal(expectedLine.HasUnpricedParts, actualLine.HasUnpricedParts);
            Assert.Equal(expectedLine.Parts.Count, actualLine.Parts.Count);

            foreach (var (expectedPart, actualPart) in expectedLine.Parts.Zip(actualLine.Parts))
            {
                Assert.Equal(expectedPart.PartNumber, actualPart.PartNumber);
                Assert.Equal(expectedPart.SortOrder, actualPart.SortOrder);
                Assert.Equal(expectedPart.Quantity, actualPart.Quantity);
                Assert.Equal(expectedPart.UnitPrice, actualPart.UnitPrice);
                Assert.Equal(expectedPart.TotalPrice, actualPart.TotalPrice);
                Assert.Equal(expectedPart.HasCountryPrice, actualPart.HasCountryPrice);
            }
        }
    }

    // ---- bulk ----------------------------------------------------------------------------------------

    [Fact]
    public async Task BulkRead_GroupsByCode_KeepsFirstAppearanceOrder_AndAnswersMissingCodesEmpty()
    {
        var first = MenuCosmosDocumentFixture.Build();
        var second = RekeyedCopy("ZZZ99", "b-");

        using var connection = SeededStore(first, second);
        var storage = new DuckDBServiceMenuLookupStorageService(connection);

        var results = await storage.GetMenuDocumentsAsync(
            new[] { "ZZZ99", MenuGraphFixture.BasicModelCode, "ZZZ99", "NOPE1" });

        Assert.Equal(3, results.Count);

        Assert.Equal("ZZZ99", results[0].BasicModelCode);
        Assert.Equal(second.Variants.Count, results[0].Variants.Count);
        Assert.Equal(second.Items.Count, results[0].Items.Count);

        Assert.Equal(MenuGraphFixture.BasicModelCode, results[1].BasicModelCode);
        Assert.Equal(first.Periods.Count, results[1].Periods.Count);
        Assert.Equal(first.Labours.Count, results[1].Labours.Count);

        Assert.Equal("NOPE1", results[2].BasicModelCode);
        Assert.True(results[2].IsEmpty);
    }

    [Fact]
    public async Task BulkLookup_OverDuckDB_FoldsEachCodeLikeTheSingleLookup()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        using var connection = SeededStore(documents);

        var lookup = Lookup(new DuckDBServiceMenuLookupStorageService(connection));

        var results = await lookup.GetMenusAsync(
            new[] { MenuGraphFixture.BasicModelCode, "NOPE1", MenuGraphFixture.BasicModelCode },
            Request());

        Assert.Equal(2, results.Count);

        Assert.False(results[0].NotFound);
        Assert.Equal(MenuGraphFixture.BasicModelCode, results[0].BasicModelCode);
        Assert.NotEmpty(results[0].Variants);

        Assert.True(results[1].NotFound);
        Assert.Equal("NOPE1", results[1].BasicModelCode);
        Assert.Empty(results[1].Variants);

        // And the bulk fold IS the single fold: same code, same answer.
        var single = await lookup.GetMenuAsync(Request(MenuGraphFixture.BasicModelCode));
        AssertMenusEqual(single, results[0]);
    }

    /// <summary>
    /// Bulk is a DuckDB-storage flow, exactly like the vehicle lookup's multi-VIN path: the Cosmos
    /// storage deliberately does not implement it. Pinned so the split stays a decision, not drift.
    /// </summary>
    [Fact]
    public async Task BulkLookup_OverCosmosStorage_IsNotImplemented_MatchingTheVehiclePattern()
    {
        var lookup = Lookup(new StubCosmosService(code => new ServiceMenuDocuments { BasicModelCode = code }));

        await Assert.ThrowsAsync<NotImplementedException>(
            () => lookup.GetMenusAsync(new[] { MenuGraphFixture.BasicModelCode }, Request()));
    }

    // ---- the sync placeholder ------------------------------------------------------------------------

    /// <summary>
    /// The menu DuckDB sync is deliberately not implemented — its design (what it pulls from, how it
    /// runs) is a separate decision. Pinned so the placeholder cannot silently pretend to sync.
    /// </summary>
    [Fact]
    public async Task TheMenuDuckDBSync_IsNotImplementedYet()
    {
        using var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();

        await Assert.ThrowsAsync<NotImplementedException>(
            () => new DuckDBServiceMenuSyncService().SyncAsync(connection));
    }

    // ---- faults --------------------------------------------------------------------------------------

    [Fact]
    public async Task AnUnpopulatedStore_ThrowsContainerNotFound_NotAnEmptyMenu()
    {
        using var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();

        var storage = new DuckDBServiceMenuLookupStorageService(connection);

        await Assert.ThrowsAsync<ServiceMenuContainerNotFoundException>(
            () => storage.GetMenuDocumentsAsync(MenuGraphFixture.BasicModelCode));
    }

    [Fact]
    public async Task AStorageFault_OverDuckDB_IsContainedByTheVehicleMenuSection()
    {
        // A closed connection is the simplest real storage fault.
        var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();
        connection.Close();

        var evaluator = new VehicleServiceMenuEvaluator(
            Lookup(new DuckDBServiceMenuLookupStorageService(connection)));

        var section = await evaluator.EvaluateAsync(
            MenuGraphFixture.BasicModelCode,
            new VehicleLookupRequestOptions
            {
                ServiceMenuOptions = new VehicleServiceMenuRequestOptions { Include = true },
            });

        Assert.Equal(VehicleServiceMenuStatus.Unavailable, section!.Status);
    }

    // ---- registration --------------------------------------------------------------------------------

    /// <summary>
    /// ONE call does the whole DuckDB switch, and it REPLACES rather than shadows: after registering
    /// in either order — DuckDB before the menu lookup (the Cosmos default is skipped) or after it
    /// (the Cosmos default is removed) — the container holds exactly ONE menu storage and no Cosmos
    /// reader at all. Nothing dead is left registered.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DuckDBStorageRegistration_IsTheOnlyStorageLeft_InEitherOrder(bool duckDBFirst)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CosmosClient(ConnectionString));
        services.AddSingleton(_ =>
        {
            var connection = new DuckDBConnection("DataSource=:memory:");
            connection.Open();
            return connection;
        });

        void AddMenus() => services.AddServiceMenuLookup();
        void AddDuckDB() => services.AddDuckDBServiceMenuLookup();

        if (duckDBFirst) { AddDuckDB(); AddMenus(); } else { AddMenus(); AddDuckDB(); }

        Assert.Single(services, x => x.ServiceType == typeof(IServiceMenuLookupStorageService));
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(ServiceMenuCosmosService));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<DuckDBServiceMenuLookupStorageService>(
            scope.ServiceProvider.GetRequiredService<IServiceMenuLookupStorageService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ServiceMenuLookupService>());
    }

    /// <summary>
    /// The connection-string overload serves hosts with no <c>DuckDBConnection</c> registration at
    /// all: each scope's reader opens its OWN connection and disposes it with the scope — proven by
    /// the database file being deletable once the provider is gone.
    /// </summary>
    [Fact]
    public async Task RegistrationByConnectionString_OpensAndOwnsItsOwnConnection()
    {
        var path = Path.Combine(Path.GetTempPath(), $"adp-menu-duckdb-test-{Guid.NewGuid():N}.duckdb");

        try
        {
            var documents = MenuCosmosDocumentFixture.Build();

            using (var seed = new DuckDBConnection($"DataSource={path}"))
            {
                seed.Open();
                SeedInto(seed, documents);
            }

            var services = new ServiceCollection();
            services.AddSingleton(new CosmosClient(ConnectionString));
            services.AddServiceMenuLookup();
            services.AddDuckDBServiceMenuLookup($"DataSource={path}");

            using (var provider = services.BuildServiceProvider(validateScopes: true))
            using (var scope = provider.CreateScope())
            {
                var menu = await scope.ServiceProvider
                    .GetRequiredService<ServiceMenuLookupService>()
                    .GetMenuAsync(Request(MenuGraphFixture.BasicModelCode));

                Assert.False(menu.NotFound);
                Assert.NotEmpty(menu.Variants);
            }

            // The scope disposed the reader; the reader disposed ITS connection — nothing pins the file.
            File.Delete(path);
        }
        finally
        {
            TryDelete(path);
            TryDelete(path + ".wal");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup of a temp file; the assertions have already spoken.
        }
    }
}
