using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public class SnapshotRebuildTests : IDisposable
{
    private readonly PublisherFixture fx = new();

    private SnapshotStore RebuildIntoFreshStore(
        IReadOnlyList<SnapshotTableDefinition> tables, out SnapshotRebuildResult result)
    {
        var fresh = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        try
        {
            result = SnapshotRebuild.Execute(fresh, tables, fx.PublishDirectory, PublisherFixture.SnapshotName);
            return fresh;
        }
        catch
        {
            fresh.Dispose();
            throw;
        }
    }

    [Fact]
    public void RebuildRestoresRowsAndReplicationState_NextPumpCycleWritesZeroOps()
    {
        // The G2 DR drill in miniature: merge → replicate → publish → lose the write DB →
        // rebuild from the newest published set → nothing is dirty (the stamps survived).
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeGadgets(("G1", "one"));
        foreach (var row in fx.Store.ReadDirtyRows(fx.Widget))
            fx.Store.MarkReplicated(fx.Widget, row.PrimaryKey, row.CapturedLastModified, "{\"id\":\"x\"}");
        foreach (var row in fx.Store.ReadDirtyRows(fx.Gadget))
            fx.Store.MarkReplicated(fx.Gadget, row.PrimaryKey, row.CapturedLastModified, "{\"id\":\"x\"}");
        fx.Publish();

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out var result);

        Assert.NotNull(result.ManifestFile);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(0, rebuilt.CountDirtyRows(fx.Widget));
        Assert.Equal(0, rebuilt.CountDirtyRows(fx.Gadget));

        Assert.Equal("{\"id\":\"x\"}", rebuilt.ExecuteScalar(
            "SELECT \"_ReplicationStamp\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));

        Assert.Equal(2L, Convert.ToInt64(rebuilt.ExecuteScalar(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Source\" = 'rebuild:test-read'")));
    }

    [Fact]
    public void RebuildRestoresSourceFileStamps_SoAColdStartDoesNotRescanEveryFeed()
    {
        // The whole point of carrying stamps in the manifest: a slot swap hands the new instance an
        // empty disk, and without this it re-reads every feed to relearn what the published set
        // already records.
        fx.MergeWidgets(("W1", "alpha", 1));
        var stampedAt = new DateTime(2026, 8, 10, 7, 30, 0, DateTimeKind.Utc);
        var lastWrite = new DateTime(2026, 8, 9, 22, 15, 0, DateTimeKind.Utc).AddTicks(4271);
        fx.Store.WriteSourceFileStamp(new SourceFileStamp(
            "csv-widgets", @"C:\feeds\widgets.csv", 1234, lastWrite, "FINGERPRINT-A", stampedAt));
        fx.Publish();

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out _);

        var restored = rebuilt.ReadSourceFileStamp("csv-widgets");
        Assert.NotNull(restored);
        Assert.Equal(1234, restored.Length);
        Assert.Equal("FINGERPRINT-A", restored.ConfigFingerprint);
        // Exact ticks survive the manifest round trip. A narrowed timestamp would make an unchanged
        // file read as changed — the defect this shape exists to prevent.
        Assert.Equal(lastWrite, restored.LastWriteUtc);
        // The ORIGINAL stamp time, not the restore time: it is the age a per-source re-ingest bound
        // measures, and refreshing it would silently extend every configured bound across a restart.
        Assert.Equal(stampedAt, restored.StampedAtUtc);
    }

    [Fact]
    public void AStampMadeAfterTheLastPublish_IsNotRestored_SoThatSourceReadsAgain()
    {
        // The failure this design exists to prevent. A stamp written between a merge and the next
        // publish describes rows that are in NO published set; restoring it against the previous
        // manifest would let the gate skip a file whose rows are absent from the rebuilt table.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        fx.Store.WriteSourceFileStamp(new SourceFileStamp(
            "csv-late", @"C:\feeds\late.csv", 99, DateTime.UtcNow, "FINGERPRINT-B", DateTime.UtcNow));

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out _);

        Assert.Null(rebuilt.ReadSourceFileStamp("csv-late"));
    }

    [Fact]
    public void AManifestWithNoStampSection_StillRebuilds()
    {
        // sourceStamps is additive, so every manifest published before it existed must keep
        // rebuilding. Absent reads as "no stamps", never as a parse failure.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        var manifest = Directory.EnumerateFiles(fx.PublishDirectory, "*.json")
            .First(f => !Path.GetFileName(f).StartsWith("latest", StringComparison.OrdinalIgnoreCase));
        var stripped = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(manifest), ",\\s*\"sourceStamps\":\\s*(null|\\[[\\s\\S]*?\\])", string.Empty);
        File.WriteAllText(manifest, stripped);

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out var result);

        Assert.NotNull(result.ManifestFile);
        Assert.Equal(1, result.TotalRows);
    }

    [Fact]
    public void RebuildPreservesTombstones()
    {
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeWidgets(("W1", "alpha", 1));   // tombstones W2
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out _);

        Assert.Equal(true, rebuilt.ExecuteScalar(
            "SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W2'"));
        Assert.NotNull(rebuilt.ExecuteScalar(
            "SELECT \"_DeletedAt\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W2'"));
    }

    [Fact]
    public void RebuiltStore_SeesUnchangedSourceAsNoOp()
    {
        // After a rebuild, re-ingesting the same source rows must hash-match: 0/0/0.
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out _);

        var staging = rebuilt.CreateStagingTable(fx.Widget);
        foreach (var (key, code, quantity) in new[] { ("W1", "alpha", 1), ("W2", "beta", 2) })
        {
            rebuilt.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                key, code, quantity);
        }
        var merge = SnapshotMerge.Execute(rebuilt, fx.Widget, staging,
            new SnapshotMergeOptions { Source = "test", DeletesEnabled = true });

        Assert.True(merge.Succeeded);
        Assert.Equal(0, merge.RowsInserted);
        Assert.Equal(0, merge.RowsUpdated);
        Assert.Equal(0, merge.RowsTombstoned);
    }

    [Fact]
    public void RebuildAfterAdditiveDrift_LoadsOldParquetWithNewColumnNull()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var widened = new SnapshotTableDefinition("Widget",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
            new SnapshotColumn("Color", "VARCHAR"),
        ]);

        using var rebuilt = RebuildIntoFreshStore([widened, fx.Gadget], out var result);

        Assert.Equal(2, result.TablesLoaded.Count);
        Assert.Equal(DBNull.Value, rebuilt.ExecuteScalar("SELECT \"Color\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));
        Assert.Equal("alpha", rebuilt.ExecuteScalar("SELECT \"Code\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));
    }

    [Fact]
    public void UndeclaredManifestTablesAreSkipped_DeclaredNewTablesAreCreatedEmpty()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var newFamily = new SnapshotTableDefinition("Sprocket", [new SnapshotColumn("Size", "INTEGER")]);

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, newFamily], out var result);

        Assert.Equal(["Gadget"], result.TablesSkipped);
        Assert.Equal(["Sprocket"], result.TablesCreatedEmpty);
        Assert.Equal(0L, Convert.ToInt64(rebuilt.ExecuteScalar("SELECT count(*) FROM data.\"Sprocket\"")));
    }

    [Fact]
    public void RebuildFallsBackToAnOlderPublish_WhenTheNewestSetIsTorn()
    {
        // A DR rebuild correlates with exactly the kind of crash that tears files: the newest
        // shim's parquet being unreadable must fall back to the next kept shim, not fail DR.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var first = fx.Publish();

        fx.MergeWidgets(("W1", "alpha", 2));
        var second = fx.Publish();

        var newestWidgetParquet = Path.Combine(fx.PublishDirectory, "Widget", $"{second.PublishId}.parquet");
        File.WriteAllBytes(newestWidgetParquet, [0xBA, 0xD0]);

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out var result);

        Assert.Equal(first.ManifestFile, result.ManifestFile);
        Assert.Equal([second.ManifestFile], result.PublishesSkipped);
        Assert.Equal(1, Convert.ToInt32(rebuilt.ExecuteScalar(
            "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'")));
    }

    [Fact]
    public void NoPublishedSnapshot_LeavesTheStoreEmpty_AndSaysSo()
    {
        using var rebuilt = RebuildIntoFreshStore([fx.Widget], out var result);

        Assert.Null(result.ManifestFile);
        Assert.Empty(result.TablesLoaded);
        Assert.Equal(["Widget"], result.TablesCreatedEmpty);
    }

    public void Dispose() => fx.Dispose();
}
