using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Two-table fixture over a throwaway publish directory: the publisher's contract needs a
/// second table to prove "only changed tables re-export".
/// </summary>
public sealed class PublisherFixture : IDisposable
{
    public SnapshotStore Store { get; }
    public SnapshotTableDefinition Widget { get; }
    public SnapshotTableDefinition Gadget { get; }
    public string PublishDirectory { get; }

    public const string SnapshotName = "test-read";

    public PublisherFixture()
    {
        Store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        Widget = new SnapshotTableDefinition("Widget",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
        ]);
        Gadget = new SnapshotTableDefinition("Gadget",
        [
            new SnapshotColumn("Label", "VARCHAR"),
        ]);
        Store.EnsureTable(Widget);
        Store.EnsureTable(Gadget);

        PublishDirectory = Path.Combine(Path.GetTempPath(), $"hawta-publish-tests-{Guid.NewGuid():N}");
    }

    public SnapshotMergeResult MergeWidgets(params (string Key, string Code, int Quantity)[] rows) =>
        MergeWidgetsWithSourceDates([.. rows.Select(r => (r.Key, r.Code, r.Quantity, (DateTime?)null))]);

    public SnapshotMergeResult MergeWidgetsWithSourceDates(params (string Key, string Code, int Quantity, DateTime? SourceModified)[] rows)
    {
        var staging = Store.CreateStagingTable(Widget);
        foreach (var row in rows)
        {
            Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}, ?
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                row.Key, row.SourceModified, row.Code, row.Quantity);
        }
        return SnapshotMerge.Execute(Store, Widget, staging,
            new SnapshotMergeOptions { Source = "test", DeletesEnabled = true });
    }

    public SnapshotMergeResult MergeGadgets(params (string Key, string Label)[] rows)
    {
        var staging = Store.CreateStagingTable(Gadget);
        foreach (var row in rows)
        {
            Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Label", "_PrimaryKey", "_RowHash")
                SELECT "Label", ?, {RowHash.Expression(["Label"])}
                FROM (SELECT ? AS "Label")
                """,
                row.Key, row.Label);
        }
        return SnapshotMerge.Execute(Store, Gadget, staging,
            new SnapshotMergeOptions { Source = "test", DeletesEnabled = true });
    }

    public SnapshotPublishResult Publish(bool force = false, int keepShims = 3, Action? onBeforeShimCommit = null) =>
        SnapshotPublisher.Publish(Store, new SnapshotPublishOptions
        {
            PublishDirectory = PublishDirectory,
            SnapshotName = SnapshotName,
            Tables = [Widget, Gadget],
            Force = force,
            KeepShims = keepShims,
            OnBeforeShimCommit = onBeforeShimCommit,
        });

    public string[] Files(string pattern) =>
        Directory.Exists(PublishDirectory)
            ? Directory.GetFiles(PublishDirectory, pattern).Select(Path.GetFileName).ToArray()!
            : [];

    public void Dispose()
    {
        Store.Dispose();
        if (Directory.Exists(PublishDirectory))
            Directory.Delete(PublishDirectory, recursive: true);
    }
}

public class SnapshotPublisherTests : IDisposable
{
    private readonly PublisherFixture fx = new();

    [Fact]
    public void FirstPublish_ExportsEveryTable_AndConsumersQueryThroughTheShim()
    {
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeGadgets(("G1", "one"));

        var result = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Equal(["Widget", "Gadget"], result.TablesExported);
        Assert.Empty(result.TablesReused);
        Assert.Single(fx.Files("*.duckdb"));
        Assert.Equal(2, fx.Files("*.parquet").Length);
        Assert.Empty(fx.Files("*.staging"));

        // The consumer contract: resolve newest, open, query the same data.* names.
        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        Assert.NotNull(published);
        using var command = published.Connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false";
        Assert.Equal(2L, Convert.ToInt64(command.ExecuteScalar()));

        var info = published.ReadInfo();
        Assert.Equal(PublisherFixture.SnapshotName, info.SnapshotName);
        Assert.Equal(SnapshotStore.CurrentSchemaVersion, info.SchemaVersion);
        Assert.Equal(2, published.ReadManifest().Count);
    }

    [Fact]
    public void UnchangedData_SkipsThePublishEntirely()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var first = fx.Publish();

        var second = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, second.Status);
        Assert.Equal(first.ShimFile, second.ShimFile);
        Assert.Single(fx.Files("*.duckdb"));
        Assert.Equal(2, fx.Files("*.parquet").Length);
    }

    [Fact]
    public void OnlyChangedTablesAreReExported_UnchangedParquetIsReused()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var first = fx.Publish();

        fx.MergeWidgets(("W1", "alpha", 99));
        var second = fx.Publish();

        Assert.Equal(["Widget"], second.TablesExported);
        Assert.Equal(["Gadget"], second.TablesReused);

        // Gadget's manifest entry still points at the FIRST publish's parquet file.
        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        var manifest = published!.ReadManifest().ToDictionary(e => e.Table);
        Assert.Contains(first.PublishId, manifest["Gadget"].ParquetFile);
        Assert.Contains(second.PublishId, manifest["Widget"].ParquetFile);

        using var command = published.Connection.CreateCommand();
        command.CommandText = "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'";
        Assert.Equal(99, Convert.ToInt32(command.ExecuteScalar()));
    }

    [Fact]
    public void ReplicationProgressAlone_TriggersReExport()
    {
        // The published set is the DR seed: stamps written by the pump must reach parquet even
        // though MarkReplicated never bumps _LastModified. Without this, a rebuilt write DB
        // would re-push everything already replicated.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var dirty = fx.Store.ReadDirtyRows(fx.Widget);
        fx.Store.MarkReplicated(fx.Widget, dirty[0].PrimaryKey, dirty[0].CapturedLastModified, "{\"id\":\"W1\"}");

        var result = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Equal(["Widget"], result.TablesExported);
        Assert.Equal(["Gadget"], result.TablesReused);
    }

    [Fact]
    public void FailureLedgerChangesAlone_TriggerReExport()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var captured = fx.Store.ReadDirtyRows(fx.Widget).Single().CapturedLastModified;
        fx.Store.MarkReplicationFailed(fx.Widget, "W1", captured, "boom");

        var result = fx.Publish();
        Assert.Equal(["Widget"], result.TablesExported);
    }

    [Fact]
    public void Force_ReExportsEverything()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var result = fx.Publish(force: true);

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Equal(["Widget", "Gadget"], result.TablesExported);
    }

    [Fact]
    public void RetentionKeepsThreeShims_AndDeletesUnreferencedParquet()
    {
        fx.MergeGadgets(("G1", "one"));
        for (var i = 1; i <= 5; i++)
        {
            fx.MergeWidgets(("W1", "alpha", i));   // change Widget each round; Gadget never changes
            var result = fx.Publish();
            Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        }

        var shims = fx.Files($"{PublisherFixture.SnapshotName}-*.duckdb");
        Assert.Equal(3, shims.Length);

        // Kept: Gadget's single parquet (referenced by every shim) + the 3 Widget parquet
        // files the kept shims reference. The first two Widget exports are unreferenced → gone.
        Assert.Single(fx.Files("Gadget-*.parquet"));
        Assert.Equal(3, fx.Files("Widget-*.parquet").Length);

        // Every kept shim must still be fully queryable against the surviving parquet.
        foreach (var shim in shims)
        {
            using var published = PublishedSnapshot.Open(Path.Combine(fx.PublishDirectory, shim!));
            using var command = published.Connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM data.\"Widget\"";
            Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
        }
    }

    [Fact]
    public void RetentionToleratesAFileHeldOpenByAConsumer()
    {
        fx.MergeGadgets(("G1", "one"));
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish(keepShims: 1);

        var firstWidgetParquet = Path.Combine(fx.PublishDirectory, fx.Files("Widget-*.parquet").Single()!);

        using (File.Open(firstWidgetParquet, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            fx.MergeWidgets(("W1", "alpha", 2));
            var result = fx.Publish(keepShims: 1);

            // The old Widget parquet is unreferenced but locked — skipped, not fatal.
            Assert.Equal(SnapshotPublishStatus.Published, result.Status);
            Assert.True(result.FilesSkippedByRetention >= 1);
            Assert.True(File.Exists(firstWidgetParquet));
        }

        // Next publish's retention pass picks it up once the consumer is gone.
        fx.MergeWidgets(("W1", "alpha", 3));
        fx.Publish(keepShims: 1);
        Assert.False(File.Exists(firstWidgetParquet));
    }

    [Fact]
    public void StaleStagingFilesFromACrashedRun_AreCleanedUp()
    {
        Directory.CreateDirectory(fx.PublishDirectory);
        var staleParquet = Path.Combine(fx.PublishDirectory, "Widget-00000000000000000.parquet.staging");
        var staleShim = Path.Combine(fx.PublishDirectory, $"{PublisherFixture.SnapshotName}-00000000000000000.duckdb.staging");
        var staleWal = staleShim + ".wal";   // DuckDB's sidecar from a crashed WriteShim
        File.WriteAllText(staleParquet, "partial");
        File.WriteAllText(staleShim, "partial");
        File.WriteAllText(staleWal, "partial");

        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        Assert.Empty(fx.Files("*.staging"));
        Assert.Empty(fx.Files("*.staging.wal"));
    }

    [Fact]
    public void FaultBeforeShimCommit_LeavesStandingSnapshotVisible_AndRecordsFailure()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        var first = fx.Publish();
        var firstShimPath = Path.Combine(fx.PublishDirectory, first.ShimFile!);

        using var standingConsumer = PublishedSnapshot.Open(firstShimPath);
        fx.MergeWidgets(("W1", "alpha", 2));

        var injected = new InvalidOperationException("Injected immediately before shim commit.");
        var observedReadyStaging = false;
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            fx.Publish(onBeforeShimCommit: () =>
            {
                var stagingFile = Assert.Single(fx.Files("*.duckdb.staging"));
                Assert.Empty(fx.Files("*.staging.wal"));

                // The hook is after close/checkpoint: the staging shim is already a complete,
                // queryable snapshot, but its non-final name keeps it invisible to resolution.
                using var staged = PublishedSnapshot.Open(Path.Combine(fx.PublishDirectory, stagingFile));
                using var stagedQuery = staged.Connection.CreateCommand();
                stagedQuery.CommandText = "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'";
                Assert.Equal(2, Convert.ToInt32(stagedQuery.ExecuteScalar()));

                observedReadyStaging = true;
                throw injected;
            }));

        Assert.Same(injected, thrown);
        Assert.True(observedReadyStaging);
        Assert.Equal(firstShimPath, PublishedSnapshot.ResolveNewest(fx.PublishDirectory, PublisherFixture.SnapshotName));
        Assert.Equal(first.ShimFile, Assert.Single(fx.Files($"{PublisherFixture.SnapshotName}-*.duckdb")));
        Assert.Single(fx.Files("*.duckdb.staging"));

        using var standingQuery = standingConsumer.Connection.CreateCommand();
        standingQuery.CommandText = "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'";
        Assert.Equal(1, Convert.ToInt32(standingQuery.ExecuteScalar()));

        Assert.Equal(1L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Failed:Exception'")));
        Assert.Equal(injected.Message, fx.Store.ExecuteScalar(
            "SELECT \"Error\" FROM meta.PublishRuns WHERE \"Status\" = 'Failed:Exception'"));
    }

    [Fact]
    public void PublishAfterPreCommitFault_CleansStagingAndRecovers()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();
        fx.MergeWidgets(("W1", "alpha", 2));

        Assert.Throws<InvalidOperationException>(() =>
            fx.Publish(onBeforeShimCommit: () => throw new InvalidOperationException("Injected failure.")));
        var staleShim = Path.Combine(fx.PublishDirectory, Assert.Single(fx.Files("*.duckdb.staging")));

        var recovered = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.Published, recovered.Status);
        Assert.False(File.Exists(staleShim));
        Assert.Empty(fx.Files("*.staging"));
        Assert.Empty(fx.Files("*.staging.wal"));
        Assert.Equal(2, fx.Files($"{PublisherFixture.SnapshotName}-*.duckdb").Length);
        Assert.Equal(3, fx.Files("*.parquet").Length);   // old set + recovered Widget; failed orphan is swept
        Assert.Equal(recovered.ShimFile, Path.GetFileName(
            PublishedSnapshot.ResolveNewest(fx.PublishDirectory, PublisherFixture.SnapshotName)));

        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        using var query = published!.Connection.CreateCommand();
        query.CommandText = "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'";
        Assert.Equal(2, Convert.ToInt32(query.ExecuteScalar()));

        Assert.Equal(1L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Failed:Exception'")));
        Assert.Equal(2L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Published'")));
    }

    [Fact]
    public void ShimTimestampsNeverCollide_EvenInTheSameMillisecond()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        var first = fx.Publish();
        fx.MergeWidgets(("W1", "alpha", 2));
        var second = fx.Publish();
        fx.MergeWidgets(("W1", "alpha", 3));
        var third = fx.Publish();

        var ids = new[] { first.PublishId, second.PublishId, third.PublishId };
        Assert.Equal(3, ids.Distinct().Count());
        Assert.Equal(ids.OrderBy(x => x, StringComparer.Ordinal), ids);
    }

    [Fact]
    public void ExportIsSortedByPrimaryKey_ByDefault()
    {
        fx.MergeWidgets(("W3", "gamma", 3), ("W1", "alpha", 1), ("W2", "beta", 2));
        fx.Publish();

        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        using var command = published!.Connection.CreateCommand();
        command.CommandText = "SELECT \"_PrimaryKey\" FROM data.\"Widget\"";
        using var reader = command.ExecuteReader();
        var keys = new List<string>();
        while (reader.Read())
            keys.Add(reader.GetString(0));

        Assert.Equal(["W1", "W2", "W3"], keys);
    }

    [Fact]
    public void BookkeepingColumnsAreExported()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        using var command = published!.Connection.CreateCommand();
        command.CommandText = "DESCRIBE data.\"Widget\"";
        using var reader = command.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
            columns.Add(reader.GetString(0));

        foreach (var bookkeeping in BookkeepingColumns.All)
            Assert.Contains(bookkeeping, columns);
    }

    [Fact]
    public void InvalidSortColumnIsRejected()
    {
        fx.MergeWidgets(("W1", "alpha", 1));

        Assert.Throws<ArgumentException>(() => SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget],
            SortColumns = new Dictionary<string, IReadOnlyList<string>> { ["Widget"] = ["NoSuchColumn"] },
        }));
    }

    [Fact]
    public void PublishRecordsARunRecord()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        var result = fx.Publish();

        Assert.Equal("Published", fx.Store.ExecuteScalar(
            "SELECT \"Status\" FROM meta.PublishRuns WHERE \"PublishId\" = ?", result.PublishId));

        // Skipped publishes stay out of PublishRuns (they would flood it at timer cadence).
        fx.Publish();
        Assert.Equal(1L, Convert.ToInt64(fx.Store.ExecuteScalar("SELECT count(*) FROM meta.PublishRuns")));
    }

    // ---- Regression tests from the 2026-08-01 adversarial review ----------------------------

    [Fact]
    public void UpdateStampedBelowAFuturePinnedMax_StillTriggersReExport()
    {
        // One row with a bogus future source save-date pins MAX(_LastModified); a later change
        // to a DIFFERENT row lands below the pin. A MAX-based signature would skip the publish
        // (confirmed data-loss finding); the per-row state hash must not.
        fx.MergeWidgetsWithSourceDates(
            ("W1", "alpha", 1, DateTime.UtcNow.AddDays(1)),
            ("W2", "beta", 2, null));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        fx.MergeWidgetsWithSourceDates(
            ("W1", "alpha", 1, DateTime.UtcNow.AddDays(1)),
            ("W2", "beta", 99, null));

        var result = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Equal(["Widget"], result.TablesExported);

        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        using var command = published!.Connection.CreateCommand();
        command.CommandText = "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W2'";
        Assert.Equal(99, Convert.ToInt32(command.ExecuteScalar()));
    }

    [Fact]
    public void TombstoneBelowAFuturePinnedMax_StillTriggersReExport()
    {
        fx.MergeWidgetsWithSourceDates(
            ("W1", "alpha", 1, DateTime.UtcNow.AddDays(1)),
            ("W2", "beta", 2, null));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        fx.MergeWidgetsWithSourceDates(("W1", "alpha", 1, DateTime.UtcNow.AddDays(1)));   // W2 vanishes

        var result = fx.Publish();
        Assert.Equal(["Widget"], result.TablesExported);
    }

    [Fact]
    public void DeadLetterResetAliasedByNewFailures_StillTriggersReExport()
    {
        // −5 from resetting one dead-letter exactly canceled by five +1 failures elsewhere:
        // a SUM-based signature is blind to it (confirmed finding); the state hash is not.
        fx.MergeWidgets(("W1", "a", 1), ("W2", "b", 2), ("W3", "c", 3), ("W4", "d", 4), ("W5", "e", 5), ("W6", "f", 6));
        fx.MergeGadgets(("G1", "one"));
        var capturedByKey = fx.Store.ReadDirtyRows(fx.Widget)
            .ToDictionary(row => row.PrimaryKey, row => row.CapturedLastModified);
        for (var i = 0; i < 5; i++)
            fx.Store.MarkReplicationFailed(fx.Widget, "W1", capturedByKey["W1"], "boom");
        fx.Publish();

        fx.Store.ResetReplicationFailures(fx.Widget);                       // W1: 5 → 0
        foreach (var key in new[] { "W2", "W3", "W4", "W5", "W6" })
            fx.Store.MarkReplicationFailed(fx.Widget, key, capturedByKey[key], "outage");       // +1 each

        var result = fx.Publish();
        Assert.Equal(["Widget"], result.TablesExported);
    }

    [Fact]
    public void HeldOpenShimKeepsItsParquetAlive_UntilTheConsumerCloses()
    {
        // The blocker finding: a shim whose delete is skipped (consumer holds it open) must
        // keep protecting the parquet only it references — the referenced set is built from
        // shims on disk, not just the kept window.
        fx.MergeGadgets(("G1", "one"));
        fx.MergeWidgets(("W1", "alpha", 1));
        var first = fx.Publish(keepShims: 1);
        var firstShimPath = Path.Combine(fx.PublishDirectory, first.ShimFile!);
        var firstWidgetParquet = fx.Files("Widget-*.parquet").Single()!;

        // On local NTFS, File.Delete POSIX-deletes even a DuckDB-held file; over the
        // production SMB share, a consumer's handle makes the delete a sharing violation.
        // Simulate the SMB behavior with a no-delete-share handle next to the real consumer.
        using (var consumer = PublishedSnapshot.Open(firstShimPath))
        using (File.Open(firstShimPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            fx.MergeWidgets(("W1", "alpha", 2));
            var second = fx.Publish(keepShims: 1);

            Assert.Equal(SnapshotPublishStatus.Published, second.Status);
            Assert.True(second.FilesSkippedByRetention >= 1);                       // the held shim
            Assert.True(File.Exists(Path.Combine(fx.PublishDirectory, firstWidgetParquet)));

            // The mid-session consumer still queries its shim successfully.
            using var query = consumer.Connection.CreateCommand();
            query.CommandText = "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'";
            Assert.Equal(1, Convert.ToInt32(query.ExecuteScalar()));
        }

        // Consumer gone → the next publish removes the old shim AND its parquet.
        fx.MergeWidgets(("W1", "alpha", 3));
        fx.Publish(keepShims: 1);
        Assert.False(File.Exists(firstShimPath));
        Assert.False(File.Exists(Path.Combine(fx.PublishDirectory, firstWidgetParquet)));
    }

    [Fact]
    public void ForeignSnapshotInTheSameDirectory_SkipsParquetCleanupInsteadOfDestroyingIt()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        // A second snapshot name publishes into the same directory (misconfiguration).
        SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = "other-read",
            Tables = [fx.Gadget],
        });
        var foreignParquet = fx.Files("Gadget-*.parquet");

        fx.MergeWidgets(("W1", "alpha", 2));
        var result = fx.Publish();

        Assert.True(result.ParquetCleanupSkipped);
        Assert.Equal(0, result.ParquetFilesDeleted);
        foreach (var parquet in foreignParquet)
            Assert.True(File.Exists(Path.Combine(fx.PublishDirectory, parquet!)));
    }

    [Fact]
    public void AdHocParquetInTheDirectory_SurvivesRetention()
    {
        Directory.CreateDirectory(fx.PublishDirectory);
        var adHoc = Path.Combine(fx.PublishDirectory, "developer-export.parquet");
        File.WriteAllText(adHoc, "not even parquet");

        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();
        fx.MergeWidgets(("W1", "alpha", 2));
        fx.Publish();

        Assert.True(File.Exists(adHoc));
    }

    [Fact]
    public void TornReferencedParquet_IsReExportedInsteadOfReused()
    {
        // File.Exists alone would re-reference a torn file forever (confirmed finding); the
        // footer probe must catch it and self-heal by re-exporting.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var gadgetParquet = Path.Combine(fx.PublishDirectory, fx.Files("Gadget-*.parquet").Single()!);
        File.WriteAllBytes(gadgetParquet, [0xDE, 0xAD]);

        var result = fx.Publish();   // no data changed — only the torn file forces work

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Contains("Gadget", result.TablesExported);

        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        using var command = published!.Connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM data.\"Gadget\"";
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public void SnapshotNameCasingDrift_StillFindsTheBaseline()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var result = SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = "TEST-READ",   // same snapshot, drifted casing
            Tables = [fx.Widget, fx.Gadget],
        });

        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, result.Status);
    }

    [Fact]
    public void ShareUnavailable_StillRecordsAFailedPublishRun()
    {
        // The publish directory path is blocked by an existing FILE → CreateDirectory throws.
        // The failure must reach meta.PublishRuns (the alarm surface), not vanish.
        Directory.CreateDirectory(fx.PublishDirectory);
        var blocked = Path.Combine(fx.PublishDirectory, "blocked");
        File.WriteAllText(blocked, "");

        fx.MergeWidgets(("W1", "alpha", 1));

        Assert.ThrowsAny<IOException>(() => SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = blocked,
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget],
        }));

        Assert.Equal(1L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Failed:Exception'")));
    }

    [Fact]
    public void DuplicateTables_AreRejectedUpFront()
    {
        Assert.Throws<ArgumentException>(() => SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget, new SnapshotTableDefinition("widget", [new SnapshotColumn("Code", "VARCHAR")])],
        }));
    }

    [Fact]
    public void SortColumnsKeyMatchingNoTable_IsRejectedUpFront()
    {
        Assert.Throws<ArgumentException>(() => SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget],
            SortColumns = new Dictionary<string, IReadOnlyList<string>> { ["DmsOrderLines"] = ["Code"] },
        }));
    }

    [Fact]
    public void WrongCasedSortColumnsKey_StillApplies()
    {
        fx.MergeWidgets(("W1", "zeta", 1), ("W2", "alpha", 2));   // key order ≠ Code order

        SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget],
            SortColumns = new Dictionary<string, IReadOnlyList<string>> { ["widget"] = ["Code"] },
        });

        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        using var command = published!.Connection.CreateCommand();
        command.CommandText = "SELECT \"Code\" FROM data.\"Widget\"";
        using var reader = command.ExecuteReader();
        var codes = new List<string>();
        while (reader.Read())
            codes.Add(reader.GetString(0));
        Assert.Equal(["alpha", "zeta"], codes);
    }

    [Fact]
    public void TombstonesFlowToTheReadTier()
    {
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeWidgets(("W1", "alpha", 1));   // W2 vanishes → tombstone
        fx.Publish();

        using var published = PublishedSnapshot.OpenNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        using var command = published!.Connection.CreateCommand();
        command.CommandText = "SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W2'";
        Assert.Equal(true, command.ExecuteScalar());
    }

    public void Dispose() => fx.Dispose();
}
