using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// What a Cosmos outage must NOT do: burn every row's 5-attempt ledger inside one cycle.
/// The wet pump pages by cursor (a failed row is not re-read in the same drain) and the
/// dispatcher breaks the drain when an entire batch fails. Deleting a dead-lettered row
/// must also re-open it for its Cosmos delete.
/// </summary>
public class PumpResilienceTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    public void Dispose() => snapshot.Dispose();

    private static IEnumerable<(string, string, int)> Rows(int count) =>
        Enumerable.Range(1, count).Select(i => ($"K{i:D4}", $"code-{i}", i));

    private static readonly CosmosFamilyMapping Family = new()
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
    };

    [Fact]
    public void TheWetDrainCursor_MovesPastRowsItAlreadyTouched()
    {
        snapshot.Merge(Rows(30));

        // Simulate a drain: batch 1 fails every row (nothing stamped, all still dirty).
        var first = snapshot.Store.ReadDirtyRows(snapshot.Table, null, 10);
        foreach (var row in first)
            snapshot.Store.MarkReplicationFailed(
                snapshot.Table, row.PrimaryKey, row.CapturedLastModified, "cosmos unreachable");

        // WITHOUT a cursor the next read returns the same cohort — the mass-dead-letter bug.
        var cursorless = snapshot.Store.ReadDirtyRows(snapshot.Table, 10);
        Assert.Equal(first.Select(r => r.PrimaryKey), cursorless.Select(r => r.PrimaryKey));

        // WITH the cursor the drain advances, so one cycle costs each row at most one attempt.
        var next = snapshot.Store.ReadDirtyRows(snapshot.Table, first[^1].PrimaryKey, 10);
        Assert.Equal("K0011", next[0].PrimaryKey);
        Assert.All(next, row => Assert.DoesNotContain(row.PrimaryKey, first.Select(f => f.PrimaryKey)));

        // Each failed row carries exactly one attempt, not five.
        Assert.Equal(1L, snapshot.Scalar<long>(
            "SELECT max(\"_ReplicationAttempts\") FROM data.\"Widget\""));
    }

    [Fact]
    public async Task DryRunPaging_IsUnaffectedByTheCursorOption()
    {
        snapshot.Merge(Rows(25));
        var replicator = new CosmosSnapshotReplicator(snapshot.Store, cosmosClient: null);

        var result = await replicator.RunOnceAsync(new CosmosSnapshotReplicatorOptions
        {
            Table = snapshot.Table,
            Families = [Family],
            BatchSize = 10,
            DryRun = true,
        }, TestContext.Current.CancellationToken);

        Assert.Equal(25, result.RowsRead);
        Assert.False(result.HasMore);
    }

    [Fact]
    public void ADeadLetteredRow_ReturnsToTheQueueWhenItIsDeletedAtSource()
    {
        snapshot.Merge([("K1", "alpha", 1), ("K2", "beta", 2)]);

        // K1 exhausts its attempts and leaves the dirty pool.
        var captured = snapshot.Store.ReadDirtyRows(snapshot.Table).Single(row => row.PrimaryKey == "K1").CapturedLastModified;
        for (var attempt = 0; attempt < SnapshotStore.MaxReplicationAttempts; attempt++)
            snapshot.Store.MarkReplicationFailed(snapshot.Table, "K1", captured, "poison");
        Assert.Equal(1L, snapshot.Store.CountDirtyRows(snapshot.Table));   // only K2

        // K1 then disappears from the source. Its Cosmos document must still be deleted —
        // a tombstone is a new row version, so the ledger resets like the update leg's.
        var merge = snapshot.Merge([("K2", "beta", 2)]);

        Assert.Equal(1, merge.RowsTombstoned);
        Assert.Equal(0L, snapshot.Scalar<long>(
            "SELECT \"_ReplicationAttempts\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'K1'"));
        Assert.Equal(2L, snapshot.Store.CountDirtyRows(snapshot.Table));   // K1's delete is queued again
    }

    [Fact]
    public void AContentChange_StillResetsTheLedger()
    {
        snapshot.Merge([("K1", "alpha", 1)]);
        var captured = snapshot.Store.ReadDirtyRows(snapshot.Table).Single().CapturedLastModified;
        for (var attempt = 0; attempt < SnapshotStore.MaxReplicationAttempts; attempt++)
            snapshot.Store.MarkReplicationFailed(snapshot.Table, "K1", captured, "poison");
        Assert.Equal(0L, snapshot.Store.CountDirtyRows(snapshot.Table));

        snapshot.Merge([("K1", "alpha", 2)]);

        Assert.Equal(1L, snapshot.Store.CountDirtyRows(snapshot.Table));
        Assert.Equal(0L, snapshot.Scalar<long>(
            "SELECT \"_ReplicationAttempts\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'K1'"));
    }
}
