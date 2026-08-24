using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The entire incumbent merge suite, re-run with every merge reaching a DEFERRED table: before
/// each staging the fixture pushes the table's rows out to parquet and marks it Deferred, so
/// the merge must hydrate first and still land on byte-identical results. Inherited tests run
/// unmodified — that is the point. The three overrides below are the failure-path tests whose
/// post-failure assertions legitimately change shape when the rows' home is the published copy:
/// "data intact" then means "still deferred, copy untouched, hydratable", not "rows visible in
/// the table".
/// </summary>
public sealed class DeferredSnapshotMergeTests() : SnapshotMergeTests(deferBeforeEachStage: true)
{
    public override void MassDeleteGuardrail_AbortsAndLeavesDataIntact()
    {
        var rows = Enumerable.Range(1, 100).Select(i => ($"W{i}", "alpha", i)).ToList();
        snapshot.Merge(rows);

        // The abort rolls back the hydration along with everything else: the table goes back
        // to Deferred, and the published copy — the copy of record — is what stayed intact.
        var result = snapshot.Merge([], minDeletedRowsAbsolute: 10);

        Assert.Equal(SnapshotMergeStatus.AbortedMassDelete, result.Status);
        Assert.Equal(100, result.PendingDeletes);
        Assert.Equal(0, result.RowsTombstoned);
        Assert.Equal(SnapshotResidency.Deferred, snapshot.Store.ReadResidency(snapshot.Table.Name));
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));

        Assert.Equal("Aborted:MassDelete", snapshot.ScalarOrNull(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"RunId\" = ?", result.RunId));

        // The copy is not just intact — it is still live: a legitimate follow-up merge
        // hydrates all 100 rows back.
        var recovered = SnapshotMerge.Execute(snapshot.Store, snapshot.Table,
            snapshot.Stage(rows), new SnapshotMergeOptions { Source = "test-source", DeletesEnabled = true });
        Assert.True(recovered.Succeeded);
        Assert.Equal(100, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    public override void FullWipe_TripsTheGuardrail_EvenBelowTheAbsoluteFloor()
    {
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2), ("W3", "gamma", 3)]);

        var result = snapshot.Merge([], minDeletedRowsAbsolute: 50);

        Assert.Equal(SnapshotMergeStatus.AbortedMassDelete, result.Status);
        Assert.Equal(3, result.PendingDeletes);
        Assert.Equal(SnapshotResidency.Deferred, snapshot.Store.ReadResidency(snapshot.Table.Name));
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
    }

    public override void DuplicateStagingKeys_FailTheRun_BeforeTouchingData()
    {
        snapshot.Merge([("W1", "alpha", 1)]);

        // Duplicate staging fails BEFORE the merge transaction opens, so no hydration ran
        // either: the table stays Deferred and the copy of record stays where it was.
        var result = snapshot.Merge([("W2", "beta", 2), ("W2", "beta", 3)]);

        Assert.Equal(SnapshotMergeStatus.FailedDuplicateStagingKeys, result.Status);
        Assert.Equal(SnapshotResidency.Deferred, snapshot.Store.ReadResidency(snapshot.Table.Name));
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
        Assert.Equal("Failed:DuplicateStagingKeys", snapshot.ScalarOrNull(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"RunId\" = ?", result.RunId));

        var recovered = SnapshotMerge.Execute(snapshot.Store, snapshot.Table,
            snapshot.Stage([("W1", "alpha", 1), ("W2", "beta", 2)]),
            new SnapshotMergeOptions { Source = "test-source", DeletesEnabled = true });
        Assert.True(recovered.Succeeded);
        Assert.Equal(2, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
    }

    [Fact]
    public void HydrationMarksTheTableResident_InTheSameTransactionAsTheMerge()
    {
        snapshot.Merge([("W1", "alpha", 1)]);
        Assert.Equal(SnapshotResidency.Resident, snapshot.Store.ReadResidency(snapshot.Table.Name));

        snapshot.DeferCurrentRows();
        Assert.Equal(SnapshotResidency.Deferred, snapshot.Store.ReadResidency(snapshot.Table.Name));
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));

        // A direct merge — no ingestor, no gate, no verdict — still hydrates first (L2).
        var result = SnapshotMerge.Execute(snapshot.Store, snapshot.Table,
            snapshot.Stage([("W1", "alpha", 1), ("W2", "beta", 2)]),
            new SnapshotMergeOptions { Source = "test-source", DeletesEnabled = true });

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.RowsInserted);
        Assert.Equal(0, result.RowsUpdated);
        Assert.Equal(SnapshotResidency.Resident, snapshot.Store.ReadResidency(snapshot.Table.Name));
        Assert.Equal(2, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
    }
}
