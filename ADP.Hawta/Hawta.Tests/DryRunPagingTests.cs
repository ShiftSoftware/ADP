using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Dry-runs stamp nothing, so they must page the FULL dirty set via the primary-key cursor —
/// a limit-only read would return the same first batch forever (invisible with toy row
/// counts; fatal with JPM's 1.4M dirty rows). And because repeated dry-runs accumulate,
/// recon ops prune by default.
/// </summary>
public class DryRunPagingTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    public void Dispose() => snapshot.Dispose();

    private CosmosSnapshotReplicatorOptions Options(int batchSize = 10, bool prune = true, string? runId = null) => new()
    {
        Table = snapshot.Table,
        Families =
        [
            new CosmosFamilyMapping
            {
                Family = "Widget",
                Database = "TestDb",
                Container = "Widgets",
                Map = row => new CosmosDocument
                {
                    Id = row.PrimaryKey,
                    PartitionKey = [(string)row.Values["Code"]!],
                    Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"], ["Quantity"] = row.Values["Quantity"] },
                },
            },
        ],
        BatchSize = batchSize,
        DryRun = true,
        PruneReconOps = prune,
        RunId = runId,
    };

    private static IEnumerable<(string, string, int)> Rows(int count) =>
        Enumerable.Range(1, count).Select(i => ($"W{i:D3}", $"code-{i}", i));

    [Fact]
    public async Task DryRun_PagesTheEntireDirtySet_AndReportsNoMore()
    {
        snapshot.Merge(Rows(25));
        var replicator = new CosmosSnapshotReplicator(snapshot.Store, cosmosClient: null);

        var result = await replicator.RunOnceAsync(Options(batchSize: 10), TestContext.Current.CancellationToken);

        Assert.True(result.DryRun);
        Assert.Equal(25, result.RowsRead);       // 10 + 10 + 5 — cursor-paged, not the same batch thrice
        Assert.Equal(25, result.Upserted);
        Assert.False(result.HasMore);            // a dry-run always finishes the set

        Assert.Equal(25L, snapshot.Scalar<long>(
            "SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = ? AND \"Op\" = 'Upsert'", result.RunId));
        Assert.Equal(25L, snapshot.Store.CountDirtyRows(snapshot.Table));   // untouched queue
    }

    [Fact]
    public async Task RepeatedDryRuns_PruneByDefault_AndAccumulateWhenAskedTo()
    {
        snapshot.Merge(Rows(5));
        var replicator = new CosmosSnapshotReplicator(snapshot.Store, cosmosClient: null);

        var first = await replicator.RunOnceAsync(Options(), TestContext.Current.CancellationToken);
        var second = await replicator.RunOnceAsync(Options(), TestContext.Current.CancellationToken);

        // Only the second run's ops remain — dark-launch dry-runs can repeat forever
        // without growing the table.
        Assert.Equal(0L, snapshot.Scalar<long>("SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = ?", first.RunId));
        Assert.Equal(5L, snapshot.Scalar<long>("SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = ?", second.RunId));

        var third = await replicator.RunOnceAsync(Options(prune: false), TestContext.Current.CancellationToken);
        Assert.Equal(5L, snapshot.Scalar<long>("SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = ?", second.RunId));
        Assert.Equal(5L, snapshot.Scalar<long>("SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = ?", third.RunId));
    }

    [Fact]
    public void PruneReconOps_CanSpareOneRun()
    {
        snapshot.Store.Execute(
            "INSERT INTO meta.ReconOps VALUES ('run-a', 'Widget', 'F', 'K', 'Upsert', NULL, NULL, NULL, ?, ?)",
            DateTime.UtcNow, DateTime.UtcNow);
        snapshot.Store.Execute(
            "INSERT INTO meta.ReconOps VALUES ('run-b', 'Widget', 'F', 'K', 'Upsert', NULL, NULL, NULL, ?, ?)",
            DateTime.UtcNow, DateTime.UtcNow);
        snapshot.Store.Execute(
            "INSERT INTO meta.ReconOps VALUES ('run-c', 'OtherTable', 'F', 'K', 'Upsert', NULL, NULL, NULL, ?, ?)",
            DateTime.UtcNow, DateTime.UtcNow);

        var pruned = snapshot.Store.PruneReconOps(snapshot.Table, keepRunId: "run-b");

        Assert.Equal(1, pruned);
        Assert.Equal(0L, snapshot.Scalar<long>("SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = 'run-a'"));
        Assert.Equal(1L, snapshot.Scalar<long>("SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = 'run-b'"));
        // Other tables' ops are never touched.
        Assert.Equal(1L, snapshot.Scalar<long>("SELECT count(*) FROM meta.ReconOps WHERE \"RunId\" = 'run-c'"));
    }
}
