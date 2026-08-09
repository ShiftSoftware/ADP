using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public class SnapshotMergeTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    [Fact]
    public void InsertsNewRows_AndRecordsTheRun()
    {
        var result = snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.RowsStaged);
        Assert.Equal(2, result.RowsInserted);
        Assert.Equal(0, result.RowsUpdated);
        Assert.Equal(0, result.RowsTombstoned);
        Assert.Equal(2, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));

        Assert.Equal("Succeeded", snapshot.ScalarOrNull(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"RunId\" = ?", result.RunId));
    }

    [Fact]
    public void UpdatesOnlyRowsWhoseHashChanged()
    {
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);

        var result = snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 99)]);

        Assert.Equal(0, result.RowsInserted);
        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal(99, snapshot.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W2"));
    }

    [Fact]
    public void TombstonesVanishedRows_WhenDeletesEnabled()
    {
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);

        var result = snapshot.Merge([("W1", "alpha", 1)], deletesEnabled: true);

        Assert.Equal(1, result.RowsTombstoned);
        Assert.Equal(true, snapshot.ScalarOrNull("SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W2"));
        Assert.NotNull(snapshot.ScalarOrNull("SELECT \"_DeletedAt\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W2"));
    }

    [Fact]
    public void AbsenceMeansUnchanged_WhenDeletesDisabled()
    {
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);

        var result = snapshot.Merge([("W1", "alpha", 1)], deletesEnabled: false);

        Assert.Equal(0, result.RowsTombstoned);
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = true"));
    }

    [Fact]
    public void OneScopesAbsence_CannotTombstoneAnotherScopesRows()
    {
        snapshot.Merge([("A1", "alpha", 1), ("A2", "alpha", 2)], scope: "file-A");
        snapshot.Merge([("B1", "beta", 1)], scope: "file-B");

        // file-A shrinks to one row; file-B's row must be untouched by A's anti-join.
        var result = snapshot.Merge([("A1", "alpha", 1)], scope: "file-A", force: true);

        Assert.Equal(1, result.RowsTombstoned);
        Assert.Equal(true, snapshot.ScalarOrNull("SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "A2"));
        Assert.Equal(false, snapshot.ScalarOrNull("SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "B1"));
    }

    [Fact]
    public void MassDeleteGuardrail_AbortsAndLeavesDataIntact()
    {
        var rows = Enumerable.Range(1, 100).Select(i => ($"W{i}", "alpha", i)).ToList();
        snapshot.Merge(rows);

        // An empty staging (the missing/truncated-file class) would tombstone all 100 rows.
        var result = snapshot.Merge([], minDeletedRowsAbsolute: 10);

        Assert.Equal(SnapshotMergeStatus.AbortedMassDelete, result.Status);
        Assert.Equal(100, result.PendingDeletes);
        Assert.Equal(0, result.RowsTombstoned);
        Assert.Equal(100, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));

        Assert.Equal("Aborted:MassDelete", snapshot.ScalarOrNull(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"RunId\" = ?", result.RunId));
        Assert.NotNull(snapshot.ScalarOrNull(
            "SELECT \"Error\" FROM meta.SyncRuns WHERE \"RunId\" = ?", result.RunId));
    }

    [Fact]
    public void FullWipe_TripsTheGuardrail_EvenBelowTheAbsoluteFloor()
    {
        // The absolute floor waves through trivial churn; it must never wave through a
        // TOTAL wipe (empty staging vs a small family — the 29-row NonJPM class).
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2), ("W3", "gamma", 3)]);

        var result = snapshot.Merge([], minDeletedRowsAbsolute: 50);

        Assert.Equal(SnapshotMergeStatus.AbortedMassDelete, result.Status);
        Assert.Equal(3, result.PendingDeletes);
        Assert.Equal(3, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    [Fact]
    public void ForceDeletes_OverridesTheGuardrail_ForIntentionalPurges()
    {
        var rows = Enumerable.Range(1, 100).Select(i => ($"W{i}", "alpha", i)).ToList();
        snapshot.Merge(rows);

        var result = snapshot.Merge([], minDeletedRowsAbsolute: 10, force: true);

        Assert.True(result.Succeeded);
        Assert.Equal(100, result.RowsTombstoned);
    }

    [Fact]
    public void SmallShrinkage_UnderTheAbsoluteFloor_Passes()
    {
        var rows = Enumerable.Range(1, 60).Select(i => ($"W{i}", "alpha", i)).ToList();
        snapshot.Merge(rows);

        // 40 deletes = 66% (over the percent bar) but under the 50-row absolute floor → allowed.
        var remaining = rows.Take(20).ToList();
        var result = snapshot.Merge(remaining, minDeletedRowsAbsolute: 50);

        Assert.True(result.Succeeded);
        Assert.Equal(40, result.RowsTombstoned);
    }

    [Fact]
    public void ReturningRow_IsResurrected_EvenWithIdenticalContent()
    {
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);
        snapshot.Merge([("W1", "alpha", 1)]);
        Assert.Equal(true, snapshot.ScalarOrNull("SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W2"));

        // W2 comes back with the same content (same hash) — the tombstone must clear anyway.
        var result = snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);

        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal(false, snapshot.ScalarOrNull("SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W2"));
        Assert.Null(snapshot.ScalarOrNull("SELECT \"_DeletedAt\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W2"));
    }

    [Fact]
    public void LargeAbsoluteChurn_UnderThePercentBar_Passes()
    {
        // Pins the AND's other arm: over the absolute floor but under the percent bar → allowed.
        var rows = Enumerable.Range(1, 400).Select(i => ($"W{i}", "alpha", i)).ToList();
        snapshot.Merge(rows);

        var result = snapshot.Merge(rows.Take(340).ToList()); // 60 deletes = 15%, > 50 absolute

        Assert.True(result.Succeeded);
        Assert.Equal(60, result.RowsTombstoned);
    }

    [Fact]
    public void AFailedMerge_RollsBackEverything_AndRecordsAFailedRun()
    {
        snapshot.Merge([("W1", "alpha", 1)]);
        var stampBefore = snapshot.Scalar<DateTime>("SELECT \"_LastModified\" FROM data.\"Widget\"");

        // Hand-built staging whose insert row cannot be cast into the INTEGER target column:
        // the merge must throw, leave W1 untouched, and still write a Failed:Exception run row.
        snapshot.Store.Execute(
            """
            CREATE OR REPLACE TEMP TABLE "bad_staging" (
                "Code" VARCHAR, "Quantity" VARCHAR,
                "_PrimaryKey" VARCHAR NOT NULL, "_RowHash" VARCHAR NOT NULL,
                "_ReplicationHash" VARCHAR, "_SourceModified" TIMESTAMP
            )
            """);
        snapshot.Store.Execute("INSERT INTO \"bad_staging\" VALUES ('alpha', '777', 'W1', 'changed-hash', NULL, NULL)");
        snapshot.Store.Execute("INSERT INTO \"bad_staging\" VALUES ('beta', 'not-a-number', 'W2', 'h2', NULL, NULL)");
        var badStaging = new StagingTable("bad_staging", "temp.main.\"bad_staging\"");

        var options = new SnapshotMergeOptions { Source = "test-source", DeletesEnabled = false };
        Assert.ThrowsAny<Exception>(() => SnapshotMerge.Execute(snapshot.Store, snapshot.Table, badStaging, options));

        Assert.Equal(1, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
        Assert.Equal(1, snapshot.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "W1"));
        Assert.Equal(stampBefore, snapshot.Scalar<DateTime>("SELECT \"_LastModified\" FROM data.\"Widget\""));

        Assert.Equal(1L, snapshot.Scalar<long>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Failed:Exception' AND \"Error\" IS NOT NULL"));
    }

    [Fact]
    public void ATableNamedS_SurvivesTheUpdateLeg()
    {
        // "s" collided with the old staging alias — ambiguous binder reference on update runs.
        var table = new SnapshotTableDefinition("s", [new SnapshotColumn("Code", "VARCHAR")]);
        snapshot.Store.EnsureTable(table);

        foreach (var quantity in new[] { "one", "two" })
        {
            var staging = snapshot.Store.CreateStagingTable(table);
            snapshot.Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", 'K1', {RowHash.Expression(["Code"])}, NULL FROM (SELECT ? AS "Code")
                """,
                quantity);

            var result = SnapshotMerge.Execute(snapshot.Store, table, staging,
                new SnapshotMergeOptions { Source = "test-source" });
            Assert.True(result.Succeeded);
        }

        Assert.Equal("two", snapshot.ScalarOrNull("SELECT \"Code\" FROM data.\"s\""));
    }

    [Fact]
    public void ReusingOneOptionsInstance_AcrossMerges_IsSafe()
    {
        var options = new SnapshotMergeOptions { Source = "test-source", DeletesEnabled = true };

        var first = SnapshotMerge.Execute(snapshot.Store, snapshot.Table,
            snapshot.Stage([("W1", "alpha", 1)]), options);
        var second = SnapshotMerge.Execute(snapshot.Store, snapshot.Table,
            snapshot.Stage([("W1", "alpha", 2)]), options);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.NotEqual(first.RunId, second.RunId);
        Assert.Equal(2, snapshot.Scalar<long>("SELECT count(*) FROM meta.SyncRuns"));
    }

    [Fact]
    public void StagingRowsWithNullHash_FailTheRun_WithAnInvalidStagingRecord()
    {
        var staging = snapshot.Store.CreateStagingTable(snapshot.Table);
        snapshot.Store.Execute(
            $"""
            INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
            VALUES ('alpha', 1, 'W1', NULL, NULL)
            """);

        var result = SnapshotMerge.Execute(snapshot.Store, snapshot.Table, staging,
            new SnapshotMergeOptions { Source = "test-source" });

        Assert.Equal(SnapshotMergeStatus.FailedInvalidStagingRows, result.Status);
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
        Assert.Equal("Failed:InvalidStagingRows", snapshot.ScalarOrNull(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"RunId\" = ?", result.RunId));
    }

    [Fact]
    public void AContentChange_ResetsTheDeadLetterLedger()
    {
        snapshot.Merge([("W1", "alpha", 1)]);
        var captured = snapshot.Store.ReadDirtyRows(snapshot.Table).Single().CapturedLastModified;
        for (var i = 0; i < SnapshotStore.MaxReplicationAttempts; i++)
            snapshot.Store.MarkReplicationFailed(snapshot.Table, "W1", captured, "bad value");
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));

        // The source fixes the offending value → the new row version gets fresh attempts.
        snapshot.Merge([("W1", "alpha", 2)]);

        Assert.Equal(1, snapshot.Store.CountDirtyRows(snapshot.Table));
        Assert.Null(snapshot.ScalarOrNull("SELECT \"_ReplicationError\" FROM data.\"Widget\""));
    }

    [Fact]
    public void DuplicateStagingKeys_FailTheRun_BeforeTouchingData()
    {
        snapshot.Merge([("W1", "alpha", 1)]);

        var result = snapshot.Merge([("W2", "beta", 2), ("W2", "beta", 3)]);

        Assert.Equal(SnapshotMergeStatus.FailedDuplicateStagingKeys, result.Status);
        Assert.Equal(1, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
        Assert.Equal("Failed:DuplicateStagingKeys", snapshot.ScalarOrNull(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"RunId\" = ?", result.RunId));
    }

    public void Dispose() => snapshot.Dispose();
}
