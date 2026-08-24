using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public class SourceRegistryTests
{
    private static readonly SnapshotTableDefinition WidgetTable = new("Widget",
        [new SnapshotColumn("Code", "VARCHAR")]);

    private static SnapshotSource Source(
        string key,
        SnapshotTableDefinition? table = null,
        string? sourceScope = null,
        TimeSpan? cadence = null,
        IReadOnlyList<CosmosFamilyMapping>? families = null,
        bool enabled = true,
        bool replicationEnabled = true,
        int replicationBatchSize = 1000,
        int replicationMaxInFlightRows = 1) => new()
    {
        Key = key,
        SourceScope = sourceScope,
        RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey((table ?? WidgetTable).Columns[0].Name),
        Table = table ?? WidgetTable,
        Cadence = cadence ?? TimeSpan.FromMinutes(1),
        Ingest = _ => throw new InvalidOperationException("Not meant to run in these tests."),
        Families = families,
        Enabled = enabled,
        ReplicationEnabled = replicationEnabled,
        ReplicationBatchSize = replicationBatchSize,
        ReplicationMaxInFlightRows = replicationMaxInFlightRows,
    };

    [Fact]
    public void ValidRegistry_ExposesSourcesAndDistinctTables()
    {
        var otherTable = new SnapshotTableDefinition("Gadget", [new SnapshotColumn("Name", "VARCHAR")]);
        var registry = new SourceRegistry(
        [
            Source("a", sourceScope: "a"),
            Source("b", table: otherTable),
            Source("c", sourceScope: "c"),   // same table as "a" — distinct owned scope
        ]);

        Assert.Equal(3, registry.Sources.Count);
        Assert.Equal(["Widget", "Gadget"], registry.Tables.Select(t => t.Name));
        Assert.Same(registry["a"], registry.Sources[0]);
        Assert.True(registry.TryGet("B", out var b));   // case-insensitive
        Assert.Equal("b", b.Key);
        Assert.False(registry.TryGet("nope", out _));
    }

    [Fact]
    public void EmptyRegistry_IsRejected() =>
        Assert.Throws<ArgumentException>(() => new SourceRegistry([]));

    [Fact]
    public void DuplicateKeys_AreRejected_CaseInsensitively() =>
        Assert.Throws<ArgumentException>(() => new SourceRegistry([Source("a"), Source("A")]));

    [Fact]
    public void BlankKey_IsRejected() =>
        Assert.Throws<ArgumentException>(() => new SourceRegistry([Source("  ")]));

    [Fact]
    public void BlankNonNullScope_IsRejected() =>
        Assert.Throws<ArgumentException>(() => new SourceRegistry([Source("a", sourceScope: "  ")]));

    [Fact]
    public void NonPositiveCadence_IsRejected() =>
        Assert.Throws<ArgumentException>(() => new SourceRegistry([Source("a", cadence: TimeSpan.Zero)]));

    [Fact]
    public void NonPositiveReplicationBatchSize_IsRejected() =>
        Assert.Throws<ArgumentException>(() =>
            new SourceRegistry([Source("a", replicationBatchSize: 0)]));

    [Fact]
    public void NonPositiveReplicationMaxInFlightRows_IsRejected() =>
        Assert.Throws<ArgumentException>(() =>
            new SourceRegistry([Source("a", replicationMaxInFlightRows: 0)]));

    [Fact]
    public void SharedTable_WithMatchingPositiveReplicationSettings_IsAccepted()
    {
        var registry = new SourceRegistry(
        [
            Source("a", sourceScope: "a", replicationBatchSize: 250, replicationMaxInFlightRows: 4),
            Source("b", sourceScope: "b", replicationBatchSize: 250, replicationMaxInFlightRows: 4),
        ]);

        Assert.Equal(2, registry.Sources.Count);
        Assert.Single(registry.Tables);
    }

    // These two rejections are the invariant the per-scope ownership map depends on: ownership
    // is derived as (table, scope) -> source key, which is only a function because the registry
    // refuses two sources claiming one table/scope. Relaxing this check would let unchanged
    // rows churn identity — these tests must fail loudly if anyone tries.
    [Fact]
    public void SharedTable_WithDuplicateScopeOwnership_IsRejectedCaseInsensitively()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SourceRegistry(
        [
            Source("a", sourceScope: "north"),
            Source("b", sourceScope: "NORTH"),
        ]));

        Assert.Contains("exactly one source owner", exception.Message);
    }

    [Fact]
    public void SharedTable_WithTwoUnscopedSources_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SourceRegistry([Source("a"), Source("b")]));

        Assert.Contains("exactly one source owner", exception.Message);
    }

    [Fact]
    public void SharedTableName_WithDifferentDefinitionInstances_IsRejected()
    {
        var twin = new SnapshotTableDefinition("Widget", [new SnapshotColumn("Code", "VARCHAR")]);
        var exception = Assert.Throws<ArgumentException>(() =>
            new SourceRegistry([Source("a"), Source("b", table: twin)]));
        Assert.Contains("share one instance", exception.Message);
    }

    [Fact]
    public void SharedTable_WithDifferentFamiliesLists_IsRejected()
    {
        CosmosFamilyMapping Mapping() => new()
        {
            Family = "F",
            Database = "D",
            Container = "C",
            Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?>() },
        };

        var exception = Assert.Throws<ArgumentException>(() => new SourceRegistry(
        [
            Source("a", families: [Mapping()]),
            Source("b", families: [Mapping()]),   // different list instance, same table
        ]));
        Assert.Contains("Families", exception.Message);
    }

    [Fact]
    public void SharedTable_WithTheSameFamiliesInstance_IsAccepted()
    {
        IReadOnlyList<CosmosFamilyMapping> shared =
        [
            new CosmosFamilyMapping
            {
                Family = "F",
                Database = "D",
                Container = "C",
                Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?>() },
            },
        ];

        var registry = new SourceRegistry(
        [
            Source("a", sourceScope: "a", families: shared),
            Source("b", sourceScope: "b", families: shared),
        ]);
        Assert.Equal(2, registry.Sources.Count);
        Assert.Single(registry.Tables);
    }

    [Fact]
    public void SharedTable_WithDifferentReplicationBatchSizes_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SourceRegistry(
        [
            Source("a", replicationBatchSize: 500),
            Source("b", replicationBatchSize: 1000),
        ]));

        Assert.Contains("ReplicationBatchSize", exception.Message);
    }

    [Fact]
    public void SharedTable_WithDifferentReplicationDegrees_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SourceRegistry(
        [
            Source("a", replicationMaxInFlightRows: 2),
            Source("b", replicationMaxInFlightRows: 4),
        ]));

        Assert.Contains("ReplicationMaxInFlightRows", exception.Message);
    }

    [Fact]
    public void SharedTable_WithDifferentReplicationGates_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SourceRegistry(
        [
            Source("a", replicationEnabled: true),
            Source("b", replicationEnabled: false),
        ]));

        Assert.Contains("ReplicationEnabled", exception.Message);
    }

    [Fact]
    public void DisabledSources_StillContributeTheirTables()
    {
        var darkTable = new SnapshotTableDefinition("Dark", [new SnapshotColumn("X", "VARCHAR")]);
        var registry = new SourceRegistry([Source("live"), Source("dark", table: darkTable, enabled: false)]);

        // "Not enabled" must never mean "missing from the publish set".
        Assert.Equal(["Widget", "Dark"], registry.Tables.Select(t => t.Name));
    }

    // ---- Exactly one ingest delegate -----------------------------------------------------------

    private static SnapshotSource AsyncSource(
        string key,
        bool alsoSynchronous = false,
        CosmosSourceRead? cosmosRead = null,
        Func<SnapshotSourceContext, Task<SnapshotMergeResult>>? ingestAsync = null) => new()
        {
            Key = key,
            RecordIdentity = SourceRecordIdentityDescriptor.DatabaseKey(WidgetTable.Columns[0].Name),
            Table = WidgetTable,
            Cadence = TimeSpan.FromMinutes(1),
            CosmosRead = cosmosRead,
            IngestAsync = ingestAsync
                          ?? (_ => throw new InvalidOperationException("Not meant to run in these tests.")),
            Ingest = alsoSynchronous
                ? _ => throw new InvalidOperationException("Not meant to run in these tests.")
                : null!,
        };

    [Fact]
    public void AnAsyncOnlySource_IsValid()
    {
        var registry = new SourceRegistry([AsyncSource("cosmos-thing")]);

        var source = registry["cosmos-thing"];
        Assert.False(source.HasSynchronousIngest);
        Assert.NotNull(source.IngestAsync);
    }

    [Fact]
    public void ASourceWithNeitherIngestDelegate_IsRejected()
    {
        // It would be scheduled on its cadence and then do nothing at all.
        var exception = Assert.Throws<ArgumentException>(() => new SourceRegistry(
        [
            new SnapshotSource
            {
                Key = "empty",
                RecordIdentity = SourceRecordIdentityDescriptor.DatabaseKey(WidgetTable.Columns[0].Name),
                Table = WidgetTable,
                Cadence = TimeSpan.FromMinutes(1),
            },
        ]));

        Assert.Contains("exactly one of Ingest or IngestAsync", exception.Message);
    }

    [Fact]
    public void ASourceWithBothIngestDelegates_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SourceRegistry([AsyncSource("both", alsoSynchronous: true)]));

        Assert.Contains("it has both", exception.Message);
    }

    [Fact]
    public void ReadingIngest_OnAnAsyncSource_NamesTheMistake()
    {
        // Rather than a NullReferenceException inside a harness that fans out over a registry.
        var exception = Assert.Throws<InvalidOperationException>(() => AsyncSource("cosmos-thing").Ingest);

        Assert.Contains("declares IngestAsync", exception.Message);
        Assert.Contains(nameof(SnapshotSource.RunIngestAsync), exception.Message);
    }

    [Fact]
    public async Task RunIngestAsync_DispatchesToWhicheverDelegateTheSourceDeclares()
    {
        using var snapshot = new TestSnapshot();
        var context = new SnapshotSourceContext
        {
            Store = snapshot.Store,
            CancellationToken = TestContext.Current.CancellationToken,
        };

        var synchronous = new SnapshotSource
        {
            Key = "sync",
            RecordIdentity = SourceRecordIdentityDescriptor.DatabaseKey(WidgetTable.Columns[0].Name),
            Table = WidgetTable,
            Cadence = TimeSpan.FromMinutes(1),
            Ingest = _ => new SnapshotMergeResult("sync-run", SnapshotMergeStatus.Succeeded, 1, 1, 0, 0),
        };
        var asynchronous = AsyncSource("async", ingestAsync: _ => Task.FromResult(
            new SnapshotMergeResult("async-run", SnapshotMergeStatus.Succeeded, 2, 2, 0, 0)));

        Assert.Equal("sync-run", (await synchronous.RunIngestAsync(context)).RunId);
        Assert.Equal("async-run", (await asynchronous.RunIngestAsync(context)).RunId);
    }

    [Fact]
    public void ACosmosReadMissingItsContainer_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SourceRegistry(
        [
            AsyncSource("cosmos-thing", cosmosRead: new CosmosSourceRead("Logs", "  ")),
        ]));

        Assert.Contains("must name both a database and a container", exception.Message);
    }
}
