using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public sealed class GroupedCosmosReplicationTests : IDisposable
{
    private sealed class SourceRow
    {
        public string? PartNumber { get; set; }
        public long? SourceOrdinal { get; set; }
        public string? Scalar { get; set; }
        public string? Supersession { get; set; }
        [SnapshotIgnoreForReplication]
        public string? SourceOnly { get; set; }
    }

    private sealed record Projection(string PartNumber, string? Scalar, IReadOnlyList<string> Supersessions);

    private readonly SnapshotStore store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
    private readonly SnapshotTableDefinition<SourceRow> table = new("GroupedPart");

    public GroupedCosmosReplicationTests() => store.EnsureTable(table);

    public void Dispose() => store.Dispose();

    [Fact]
    public async Task BatchSize_CountsDistinctChangedGroups_AndLoadsEachAffectedGroupSetWise()
    {
        Merge(
            Row("A", 1, "a-first", "A-2"),
            Row("A", 2, "a-second", "A-3"),
            Row("B", 1, "b-first", null),
            Row("C", 1, "c-first", null));
        var projected = new ConcurrentQueue<string>();
        var family = Family(projected);
        var state = new CountingStateStore(store);
        var transport = new FakeTransport();

        var result = await new CosmosSnapshotReplicator(state, transport).RunOnceAsync(
            Options(family, batchSize: 2, maxInFlight: 2),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.GroupsRead);
        Assert.Equal(2, result.GroupsRecomputed);
        Assert.Equal(3, result.RowsRead);          // A has two changed rows; B has one.
        Assert.Equal(3, result.SourceRowsLoaded); // all live rows for A + B, in one grouped read.
        Assert.Equal(["A", "B"], projected.Order().ToArray());
        Assert.Equal(["A", "B"], transport.Upserted.Order().ToArray());
        Assert.Equal(1, state.GroupReadCalls);
        Assert.Equal(1, store.CountDirtyRows(table));
        var documentA = Assert.Single(transport.Documents, document => document.Id == "A");
        Assert.Equal("a-first", documentA.Body["Scalar"]);
        Assert.Equal(["A-2", "A-3"], Assert.IsAssignableFrom<IReadOnlyList<string>>(
            documentA.Body["Supersessions"]));

        var recon = await new SnapshotRecon(store, cosmosClient: null).RunAsync(
            new SnapshotReconOptions
            {
                Tables = [table],
                Families =
                [
                    new SnapshotReconFamily
                    {
                        Mapping = family,
                        EnumerationSql = "SELECT * FROM c",
                        PartitionKeyPaths = ["/PartNumber"],
                    },
                ],
            },
            (_, _) => AsAsync(transport.Documents.Select(ToJson)),
            TestContext.Current.CancellationToken);
        var verdict = Assert.Single(recon.Families);
        Assert.Equal(3, verdict.ExpectedDocs);
        Assert.Equal(2, verdict.ActualDocs);
        Assert.Equal(2, verdict.InSync);
        Assert.Equal(1, verdict.PendingAdd);
        Assert.Equal(0, verdict.DuplicateExpectedCoordinates);
    }

    [Fact]
    public async Task OnlyAffectedGroup_IsRecomputed_AndCanonicalNoChangeSkipsCosmos()
    {
        var rows = new[]
        {
            Row("A", 1, "a-first", "A-2"),
            Row("A", 2, "a-second", "A-3"),
            Row("B", 1, "b-first", null),
        };
        Merge(rows);
        var projected = new ConcurrentQueue<string>();
        var family = Family(projected);
        var transport = new FakeTransport();
        var replicator = new CosmosSnapshotReplicator(new SnapshotReplicationStateStore(store), transport);
        await replicator.DrainAsync(
            Options(family, batchSize: 10, maxInFlight: 2),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, store.CountDirtyRows(table));
        var initialRemoteCalls = transport.Calls;
        projected.Clear();

        // A destination-tracked source row changed, so group A is recomputed. It is not the
        // first row and contributes no supersession change, therefore the canonical document
        // is byte-for-byte unchanged and no Cosmos request is justified.
        rows[1] = rows[1] with { Scalar = "changed-but-not-first" };
        Merge(rows);
        var result = await replicator.RunOnceAsync(
            Options(family, batchSize: 10, maxInFlight: 2),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.RowsRead);
        Assert.Equal(1, result.GroupsRead);
        Assert.Equal(1, result.GroupsRecomputed);
        Assert.Equal(2, result.SourceRowsLoaded);
        Assert.Equal(["A"], projected.ToArray());
        Assert.Equal(0, result.Upserted);
        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.NoOpRows);
        Assert.Equal(initialRemoteCalls, transport.Calls);
        Assert.Equal(0, store.CountDirtyRows(table));
    }

    [Fact]
    public async Task RowMapping_DestinationTrackedChangeWithSameCanonicalDocument_SkipsCosmos()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge([("K001", " value ", 1)]);
        var family = new CosmosFamilyMapping
        {
            Family = "Widget",
            Database = "Test",
            Container = "Widgets",
            Map = row => new CosmosDocument
            {
                Id = row.PrimaryKey,
                PartitionKey = [row.PrimaryKey],
                Body = new Dictionary<string, object?>
                {
                    ["Code"] = ((string)row.Values["Code"]!).Trim(),
                    ["Quantity"] = row.Values["Quantity"],
                },
            },
        };
        var transport = new FakeTransport();
        var replicator = new CosmosSnapshotReplicator(
            new SnapshotReplicationStateStore(snapshot.Store), transport);
        await replicator.DrainAsync(new CosmosSnapshotReplicatorOptions
        {
            Table = snapshot.Table,
            Families = [family],
        }, cancellationToken: TestContext.Current.CancellationToken);
        var initialCalls = transport.Calls;

        snapshot.Merge([("K001", "value", 1)]);
        var result = await replicator.RunOnceAsync(new CosmosSnapshotReplicatorOptions
        {
            Table = snapshot.Table,
            Families = [family],
        }, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.RowsRead);
        Assert.Equal(0, result.Upserted);
        Assert.Equal(1, result.NoOpRows);
        Assert.Equal(initialCalls, transport.Calls);
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    private CosmosFamilyMapping Family(ConcurrentQueue<string> projected)
    {
        var grouping = new CosmosGroupProjection<SourceRow, string, Projection>(
            table,
            row => row.PartNumber!,
            row => row.SourceOrdinal,
            (partNumber, rows) =>
            {
                projected.Enqueue(partNumber);
                if (string.IsNullOrWhiteSpace(partNumber) || rows.Count == 0)
                    return null;
                return new Projection(
                    partNumber,
                    rows[0].Scalar,
                    rows.Select(row => row.Supersession)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value!)
                        .Distinct(StringComparer.Ordinal)
                        .ToList());
            },
            (partNumber, projection) => new CosmosDocument
            {
                Id = partNumber,
                PartitionKey = [partNumber],
                Body = new Dictionary<string, object?>
                {
                    ["PartNumber"] = projection.PartNumber,
                    ["Scalar"] = projection.Scalar,
                    ["Supersessions"] = projection.Supersessions,
                },
            });

        return new CosmosFamilyMapping
        {
            Family = "CatalogPart",
            Database = "Test",
            Container = "Parts",
            Grouping = grouping,
        };
    }

    private CosmosSnapshotReplicatorOptions Options(
        CosmosFamilyMapping family,
        int batchSize,
        int maxInFlight) => new()
    {
        Table = table,
        Families = [family],
        BatchSize = batchSize,
        MaxInFlightRows = maxInFlight,
    };

    private static InputRow Row(
        string partNumber,
        long ordinal,
        string scalar,
        string? supersession,
        string sourceOnly = "metadata") =>
        new(partNumber, ordinal, scalar, supersession, sourceOnly);

    private void Merge(params InputRow[] rows)
    {
        var staging = store.CreateStagingTable(table);
        foreach (var row in rows)
        {
            store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName}
                    ("PartNumber", "SourceOrdinal", "Scalar", "Supersession", "SourceOnly",
                     "_PrimaryKey", "_RowHash", "_ReplicationHash", "_SourceModified")
                VALUES (?, ?, ?, ?, ?, ?, NULL, NULL, NULL)
                """,
                row.PartNumber,
                row.Ordinal,
                row.Scalar,
                row.Supersession,
                row.SourceOnly,
                $"{row.PartNumber}|{row.Ordinal:D4}");
        }

        store.Execute(
            $"""
            UPDATE {staging.QualifiedName}
            SET "_RowHash" = {RowHash.Expression(table.Columns.Select(column => column.Name))},
                "_ReplicationHash" = {RowHash.Expression(table.ReplicationColumns.Select(column => column.Name))}
            """);
        var result = SnapshotMerge.Execute(store, table, staging, new SnapshotMergeOptions
        {
            Source = "group-test",
            DeletesEnabled = true,
        });
        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
    }

    private sealed record InputRow(
        string PartNumber,
        long Ordinal,
        string Scalar,
        string? Supersession,
        string SourceOnly);

    private sealed class FakeTransport : ICosmosSnapshotTransport
    {
        private readonly FakeContainer container = new();

        public int Calls => container.Calls;
        public IReadOnlyCollection<string> Upserted => container.Upserted;
        public IReadOnlyCollection<CosmosDocument> Documents => container.Documents.Values.ToArray();

        public ICosmosSnapshotContainer GetContainer(string database, string containerName) => container;
    }

    private sealed class FakeContainer : ICosmosSnapshotContainer
    {
        private int calls;
        public int Calls => Volatile.Read(ref calls);
        public ConcurrentQueue<string> Upserted { get; } = new();
        public ConcurrentDictionary<string, CosmosDocument> Documents { get; } = new(StringComparer.Ordinal);

        public Task<CosmosTransportResponse> UpsertAsync(
            CosmosDocument document,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref calls);
            Upserted.Enqueue(document.Id);
            Documents[document.Id] = document;
            return Task.FromResult(new CosmosTransportResponse(
                HttpStatusCode.OK, true, 1, RetryAfter: null, ErrorMessage: null));
        }

        public Task<CosmosTransportResponse> DeleteAsync(
            string id,
            IReadOnlyList<object?> partitionKey,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref calls);
            Documents.TryRemove(id, out _);
            return Task.FromResult(new CosmosTransportResponse(
                HttpStatusCode.NoContent, true, 1, RetryAfter: null, ErrorMessage: null));
        }
    }

    private static JsonObject ToJson(CosmosDocument document)
    {
        var json = JsonSerializer.SerializeToNode(document.Body)!.AsObject();
        json["id"] = document.Id;
        return json;
    }

    private static async IAsyncEnumerable<JsonObject> AsAsync(IEnumerable<JsonObject> documents)
    {
        foreach (var document in documents)
            yield return document;
        await Task.CompletedTask;
    }

    private sealed class CountingStateStore(SnapshotStore store) : IReplicationStateStore
    {
        private readonly IReplicationStateStore inner = new SnapshotReplicationStateStore(store);
        private int groupReadCalls;

        public int GroupReadCalls => Volatile.Read(ref groupReadCalls);

        public IReadOnlyList<DirtyRow> ReadDirtyRows(
            SnapshotTableDefinition sourceTable,
            string? afterPrimaryKey,
            int limit) =>
            inner.ReadDirtyRows(sourceTable, afterPrimaryKey, limit);

        public ReplicationGroupPage ReadReplicationGroups(
            SnapshotTableDefinition sourceTable,
            CosmosGroupProjection grouping,
            string? afterGroupKey,
            int limit,
            bool dirtyGroupsOnly)
        {
            Interlocked.Increment(ref groupReadCalls);
            return inner.ReadReplicationGroups(sourceTable, grouping, afterGroupKey, limit, dirtyGroupsOnly);
        }

        public void PruneReconOps(SnapshotTableDefinition sourceTable) => inner.PruneReconOps(sourceTable);
        public void AppendReconOp(ReplicationReconOperation operation) => inner.AppendReconOp(operation);
        public void CommitReplicationOutcomes(
            SnapshotTableDefinition sourceTable,
            IReadOnlyList<ReplicationStateOutcome> outcomes,
            Action ensureCommitAllowed) =>
            inner.CommitReplicationOutcomes(sourceTable, outcomes, ensureCommitAllowed);
    }
}
