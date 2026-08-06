using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Dry-run is the dual-run window's writer mode: every intended Cosmos op lands in
/// <c>meta.ReconOps</c>, nothing is pushed, nothing is stamped, the dirty state is
/// untouched. Fully verifiable with no Cosmos at all.
/// </summary>
public class CosmosReplicatorDryRunTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    private static List<CosmosFamilyMapping> Families() =>
    [
        new CosmosFamilyMapping
        {
            Family = "EvenWidget",
            Database = "CompanyData",
            Container = "Widgets",
            Predicate = row => row.Values["Quantity"] is int q && q % 2 == 0,
            Map = row => new CosmosDocument
            {
                Id = row.PrimaryKey,
                PartitionKey = [(string)row.Values["Code"]!, "EvenWidget"],
                Body = new Dictionary<string, object?>
                {
                    ["Code"] = row.Values["Code"],
                    ["Quantity"] = row.Values["Quantity"],
                    ["ItemType"] = "EvenWidget",
                },
            },
        },
        new CosmosFamilyMapping
        {
            Family = "OddWidget",
            Database = "CompanyData",
            Container = "Widgets",
            Predicate = row => row.Values["Quantity"] is int q && q % 2 == 1 && q < 100,
            Map = row => new CosmosDocument
            {
                Id = row.PrimaryKey,
                PartitionKey = [(string)row.Values["Code"]!, "OddWidget"],
                Body = new Dictionary<string, object?>
                {
                    ["Code"] = row.Values["Code"],
                    ["Quantity"] = row.Values["Quantity"],
                    ["ItemType"] = "OddWidget",
                },
            },
        },
    ];

    private Task<ReplicationRunResult> RunAsync() =>
        new CosmosSnapshotReplicator(snapshot.Store, cosmosClient: null)
            .RunOnceAsync(new CosmosSnapshotReplicatorOptions
            {
                Table = snapshot.Table,
                Families = Families(),
                DryRun = true,
            });

    [Fact]
    public async Task DryRun_EmitsIntendedOps_WithoutStampingOrPushing()
    {
        // W1 odd, W2 even, W3 matches neither predicate (excluded).
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2), ("W3", "gamma", 101)]);

        var result = await RunAsync();

        Assert.True(result.DryRun);
        Assert.Equal(3, result.RowsRead);
        Assert.Equal(2, result.Upserted);
        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.Excluded);
        Assert.Equal(0, result.Failed);

        Assert.Equal("OddWidget", snapshot.ScalarOrNull(
            "SELECT \"Family\" FROM meta.ReconOps WHERE \"PrimaryKey\" = ? AND \"Op\" = 'Upsert'", "W1"));
        Assert.Equal("beta|EvenWidget", snapshot.ScalarOrNull(
            "SELECT \"PartitionKey\" FROM meta.ReconOps WHERE \"PrimaryKey\" = ?", "W2"));
        Assert.NotNull(snapshot.ScalarOrNull(
            "SELECT \"DocHash\" FROM meta.ReconOps WHERE \"PrimaryKey\" = ?", "W2"));
        Assert.Equal("Excluded", snapshot.ScalarOrNull(
            "SELECT \"Op\" FROM meta.ReconOps WHERE \"PrimaryKey\" = ?", "W3"));

        // Nothing was stamped: every row is still dirty, and the recon is repeatable.
        Assert.Equal(3, snapshot.Store.CountDirtyRows(snapshot.Table));
        Assert.Equal(0L, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_ReplicationStamp\" IS NOT NULL"));
    }

    [Fact]
    public async Task DryRun_DocHash_IsCanonical_PropertyOrderDoesNotMatter()
    {
        var a = new CosmosDocument
        {
            Id = "X",
            PartitionKey = ["p"],
            Body = new Dictionary<string, object?> { ["b"] = 1, ["a"] = "v" },
        };
        var b = new CosmosDocument
        {
            Id = "X",
            PartitionKey = ["p"],
            Body = new Dictionary<string, object?> { ["a"] = "v", ["b"] = 1 },
        };

        Assert.Equal(CosmosDocHash.Compute(a), CosmosDocHash.Compute(b));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WetMode_WithoutAClient_Throws()
    {
        snapshot.Merge([("W1", "alpha", 1)]);
        var replicator = new CosmosSnapshotReplicator(snapshot.Store, cosmosClient: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            replicator.RunOnceAsync(new CosmosSnapshotReplicatorOptions
            {
                Table = snapshot.Table,
                Families = Families(),
                DryRun = false,
            }));
    }

    public void Dispose() => snapshot.Dispose();
}
