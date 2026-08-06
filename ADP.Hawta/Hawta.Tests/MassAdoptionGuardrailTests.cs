using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The mass-adoption guardrail — the delete guardrail's mirror image. Adoption exists so
/// two scopes can't tombstone/resurrect a migrating row forever; a BULK adoption is instead
/// the mis-pasted-connection-string signature (dealer B pointed at dealer A's database),
/// which in wet mode would rewrite an entire estate under the wrong dealer's coordinates.
/// </summary>
public class MassAdoptionGuardrailTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    public void Dispose() => snapshot.Dispose();

    private static IEnumerable<(string, string, int)> Rows(int count) =>
        Enumerable.Range(1, count).Select(i => ($"K{i:D4}", $"code-{i}", i));

    [Fact]
    public void AWholeEstateTakeover_IsAborted_AndNothingChanges()
    {
        snapshot.Merge(Rows(200), scope: "AAD");

        // Dealer BYY is onboarded with AAD's connection string: its "own" universe is
        // AAD's entire estate. Note BYY has ZERO live rows, so the DELETE guardrail can
        // never trip here — this is exactly the gap the adoption guardrail closes.
        var takeover = snapshot.Merge(Rows(200), scope: "BYY");

        Assert.Equal(SnapshotMergeStatus.AbortedMassAdoption, takeover.Status);
        Assert.Equal(200, takeover.RowsRescoped);
        Assert.Equal(0, takeover.RowsInserted);
        Assert.Equal(0, takeover.RowsUpdated);

        // Rolled back: every row still belongs to AAD, unstamped and unmoved.
        Assert.Equal(200L, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_SourceScope\" = 'AAD' AND \"_Deleted\" = false"));
        Assert.Equal(0L, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_SourceScope\" = 'BYY'"));

        // And the abort is on the record, not just in a log.
        Assert.Equal("Aborted:MassAdoption", snapshot.Scalar<string>(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"RunId\" = ?", takeover.RunId));
    }

    [Fact]
    public void ASmallOverlap_StillAdopts_TheGuardIsForBulkTakeovers()
    {
        snapshot.Merge(Rows(200), scope: "AAD");

        // A handful of rows genuinely moving dealers: 3 adopted out of 200 staged.
        var moved = snapshot.Merge(
            Rows(200).Take(3).Concat(Enumerable.Range(300, 197).Select(i => ($"K{i:D4}", $"code-{i}", i))),
            scope: "BYY");

        Assert.True(moved.Succeeded);
        Assert.Equal(3, moved.RowsRescoped);
    }

    [Fact]
    public void ForceAdoptions_PerformsTheMigration()
    {
        snapshot.Merge(Rows(200), scope: "AAD");

        var staging = snapshot.Stage(Rows(200));
        var migration = SnapshotMerge.Execute(snapshot.Store, snapshot.Table, staging, new SnapshotMergeOptions
        {
            Source = "intentional-migration",
            SourceScope = "BYY",
            DeletesEnabled = true,
            ForceAdoptions = true,
        });

        Assert.True(migration.Succeeded);
        Assert.Equal(200, migration.RowsRescoped);
        Assert.Equal(200L, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_SourceScope\" = 'BYY' AND \"_Deleted\" = false"));
    }

    [Fact]
    public void BelowTheAbsoluteFloor_TheGuardNeverTrips()
    {
        // 40 rows, all adopted: 100% — but under the 50-row floor, so this is churn, not a
        // takeover. (Both thresholds must trip, exactly like the delete guardrail.)
        snapshot.Merge(Rows(40), scope: "AAD");
        var adoption = snapshot.Merge(Rows(40), scope: "BYY");

        Assert.True(adoption.Succeeded);
        Assert.Equal(40, adoption.RowsRescoped);
    }

    [Fact]
    public void TombstonedRows_AreNotCountedAsAdoptions()
    {
        snapshot.Merge(Rows(200), scope: "AAD");
        snapshot.Merge([], scope: "AAD", force: true);   // intentional purge of scope AAD

        // Every key exists but is tombstoned: this is a resurrection, not a takeover.
        var resurrection = snapshot.Merge(Rows(200), scope: "BYY");

        Assert.True(resurrection.Succeeded);
        Assert.Equal(0, resurrection.RowsRescoped);
        Assert.Equal(200, resurrection.RowsUpdated);
    }
}
