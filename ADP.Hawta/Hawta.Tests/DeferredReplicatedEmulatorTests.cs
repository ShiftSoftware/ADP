using System.Net.Sockets;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Deferred × replicated against the LOCAL Cosmos emulator — the combination the residency
/// suite settles with doubles and the emulator suite runs resident-only. Every arc here uses
/// REAL drains through <see cref="CosmosSnapshotReplicator"/> and runs for both family
/// shapes: (a) row-mapped with a row predicate and an explicit-null hierarchical
/// partition-key level into its own container, and (b) a grouped projection sharing a
/// document space with a sibling grouped table. Skipped automatically when no emulator is
/// listening on localhost:8081.
///
/// <para>The class shares one database and two containers (the emulator throttles container
/// creation long before it throttles documents); tests isolate by key prefix — xUnit runs
/// the methods of one class serially, and every id this suite writes carries the instance's
/// prefix, so no assertion can be satisfied by another test's documents.</para>
///
/// <para>What only real Cosmos (not the emulator) can prove stays out of scope on purpose
/// and is tracked as cutover-day watch items: hierarchical-partition-key edge behavior,
/// RU/throttle semantics, and real continuation paging.</para>
/// </summary>
public sealed class DeferredReplicatedEmulatorTests :
    IClassFixture<DeferredReplicatedEmulatorTests.EmulatorDatabase>, IDisposable
{
    // ---- Emulator plumbing (one client, one database, two containers per class) --------------

    public sealed class EmulatorDatabase : IAsyncLifetime
    {
        // 127.0.0.1, not localhost: the emulator listens on IPv4 only.
        private const string Endpoint = "https://127.0.0.1:8081/";
        private const string Key =
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public CosmosClient? Client { get; private set; }
        public Database? Database { get; private set; }
        public Container? Widgets { get; private set; }
        public Container? Parts { get; private set; }

        /// <summary>False = no emulator, or it would not provision the containers — skip.</summary>
        public bool Ready { get; private set; }

        public async ValueTask InitializeAsync()
        {
            if (!Probe())
                return;

            Client = new CosmosClient(Endpoint, Key, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true,
                HttpClientFactory = () => new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                }),
            });
            Database = await Client.CreateDatabaseAsync($"HawtaDeferRep-{Guid.NewGuid():N}");
            Widgets = await CreateContainerAsync("Widgets", ["/Code", "/Kind", "/Region"]);
            Parts = await CreateContainerAsync("Parts", ["/PartNumber"]);
            Ready = Widgets is not null && Parts is not null;
        }

        public async ValueTask DisposeAsync()
        {
            if (Database is not null)
                await Database.DeleteAsync();
            Client?.Dispose();
        }

        private static bool Probe()
        {
            try
            {
                using var client = new TcpClient();
                return client.ConnectAsync("127.0.0.1", 8081).Wait(500);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// The emulator intermittently 503s container creation under pressure. Retry briefly;
        /// if it persists the environment (not the code) can't run the suite — null makes
        /// every test skip rather than fail.
        /// </summary>
        private async Task<Container?> CreateContainerAsync(string name, IReadOnlyList<string> partitionKeyPaths)
        {
            for (var attempt = 1; attempt <= 4; attempt++)
            {
                try
                {
                    await Database!.CreateContainerAsync(new ContainerProperties
                    {
                        Id = name,
                        PartitionKeyPaths = [.. partitionKeyPaths],
                    });
                    return Database!.GetContainer(name);
                }
                catch (CosmosException busy) when (busy.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                }
            }

            return null;
        }
    }

    private readonly EmulatorDatabase db;
    private readonly ResidencyFixture fx = new();

    /// <summary>Per-test key prefix — the isolation unit inside the shared containers.</summary>
    private readonly string suffix = Guid.NewGuid().ToString("N")[..8];

    public DeferredReplicatedEmulatorTests(EmulatorDatabase db)
    {
        this.db = db;
        // The shared fixture ensures only its own tables; the grouped pair is this suite's.
        fx.Store.EnsureTable(groupedTable);
        fx.Store.EnsureTable(siblingTable);
    }

    public void Dispose() => fx.Dispose();

    // ---- Shape (a): row-mapped — predicate + explicit-null hierarchical PK level -------------
    // The shape whose host pin this proof exists to lift: one row → one document, a predicate
    // that excludes malformed rows, and a 3-level hierarchical partition key whose last level
    // is an EXPLICIT null (AddNullValue semantics, not PartitionKey.None).

    private string C(string code) => $"{code}-{suffix}";

    /// <summary>
    /// The poison switch for the dead-letter arcs: while a code is poisoned, its document's
    /// partition-key VALUE disagrees with the document body at the same path — the canonical
    /// per-document 400, a REAL remote rejection through the real drain path, repeatable
    /// until the poison is cleared.
    /// </summary>
    private readonly HashSet<string> poisonedCodes = new(StringComparer.Ordinal);

    private CosmosFamilyMapping RowFamily() => new()
    {
        Family = "Widget",
        Database = db.Database?.Id ?? "pending",
        Container = "Widgets",
        // Rows whose code starts with 'x' are malformed by contract and excluded — the
        // predicate half of the shape. They still ingest, publish, and must still settle.
        Predicate = row => !((string)row.Values["Code"]!).StartsWith('x'),
        Map = row =>
        {
            var code = (string)row.Values["Code"]!;
            return new CosmosDocument
            {
                Id = row.PrimaryKey,
                PartitionKey = [code, poisonedCodes.Contains(code) ? "poisoned" : "Widget", null],
                Body = new Dictionary<string, object?>
                {
                    ["Code"] = code,
                    ["Kind"] = "Widget",
                    ["Region"] = null,
                    ["Quantity"] = row.Values["Quantity"],
                },
            };
        },
    };

    private PartitionKey WidgetPk(string code) =>
        new PartitionKeyBuilder().Add(code).Add("Widget").Add((string?)null).Build();

    private string RowFeedV1 =>
        $"Code,Quantity\n{C("A")},1\n{C("B")},2\n{C("C")},3\nx{C("E")},9\n"; // xE…: predicate-excluded
    private string RowFeedV2 =>
        $"Code,Quantity\n{C("A")},1\n{C("B")},5\n{C("C")},3\nx{C("E")},9\n"; // one changed row

    private SourceRegistry RowRegistry(bool replicationEnabled = true, bool pin = false, bool wired = true) =>
        fx.Registry(fx.FileSource(
            fx.FileOptions(),
            pin: pin,
            families: wired ? [RowFamily()] : null,
            replicationEnabled: replicationEnabled));

    private CosmosSnapshotReplicatorOptions RowOptions(int batchSize = 1000) => new()
    {
        Table = fx.Widget,
        Families = [RowFamily()],
        BatchSize = batchSize,
    };

    private CosmosSnapshotReplicator Replicator(SnapshotStore store) => new(store, db.Client);

    // ---- Shape (b): grouped projection sharing a document space ------------------------------
    // Many source rows → one document, keyed and ordered by the occurrence machinery, into a
    // container a SIBLING grouped table also writes — the production catalog topology.

    private sealed class GroupRow
    {
        public string? PartNumber { get; set; }
        public long? SourceOrdinal { get; set; }
        public string? Scalar { get; set; }
        public string? Supersession { get; set; }
    }

    private sealed record GroupProjection(string PartNumber, string? Scalar, IReadOnlyList<string> Supersessions);

    private readonly SnapshotTableDefinition<GroupRow> groupedTable = new("GroupedPart");
    private readonly SnapshotTableDefinition<GroupRow> siblingTable = new("GroupedPartB");
    private readonly HashSet<string> poisonedParts = new(StringComparer.Ordinal);

    private CosmosFamilyMapping GroupedFamily(SnapshotTableDefinition<GroupRow> table) => new()
    {
        Family = "CatalogPart",
        Database = db.Database?.Id ?? "pending",
        Container = "Parts",
        Grouping = new CosmosGroupProjection<GroupRow, string, GroupProjection>(
            table,
            row => row.PartNumber!,
            row => row.SourceOrdinal,
            (partNumber, rows) =>
            {
                if (string.IsNullOrWhiteSpace(partNumber) || rows.Count == 0)
                    return null;
                return new GroupProjection(
                    partNumber,
                    rows[0].Scalar,
                    [.. rows.Select(row => row.Supersession)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value!)
                        .Distinct(StringComparer.Ordinal)]);
            },
            (partNumber, projection) => new CosmosDocument
            {
                Id = partNumber,
                // Poisoned groups supply a PK value that disagrees with the body's
                // /PartNumber — the same canonical 400 as the row shape's poison.
                PartitionKey = [poisonedParts.Contains(partNumber) ? "poisoned" : partNumber],
                Body = new Dictionary<string, object?>
                {
                    ["PartNumber"] = projection.PartNumber,
                    ["Scalar"] = projection.Scalar,
                    ["Supersessions"] = projection.Supersessions,
                },
            }),
    };

    private string GroupFeedV1 =>
        $"PartNumber,Scalar,Supersession\n{C("P1")},first,S-2\n{C("P1")},second,S-3\n{C("P2")},only,\n{C("P3")},tail,\n";
    // One row of the multi-row group changes; the FIRST row of the group does not.
    private string GroupFeedV2 =>
        $"PartNumber,Scalar,Supersession\n{C("P1")},first,S-2\n{C("P1")},second,S-9\n{C("P2")},only,\n{C("P3")},tail,\n";
    private string SiblingFeed => $"PartNumber,Scalar,Supersession\n{C("Q1")},solo,\n";

    private string GroupedFeedPath => Path.Combine(fx.FeedDirectory, "grouped.csv");
    private string SiblingFeedPath => Path.Combine(fx.FeedDirectory, "grouped-b.csv");

    private FileSnapshotIngestorOptions GroupedFeedOptions(
        SnapshotTableDefinition<GroupRow> table, string path, string sourceKey) => new()
    {
        Table = table,
        FilePath = path,
        OccurrenceRowIdentity = new FileOccurrenceRowIdentity(
            table.Column(row => row.SourceOrdinal),
            new FileLogicalKeyPart(table.Column(row => row.PartNumber))),
        ChangeGate = fx.Gate,
        MergeOptions = new SnapshotMergeOptions { Source = sourceKey, DeletesEnabled = true },
    };

    private SnapshotSource GroupedSource(
        SnapshotTableDefinition<GroupRow> table,
        string path,
        string sourceKey,
        bool replicationEnabled = true)
    {
        var options = GroupedFeedOptions(table, path, sourceKey);
        return new SnapshotSource
        {
            Key = sourceKey,
            RecordIdentity = SourceRecordIdentityDescriptor.OccurrenceOrdinal(options.OccurrenceRowIdentity!),
            Table = table,
            Cadence = TimeSpan.FromMinutes(1),
            Ingest = context => FileSnapshotIngestor.Ingest(context.Store, options, context.FileMetadata),
            FileIngestion = options,
            Families = [GroupedFamily(table)],
            ReplicationEnabled = replicationEnabled,
        };
    }

    private SnapshotMergeResult IngestGrouped(
        SnapshotStore store, SnapshotTableDefinition<GroupRow> table, string path, string sourceKey) =>
        FileSnapshotIngestor.Ingest(
            store, GroupedFeedOptions(table, path, sourceKey), new DirectoryListingFileMetadataProbe());

    private CosmosSnapshotReplicatorOptions GroupedOptions(int batchSize = 1000) => new()
    {
        Table = groupedTable,
        Families = [GroupedFamily(groupedTable)],
        BatchSize = batchSize,
    };

    private static async Task<System.Text.Json.Nodes.JsonObject> ReadDocAsync(
        Container container, string id, PartitionKey pk)
    {
        using var response = await container.ReadItemStreamAsync(id, pk);
        Assert.True(response.IsSuccessStatusCode, $"document '{id}' should exist ({response.StatusCode})");
        return System.Text.Json.Nodes.JsonNode.Parse(response.Content)!.AsObject();
    }

    // =========================================================================================
    // Arc 1 — settle → defer. Real drain to QueueEmpty; manifest records pending 0; the cold
    // restart defers with zero resident rows and an explicit state; the tick skips; the next
    // publish carries.
    // =========================================================================================

    [Fact]
    public async Task Arc1_RowMapped_SettleThenDefer()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(RowFeedV1);
        var registry = RowRegistry();
        Assert.True(fx.Ingest(fx.Store).Succeeded);

        var drain = await Replicator(fx.Store).DrainAsync(
            RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drain.Drained);
        Assert.Equal(ReplicationDrainStop.QueueEmpty, drain.Stopped);
        Assert.Equal(3, drain.Upserted);  // A, B, C
        Assert.Equal(1, drain.Excluded);  // xE settles WITHOUT a document — stamped, not written
        Assert.Equal(0, drain.Failed);
        Assert.Equal(0, fx.Store.CountDirtyRows(fx.Widget));
        Assert.Equal("2",
            (await ReadDocAsync(db.Widgets!, C("B"), WidgetPk(C("B"))))["Quantity"]!.ToString());

        // The export computes pending from live bookkeeping: a fully drained queue records 0.
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
        Assert.Equal(0, ResidencyFixture.Rows(fresh, "Widget"));
        Assert.Equal(SnapshotResidency.Deferred, fresh.ReadResidency("Widget"));
        Assert.Equal(0, fresh.ReadDeferredTableRecord("Widget")!.ReplicationPending);

        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, fx.Ingest(fresh).Status);
        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, fx.Publish(fresh, registry).Status);
    }

    [Fact]
    public async Task Arc1_Grouped_SettleThenDefer_TwoTablesSharingTheDocumentSpace()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(GroupFeedV1, GroupedFeedPath);
        fx.WriteFeed(SiblingFeed, SiblingFeedPath);
        var registry = fx.Registry(
            GroupedSource(groupedTable, GroupedFeedPath, "file-grouped"),
            GroupedSource(siblingTable, SiblingFeedPath, "file-grouped-b"));
        Assert.True(IngestGrouped(fx.Store, groupedTable, GroupedFeedPath, "file-grouped").Succeeded);
        Assert.True(IngestGrouped(fx.Store, siblingTable, SiblingFeedPath, "file-grouped-b").Succeeded);

        // Both tables drain into ONE container — the shared document space must not blur:
        // each family touches only its own group-keyed ids.
        var replicator = Replicator(fx.Store);
        var drainA = await replicator.DrainAsync(
            GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        var drainB = await replicator.DrainAsync(
            new CosmosSnapshotReplicatorOptions { Table = siblingTable, Families = [GroupedFamily(siblingTable)] },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drainA.Drained);
        Assert.Equal(3, drainA.GroupsRecomputed);
        Assert.Equal(3, drainA.Upserted);
        Assert.True(drainB.Drained);
        Assert.Equal(1, drainB.Upserted);

        var p1 = await ReadDocAsync(db.Parts!, C("P1"), new PartitionKey(C("P1")));
        Assert.Equal("first", p1["Scalar"]!.ToString());
        Assert.Equal(["S-2", "S-3"], p1["Supersessions"]!.AsArray().Select(n => n!.ToString()));
        await ReadDocAsync(db.Parts!, C("Q1"), new PartitionKey(C("Q1")));

        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(0, fx.Entry("GroupedPart").ReplicationPending);
        Assert.Equal(0, fx.Entry("GroupedPartB").ReplicationPending);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["GroupedPart", "GroupedPartB"],
            rebuild.TablesDeferred.Select(t => t.Table).Order());
        Assert.Equal(SnapshotResidency.Deferred, fresh.ReadResidency("GroupedPart"));
        Assert.Equal(SnapshotResidency.Deferred, fresh.ReadResidency("GroupedPartB"));

        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged,
            IngestGrouped(fresh, groupedTable, GroupedFeedPath, "file-grouped").Status);
        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, fx.Publish(fresh, registry).Status);

        // The shared space survived deferral untouched: both documents still there.
        await ReadDocAsync(db.Parts!, C("P2"), new PartitionKey(C("P2")));
        await ReadDocAsync(db.Parts!, C("Q1"), new PartitionKey(C("Q1")));
    }

    // =========================================================================================
    // Arc 2 — publish mid-backlog → restart → finish the drain. The routine case at cutover:
    // publishes land inside the initial burst, so a cold restart must load the table (owed),
    // and the drain after the restart pushes EXACTLY the owed rows — zero repeat operations
    // for rows the pre-restart drain already settled (bookkeeping survived the DR load).
    // =========================================================================================

    [Fact]
    public async Task Arc2_RowMapped_PublishMidBacklog_RestartFinishesTheDrain()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(RowFeedV1);
        var registry = RowRegistry();
        Assert.True(fx.Ingest(fx.Store).Succeeded);

        // A bounded drain leaves a real partially-drained backlog: A and B settle, C and xE owe.
        var partial = await Replicator(fx.Store).DrainAsync(
            RowOptions(batchSize: 1), maxBatches: 2,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(partial.Drained);
        Assert.Equal(ReplicationDrainStop.BatchBound, partial.Stopped);
        Assert.Equal(2, partial.RowsRead);

        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(2, fx.Entry("Widget").ReplicationPending);

        // The cold restart LOADS: the copy is owed to the pump.
        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(["Widget"], rebuild.TablesLoaded.Select(t => t.Table));
        Assert.Equal(
            fx.Entry("Widget").ReplicationPending,
            fresh.CountDirtyRows(fx.Widget) + fresh.CountDeadLetteredRows(fx.Widget));

        // The post-restart drain pushes exactly the owed rows. RowsRead is the engine's own
        // count over the restored bookkeeping: had the DR load lost the stamps, A and B would
        // re-enter the dirty predicate and this would read 4.
        var finish = await Replicator(fresh).DrainAsync(
            RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(finish.Drained);
        Assert.Equal(2, finish.RowsRead);
        Assert.Equal(1, finish.Upserted);   // C
        Assert.Equal(1, finish.Excluded);   // xE
        Assert.Equal(0, finish.Failed);
        await ReadDocAsync(db.Widgets!, C("C"), WidgetPk(C("C")));

        // Drain progress forces the recount: the re-export records pending 0…
        var republish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, republish.Status);
        Assert.Contains("Widget", republish.TablesExported);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);

        // …and the restart after THAT defers.
        using var settled = fx.ColdStart(registry, out var second);
        Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public async Task Arc2_Grouped_PublishMidBacklog_RestartFinishesTheDrain()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(GroupFeedV1, GroupedFeedPath);
        var registry = fx.Registry(GroupedSource(groupedTable, GroupedFeedPath, "file-grouped"));
        Assert.True(IngestGrouped(fx.Store, groupedTable, GroupedFeedPath, "file-grouped").Succeeded);

        // Batch size counts GROUPS here: one batch settles P1 (both its rows); P2/P3 owe.
        var partial = await Replicator(fx.Store).DrainAsync(
            GroupedOptions(batchSize: 1), maxBatches: 1,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(partial.Drained);
        Assert.Equal(1, partial.GroupsRecomputed);
        Assert.Equal(2, partial.RowsRead); // P1's two source rows stamped set-wise

        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(2, fx.Entry("GroupedPart").ReplicationPending);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(
            fx.Entry("GroupedPart").ReplicationPending,
            fresh.CountDirtyRows(groupedTable) + fresh.CountDeadLetteredRows(groupedTable));

        var finish = await Replicator(fresh).DrainAsync(
            GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(finish.Drained);
        Assert.Equal(2, finish.GroupsRecomputed); // P2 and P3 — P1 is NOT recomputed again
        Assert.Equal(2, finish.RowsRead);
        Assert.Equal(2, finish.Upserted);
        await ReadDocAsync(db.Parts!, C("P2"), new PartitionKey(C("P2")));
        await ReadDocAsync(db.Parts!, C("P3"), new PartitionKey(C("P3")));

        var republish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, republish.Status);
        Assert.Equal(0, fx.Entry("GroupedPart").ReplicationPending);

        using var settled = fx.ColdStart(registry, out var second);
        Assert.Equal(["GroupedPart"], second.TablesDeferred.Select(t => t.Table));
    }

    // =========================================================================================
    // Arc 3 — replication flips OFF→ON across a COLD restart. The safe flip: pending was
    // recorded RAW while the pump was off, so the restarted cold start sees the full backlog,
    // loads, and the drain performs the initial population.
    // =========================================================================================

    [Fact]
    public async Task Arc3_RowMapped_FlipOffToOn_AcrossAColdRestart()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(RowFeedV1);
        var off = RowRegistry(replicationEnabled: false);
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, off).Status);
        Assert.Equal(4, fx.Entry("Widget").ReplicationPending); // RAW: the full backlog, pump off

        // Deferred while off — correct: nothing enabled is owed anything.
        using (var dark = fx.ColdStart(off, out var deferredRebuild))
            Assert.Equal(["Widget"], deferredRebuild.TablesDeferred.Select(t => t.Table));

        // The flip ships as a cold start (config change → deployment → fresh write DB).
        var on = RowRegistry(replicationEnabled: true);
        using var fresh = fx.ColdStart(on, out var rebuild);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(["Widget"], rebuild.TablesLoaded.Select(t => t.Table));

        var drain = await Replicator(fresh).DrainAsync(
            RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drain.Drained);
        Assert.Equal(3, drain.Upserted);
        Assert.Equal(1, drain.Excluded);
        await ReadDocAsync(db.Widgets!, C("A"), WidgetPk(C("A")));
        await ReadDocAsync(db.Widgets!, C("C"), WidgetPk(C("C")));

        var republish = fx.Publish(fresh, on);
        Assert.Equal(SnapshotPublishStatus.Published, republish.Status);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);

        using var settled = fx.ColdStart(on, out var second);
        Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public async Task Arc3_Grouped_FlipOffToOn_AcrossAColdRestart()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(GroupFeedV1, GroupedFeedPath);
        var off = fx.Registry(GroupedSource(groupedTable, GroupedFeedPath, "file-grouped", replicationEnabled: false));
        Assert.True(IngestGrouped(fx.Store, groupedTable, GroupedFeedPath, "file-grouped").Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, off).Status);
        Assert.Equal(4, fx.Entry("GroupedPart").ReplicationPending);

        using (var dark = fx.ColdStart(off, out var deferredRebuild))
            Assert.Equal(["GroupedPart"], deferredRebuild.TablesDeferred.Select(t => t.Table));

        var on = fx.Registry(GroupedSource(groupedTable, GroupedFeedPath, "file-grouped"));
        using var fresh = fx.ColdStart(on, out var rebuild);
        Assert.Empty(rebuild.TablesDeferred);

        var drain = await Replicator(fresh).DrainAsync(
            GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drain.Drained);
        Assert.Equal(3, drain.Upserted);
        var p1 = await ReadDocAsync(db.Parts!, C("P1"), new PartitionKey(C("P1")));
        Assert.Equal(["S-2", "S-3"], p1["Supersessions"]!.AsArray().Select(n => n!.ToString()));

        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fresh, on).Status);
        Assert.Equal(0, fx.Entry("GroupedPart").ReplicationPending);

        using var settled = fx.ColdStart(on, out var second);
        Assert.Equal(["GroupedPart"], second.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public async Task Arc3_Variant_NewlyWiredFamily_NullPendingLoads_AndTheDrainIsTheInitialPopulation()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        // A previously family-less table: its committed entry carries replicationPending null.
        fx.WriteFeed(RowFeedV1);
        var unwired = RowRegistry(wired: false);
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, unwired).Status);
        Assert.Null(fx.Entry("Widget").ReplicationPending);

        // Wiring the family and enabling replication: null proves NOTHING about the backlog,
        // so the cold start must load, and the first drain populates the container.
        var wired = RowRegistry(replicationEnabled: true);
        using var fresh = fx.ColdStart(wired, out var rebuild);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(["Widget"], rebuild.TablesLoaded.Select(t => t.Table));

        var drain = await Replicator(fresh).DrainAsync(
            RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drain.Drained);
        Assert.Equal(3, drain.Upserted);
        await ReadDocAsync(db.Widgets!, C("A"), WidgetPk(C("A")));

        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fresh, wired).Status);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);
        using var settled = fx.ColdStart(wired, out var second);
        Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
    }

    // =========================================================================================
    // Arc 4 — hydrate under ACTIVE replication, the full loop. The dirty set after a hydrated
    // merge must be ONLY the changed rows (a full-table re-dirty would be a full-catalog
    // Cosmos burst per edit), the real drain must land the delta, and the re-export must
    // recompute pending and the content hash so the next restart defers again.
    // =========================================================================================

    [Fact]
    public async Task Arc4_RowMapped_HydrateUnderActiveReplication()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(RowFeedV1);
        var registry = RowRegistry();
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        await Replicator(fx.Store).DrainAsync(RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        var hashBefore = fx.Entry("Widget").SourceCatalog.Single().ContentHash;

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));

        // A real change on the deferred table: hydrate inside the merge transaction…
        fx.WriteFeed(RowFeedV2);
        var merge = fx.Ingest(fresh);
        Assert.True(merge.Succeeded);
        Assert.Equal(1, merge.RowsUpdated);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(4, ResidencyFixture.Rows(fresh, "Widget"));

        // …and the dirty set is ONLY the changed row — the hydrated rows came back settled.
        Assert.Equal(1, fresh.CountDirtyRows(fx.Widget));

        var drain = await Replicator(fresh).DrainAsync(
            RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drain.Drained);
        Assert.Equal(1, drain.RowsRead);
        Assert.Equal(1, drain.Upserted);
        Assert.Equal("5",
            (await ReadDocAsync(db.Widgets!, C("B"), WidgetPk(C("B"))))["Quantity"]!.ToString());

        // Re-export recomputes both new fields.
        var republish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, republish.Status);
        Assert.Contains("Widget", republish.TablesExported);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);
        var hashAfter = fx.Entry("Widget").SourceCatalog.Single().ContentHash;
        Assert.NotNull(hashAfter);
        Assert.NotEqual(hashBefore, hashAfter);

        using var settled = fx.ColdStart(registry, out var second);
        Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public async Task Arc4_Grouped_GroupRecomputeAfterHydration_ReadsTheCompleteRestoredRowSet()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(GroupFeedV1, GroupedFeedPath);
        var registry = fx.Registry(GroupedSource(groupedTable, GroupedFeedPath, "file-grouped"));
        Assert.True(IngestGrouped(fx.Store, groupedTable, GroupedFeedPath, "file-grouped").Succeeded);
        await Replicator(fx.Store).DrainAsync(GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["GroupedPart"], rebuild.TablesDeferred.Select(t => t.Table));

        // ONE row of the two-row group changes. The merge hydrates the whole table first.
        fx.WriteFeed(GroupFeedV2, GroupedFeedPath);
        var merge = IngestGrouped(fresh, groupedTable, GroupedFeedPath, "file-grouped");
        Assert.True(merge.Succeeded);
        Assert.Equal(1, merge.RowsUpdated);
        Assert.Equal(1, fresh.CountDirtyRows(groupedTable)); // only the changed row is dirty

        // The recompute reads the COMPLETE restored group — this is the wrong-documents
        // hazard: a partial hydration would still produce a document, with the wrong scalar
        // (taken from the only row it can see) and a one-element supersession list, and the
        // pipeline would report green.
        var drain = await Replicator(fresh).DrainAsync(
            GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drain.Drained);
        Assert.Equal(1, drain.GroupsRecomputed);
        Assert.Equal(2, drain.SourceRowsLoaded); // both of P1's rows, set-wise
        var p1 = await ReadDocAsync(db.Parts!, C("P1"), new PartitionKey(C("P1")));
        Assert.Equal("first", p1["Scalar"]!.ToString()); // from the UNCHANGED restored row
        Assert.Equal(["S-2", "S-9"], p1["Supersessions"]!.AsArray().Select(n => n!.ToString()));

        var republish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, republish.Status);
        Assert.Equal(0, fx.Entry("GroupedPart").ReplicationPending);

        using var settled = fx.ColdStart(registry, out var second);
        Assert.Equal(["GroupedPart"], second.TablesDeferred.Select(t => t.Table));
    }

    // =========================================================================================
    // Arc 5 — dead-letter. One REAL transport failure through the real drain path proves the
    // attempts increment; doubles take the row to the limit; the divergence must be VISIBLE
    // (DeadLettered > 0 while Drained == true, and the loop warns); the cold start LOADS
    // (attempts-blind pending); the reset restores the deferral win.
    // =========================================================================================

    [Fact]
    public async Task Arc5_RowMapped_DeadLetter_VisibleDivergence_AndTheResetRestoresTheDeferralWin()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        var estateDirectory = Path.Combine(Path.GetTempPath(), $"hawta-deadletter-{suffix}");
        Directory.CreateDirectory(estateDirectory);
        var writeDbPath = Path.Combine(estateDirectory, "write.duckdb");
        try
        {
            fx.WriteFeed(RowFeedV1);
            var registry = RowRegistry();

            // Ingest on a DISK store (the loop below reopens it warm), then a drain in which
            // row B's document carries a partition-key value that disagrees with its body —
            // a real per-document rejection on the wire.
            using (var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = writeDbPath }))
            {
                store.EnsureTable(fx.Widget);
                Assert.True(fx.Ingest(store).Succeeded);

                poisonedCodes.Add(C("B"));
                var drain = await Replicator(store).DrainAsync(
                    RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
                Assert.Equal(1, drain.Failed);
                Assert.Equal(1, drain.RemoteFailedRows); // the failure went out on the wire
                Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar(
                    "SELECT \"_ReplicationAttempts\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", C("B"))));

                // Doubles take it the rest of the way to the limit (the increment path is
                // proven; five real failures would prove nothing more).
                store.Execute(
                    $"UPDATE data.\"Widget\" SET \"_ReplicationAttempts\" = {SnapshotStore.MaxReplicationAttempts} " +
                    "WHERE \"_PrimaryKey\" = ?", C("B"));

                // The divergence is visible in the drain report itself.
                var drained = await Replicator(store).DrainAsync(
                    RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
                Assert.True(drained.Drained);
                Assert.Equal(ReplicationDrainStop.QueueEmpty, drained.Stopped);
                Assert.Equal(1, drained.DeadLettered);

                Assert.Equal(SnapshotPublishStatus.Published, SnapshotPublisher.Publish(store, new SnapshotPublishOptions
                {
                    PublishDirectory = fx.PublishDirectory,
                    SnapshotName = ResidencyFixture.SnapshotName,
                    Tables = registry.Tables,
                    Sources = registry.Sources,
                }).Status);
            }
            Assert.Equal(1, fx.Entry("Widget").ReplicationPending); // attempts-blind: B counts

            // The LOOP warns about the divergence on an ordinary warm cycle.
            var events = new List<SnapshotAgentEvent>();
            using (var loop = new SnapshotAgentLoop(new SnapshotAgentOptions
            {
                Registry = registry,
                WriteDatabasePath = writeDbPath,
                PublishDirectory = fx.PublishDirectory,
                SnapshotName = ResidencyFixture.SnapshotName,
                OnEvent = events.Add,
            }, db.Client))
            {
                var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);
                var pump = Assert.Single(cycle.Pumps);
                Assert.True(pump.Drained);
                Assert.Equal(1, pump.DeadLettered);
            }
            Assert.Contains(events, e =>
                e.Level == SnapshotAgentEventLevel.Warning && e.Message.Contains("dead-lettered"));
            Assert.DoesNotContain(events, e => e.Level == SnapshotAgentEventLevel.Error);

            // The cold start LOADS — the raw pending count sees what the pump cannot.
            using var fresh = fx.ColdStart(registry, out var rebuild);
            Assert.Empty(rebuild.TablesDeferred);
            Assert.Equal(["Widget"], rebuild.TablesLoaded.Select(t => t.Table));
            Assert.Equal(1, fresh.CountDeadLetteredRows(fx.Widget));

            // Recovery: reset the ledger, clear the poison (the deterministic fault is
            // fixed), drain settles, the re-export records 0, and the NEXT restart defers.
            Assert.Equal(1, fresh.ResetReplicationFailures(fx.Widget));
            poisonedCodes.Remove(C("B"));
            var settledDrain = await Replicator(fresh).DrainAsync(
                RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(settledDrain.Drained);
            Assert.Equal(0, settledDrain.DeadLettered);
            Assert.Equal("2",
                (await ReadDocAsync(db.Widgets!, C("B"), WidgetPk(C("B"))))["Quantity"]!.ToString());

            var republish = fx.Publish(fresh, registry);
            Assert.Equal(SnapshotPublishStatus.Published, republish.Status);
            Assert.Equal(0, fx.Entry("Widget").ReplicationPending);

            using var deferredAgain = fx.ColdStart(registry, out var second);
            Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
        }
        finally
        {
            try { Directory.Delete(estateDirectory, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task Arc5_Grouped_ARealGroupFailure_IncrementsEveryRowOfTheGroup_AndResetRecovers()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(GroupFeedV1, GroupedFeedPath);
        var registry = fx.Registry(GroupedSource(groupedTable, GroupedFeedPath, "file-grouped"));
        Assert.True(IngestGrouped(fx.Store, groupedTable, GroupedFeedPath, "file-grouped").Succeeded);

        // P1's document carries a partition-key value that disagrees with its body: a real
        // remote rejection, and the whole group's source rows share the outcome — grouped
        // bookkeeping is set-wise in both directions.
        poisonedParts.Add(C("P1"));
        var drain = await Replicator(fx.Store).DrainAsync(
            GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, drain.Failed); // both of P1's rows
        Assert.Equal(1, drain.RemoteFailedRows);
        Assert.Equal(2L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM data.\"GroupedPart\" WHERE \"_ReplicationAttempts\" = 1")));

        fx.Store.Execute(
            $"UPDATE data.\"GroupedPart\" SET \"_ReplicationAttempts\" = {SnapshotStore.MaxReplicationAttempts} " +
            "WHERE \"PartNumber\" = ?", C("P1"));
        var drained = await Replicator(fx.Store).DrainAsync(
            GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(drained.Drained);
        Assert.Equal(2, drained.DeadLettered);

        poisonedParts.Remove(C("P1"));
        Assert.Equal(2, fx.Store.ResetReplicationFailures(groupedTable));
        var settled = await Replicator(fx.Store).DrainAsync(
            GroupedOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(settled.Drained);
        Assert.Equal(0, settled.DeadLettered);
        var p1 = await ReadDocAsync(db.Parts!, C("P1"), new PartitionKey(C("P1")));
        Assert.Equal(["S-2", "S-3"], p1["Supersessions"]!.AsArray().Select(n => n!.ToString()));

        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(0, fx.Entry("GroupedPart").ReplicationPending);
        using var deferredAgain = fx.ColdStart(registry, out var second);
        Assert.Equal(["GroupedPart"], second.TablesDeferred.Select(t => t.Table));
    }

    // =========================================================================================
    // Arc 6 — warm reopen with deferred replicated tables. A full loop cycle over BOTH shapes
    // at once: the wet pump's no-op is truthful (0 rows, QueueEmpty, zero bookkeeping
    // transactions), no error events, and the publish carries the standing manifest.
    // Warm reopen remains an engine capability for local workflows that deliberately reuse
    // estates — production hosts wipe at start, so their every start is the COLD path the
    // other arcs prove.
    // =========================================================================================

    [Fact]
    public async Task Arc6_WarmReopen_BothShapes_FullLoopCycle_CleanQuietAndCarrying()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(RowFeedV1);
        fx.WriteFeed(GroupFeedV1, GroupedFeedPath);
        var registry = fx.Registry(
            fx.FileSource(fx.FileOptions(), families: [RowFamily()]),
            GroupedSource(groupedTable, GroupedFeedPath, "file-grouped"));

        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.True(IngestGrouped(fx.Store, groupedTable, GroupedFeedPath, "file-grouped").Succeeded);
        var replicator = Replicator(fx.Store);
        Assert.True((await replicator.DrainAsync(RowOptions(),
            cancellationToken: TestContext.Current.CancellationToken)).Drained);
        Assert.True((await replicator.DrainAsync(GroupedOptions(),
            cancellationToken: TestContext.Current.CancellationToken)).Drained);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        var estateDirectory = Path.Combine(Path.GetTempPath(), $"hawta-warmreopen-{suffix}");
        Directory.CreateDirectory(estateDirectory);
        var writeDbPath = Path.Combine(estateDirectory, "write.duckdb");
        try
        {
            // Cold start onto DISK: both tables defer with intact records.
            using (var cold = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = writeDbPath }))
            {
                var rebuild = SnapshotRebuild.Execute(
                    cold, registry, fx.PublishDirectory, ResidencyFixture.SnapshotName);
                Assert.Equal(["GroupedPart", "Widget"], rebuild.TablesDeferred.Select(t => t.Table).Order());

                // The wet drain against a Deferred table is a TRUTHFUL no-op, precisely:
                // zero rows read, zero bookkeeping transactions, queue empty.
                var noOp = await Replicator(cold).DrainAsync(
                    RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
                Assert.True(noOp.Drained);
                Assert.Equal(ReplicationDrainStop.QueueEmpty, noOp.Stopped);
                Assert.Equal(0, noOp.RowsRead);
                Assert.Equal(0, noOp.BookkeepingTransactions);
                Assert.Equal(0, noOp.DeadLettered);
            }

            // The process restarts WITHOUT losing the disk. The loop reopens the write DB
            // warm — no rebuild — and a full cycle must be quiet: gate skips, truthful pump
            // no-ops, carried publish, not a single error or warning event.
            var events = new List<SnapshotAgentEvent>();
            using var loop = new SnapshotAgentLoop(new SnapshotAgentOptions
            {
                Registry = registry,
                WriteDatabasePath = writeDbPath,
                PublishDirectory = fx.PublishDirectory,
                SnapshotName = ResidencyFixture.SnapshotName,
                OnEvent = events.Add,
            }, db.Client);
            var cycle = await loop.RunCycleAsync(TestContext.Current.CancellationToken);

            Assert.False(cycle.ColdStartRebuild);
            Assert.All(cycle.Sources, run =>
                Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, run.Merge!.Status));
            Assert.Equal(2, cycle.Pumps.Count);
            Assert.All(cycle.Pumps, pump =>
            {
                Assert.True(pump.Drained);
                Assert.Equal(0, pump.RowsRead);
                Assert.Equal(0, pump.Upserted);
                Assert.Equal(0, pump.DeadLettered);
            });
            Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, cycle.Publish!.Status);
            Assert.DoesNotContain(events, e => e.Level != SnapshotAgentEventLevel.Info);

            using var warm = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = writeDbPath });
            Assert.Equal(SnapshotResidency.Deferred, warm.ReadResidency("Widget"));
            Assert.Equal(SnapshotResidency.Deferred, warm.ReadResidency("GroupedPart"));
        }
        finally
        {
            try { Directory.Delete(estateDirectory, recursive: true); } catch { }
        }
    }

    // =========================================================================================
    // The one-cycle re-drain window after a restart. Under wipe-on-start every process start
    // is a cold start, and a start can land between a drain and the export that would have
    // recorded it: the restored bookkeeping is then PRE-drain, so the first cycle re-pushes
    // documents Cosmos already holds. That window must be idempotent — same documents, zero
    // failures — because upserts and point deletes are the only operations the pump emits.
    // =========================================================================================

    [Fact]
    public async Task RestartReDrainWindow_IsIdempotent_AgainstTheEmulator()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        fx.WriteFeed(RowFeedV1);
        var registry = RowRegistry();
        Assert.True(fx.Ingest(fx.Store).Succeeded);

        // Publish BEFORE the drain: the committed copy records the full backlog (pending 4),
        // then the drain settles it — locally, newer than the copy. A restart from this copy
        // is the widest possible re-drain window.
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(4, fx.Entry("Widget").ReplicationPending);
        var first = await Replicator(fx.Store).DrainAsync(
            RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(first.Drained);
        var before = await ReadDocAsync(db.Widgets!, C("B"), WidgetPk(C("B")));

        // The restart loads (pending > 0), restored bookkeeping is pre-drain, and the whole
        // set re-drains — every operation landing on a document that already exists.
        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(4, fresh.CountDirtyRows(fx.Widget));

        var redrain = await Replicator(fresh).DrainAsync(
            RowOptions(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(redrain.Drained);
        Assert.Equal(0, redrain.Failed);
        Assert.Equal(3, redrain.Upserted);
        Assert.Equal(1, redrain.Excluded);

        // Identical mapped fields — the upsert overwrote the document with itself.
        var after = await ReadDocAsync(db.Widgets!, C("B"), WidgetPk(C("B")));
        foreach (var field in new[] { "Code", "Kind", "Region", "Quantity" })
            Assert.Equal(before[field]?.ToJsonString(), after[field]?.ToJsonString());

        // And the window closes: the re-export records 0 and the next start defers.
        var republish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, republish.Status);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);
        using var settled = fx.ColdStart(registry, out var second);
        Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
    }

    // =========================================================================================
    // The pin-lift transition — the production transition this proof unlocks, rehearsed in
    // both directions before the lever is pulled in anger.
    // =========================================================================================

    [Fact]
    public async Task PinLift_UnpinDefers_TicksCarryAndHydrateWork_AndRePinRollsBack()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        // The pinned posture: the source is pinned Resident, its pump is wet and settles,
        // and the manifest entries are written by the RESIDENT export path (pending 0).
        fx.WriteFeed(RowFeedV1);
        var pinned = RowRegistry(pin: true);
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.True((await Replicator(fx.Store).DrainAsync(RowOptions(),
            cancellationToken: TestContext.Current.CancellationToken)).Drained);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, pinned).Status);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);

        // While pinned, cold starts load resident — the incumbent behaviour.
        using (var still = fx.ColdStart(pinned, out var pinnedRebuild))
        {
            Assert.Empty(pinnedRebuild.TablesDeferred);
            Assert.Equal(4, ResidencyFixture.Rows(still, "Widget"));
        }

        // Remove the pin: the FIRST cold start defers, and the whole quiet lifecycle works.
        var unpinned = RowRegistry(pin: false);
        using (var fresh = fx.ColdStart(unpinned, out var rebuild))
        {
            Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
            Assert.Equal(0, ResidencyFixture.Rows(fresh, "Widget"));
            Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, fx.Ingest(fresh).Status);
            Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, fx.Publish(fresh, unpinned).Status);

            // Hydrate-on-change still works under the lifted pin, drain and all.
            fx.WriteFeed(RowFeedV2);
            var merge = fx.Ingest(fresh);
            Assert.True(merge.Succeeded);
            Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
            Assert.True((await Replicator(fresh).DrainAsync(RowOptions(),
                cancellationToken: TestContext.Current.CancellationToken)).Drained);
            Assert.Equal("5",
                (await ReadDocAsync(db.Widgets!, C("B"), WidgetPk(C("B"))))["Quantity"]!.ToString());
            Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fresh, unpinned).Status);
        }

        // The rollback lever, proven before it is ever needed: pin back → resident again.
        using var rolledBack = fx.ColdStart(pinned, out var rollback);
        Assert.Empty(rollback.TablesDeferred);
        Assert.Equal(["Widget"], rollback.TablesLoaded.Select(t => t.Table));
        Assert.Equal(4, ResidencyFixture.Rows(rolledBack, "Widget"));
        Assert.Equal(SnapshotResidency.Resident, rolledBack.ReadResidency("Widget"));
    }

    // =========================================================================================
    // The cutover preflight — the check no drill could have made (they all create their own
    // containers): a pre-provisioned container must exist and agree on partition-key paths
    // BEFORE the day-one drain.
    // =========================================================================================

    [Fact]
    public async Task ContainerPreflight_PassesTheMatch_AndNamesTheMissingAndTheMismatched()
    {
        Assert.SkipWhen(!db.Ready, "Cosmos emulator is not running (or would not provision containers).");

        var matching = new CosmosContainerExpectation(RowFamily(), ["/Code", "/Kind", "/Region"]);
        var mismatched = new CosmosContainerExpectation(RowFamily(), ["/Code"]);
        var missing = new CosmosContainerExpectation(new CosmosFamilyMapping
        {
            Family = "Phantom",
            Database = db.Database!.Id,
            Container = $"NoSuch-{suffix}",
            Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?>() },
        }, ["/id"]);

        var results = await CosmosContainerPreflight.ProbeAsync(
            db.Client!, [matching, mismatched, missing], TestContext.Current.CancellationToken);

        Assert.True(results[0].Ready);
        Assert.Equal(["/Code", "/Kind", "/Region"], results[0].ActualPartitionKeyPaths);

        Assert.False(results[1].Ready);
        Assert.True(results[1].Exists);
        Assert.Contains("disagree", results[1].Problem);

        Assert.False(results[2].Ready);
        Assert.False(results[2].Exists);

        var gate = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CosmosContainerPreflight.EnsureReadyAsync(
                db.Client!, [matching, mismatched], TestContext.Current.CancellationToken));
        Assert.Contains("do not start the drain", gate.Message);
        Assert.Contains("Widget", gate.Message);
    }
}
