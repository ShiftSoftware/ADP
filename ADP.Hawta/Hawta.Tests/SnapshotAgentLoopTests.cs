using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Dispatcher semantics: cadences, failure isolation, cold-start rebuild, dark-launch mode,
/// scope adoption warnings. Uses file-backed stores in per-test temp directories (the loop
/// owns the store lifecycle) and a manual clock — no real waiting anywhere.
/// </summary>
public sealed class SnapshotAgentLoopTests : IDisposable
{
    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan by) => now += by;
    }

    private readonly string root = Path.Combine(Path.GetTempPath(), $"hawta-loop-{Guid.NewGuid():N}");
    private readonly ManualClock clock = new();
    private readonly List<SnapshotAgentEvent> events = [];

    private string WriteDbPath => Path.Combine(root, "write.duckdb");
    private string PublishDir => Path.Combine(root, "publish");

    public SnapshotAgentLoopTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch { /* Windows file-lock stragglers */ }
    }

    private static readonly SnapshotTableDefinition Table = new("Widget",
        [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Quantity", "INTEGER")]);

    /// <summary>An ingest delegate that stages whatever <paramref name="rows"/> currently returns.</summary>
    private static Func<SnapshotSourceContext, SnapshotMergeResult> IngestOf(
        string key, Func<IEnumerable<(string Key, string Code, int Quantity)>> rows, string? scope = null) => context =>
    {
        var staging = context.Store.CreateStagingTable(Table);
        foreach (var row in rows())
        {
            context.Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}, NULL
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                row.Key, row.Code, row.Quantity);
        }

        return SnapshotMerge.Execute(context.Store, Table, staging, new SnapshotMergeOptions
        {
            Source = key,
            SourceScope = scope,
            DeletesEnabled = true,
        });
    };

    private SnapshotAgentLoop Loop(SourceRegistry registry, bool dryRun = false, TimeSpan? publishCadence = null) =>
        new(new SnapshotAgentOptions
        {
            Registry = registry,
            WriteDatabasePath = WriteDbPath,
            PublishDirectory = PublishDir,
            SnapshotName = "agent-test",
            PublishCadence = publishCadence ?? TimeSpan.FromMinutes(1),
            DryRun = dryRun,
            TimeProvider = clock,
            OnEvent = events.Add,
        }, cosmosClient: null);

    [Fact]
    public async Task FirstCycle_RunsDueSources_AndPublishes_ThenIdlesUntilCadence()
    {
        var rows = new List<(string, string, int)> { ("W1", "alpha", 1), ("W2", "beta", 2) };
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "widgets",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(5),
                Ingest = IngestOf("widgets", () => rows),
            },
        ]);

        using var loop = Loop(registry);

        var first = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.True(first.GateAcquired);
        Assert.True(first.ColdStartRebuild);   // no write DB, nothing published yet — still a cold start
        var run = Assert.Single(first.Sources);
        Assert.Equal(2, run.Merge!.RowsInserted);
        Assert.Equal(SnapshotPublishStatus.Published, first.Publish!.Status);

        // A consumer can read what the cycle published, through the manifest.
        var published = PublishedSnapshot.ReadNewest(PublishDir, "agent-test");
        Assert.NotNull(published);
        var widget = published!.Tables.Single(t => t.Table == "Widget");

        using (var consumer = new DuckDB.NET.Data.DuckDBConnection("Data Source=:memory:"))
        {
            consumer.Open();
            using var command = consumer.CreateCommand();
            command.CommandText =
                $"SELECT count(*) FROM {widget.ReadParquetSql(PublishDir)} WHERE \"_Deleted\" = false";
            Assert.Equal(2L, Convert.ToInt64(command.ExecuteScalar()));
        }

        // Immediately after: nothing due, publish not due — the cycle is a no-op that never
        // touches the gate or the store.
        var second = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Same(SnapshotAgentCycle.Idle, second);

        // Publish cadence elapses first (1 min < 5 min source cadence): publish-only cycle,
        // and the unchanged store publishes as SkippedNoChanges.
        clock.Advance(TimeSpan.FromMinutes(1));
        var third = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Empty(third.Sources);
        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, third.Publish!.Status);

        // Source cadence elapses: the source runs again; unchanged rows = 0/0/0.
        clock.Advance(TimeSpan.FromMinutes(4));
        var fourth = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        var rerun = Assert.Single(fourth.Sources);
        Assert.Equal((0L, 0L, 0L), (rerun.Merge!.RowsInserted, rerun.Merge.RowsUpdated, rerun.Merge.RowsTombstoned));
    }

    [Fact]
    public async Task ACrashingSource_DoesNotStopTheOthers_AndRetriesNextCadence()
    {
        var attempts = 0;
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "broken",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = _ => { attempts++; throw new InvalidOperationException("source database unreachable"); },
            },
            new SnapshotSource
            {
                Key = "healthy",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = IngestOf("healthy", () => [("W1", "alpha", 1)]),
            },
        ]);

        using var loop = Loop(registry);
        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, cycle.Sources.Count);
        Assert.NotNull(cycle.Sources[0].Error);
        Assert.Null(cycle.Sources[0].Merge);
        Assert.Equal(1, cycle.Sources[1].Merge!.RowsInserted);
        Assert.Contains(events, e => e.Level == SnapshotAgentEventLevel.Error && e.SourceKey == "broken");

        // The crash still advanced the source's next-due time — no hot-looping a dead source.
        var again = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Empty(again.Sources);
        Assert.Equal(1, attempts);

        clock.Advance(TimeSpan.FromMinutes(1));
        await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ColdStart_RebuildsFromThePublishedSet()
    {
        var rows = new List<(string, string, int)> { ("W1", "alpha", 1), ("W2", "beta", 2), ("W3", "gamma", 3) };
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "widgets",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(5),
                Ingest = IngestOf("widgets", () => rows),
            },
        ]);

        using (var seed = Loop(registry))
            await seed.RunCycleAsync(TestContext.Current.CancellationToken);

        // The slot-swap story: new instance, empty local disk, published set is the seed.
        File.Delete(WriteDbPath);
        var wal = WriteDbPath + ".wal";
        if (File.Exists(wal)) File.Delete(wal);

        clock.Advance(TimeSpan.FromMinutes(10));
        using var fresh = Loop(registry);
        var cycle = await fresh.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.True(cycle.ColdStartRebuild);
        Assert.Contains(events, e => e.Message.Contains("Cold start: rebuilt 3 row(s)"));

        // The re-ingest against unchanged sources is 0/0/0 — the rebuild restored hashes.
        var run = Assert.Single(cycle.Sources);
        Assert.Equal((0L, 0L, 0L), (run.Merge!.RowsInserted, run.Merge.RowsUpdated, run.Merge.RowsTombstoned));
    }

    [Fact]
    public async Task SchemaMismatch_DeletesTheWriteDb_AndRebuildsFromThePublishedSet()
    {
        var rows = new List<(string, string, int)> { ("W1", "alpha", 1) };
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "widgets",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(5),
                Ingest = IngestOf("widgets", () => rows),
            },
        ]);

        using (var seed = Loop(registry))
            await seed.RunCycleAsync(TestContext.Current.CancellationToken);

        // Replace the write DB with one carrying a foreign schema version.
        File.Delete(WriteDbPath);
        using (SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = WriteDbPath, SchemaVersion = 999 }))
        {
        }

        clock.Advance(TimeSpan.FromMinutes(10));
        using var fresh = Loop(registry);
        var cycle = await fresh.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.True(cycle.ColdStartRebuild);
        Assert.Contains(events, e => e.Message.Contains("schema", StringComparison.OrdinalIgnoreCase)
                                  && e.Level == SnapshotAgentEventLevel.Warning);
        var run = Assert.Single(cycle.Sources);
        Assert.Equal(0L, run.Merge!.RowsInserted);
    }

    [Fact]
    public async Task DryRunMode_NeverPumps_EvenWithReplicatedTables()
    {
        IReadOnlyList<CosmosFamilyMapping> families =
        [
            new CosmosFamilyMapping
            {
                Family = "Widget",
                Database = "D",
                Container = "C",
                Map = row => new CosmosDocument
                {
                    Id = row.PrimaryKey,
                    PartitionKey = [row.PrimaryKey],
                    Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] },
                },
            },
        ];

        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "widgets",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(5),
                Ingest = IngestOf("widgets", () => [("W1", "alpha", 1)]),
                Families = families,
            },
        ]);

        // Wet mode without a client is a construction error…
        Assert.Throws<InvalidOperationException>(() => Loop(registry, dryRun: false));

        // …dark launch is not: ingest + publish happen, the pump does not.
        using var loop = Loop(registry, dryRun: true);
        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Single(cycle.Sources);
        Assert.Empty(cycle.Pumps);
        Assert.Equal(SnapshotPublishStatus.Published, cycle.Publish!.Status);
        Assert.Equal(1L, loop.Store!.CountDirtyRows(Table));   // nothing stamped — the queue is intact
    }

    [Fact]
    public async Task RunSourceOnce_IgnoresCadence_AndPublishes()
    {
        var rows = new List<(string, string, int)> { ("W1", "alpha", 1) };
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "widgets",
                Table = Table,
                Cadence = TimeSpan.FromHours(1),
                Ingest = IngestOf("widgets", () => rows),
            },
        ]);

        using var loop = Loop(registry);
        await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        rows.Add(("W2", "beta", 2));
        var forced = await loop.RunSourceOnceAsync("widgets", TestContext.Current.CancellationToken);

        var run = Assert.Single(forced.Sources);
        Assert.Equal(1L, run.Merge!.RowsInserted);
        Assert.Equal(SnapshotPublishStatus.Published, forced.Publish!.Status);
    }

    [Fact]
    public async Task CrossScopeAdoption_RaisesTheChurnWarning()
    {
        IReadOnlyList<CosmosFamilyMapping>? sharedFamilies = null;
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "scope-a",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = IngestOf("scope-a", () => [("K", "alpha", 1)], scope: "A"),
                Families = sharedFamilies,
            },
            new SnapshotSource
            {
                Key = "scope-b",
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = IngestOf("scope-b", () => [("K", "alpha", 1)], scope: "B"),
                Families = sharedFamilies,
            },
        ]);

        using var loop = Loop(registry);
        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        // Registry order: A inserts K, then B adopts it.
        Assert.Equal(1L, cycle.Sources[0].Merge!.RowsInserted);
        Assert.Equal(1L, cycle.Sources[1].Merge!.RowsRescoped);
        Assert.Contains(events, e => e.SourceKey == "scope-b" && e.Message.Contains("_SourceScope"));
    }
}
