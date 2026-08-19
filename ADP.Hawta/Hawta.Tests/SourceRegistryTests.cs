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
}
