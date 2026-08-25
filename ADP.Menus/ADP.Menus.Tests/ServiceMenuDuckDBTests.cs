using System.Reflection;

using DuckDB.NET.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Extensions;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Extensions;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Sync;
using ShiftSoftware.ADP.Menus.Sync.Extensions;
using ShiftSoftware.ADP.Models.Service.DuckDB;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// The menu lookup's DuckDB backend, end to end and offline: fixture ENTITY graphs go through the
/// REAL sync (<c>ServiceMenuDuckDBSyncService</c>, only its SQL read stubbed through the seam it
/// exposes for exactly this) into a real in-memory DuckDB database — normalized tables, production
/// DDL, per-table watermarks — and come back through the real reader, which JOINS them back into the
/// document shape at read time.
///
/// <para>The test that matters most is DIFFERENTIAL, like this project's export-vs-lookup golden: the
/// same entity graph served through the Cosmos path (projected by the production
/// <c>MenuCosmosMappers</c>) and through the sync-then-join path must generate IDENTICAL menus —
/// codes, descriptions, money. That comparison is what pins the reader's join-time assembly to the
/// Cosmos projections' embed-time rules.</para>
/// </summary>
public class ServiceMenuDuckDBTests
{
    /// <summary>Well-known local emulator endpoint/key. Never connected to — construction is lazy.</summary>
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    // ---- the seams -----------------------------------------------------------------------------------

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

    /// <summary>
    /// The real sync with only its SQL side overridden, through the two seams it exposes for exactly
    /// this: the source attach (a fixture-fed <c>SetupGetSourceBatchItems</c> instead of the EF Core
    /// source adapter, honouring the watermark it is handed) and the prune's source-id read. Fixture
    /// entity graphs are flattened into per-entity-type sets (deduplicated by id, since the graph
    /// shares references) — so the whole engine-and-DuckDB flow, including incremental pulls and
    /// full-reload prunes, runs for real against the fixture.
    /// </summary>
    private sealed class StubSyncService : ServiceMenuDuckDBSyncService
    {
        private readonly Dictionary<Type, List<object>> sources;

        /// <summary>The watermark each table's source attach was given, keyed by entity type name.</summary>
        internal Dictionary<string, DateTimeOffset?> ObservedWatermarks { get; } = new(StringComparer.Ordinal);

        internal StubSyncService(params MenuGraphFixture.Fixture[] fixtures)
        {
            sources = CollectEntities(fixtures);
        }

        protected override void AttachSqlSource<TEntity, TRow>(
            ShiftSoftware.ADP.SyncAgent.Services.Interfaces.ISyncEngine<TEntity, TRow> engine,
            DbContext database,
            DateTimeOffset? watermark)
        {
            ObservedWatermarks[typeof(TEntity).Name] = watermark;

            var rows = sources.TryGetValue(typeof(TEntity), out var list) ? list : [];
            var filtered = rows
                .Where(row => watermark is null || GetLastSaveDate(row) >= watermark)
                .Cast<TEntity>()
                .ToList();

            engine.SetupGetSourceBatchItems(input =>
                new ValueTask<IEnumerable<TEntity?>?>(filtered
                    .Skip((int)(input.Input.Status.CurrentStep * input.Input.Status.BatchSize))
                    .Take((int)input.Input.Status.BatchSize)));
        }

        protected override Task<List<long>> ReadSourceIdsAsync<TEntity>(
            DbContext database, CancellationToken cancellationToken)
        {
            var rows = sources.TryGetValue(typeof(TEntity), out var list) ? list : [];
            return Task.FromResult(rows.Select(GetId).ToList());
        }
    }

    // ---- fixture plumbing ----------------------------------------------------------------------------

    /// <summary>Every entity the fixture graphs reach, flattened per type and deduplicated by id.</summary>
    private static Dictionary<Type, List<object>> CollectEntities(params MenuGraphFixture.Fixture[] fixtures)
    {
        var byType = new Dictionary<Type, Dictionary<long, object>>();

        void Add(object? entity)
        {
            if (entity is null)
                return;

            var bucket = byType.TryGetValue(entity.GetType(), out var existing)
                ? existing
                : byType[entity.GetType()] = new Dictionary<long, object>();

            bucket[GetId(entity)] = entity;
        }

        foreach (var fixture in fixtures)
        {
            foreach (var variant in fixture.Variants)
            {
                Add(variant);
                Add(variant.Menu);
                Add(variant.Menu?.VehicleModel);

                foreach (var rate in variant.LabourRates) Add(rate);

                foreach (var period in variant.PeriodicAvailabilities)
                {
                    Add(period);
                    Add(period.ServiceInterval);
                    Add(period.ServiceInterval?.ServiceIntervalGroup);
                }

                foreach (var labour in variant.LabourDetails)
                {
                    Add(labour);
                    Add(labour.ServiceIntervalGroup);
                    foreach (var interval in labour.ServiceIntervalGroup?.ServiceIntervals ?? []) Add(interval);
                }

                foreach (var item in variant.Items)
                {
                    Add(item);

                    foreach (var part in item.Parts)
                    {
                        Add(part);
                        foreach (var price in part.CountryPrices) Add(price);
                    }

                    var link = item.ReplacementItemVehicleModel;
                    Add(link);

                    var replacementItem = link?.ReplacementItem;
                    Add(replacementItem);
                    Add(replacementItem?.StandaloneReplacementItemGroup);

                    foreach (var groupLink in replacementItem?.ReplacementItemServiceIntervalGroups ?? [])
                    {
                        Add(groupLink);
                        Add(groupLink.ServiceIntervalGroup);
                        foreach (var interval in groupLink.ServiceIntervalGroup?.ServiceIntervals ?? []) Add(interval);
                    }
                }
            }

            foreach (var mapping in fixture.LabourRateMappings.Values) Add(mapping);
            foreach (var mapping in fixture.BrandMappings.Values) Add(mapping);
        }

        return byType.ToDictionary(x => x.Key, x => x.Value.Values.ToList());
    }

    private static long GetId(object entity) =>
        (long)entity.GetType().GetProperty("ID")!.GetValue(entity)!;

    private static DateTimeOffset GetLastSaveDate(object entity) =>
        entity.GetType().GetProperty("LastSaveDate")?.GetValue(entity) is DateTimeOffset value ? value : default;

    /// <summary>Stamps LastSaveDate on every entity the fixture reaches — the watermark tests' clock.</summary>
    private static void StampAll(MenuGraphFixture.Fixture fixture, DateTimeOffset lastSaveDate)
    {
        foreach (var entity in CollectEntities(fixture).Values.SelectMany(x => x))
            Stamp(entity, lastSaveDate);
    }

    private static void Stamp(object entity, DateTimeOffset lastSaveDate)
    {
        var property = entity.GetType().GetProperty("LastSaveDate")
            ?? throw new InvalidOperationException($"{entity.GetType().Name} has no LastSaveDate.");

        (property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"{entity.GetType().Name}.LastSaveDate has no setter."))
            .Invoke(entity, [lastSaveDate]);
    }

    /// <summary>
    /// Re-keys a fixture's MENU GRAPH onto offset ids (master data keeps its ids and deduplicates on
    /// merge), optionally onto another basic model code — how the tests build a second variant or a
    /// second model beside the first.
    /// </summary>
    private static void OffsetGraph(MenuGraphFixture.Fixture fixture, long offset, string? basicModelCode = null)
    {
        foreach (var menu in fixture.Variants.Select(x => x.Menu).Distinct())
        {
            menu.ID += offset;

            if (basicModelCode is not null)
                menu.BasicModelCode = basicModelCode;
        }

        foreach (var variant in fixture.Variants)
        {
            variant.ID += offset;
            variant.MenuID = variant.Menu.ID;

            foreach (var rate in variant.LabourRates) { rate.ID += offset; rate.MenuVariantID = variant.ID; }
            foreach (var period in variant.PeriodicAvailabilities) { period.ID += offset; period.MenuVariantID = variant.ID; }
            foreach (var labour in variant.LabourDetails) { labour.ID += offset; labour.MenuVariantID = variant.ID; }

            foreach (var item in variant.Items)
            {
                item.ID += offset;
                item.MenuVariantID = variant.ID;

                foreach (var part in item.Parts)
                {
                    part.ID += offset;
                    part.MenuItemID = item.ID;

                    foreach (var price in part.CountryPrices) { price.ID += offset; price.MenuItemPartID = part.ID; }
                }
            }
        }
    }

    private static ServiceMenuDocuments MergedCosmosDocuments(params MenuGraphFixture.Fixture[] fixtures)
    {
        var documents = MenuCosmosDocumentFixture.From(fixtures[0]);

        foreach (var fixture in fixtures.Skip(1))
        {
            var more = MenuCosmosDocumentFixture.From(fixture);
            documents.Variants.AddRange(more.Variants);
            documents.Periods.AddRange(more.Periods);
            documents.Labours.AddRange(more.Labours);
            documents.Items.AddRange(more.Items);
        }

        return documents;
    }

    private static async Task<DuckDBConnection> SyncedStoreAsync(params MenuGraphFixture.Fixture[] fixtures)
    {
        var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();

        var result = await new StubSyncService(fixtures).SyncAllAsync(null!, connection);
        Assert.True(result.Succeeded);

        return connection;
    }

    private static ServiceMenuLookupService Lookup(IServiceMenuLookupStorageService storage) =>
        new(storage, new ServiceMenuGenerationEvaluator(Options.Create(new ServiceMenuLookupOptions())));

    private static ServiceMenuLookupRequest Request(string? basicModelCode = null) => new()
    {
        BasicModelCode = basicModelCode,
        CountryID = 2,
        Language = "en",
    };

    // ---- the differential claim ----------------------------------------------------------------------

    /// <summary>
    /// One entity graph, two storage pipelines, one menu: the Cosmos path embeds at write time
    /// (production mappers), the DuckDB path normalizes at write time and joins at read time — and
    /// the generated menus must be identical, codes, descriptions and money. Two variants (one free)
    /// so the comparison covers variant selection too.
    /// </summary>
    [Fact]
    public async Task OneGraph_TwoBackends_OneIdenticalMenu()
    {
        var paid = MenuGraphFixture.Build();
        var free = MenuGraphFixture.Build();
        OffsetGraph(free, 100_000);
        free.Variants.Single().IsFree = true;

        var viaCosmos = await Lookup(new StubCosmosService(_ => MergedCosmosDocuments(paid, free)))
            .GetMenuAsync(Request(MenuGraphFixture.BasicModelCode));

        // Guard against a vacuous pass: the expected side must actually be a generated menu — two
        // variants with lines and priced parts — before the Zip-based comparison means anything.
        Assert.False(viaCosmos.NotFound);
        Assert.Equal(2, viaCosmos.Variants.Count);
        Assert.All(viaCosmos.Variants, variant => Assert.NotEmpty(variant.PeriodicServices));
        Assert.Contains(
            viaCosmos.Variants.SelectMany(variant => variant.PeriodicServices.Concat(variant.StandaloneServices)),
            line => line.Parts.Count > 0);

        using var connection = await SyncedStoreAsync(paid, free);
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

    // ---- watermarks ----------------------------------------------------------------------------------

    /// <summary>
    /// The incremental contract, tiq-style: the FIRST run of every table is a full pull (no
    /// watermark), and the next run's pull starts from the DESTINATION's MAX(LastSaveDate) — no
    /// replication bookkeeping anywhere. An edit stamped later flows through the incremental run.
    /// </summary>
    [Fact]
    public async Task ASecondSync_PullsFromTheDestinationWatermark()
    {
        var fixture = MenuGraphFixture.Build();
        var t1 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        StampAll(fixture, t1);

        using var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();

        var firstSync = new StubSyncService(fixture);
        var first = await firstSync.SyncAllAsync(null!, connection);

        Assert.True(first.Succeeded);
        Assert.All(first.Tables, table => Assert.Null(table.Watermark));

        // An edit lands upstream after the first run.
        var variant = fixture.Variants.Single();
        variant.Name = "RENAMED";
        Stamp(variant, DateTimeOffset.Parse("2026-02-01T00:00:00Z"));

        var secondSync = new StubSyncService(fixture);
        var second = await secondSync.SyncAllAsync(null!, connection);

        Assert.True(second.Succeeded);
        Assert.All(second.Tables, table => Assert.Equal(t1, table.Watermark));
        Assert.Equal(t1, secondSync.ObservedWatermarks[nameof(MenuVariant)]);

        var documents = await new DuckDBServiceMenuLookupStorageService(connection)
            .GetMenuDocumentsAsync(MenuGraphFixture.BasicModelCode);

        Assert.Equal("RENAMED", documents.Variants.Single().VariantName);
        Assert.Single(documents.Variants);
    }

    /// <summary>
    /// Incremental pulls cannot see hard deletes (the row is simply gone from the source), so a FULL
    /// reload prunes rows whose ids left the source — the reconciler a host schedules periodically.
    /// </summary>
    [Fact]
    public async Task AFullReload_PrunesRowsTheSourceNoLongerHas()
    {
        var fixture = MenuGraphFixture.Build();

        using var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();

        var first = await new StubSyncService(fixture).SyncAllAsync(null!, connection);
        Assert.True(first.Succeeded);

        var variant = fixture.Variants.Single();
        var removed = variant.PeriodicAvailabilities.First();
        variant.PeriodicAvailabilities.Remove(removed);

        var second = await new StubSyncService(fixture).SyncAllAsync(null!, connection, fullReload: true);

        Assert.True(second.Succeeded);
        Assert.Equal(1, second.Tables.Single(x => x.Table == ServiceMenuDuckDBTables.MenuPeriodicAvailability).Pruned);

        var documents = await new DuckDBServiceMenuLookupStorageService(connection)
            .GetMenuDocumentsAsync(MenuGraphFixture.BasicModelCode);

        Assert.DoesNotContain(documents.Periods, x => x.id == removed.ID.ToString());
    }

    /// <summary>An empty source still leaves a synced, readable store — NotFound, not unprovisioned.</summary>
    [Fact]
    public async Task ASyncOfAnEmptySource_LeavesAReadableEmptyStore()
    {
        using var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();

        var result = await new StubSyncService().SyncAllAsync(null!, connection);
        Assert.True(result.Succeeded);

        var menu = await Lookup(new DuckDBServiceMenuLookupStorageService(connection))
            .GetMenuAsync(Request("ABC12"));

        Assert.True(menu.NotFound);
    }

    // ---- bulk ----------------------------------------------------------------------------------------

    [Fact]
    public async Task BulkRead_GroupsByCode_KeepsFirstAppearanceOrder_AndAnswersMissingCodesEmpty()
    {
        var first = MenuGraphFixture.Build();
        var second = MenuGraphFixture.Build();
        OffsetGraph(second, 200_000, basicModelCode: "ZZZ99");

        using var connection = await SyncedStoreAsync(first, second);
        var storage = new DuckDBServiceMenuLookupStorageService(connection);

        var results = await storage.GetMenuDocumentsAsync(
            new[] { "ZZZ99", MenuGraphFixture.BasicModelCode, "ZZZ99", "NOPE1" });

        Assert.Equal(3, results.Count);

        Assert.Equal("ZZZ99", results[0].BasicModelCode);
        Assert.Single(results[0].Variants);
        Assert.NotEmpty(results[0].Items);

        Assert.Equal(MenuGraphFixture.BasicModelCode, results[1].BasicModelCode);
        Assert.Single(results[1].Variants);
        Assert.NotEmpty(results[1].Periods);
        Assert.NotEmpty(results[1].Labours);

        Assert.Equal("NOPE1", results[2].BasicModelCode);
        Assert.True(results[2].IsEmpty);
    }

    [Fact]
    public async Task BulkLookup_OverDuckDB_FoldsEachCodeLikeTheSingleLookup()
    {
        using var connection = await SyncedStoreAsync(MenuGraphFixture.Build());

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

    [Fact]
    public void TheSyncRegistration_Resolves()
    {
        var services = new ServiceCollection();
        services.AddServiceMenuDuckDBSync();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ServiceMenuDuckDBSyncService>());
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
            using (var writeConnection = new DuckDBConnection($"DataSource={path}"))
            {
                writeConnection.Open();
                var result = await new StubSyncService(MenuGraphFixture.Build()).SyncAllAsync(null!, writeConnection);
                Assert.True(result.Succeeded);
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
