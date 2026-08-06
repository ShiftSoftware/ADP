using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Pins the monotonic-_LastModified guarantee: clock skew between the source's save dates and
/// the agent's clock — in either direction — must never take a changed row out of the dirty
/// predicate. Every scenario here was a reproduced silent-data-loss path before the
/// greatest(candidate, previous + 1µs) stamp discipline.
/// </summary>
public class StampMonotonicityTests : IDisposable
{
    private static readonly DateTime T1 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly TestSnapshot snapshot = new();
    private SnapshotStore Store => snapshot.Store;
    private SnapshotTableDefinition Table => snapshot.Table;

    private void ReplicateEverything()
    {
        foreach (var row in Store.ReadDirtyRows(Table))
            Store.MarkReplicated(Table, row.PrimaryKey, row.CapturedLastModified, null);
        Assert.Equal(0, Store.CountDirtyRows(Table));
    }

    [Fact]
    public void TombstoneOnASourceStampedRow_IsDue_EvenWhenTheAgentClockLagsTheSource()
    {
        // Source reports naive local time ahead of the agent's UTC (the UTC+3 case).
        var sourceAhead = DateTime.UtcNow.AddHours(3);
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: sourceAhead);
        ReplicateEverything();

        // Row vanishes; the tombstone stamps agent clock — which is BEHIND the watermark.
        // (force: this wipes the whole 1-row universe, which the guardrail now rightly
        // aborts by default — the subject here is stamp semantics, not the guard.)
        var result = snapshot.Merge([], force: true);

        Assert.Equal(1, result.RowsTombstoned);
        var dirty = Assert.Single(Store.ReadDirtyRows(Table));
        Assert.True(dirty.Deleted);
        Assert.True(dirty.CapturedLastModified > sourceAhead);
    }

    [Fact]
    public void ResurrectionWithAnOlderSourceStamp_AfterAReplicatedDelete_IsStillDue()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);
        ReplicateEverything();

        snapshot.Merge([], force: true);       // tombstoned (agent clock; force — full 1-row wipe)
        ReplicateEverything();                 // the Cosmos delete is delivered

        // The row returns carrying its ORIGINAL source save date (recovered file, key-sweep glitch).
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);

        var dirty = Assert.Single(Store.ReadDirtyRows(Table));
        Assert.False(dirty.Deleted);
        Assert.Equal(1, Store.CountDirtyRows(Table));
    }

    [Fact]
    public void AContentChangeWhoseSourceStampRegressed_IsStillDue()
    {
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T2);
        ReplicateEverything();

        // Content changes but the source save date moves BACKWARD (restore/backfill class).
        var result = snapshot.Merge([("W1", "alpha", 999)], sourceModified: T1);

        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal(999, snapshot.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\""));
        Assert.Equal(1, Store.CountDirtyRows(Table));
    }

    [Fact]
    public void AContentChangeWithinTheSameSourceStamp_IsStillDue()
    {
        // Two edits inside one save-date granule: hash changes, stamp does not advance.
        snapshot.Merge([("W1", "alpha", 1)], sourceModified: T1);
        ReplicateEverything();

        var result = snapshot.Merge([("W1", "alpha", 2)], sourceModified: T1);

        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal(1, Store.CountDirtyRows(Table));
    }

    public void Dispose() => snapshot.Dispose();
}
