using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Pins the replication watermark semantics — the same contract ShiftEntity pins in
/// ShiftEntity.Tests/Replication/ReplicationWatermarkTests.cs for IShiftEntityReplication:
/// the watermark written on success is the <c>_LastModified</c> CAPTURED when the row was
/// loaded — never the current time, never the row's latest value. This is the single easiest
/// semantic to "simplify" into a data-loss bug; these tests are the tripwire.
/// </summary>
public class ReplicationWatermarkTests : IDisposable
{
    private static readonly DateTime T1 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

    private readonly TestSnapshot snapshot = new();
    private SnapshotStore Store => snapshot.Store;
    private SnapshotTableDefinition Table => snapshot.Table;

    [Fact]
    public void MarkReplicated_StampsTheCapturedLastModified_NeverTheCurrentTime()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);

        var dirty = Assert.Single(Store.ReadDirtyRows(Table));
        Assert.Equal(T1, dirty.CapturedLastModified);

        Store.MarkReplicated(Table, dirty.PrimaryKey, dirty.CapturedLastModified, "{\"id\":\"W1\"}");

        var watermark = snapshot.Scalar<DateTime>(
            "SELECT \"_LastReplicationDate\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W1");

        Assert.Equal(T1, watermark);
        Assert.Equal(0, Store.CountDirtyRows(Table));
    }

    [Fact]
    public void MergeDuringReplication_LeavesTheRowDue_ForTheNextCycle()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);
        var captured = Assert.Single(Store.ReadDirtyRows(Table)).CapturedLastModified;

        // While the push is in flight, the source changes and a merge lands the new version.
        snapshot.Merge([("W1", "alpha", 99)], sourceModified: T2);

        // The push then succeeds and stamps what it CAPTURED (T1) — not the row's new value.
        Store.MarkReplicated(Table, "W1", captured, "{\"id\":\"W1\"}");

        // Captured (T1) < current _LastModified (T2) → the edit is still due.
        Assert.Equal(1, Store.CountDirtyRows(Table));
        var redue = Assert.Single(Store.ReadDirtyRows(Table));
        Assert.Equal(T2, redue.CapturedLastModified);
    }

    [Fact]
    public void MarkReplicated_ExactEquality_MeansInSync()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);
        Store.MarkReplicated(Table, "W1", T1, null);

        Assert.Equal(0, Store.CountDirtyRows(Table));
        Assert.Empty(Store.ReadDirtyRows(Table));
    }

    [Fact]
    public void MarkReplicated_WritesTheWatermarkAndTheStampTogether()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);

        Assert.Null(snapshot.ScalarOrNull("SELECT \"_LastReplicationDate\" FROM data.\"Widget\""));
        Assert.Null(snapshot.ScalarOrNull("SELECT \"_ReplicationStamp\" FROM data.\"Widget\""));

        Store.MarkReplicated(Table, "W1", T1, "{\"id\":\"W1\",\"pk\":[\"V\",\"T\",\"7\"]}");

        Assert.NotNull(snapshot.ScalarOrNull("SELECT \"_LastReplicationDate\" FROM data.\"Widget\""));
        Assert.Equal("{\"id\":\"W1\",\"pk\":[\"V\",\"T\",\"7\"]}",
            snapshot.ScalarOrNull("SELECT \"_ReplicationStamp\" FROM data.\"Widget\""));
        Assert.NotNull(snapshot.ScalarOrNull("SELECT \"_ReplicatedAt\" FROM data.\"Widget\""));
    }

    [Fact]
    public void UnchangedRows_AreNeverTouched_SoACleanRowStaysClean()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);
        Store.MarkReplicated(Table, "W1", T1, null);

        // Re-ingesting identical content must not bump _LastModified (IS DISTINCT FROM discipline)…
        var rerun = snapshot.Merge([("W1", "alpha", 1)], sourceModified: T2);

        Assert.Equal(0, rerun.RowsUpdated);
        Assert.Equal(T1, snapshot.Scalar<DateTime>("SELECT \"_LastModified\" FROM data.\"Widget\""));

        // …which is exactly what keeps the replicated row out of the dirty predicate.
        Assert.Equal(0, Store.CountDirtyRows(Table));
    }

    [Fact]
    public void FailedRows_DeadLetterAtMaxAttempts_AndReturnOnReset()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);

        for (var attempt = 0; attempt < SnapshotStore.MaxReplicationAttempts; attempt++)
        {
            Assert.Equal(1, Store.CountDirtyRows(Table));
            Store.MarkReplicationFailed(Table, "W1", T1, $"attempt {attempt + 1} failed");
        }

        // Dead-lettered: out of the dirty predicate, error preserved for diagnostics.
        Assert.Equal(0, Store.CountDirtyRows(Table));
        Assert.Equal($"attempt {SnapshotStore.MaxReplicationAttempts} failed",
            snapshot.ScalarOrNull("SELECT \"_ReplicationError\" FROM data.\"Widget\""));

        Assert.Equal(1, Store.ResetReplicationFailures(Table));
        Assert.Equal(1, Store.CountDirtyRows(Table));
    }

    [Fact]
    public void TombstonedRows_AreDirty_UntilTheDeleteIsReplicated()
    {
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)], sourceModified: T1);
        foreach (var row in Store.ReadDirtyRows(Table))
            Store.MarkReplicated(Table, row.PrimaryKey, row.CapturedLastModified, null);
        Assert.Equal(0, Store.CountDirtyRows(Table));

        // W2 vanishes from the full universe → tombstoned → dirty again as a delete.
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T2);

        var dirty = Assert.Single(Store.ReadDirtyRows(Table));
        Assert.Equal("W2", dirty.PrimaryKey);
        Assert.True(dirty.Deleted);

        Store.MarkReplicated(Table, "W2", dirty.CapturedLastModified, null);
        Assert.Equal(0, Store.CountDirtyRows(Table));
    }

    public void Dispose() => snapshot.Dispose();
}
