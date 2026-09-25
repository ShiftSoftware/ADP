using System.Collections.Concurrent;
using System.Net;
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

    private SnapshotAgentLoop Loop(
        SourceRegistry registry,
        bool dryRun = false,
        TimeSpan? publishCadence = null,
        int ingestDegree = 1,
        SnapshotRunLogOptions? runLog = null,
        ICosmosSnapshotTransport? transport = null) =>
        new(new SnapshotAgentOptions
        {
            Registry = registry,
            WriteDatabasePath = WriteDbPath,
            PublishDirectory = PublishDir,
            SnapshotName = "agent-test",
            PublishCadence = publishCadence ?? TimeSpan.FromMinutes(1),
            DryRun = dryRun,
            IngestDegree = ingestDegree,
            TimeProvider = clock,
            OnEvent = events.Add,
            RunLog = runLog,
            ReplicationTransport = transport,
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
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
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
                SourceScope = "broken",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = _ => { attempts++; throw new InvalidOperationException("source database unreachable"); },
            },
            new SnapshotSource
            {
                Key = "healthy",
                SourceScope = "healthy",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = IngestOf("healthy", () => [("W1", "alpha", 1)], scope: "healthy"),
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

    // ---- The bounded ingest fan-out ------------------------------------------------------------

    /// <summary>A two-phase source: the fetch materialises rows off the store, the drain merges them.</summary>
    private static SnapshotSource FetchingSource(
        string key,
        Func<IEnumerable<(string Key, string Code, int Quantity)>> rows,
        Exception? fetchThrows = null) => new()
        {
            Key = key,
            SourceScope = key,
            ConcurrencyGroup = "one-box",
            RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
            Table = Table,
            Cadence = TimeSpan.FromMinutes(1),
            Fetch = _ =>
            {
                if (fetchThrows is not null)
                    throw fetchThrows;

                var buffered = rows().ToList();
                return SnapshotSourceFetch.Staged(
                    buffered.Count, context => IngestOf(key, () => buffered, scope: key)(context));
            },
        };

    [Fact]
    public async Task FanningOutTheFetches_ChangesNothingTheCycleReports()
    {
        // Everything the loop folds — run records, warnings, cadence state — is written by the
        // serial drain, so a fanned-out cycle has to be indistinguishable from a serial one apart
        // from its wall clock.
        var registry = new SourceRegistry(Enumerable.Range(0, 6)
            .Select(index => FetchingSource($"s{index}", () => [($"W{index}", "alpha", index)]))
            .ToList());

        using var loop = Loop(registry, ingestDegree: 4);
        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        // Registry order, one run each, every row landed.
        Assert.Equal(["s0", "s1", "s2", "s3", "s4", "s5"], cycle.Sources.Select(run => run.SourceKey));
        Assert.All(cycle.Sources, run => Assert.Equal(1, run.Merge!.RowsInserted));
        Assert.Equal(SnapshotPublishStatus.Published, cycle.Publish!.Status);

        // Cadence state advanced for every source — nextDue is a loop FIELD, and a lost write
        // there is permanent for the process rather than for the cycle.
        Assert.Same(SnapshotAgentCycle.Idle, await loop.RunCycleAsync(TestContext.Current.CancellationToken));

        Assert.Contains(events, e => e.Message.Contains("Ingest fan-out: 6 source(s) fetched ahead"));
    }

    [Fact]
    public async Task ACrashingFetch_IsContained_AndStillAdvancesItsCadence()
    {
        var registry = new SourceRegistry(
        [
            FetchingSource("broken", () => [], new InvalidOperationException("dealer box unreachable")),
            FetchingSource("healthy", () => [("W1", "alpha", 1)]),
        ]);

        using var loop = Loop(registry, ingestDegree: 2);
        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, cycle.Sources.Count);
        Assert.Equal("dealer box unreachable", cycle.Sources[0].Error!.Message);
        Assert.Null(cycle.Sources[0].Merge);
        Assert.Equal(1, cycle.Sources[1].Merge!.RowsInserted);
        Assert.Contains(events, e => e is { Level: SnapshotAgentEventLevel.Error, SourceKey: "broken" });

        // No hot-looping a dead source, exactly as a crashing one-phase source behaves.
        Assert.Same(SnapshotAgentCycle.Idle, await loop.RunCycleAsync(TestContext.Current.CancellationToken));

        // The loop is idle now, so its store may be read. The crash is a run record, not silence,
        // and the cycle still counts the source as failed rather than as run.
        var store = loop.Store!;
        Assert.Equal("Failed:Fetch", store.ExecuteScalar("SELECT \"Status\" FROM meta.SyncRuns WHERE \"Source\" = 'broken'"));
        Assert.Equal("dealer box unreachable", store.ExecuteScalar("SELECT \"Error\" FROM meta.SyncRuns WHERE \"Source\" = 'broken'"));
        Assert.Equal(1, Convert.ToInt32(store.ExecuteScalar("SELECT \"SourcesRun\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", cycle.CycleId)));
        Assert.Equal(1, Convert.ToInt32(store.ExecuteScalar("SELECT \"SourcesFailed\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", cycle.CycleId)));

        // Both reads are timed, each linked to its source's run record.
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.FetchRuns f JOIN meta.SyncRuns s USING (\"RunId\") WHERE f.\"Source\" = s.\"Source\"")));
        Assert.Equal(true, store.ExecuteScalar("SELECT \"Failed\" FROM meta.FetchRuns WHERE \"Source\" = 'broken'"));
        Assert.Equal(false, store.ExecuteScalar("SELECT \"Failed\" FROM meta.FetchRuns WHERE \"Source\" = 'healthy'"));
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
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
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
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
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
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
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
        Assert.Equal(0L, Convert.ToInt64(loop.Store.ExecuteScalar("SELECT count(*) FROM meta.PumpRuns")));   // and no drain to record
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
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
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
                SourceScope = "A",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = IngestOf("scope-a", () => [("K", "alpha", 1)], scope: "A"),
                Families = sharedFamilies,
            },
            new SnapshotSource
            {
                Key = "scope-b",
                SourceScope = "B",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
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

    // ---- Asynchronous sources, and the Cosmos-read startup guard -------------------------------

    private static SnapshotSource CosmosReadingSource(
        string key,
        Func<IEnumerable<(string Key, string Code, int Quantity)>> rows,
        CosmosSourceRead? cosmosRead = null) => new()
        {
            Key = key,
            RecordIdentity = SourceRecordIdentityDescriptor.DatabaseKey("Code"),
            Table = Table,
            Cadence = TimeSpan.FromMinutes(5),
            CosmosRead = cosmosRead ?? new CosmosSourceRead("Logs", "SSC"),
            // Genuinely asynchronous: it yields before staging, the way a network read does.
            IngestAsync = async context =>
            {
                await Task.Yield();
                return IngestOf(key, rows)(context);
            },
        };

    [Fact]
    public async Task AnAsyncSource_RunsAndPublishesLikeAnyOther()
    {
        var registry = new SourceRegistry(
            [CosmosReadingSource("cosmos-widgets", () => [("W1", "alpha", 1), ("W2", "beta", 2)])]);

        // The guard below demands a client for a Cosmos-reading source; this is the shape a host
        // with a configured endpoint has. It is never connected to.
        using var client = new Microsoft.Azure.Cosmos.CosmosClient(
            "AccountEndpoint=https://localhost:8081/;AccountKey=" +
            Convert.ToBase64String("startup-guard-only-never-connects"u8.ToArray()) + ";");
        using var loop = new SnapshotAgentLoop(
            new SnapshotAgentOptions
            {
                Registry = registry,
                WriteDatabasePath = WriteDbPath,
                PublishDirectory = PublishDir,
                SnapshotName = "agent-test",
                TimeProvider = clock,
                OnEvent = events.Add,
                DryRun = true,
            },
            client);

        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2L, cycle.Sources.Single().Merge!.RowsInserted);
        Assert.Equal(SnapshotPublishStatus.Published, cycle.Publish!.Status);
    }

    [Fact]
    public void ACosmosReadingSource_WithNoClient_FailsAtStartup()
    {
        // Never per cadence tick. Without this the host starts clean, throws on every tick, and
        // publishes a well-formed EMPTY table that a consumer cannot tell from "no rows today" —
        // and for this table, absent rows mean "the dealer did not do it".
        var registry = new SourceRegistry([CosmosReadingSource("cosmos-widgets", () => [])]);

        var exception = Assert.Throws<InvalidOperationException>(() => Loop(registry));

        Assert.Contains("Cosmos-reading source(s) require a CosmosClient", exception.Message);
        Assert.Contains("'cosmos-widgets' (Logs/SSC)", exception.Message);
    }

    [Fact]
    public void DryRun_DoesNotExcuseAMissingClientForACosmosReadingSource()
    {
        // DryRun darkens the PUMP. A read-only source still has to reach its container.
        var registry = new SourceRegistry([CosmosReadingSource("cosmos-widgets", () => [])]);

        Assert.Throws<InvalidOperationException>(() => Loop(registry, dryRun: true));
    }

    // ---- The warm-reopen replication flip -----------------------------------------------------
    // The load/skip decision runs ONLY at cold start. A table deferred with a recorded backlog
    // while replication was OFF stays Deferred through a WARM restart whose configuration turns
    // replication ON — the pump drains an empty table every cycle, reporting clean, while the
    // owed rows sit in the published copy indefinitely. The loop cannot fix the state safely;
    // it must refuse to be quiet about it.

    private static readonly CosmosFamilyMapping FlipFamily = new()
    {
        Family = "Widget",
        Database = "TestDb",
        Container = "Widgets",
        Map = row => new CosmosDocument
        {
            Id = row.PrimaryKey,
            PartitionKey = [row.PrimaryKey],
            Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] },
        },
    };

    private FileSnapshotIngestorOptions FlipFeedOptions(string feedPath, SourceChangeGate gate) => new()
    {
        Table = Table,
        FilePath = feedPath,
        LogicalKey = FileLogicalKey.Single("Code"),
        ChangeGate = gate,
        MergeOptions = new SnapshotMergeOptions { Source = "file-widget", DeletesEnabled = true },
    };

    private SourceRegistry FlipRegistry(FileSnapshotIngestorOptions options, bool replicationEnabled) =>
        new(
        [
            new SnapshotSource
            {
                Key = "file-widget",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = context => FileSnapshotIngestor.Ingest(context.Store, options, context.FileMetadata),
                FileIngestion = options,
                Families = [FlipFamily],
                ReplicationEnabled = replicationEnabled,
            },
        ]);

    /// <summary>Ingest + publish on a throwaway primary store so the loop has a seed to cold-start from.</summary>
    private void SeedPublishedSet(FileSnapshotIngestorOptions options, SourceRegistry registry, bool settle)
    {
        using var primary = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        primary.EnsureTable(Table);
        Assert.True(FileSnapshotIngestor.Ingest(
            primary, options, new DirectoryListingFileMetadataProbe()).Succeeded);
        if (settle)
        {
            foreach (var row in primary.ReadDirtyRows(Table))
                primary.MarkReplicated(Table, row.PrimaryKey, row.CapturedLastModified, "{\"id\":\"x\"}");
        }

        Assert.Equal(SnapshotPublishStatus.Published, SnapshotPublisher.Publish(primary, new SnapshotPublishOptions
        {
            PublishDirectory = PublishDir,
            SnapshotName = "agent-test",
            Tables = registry.Tables,
            Sources = registry.Sources,
        }).Status);
    }

    [Fact]
    public async Task AWarmReplicationFlip_OnADeferredBackloggedTable_WarnsLoudly_EveryCycle()
    {
        var feed = Path.Combine(root, "widgets.csv");
        File.WriteAllText(feed, "Code,Quantity\nA,1\nB,2\n");
        var options = FlipFeedOptions(feed, new SourceChangeGate());
        SeedPublishedSet(options, FlipRegistry(options, replicationEnabled: false), settle: false);

        // Cold start with replication OFF: pending is 2, but nothing is enabled, so the table
        // defers — correctly — with the owed count recorded. No warning yet.
        using (var cold = Loop(FlipRegistry(options, replicationEnabled: false)))
            await cold.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(events, e => e.Message.Contains("owed to the replication pump"));

        // The flip ships as a bare config edit on a warm instance: the write DB survives, so
        // no cold start runs and nothing re-evaluates the skip. DryRun here is the dark-launch
        // posture — the warning must fire whether or not the pump actually runs.
        events.Clear();
        using var warm = Loop(FlipRegistry(options, replicationEnabled: true), dryRun: true);
        await warm.RunCycleAsync(TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(2));
        await warm.RunCycleAsync(TestContext.Current.CancellationToken);

        var warnings = events.Where(e =>
            e.Level == SnapshotAgentEventLevel.Warning
            && e.Message.Contains("owed to the replication pump")).ToList();
        Assert.Equal(2, warnings.Count); // every cycle, deliberately — this state is never fine
        Assert.Contains("Cold-start the agent", warnings[0].Message);
        Assert.Contains("2 row(s)", warnings[0].Message);
    }

    [Fact]
    public async Task TheFlipWarning_StaysQuiet_WhenNothingIsOwed()
    {
        var feed = Path.Combine(root, "widgets.csv");
        File.WriteAllText(feed, "Code,Quantity\nA,1\nB,2\n");
        var options = FlipFeedOptions(feed, new SourceChangeGate());
        SeedPublishedSet(options, FlipRegistry(options, replicationEnabled: false), settle: true);

        // Same flip, but the published copy owes nothing (pending 0): deferral is exactly
        // right and the warm flip changes nothing — the warning must not cry wolf.
        using (var cold = Loop(FlipRegistry(options, replicationEnabled: false)))
            await cold.RunCycleAsync(TestContext.Current.CancellationToken);
        events.Clear();

        using var warm = Loop(FlipRegistry(options, replicationEnabled: true), dryRun: true);
        await warm.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(events, e => e.Message.Contains("owed to the replication pump"));
    }

    // ---- The wipe-on-start posture ------------------------------------------------------------
    // Production hosts delete the write DB at every process start, so every production start
    // is a cold start and the warm/cold asymmetry (the flip trap above) disappears there.
    // These prove the boot sequence that policy depends on: wiped boots converge (the second
    // one defers again), and a boot that cannot reach the publish tier fails CLEAN — no
    // half-started empty estate left behind — then completes the full cold start when the
    // tier returns.

    [Fact]
    public async Task WipeOnStart_TwoSuccessiveBoots_TheSecondDefersAgain()
    {
        var feed = Path.Combine(root, "widgets.csv");
        File.WriteAllText(feed, "Code,Quantity\nA,1\nB,2\n");
        var options = FlipFeedOptions(feed, new SourceChangeGate());
        var registry = FlipRegistry(options, replicationEnabled: false);
        SeedPublishedSet(options, registry, settle: true);

        for (var boot = 1; boot <= 2; boot++)
        {
            // The wipe: what a production host does before starting the loop.
            if (File.Exists(WriteDbPath + ".wal")) File.Delete(WriteDbPath + ".wal");
            if (File.Exists(WriteDbPath)) File.Delete(WriteDbPath);

            events.Clear();
            using (var loop = Loop(registry))
            {
                var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
                Assert.True(cycle.ColdStartRebuild);
                Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, cycle.Sources.Single().Merge!.Status);
                Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, cycle.Publish!.Status);
            }

            Assert.Contains(events, e => e.Message.Contains("stay deferred to the published copy"));
            Assert.DoesNotContain(events, e => e.Level == SnapshotAgentEventLevel.Error);

            using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = WriteDbPath });
            Assert.Equal(SnapshotResidency.Deferred, store.ReadResidency("Widget"));
        }
    }

    /// <summary>Delegates to a real local store; EnsureReady throws while the outage is armed.</summary>
    private sealed class OutageStore(PublishStore inner) : PublishStore
    {
        public bool Armed { get; set; } = true;

        public override string Root => inner.Root;
        public override void EnsureReady()
        {
            if (Armed)
                throw new IOException("The publish tier is unreachable (simulated outage).");
            inner.EnsureReady();
        }

        public override IReadOnlyList<PublishEntry> List(string? relativePrefix = null) => inner.List(relativePrefix);
        public override bool Exists(string location) => inner.Exists(location);
        public override string ReadAllText(string location) => inner.ReadAllText(location);
        public override void WriteAllText(string location, string content) => inner.WriteAllText(location, content);
        public override PendingCommit PrepareCommit(string location, string content) => inner.PrepareCommit(location, content);
        public override bool Commit(PendingCommit pending) => inner.Commit(pending);
        public override bool Delete(string location) => inner.Delete(location);
        public override DateTime? LastWriteUtc(string location) => inner.LastWriteUtc(location);
        public override bool BulkWriteNeedsStaging => inner.BulkWriteNeedsStaging;
        public override void PromoteStaged(string stagingLocation, string finalLocation) => inner.PromoteStaged(stagingLocation, finalLocation);
        public override void EnsureFolderFor(string location) => inner.EnsureFolderFor(location);
    }

    [Fact]
    public async Task ABootWithTheTierUnreachable_FailsClean_ThenCompletesTheFullColdStart()
    {
        var feed = Path.Combine(root, "widgets.csv");
        File.WriteAllText(feed, "Code,Quantity\nA,1\nB,2\n");
        var options = FlipFeedOptions(feed, new SourceChangeGate());
        var registry = FlipRegistry(options, replicationEnabled: false);
        SeedPublishedSet(options, registry, settle: true);

        var outage = new OutageStore(new LocalPublishStore(PublishDir));
        using var loop = new SnapshotAgentLoop(new SnapshotAgentOptions
        {
            Registry = registry,
            WriteDatabasePath = WriteDbPath,
            PublishDirectory = PublishDir,
            PublishStore = outage,
            SnapshotName = "agent-test",
            TimeProvider = clock,
            OnEvent = events.Add,
        }, cosmosClient: null);

        // Boot while the tier is down: the cycle fails at store level — and leaves NOTHING.
        // A surviving empty write DB would make the next cycle a "warm" open over an empty
        // all-resident estate, whose first publish would paper the real set with empties.
        await Assert.ThrowsAsync<IOException>(() => loop.RunCycleAsync(TestContext.Current.CancellationToken));
        Assert.False(File.Exists(WriteDbPath));

        // The tier returns: the SAME loop's next cycle performs the complete cold start.
        outage.Armed = false;
        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.True(cycle.ColdStartRebuild);
        Assert.Contains(events, e => e.Message.Contains("stay deferred to the published copy"));
        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, cycle.Publish!.Status);
    }

    // ---- The run log inside the loop -----------------------------------------------------------

    private string RunLogDir => Path.Combine(root, "run-log");

    private SourceRegistry OneWidgetSource(TimeSpan cadence, Func<IEnumerable<(string, string, int)>>? rows = null) => new(
    [
        new SnapshotSource
        {
            Key = "widgets",
            RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
            Table = Table,
            Cadence = cadence,
            Ingest = IngestOf("widgets", rows ?? (() => [("W1", "alpha", 1)])),
        },
    ]);

    private static long Count(string parquet)
    {
        using var reader = new DuckDB.NET.Data.DuckDBConnection("Data Source=:memory:");
        reader.Open();
        using var command = reader.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM read_parquet('{parquet.Replace('\\', '/').Replace("'", "''")}')";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private int RunLogWrites => events.Count(e => e.Level == SnapshotAgentEventLevel.Info && e.Message.StartsWith("Run log:"));

    [Fact]
    public async Task RunLog_FlushesAtItsCadence_AndNotOnEveryCycle()
    {
        var registry = OneWidgetSource(TimeSpan.FromMinutes(1));
        using var loop = Loop(registry, runLog: new SnapshotRunLogOptions
        {
            Store = new LocalPublishStore(RunLogDir),
            Cadence = TimeSpan.FromMinutes(5),
            HostInstance = "instance-1",
        });

        // The first cycle after a boot flushes at once: the source run, the publish and the
        // cycle itself each land under their own folder, named by the boot.
        var first = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(first.RunLog);
        Assert.False(first.RunLog!.Skipped);
        Assert.Equal(3, first.RunLog.FilesWritten.Count);
        var syncRuns = Assert.Single(first.RunLog.FilesWritten, f => f.Contains(SnapshotRunLog.SyncRunsFolder));
        var publishRuns = Assert.Single(first.RunLog.FilesWritten, f => f.Contains(SnapshotRunLog.PublishRunsFolder));
        var cycleRuns = Assert.Single(first.RunLog.FilesWritten, f => f.Contains(SnapshotRunLog.CycleRunsFolder));
        Assert.All(first.RunLog.FilesWritten, f => Assert.Equal($"{loop.BootId}.parquet", Path.GetFileName(f)));
        Assert.Equal(1, Count(syncRuns));
        Assert.Equal(1, Count(publishRuns));
        Assert.Equal(1, Count(cycleRuns));
        Assert.Equal(1, RunLogWrites);

        // One minute later the source runs again, but the run log is not due: nothing is written
        // even though there is something new to write.
        clock.Advance(TimeSpan.FromMinutes(1));
        var second = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Single(second.Sources);
        Assert.Null(second.RunLog);
        Assert.Equal(1, Count(syncRuns));
        Assert.Equal(1, RunLogWrites);

        // At the cadence it is due, and the day file is rewritten with every row of the day so far.
        clock.Advance(TimeSpan.FromMinutes(4));
        var third = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(third.RunLog);
        Assert.False(third.RunLog!.Skipped);
        Assert.Equal(3, Count(syncRuns));
        Assert.Equal(3, Count(cycleRuns));
        Assert.Equal(2, RunLogWrites);
    }

    [Fact]
    public async Task RunLog_RewritesOnlyTheTablesThatGainedRows_AndSkipsWhenNothingIsNew()
    {
        // A quiet estate: the source's cadence is long, so the cycles between are publish-only,
        // and an unchanged set writes no publish run. Only the cycle table gains a row per cycle.
        var registry = OneWidgetSource(TimeSpan.FromMinutes(30));
        using var loop = Loop(registry, runLog: new SnapshotRunLogOptions
        {
            Store = new LocalPublishStore(RunLogDir),
            Cadence = TimeSpan.FromMinutes(5),
        });

        var first = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        var syncRuns = Assert.Single(first.RunLog!.FilesWritten, f => f.Contains(SnapshotRunLog.SyncRunsFolder));
        var publishRuns = Assert.Single(first.RunLog.FilesWritten, f => f.Contains(SnapshotRunLog.PublishRunsFolder));
        var syncRunsWritten = File.GetLastWriteTimeUtc(syncRuns);
        var publishRunsWritten = File.GetLastWriteTimeUtc(publishRuns);

        clock.Advance(TimeSpan.FromMinutes(5));
        var second = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, second.Publish!.Status);
        Assert.NotNull(second.RunLog);
        Assert.False(second.RunLog!.Skipped);
        // The source and publish files gained nothing and were not touched; the cycle file was.
        Assert.Single(second.RunLog.FilesWritten);
        Assert.Contains(SnapshotRunLog.CycleRunsFolder, second.RunLog.FilesWritten[0]);
        Assert.Equal(syncRunsWritten, File.GetLastWriteTimeUtc(syncRuns));
        Assert.Equal(publishRunsWritten, File.GetLastWriteTimeUtc(publishRuns));

        // With nothing new at all, a flush writes nothing: the shutdown flush straight after a
        // cadence flush is exactly that case.
        var writesBefore = RunLogWrites;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await loop.RunAsync(cancelled.Token);
        Assert.Equal(writesBefore, RunLogWrites);
        Assert.DoesNotContain(events, e => e.Level == SnapshotAgentEventLevel.Warning && e.Message.StartsWith("Run log:"));
    }

    [Fact]
    public async Task RunLog_StoppingTheLoop_FlushesTheTailOnce()
    {
        var registry = OneWidgetSource(TimeSpan.FromMinutes(1));
        using var loop = Loop(registry, runLog: new SnapshotRunLogOptions
        {
            Store = new LocalPublishStore(RunLogDir),
            Cadence = TimeSpan.FromMinutes(5),
        });

        var first = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        var syncRuns = Assert.Single(first.RunLog!.FilesWritten, f => f.Contains(SnapshotRunLog.SyncRunsFolder));

        // A second cycle adds a run the cadence has not flushed yet: the tail.
        clock.Advance(TimeSpan.FromMinutes(1));
        var second = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Null(second.RunLog);
        Assert.Equal(1, Count(syncRuns));

        // The loop is asked to stop: it runs no cycle and flushes the tail on its way out.
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await loop.RunAsync(cancelled.Token);

        Assert.Equal(2, Count(syncRuns));
        Assert.Equal(2, RunLogWrites);
        Assert.Contains(events, e => e.Message.StartsWith("Run log:") && e.Message.Contains("(shutdown)"));

        // Stopping again writes nothing more: the tail was flushed once.
        await loop.RunAsync(cancelled.Token);
        Assert.Equal(2, RunLogWrites);
    }

    [Fact]
    public async Task RunLog_AnUnusableDestination_IsAWarning_AndTheCycleStillCounts()
    {
        // A file sitting where the run-log directory should be: creating the directory fails, so
        // every flush fails. The agent must not care beyond saying so.
        var blocked = Path.Combine(root, "blocked-run-log");
        File.WriteAllText(blocked, "not a directory");
        var registry = OneWidgetSource(TimeSpan.FromMinutes(1));
        using var loop = Loop(registry, runLog: new SnapshotRunLogOptions
        {
            Store = new LocalPublishStore(blocked),
            Cadence = TimeSpan.FromMinutes(5),
        });

        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.True(cycle.GateAcquired);
        Assert.Single(cycle.Sources);
        Assert.Equal(SnapshotPublishStatus.Published, cycle.Publish!.Status);
        Assert.Null(cycle.RunLog);
        var warning = Assert.Single(events, e => e.Level == SnapshotAgentEventLevel.Warning && e.Message.StartsWith("Run log:"));
        Assert.Contains(blocked, warning.Message);
        Assert.NotNull(warning.Exception);
        Assert.DoesNotContain(events, e => e.Level == SnapshotAgentEventLevel.Error);

        // Not retried on the very next cycle: the warning repeats at the cadence, not per cycle.
        clock.Advance(TimeSpan.FromMinutes(1));
        await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Single(events, e => e.Level == SnapshotAgentEventLevel.Warning && e.Message.StartsWith("Run log:"));

        clock.Advance(TimeSpan.FromMinutes(4));
        await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, events.Count(e => e.Level == SnapshotAgentEventLevel.Warning && e.Message.StartsWith("Run log:")));

        // The loop still reports itself serviceable when driven the production way.
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await loop.RunAsync(cancelled.Token);
        Assert.Equal(3, events.Count(e => e.Level == SnapshotAgentEventLevel.Warning && e.Message.StartsWith("Run log:")));
    }

    [Fact]
    public async Task EveryGatedCycle_RecordsARow_WithTheCountsTheCycleCarries()
    {
        var attempts = 0;
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "widgets",
                SourceScope = "widgets",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = IngestOf("widgets", () => [("W1", "alpha", 1)], scope: "widgets"),
            },
            new SnapshotSource
            {
                Key = "broken",
                SourceScope = "broken",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = _ => { attempts++; throw new InvalidOperationException("unreachable"); },
            },
        ]);
        using var loop = Loop(registry);

        var first = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(1));
        var second = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(first.CycleId);
        Assert.StartsWith(loop.BootId, first.CycleId);
        Assert.NotEqual(first.CycleId, second.CycleId);

        // The loop is idle now, so its store may be read: one row per cycle, facts only.
        var store = loop.Store!;
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.CycleRuns")));
        Assert.Equal("Ran", store.ExecuteScalar("SELECT \"Outcome\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId));
        Assert.Equal(true, store.ExecuteScalar("SELECT \"ColdStart\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId));
        Assert.Equal(false, store.ExecuteScalar("SELECT \"ColdStart\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", second.CycleId));
        Assert.Equal(2, Convert.ToInt32(store.ExecuteScalar("SELECT \"SourcesDue\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId)));
        Assert.Equal(1, Convert.ToInt32(store.ExecuteScalar("SELECT \"SourcesRun\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId)));
        Assert.Equal(1, Convert.ToInt32(store.ExecuteScalar("SELECT \"SourcesFailed\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId)));
        Assert.Equal("Published", store.ExecuteScalar("SELECT \"PublishStatus\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId));
        Assert.Equal(first.Publish!.PublishId, store.ExecuteScalar("SELECT \"PublishId\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId));
        Assert.Equal("SkippedNoChanges", store.ExecuteScalar("SELECT \"PublishStatus\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", second.CycleId));
        Assert.Equal(0, Convert.ToInt32(store.ExecuteScalar("SELECT \"PumpTables\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", first.CycleId)));
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.CycleRuns WHERE \"FinishedAt\" >= \"StartedAt\" AND \"Error\" IS NULL")));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ACycleThatFailsAtStoreLevel_RecordsAFailedRow_WhenTheStoreIsOpen()
    {
        var registry = OneWidgetSource(TimeSpan.FromMinutes(1));
        using var loop = Loop(registry);
        await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        // Force the next cycle to fail after the store is open: the publish directory becomes a
        // file, so the publisher throws at store level and the loop's caller sees the exception.
        clock.Advance(TimeSpan.FromMinutes(1));
        Directory.Delete(PublishDir, recursive: true);
        File.WriteAllText(PublishDir, "not a directory");
        await Assert.ThrowsAnyAsync<Exception>(() => loop.RunCycleAsync(TestContext.Current.CancellationToken));

        var store = loop.Store!;
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.CycleRuns")));
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.CycleRuns WHERE \"Outcome\" = 'Failed' AND \"Error\" IS NOT NULL AND \"SourcesRun\" = 1")));
    }

    // ---- The pump's own rows, with no Cosmos anywhere ------------------------------------------

    /// <summary>An in-memory container, so a wet cycle can pump without a Cosmos account or emulator.</summary>
    private sealed class InMemoryTransport : ICosmosSnapshotTransport
    {
        private readonly InMemoryContainer container = new();

        public int Documents => container.Documents.Count;

        public ICosmosSnapshotContainer GetContainer(string database, string containerName) => container;

        private sealed class InMemoryContainer : ICosmosSnapshotContainer
        {
            public ConcurrentDictionary<string, CosmosDocument> Documents { get; } = new(StringComparer.Ordinal);

            public Task<CosmosTransportResponse> UpsertAsync(CosmosDocument document, CancellationToken cancellationToken)
            {
                Documents[document.Id] = document;
                return Task.FromResult(new CosmosTransportResponse(HttpStatusCode.OK, true, 1, RetryAfter: null, ErrorMessage: null));
            }

            public Task<CosmosTransportResponse> DeleteAsync(string id, IReadOnlyList<object?> partitionKey, CancellationToken cancellationToken)
            {
                Documents.TryRemove(id, out _);
                return Task.FromResult(new CosmosTransportResponse(HttpStatusCode.NoContent, true, 1, RetryAfter: null, ErrorMessage: null));
            }
        }
    }

    private static readonly IReadOnlyList<CosmosFamilyMapping> WidgetFamily =
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

    [Fact]
    public async Task AWetCycle_RecordsOnePumpRowPerTable_AndTheRunLogCopiesIt()
    {
        var transport = new InMemoryTransport();
        var registry = new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "widgets",
                RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
                Table = Table,
                Cadence = TimeSpan.FromMinutes(1),
                Ingest = IngestOf("widgets", () => [("W1", "alpha", 1), ("W2", "beta", 2)]),
                Families = WidgetFamily,
            },
        ]);
        using var loop = Loop(registry, transport: transport, runLog: new SnapshotRunLogOptions
        {
            Store = new LocalPublishStore(RunLogDir),
            Cadence = TimeSpan.FromMinutes(5),
        });

        var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        // The pump ran through the in-memory container and reported the drain as facts, timed.
        var pump = Assert.Single(cycle.Pumps);
        Assert.Equal("Widget", pump.Table);
        Assert.Equal(2, pump.RowsRead);
        Assert.Equal(2, pump.Upserted);
        Assert.True(pump.Drained);
        Assert.Equal("QueueEmpty", pump.StopReason);
        Assert.True(pump.StartedAt >= loop.BootStartedAt, "the drain's start time is missing");
        Assert.True(pump.FinishedAt >= pump.StartedAt, "the drain's finish time is missing");
        Assert.Equal(2, transport.Documents);

        // One row per pumped table in the write DB, keyed by the cycle, carrying the same facts;
        // the cycle row carries their sum.
        var store = loop.Store!;
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.PumpRuns")));
        Assert.Equal(cycle.CycleId, store.ExecuteScalar("SELECT \"CycleId\" FROM meta.PumpRuns WHERE \"Table\" = 'Widget'"));
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar("SELECT \"Upserted\" FROM meta.PumpRuns")));
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar("SELECT \"RemoteAttemptedRows\" FROM meta.PumpRuns")));
        Assert.Equal("QueueEmpty", store.ExecuteScalar("SELECT \"StopReason\" FROM meta.PumpRuns"));
        Assert.Equal(true, store.ExecuteScalar("SELECT \"Drained\" FROM meta.PumpRuns"));
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.PumpRuns WHERE \"FinishedAt\" >= \"StartedAt\"")));
        Assert.Equal(1, Convert.ToInt32(store.ExecuteScalar("SELECT \"PumpTables\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", cycle.CycleId)));
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar("SELECT \"PumpUpserted\" FROM meta.CycleRuns WHERE \"CycleId\" = ?", cycle.CycleId)));

        // The run log copied it out beside the other three tables.
        Assert.NotNull(cycle.RunLog);
        Assert.Equal(4, cycle.RunLog!.FilesWritten.Count);
        var pumpRuns = Assert.Single(cycle.RunLog.FilesWritten, f => f.Contains(SnapshotRunLog.PumpRunsFolder));
        Assert.Equal($"{loop.BootId}.parquet", Path.GetFileName(pumpRuns));
        Assert.Equal(1, Count(pumpRuns));

        // A cycle that drains a table with nothing dirty still records the drain it made.
        clock.Advance(TimeSpan.FromMinutes(1));
        var second = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
        var quiet = Assert.Single(second.Pumps);
        Assert.Equal(0, quiet.RowsRead);
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.PumpRuns")));
        Assert.Equal(2, transport.Documents);
    }

    // ---- Pruning the write DB's run tables -----------------------------------------------------

    /// <summary>A run row from an earlier boot, placed by hand with a chosen start time.</summary>
    private static void InsertRun(SnapshotStore store, string runId, string source, DateTime startedAt) =>
        store.Execute(
            """
            INSERT INTO meta.SyncRuns ("RunId", "Source", "TargetTable", "StartedAt", "FinishedAt", "Status")
            VALUES (?, ?, 'Widget', ?, ?, 'Succeeded')
            """,
            runId, source, startedAt, startedAt.AddSeconds(1));

    [Fact]
    public async Task TheFirstCadenceFlush_PrunesFlushedRowsOfEarlierDays_AndOnlyOnceADay()
    {
        // A warm boot over a write DB that earlier boots left run rows in, on earlier days: the
        // shape of a persistent estate. Two sources: one this boot runs, one that only ran before.
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
        using (var earlier = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = WriteDbPath }))
        {
            InsertRun(earlier, "gone-widgets", "widgets", twoDaysAgo);
            InsertRun(earlier, "gone-retired", "retired", threeDaysAgo);
            InsertRun(earlier, "kept-retired", "retired", twoDaysAgo);
            earlier.Execute("INSERT INTO meta.CycleRuns (\"CycleId\", \"StartedAt\", \"Outcome\") VALUES ('gone-cycle', ?, 'Ran')", twoDaysAgo);
            earlier.Execute("INSERT INTO meta.PublishRuns (\"PublishId\", \"SnapshotName\", \"StartedAt\", \"Status\") VALUES ('gone-publish', 'agent-test', ?, 'Published')", twoDaysAgo);
            earlier.Execute("INSERT INTO meta.PumpRuns (\"CycleId\", \"Table\", \"StartedAt\") VALUES ('gone-cycle', 'Widget', ?)", twoDaysAgo);
        }
        var registry = OneWidgetSource(TimeSpan.FromMinutes(1));
        using var loop = Loop(registry, runLog: new SnapshotRunLogOptions
        {
            Store = new LocalPublishStore(RunLogDir),
            Cadence = TimeSpan.FromMinutes(5),
        });

        var first = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.False(first.ColdStartRebuild);
        Assert.NotNull(first.RunLog);
        // The first cadence flush wrote today's rows, then the prune took the earlier days' rows:
        // six placed, five gone, and the one kept is the newest row of the source this boot has
        // not run, which the publisher reads into the manifest.
        var store = loop.Store!;
        var pruned = Assert.Single(events, e => e.Level == SnapshotAgentEventLevel.Info && e.Message.StartsWith("Run log: pruned"));
        Assert.Contains("5 flushed row(s)", pruned.Message);
        Assert.Equal(0L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.SyncRuns WHERE \"RunId\" LIKE 'gone-%'")));
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.SyncRuns WHERE \"RunId\" = 'kept-retired'")));
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.CycleRuns")));
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.PublishRuns")));
        Assert.Equal(0L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.PumpRuns")));
        Assert.Contains(store.ReadLatestRunPerSource(), run => run.SourceKey == "retired");
        // Earlier boots' rows were never this boot's to export: no file for their days.
        Assert.DoesNotContain(first.RunLog!.FilesWritten, f => f.Contains($"date={twoDaysAgo:yyyy-MM-dd}"));
        Assert.DoesNotContain(events, e => e.Level == SnapshotAgentEventLevel.Warning && e.Message.StartsWith("Run log:"));

        // Once a day: a row that turns up later the same day waits for tomorrow's first flush.
        InsertRun(store, "later-widgets", "widgets", threeDaysAgo);
        clock.Advance(TimeSpan.FromMinutes(5));
        var second = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(second.RunLog);
        Assert.Single(events, e => e.Message.StartsWith("Run log: pruned"));
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.SyncRuns WHERE \"RunId\" = 'later-widgets'")));
    }
}
