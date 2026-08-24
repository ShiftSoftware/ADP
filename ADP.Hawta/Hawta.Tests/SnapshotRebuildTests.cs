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

    /// <summary>
    /// Corrupts the newest published Widget parquet in place, count-preservingly: W2's row
    /// keeps its own change sequence and metadata but takes W1's key. Appending a row instead
    /// would change the row count and be caught as a torn file — letting these tests pass
    /// without ever reaching the duplicate guard. (That other corruption has its own tests
    /// below, via <see cref="CorruptNewestWidgetParquetWithAnExtraRow"/>.)
    /// </summary>
    private void CorruptNewestWidgetParquetWithDuplicateKey()
    {
        var entry = fx.Entry("Widget");
        var source = entry.ReadParquetSql(fx.PublishDirectory);
        var file = entry.Resolve(fx.PublishDirectory).Single().Replace('\\', '/');

        fx.Store.Execute(
            $"""
            CREATE OR REPLACE TEMP TABLE corrupt AS
            SELECT * REPLACE ('W1' AS "_PrimaryKey") FROM {source} WHERE "_PrimaryKey" = 'W2'
            UNION ALL
            SELECT * FROM {source} WHERE "_PrimaryKey" <> 'W2'
            """);
        fx.Store.Execute($"COPY corrupt TO '{file}' (FORMAT PARQUET)");
        fx.Store.Execute("DROP TABLE corrupt");
    }

    /// <summary>
    /// The other tear: a file that parses perfectly and simply holds the wrong number of rows.
    /// Appends a copy of W2 under a fresh key, so the parquet is valid, its footer is honest, and
    /// only the manifest disagrees.
    /// </summary>
    private void CorruptNewestWidgetParquetWithAnExtraRow()
    {
        var entry = fx.Entry("Widget");
        var source = entry.ReadParquetSql(fx.PublishDirectory);
        var file = entry.Resolve(fx.PublishDirectory).Single().Replace('\\', '/');

        fx.Store.Execute(
            $"""
            CREATE OR REPLACE TEMP TABLE corrupt AS
            SELECT * FROM {source}
            UNION ALL
            SELECT * REPLACE ('W9' AS "_PrimaryKey") FROM {source} WHERE "_PrimaryKey" = 'W2'
            """);
        fx.Store.Execute($"COPY corrupt TO '{file}' (FORMAT PARQUET)");
        fx.Store.Execute("DROP TABLE corrupt");
    }

    [Fact]
    public void ASeedHoldingMoreRowsThanItsManifest_IsRefused_AndRebuildFallsBackToTheOlderCleanSet()
    {
        // The row-count half of "missing or torn", and the one nothing else would catch. A file
        // that fails to open announces itself; this one loads cleanly and would quietly seed the
        // estate with rows the manifest never described — rows no consumer has been told about,
        // carrying whatever change sequences the corruption left on them. The rebuild used to
        // prove this from the parquet footer BEFORE loading, at the cost of opening every file an
        // extra time over the network; it now proves it from the rows actually inserted, which is
        // the same claim made by the load itself. Either way the set is refused and DR falls back.
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        var first = fx.Publish();

        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 3));
        var second = fx.Publish();
        CorruptNewestWidgetParquetWithAnExtraRow();

        using var rebuilt = RebuildIntoFreshStore([fx.Widget], out var result);

        Assert.Equal(first.ManifestFile, result.ManifestFile);
        Assert.Equal([second.ManifestFile], result.PublishesSkipped);

        // And the fallback seed is whole: the rolled-back attempt left nothing behind.
        Assert.Equal(2L, Convert.ToInt64(rebuilt.ExecuteScalar("SELECT count(*) FROM data.\"Widget\"")));
        Assert.Equal(2, Convert.ToInt32(rebuilt.ExecuteScalar(
            "SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W2'")));
    }

    [Fact]
    public void WhenTheOnlySetHoldsMoreRowsThanItsManifest_TheTerminalFailureNamesTheCount()
    {
        // No older set to fall back to. The cause must say the copy is torn and name both counts,
        // rather than surfacing as a bare "no published set could be loaded".
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.Publish();
        CorruptNewestWidgetParquetWithAnExtraRow();

        var refusal = Assert.Throws<InvalidDataException>(() => RebuildIntoFreshStore([fx.Widget], out _));

        var cause = Assert.IsType<InvalidDataException>(refusal.InnerException);
        Assert.Contains("is torn", cause.Message);
        Assert.Contains("loaded 3 row(s)", cause.Message);
        Assert.Contains("manifest records 2", cause.Message);
    }

    [Fact]
    public void ASeedWithADuplicatedKey_IsRefused_AndRebuildFallsBackToTheOlderCleanSet()
    {
        // The publish contract refuses to export a store with duplicated keys, but a published
        // set does not have to come from this publisher: an older engine, another tool, or
        // in-place corruption can hand the rebuild duplicates whose parquet still matches its
        // manifest's row count. The rebuild's seed check must refuse such a set the way it
        // refuses a torn one: fall back to the older clean publish, on record.
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        var first = fx.Publish();

        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 3));
        var second = fx.Publish();
        CorruptNewestWidgetParquetWithDuplicateKey();

        using var rebuilt = RebuildIntoFreshStore([fx.Widget], out var result);

        Assert.Equal(first.ManifestFile, result.ManifestFile);
        Assert.Equal([second.ManifestFile], result.PublishesSkipped);
        Assert.Equal(2L, Convert.ToInt64(rebuilt.ExecuteScalar("SELECT count(*) FROM data.\"Widget\"")));
    }

    [Fact]
    public void WhenTheOnlySetHasADuplicatedKey_TheTerminalFailureNamesIt()
    {
        // No older set to fall back to: the rebuild must fail, point at from-sources recovery
        // (which genuinely fixes this — staging is deduplicated), and carry the duplicate as
        // the cause rather than dressing it up as a missing published set.
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.Publish();
        CorruptNewestWidgetParquetWithDuplicateKey();

        var refusal = Assert.Throws<InvalidDataException>(() =>
            RebuildIntoFreshStore([fx.Widget], out _));

        Assert.Contains("rebuild from sources", refusal.Message);
        Assert.Contains("duplicated", Assert.IsType<InvalidDataException>(refusal.InnerException).Message);
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
    public void RebuildRestoresCosmosCursors_SoDrIsNotSilentlyAFullContainerReRead()
    {
        // Without this a rebuild reseeds the write DB from parquet, the cursor is gone, and the
        // next tick reads the whole container from the beginning — harmless at merge level, but a
        // full read nobody asked for, growing more expensive every day.
        fx.MergeWidgets(("W1", "alpha", 1));
        var stampedAt = new DateTime(2026, 8, 18, 6, 15, 0, DateTimeKind.Utc);
        fx.Store.WriteSourceCosmosCursor(new SourceCosmosCursor(
            "cosmos-ssc-lookup", "Logs", "SSC", "TOKEN-A", "v3", stampedAt));
        fx.Publish();

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out _);

        var restored = rebuilt.ReadSourceCosmosCursor("cosmos-ssc-lookup");
        Assert.NotNull(restored);
        Assert.Equal("TOKEN-A", restored.ContinuationToken);
        Assert.Equal("Logs", restored.Database);
        Assert.Equal("SSC", restored.Container);
        Assert.Equal("v3", restored.IngestVersion);
        // The ORIGINAL stamp time: "when did this source last actually read?" has to survive DR
        // truthfully, or a source quiet since before the outage looks freshly read.
        Assert.Equal(stampedAt, restored.StampedAtUtc);
    }

    [Fact]
    public void ACursorMovedAfterTheLastPublish_IsNotRestored()
    {
        // The same failure direction the file stamps guard, and worse here: restoring a cursor
        // against an older manifest would skip documents whose rows are in no published set.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        fx.Store.WriteSourceCosmosCursor(new SourceCosmosCursor(
            "cosmos-late", "Logs", "SSC", "TOKEN-LATE", null, DateTime.UtcNow));

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out _);

        Assert.Null(rebuilt.ReadSourceCosmosCursor("cosmos-late"));
    }

    [Fact]
    public void AManifestWithNoCursorSection_StillRebuilds()
    {
        // sourceCursors is additive on a manifest version that does not move for it, so every set
        // published before it existed must keep rebuilding.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Store.WriteSourceCosmosCursor(new SourceCosmosCursor(
            "cosmos-ssc-lookup", "Logs", "SSC", "TOKEN-A", null, DateTime.UtcNow));
        fx.Publish();

        var manifest = Directory.EnumerateFiles(fx.PublishDirectory, "*.json")
            .First(f => !Path.GetFileName(f).StartsWith("latest", StringComparison.OrdinalIgnoreCase));
        var stripped = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(manifest), ",\\s*\"sourceCursors\":\\s*(null|\\[[\\s\\S]*?\\])", string.Empty);
        Assert.DoesNotContain("sourceCursors", stripped);
        File.WriteAllText(manifest, stripped);

        using var rebuilt = RebuildIntoFreshStore([fx.Widget, fx.Gadget], out var result);

        Assert.NotNull(result.ManifestFile);
        Assert.Equal(1, result.TotalRows);
        Assert.Null(rebuilt.ReadSourceCosmosCursor("cosmos-ssc-lookup"));
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
            new SnapshotMergeOptions { Source = fx.WidgetSource.Key, DeletesEnabled = true });

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

        var skippedHighWatermark = PublishedSnapshot.Read(
            Path.Combine(fx.PublishDirectory, second.ManifestFile!)).ChangeSequenceHighWatermark!.Value;
        var staging = rebuilt.CreateStagingTable(fx.Widget);
        rebuilt.Execute(
            $"""
            INSERT INTO {staging.QualifiedName}
                ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
            SELECT "Code", "Quantity", 'W2', {RowHash.Expression(["Code", "Quantity"])}, NULL
            FROM (SELECT 'beta' AS "Code", 2 AS "Quantity")
            """);
        SnapshotMerge.Execute(rebuilt, fx.Widget, staging,
            new SnapshotMergeOptions { Source = fx.WidgetSource.Key, DeletesEnabled = false });

        Assert.True(Convert.ToInt64(rebuilt.ExecuteScalar(
            "SELECT \"_ChangeSequence\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W2'"))
            > skippedHighWatermark);
    }

    [Fact]
    public void NoPublishedSnapshot_LeavesTheStoreEmpty_AndSaysSo()
    {
        using var rebuilt = RebuildIntoFreshStore([fx.Widget], out var result);

        Assert.Null(result.ManifestFile);
        Assert.Empty(result.TablesLoaded);
        Assert.Equal(["Widget"], result.TablesCreatedEmpty);
    }

    [Fact]
    public void RebuildIntoAWarmStore_BlamesTheCallerRatherThanThePublishedSet()
    {
        // A host that forgets the cold-start guard reaches here with a populated store. The seed is
        // a bare INSERT, so before this precondition existed the primary-key violation surfaced
        // from inside the per-manifest loop wearing the costume of a torn parquet file: "no
        // published set could be loaded ... rebuild from sources instead". Both halves of that were
        // wrong, and the second half recommends the most expensive path in the system.
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.Publish();

        var refusal = Assert.Throws<InvalidOperationException>(() =>
            SnapshotRebuild.Execute(fx.Store, [fx.Widget], fx.PublishDirectory, PublisherFixture.SnapshotName));

        Assert.Contains("already holds 2 row(s)", refusal.Message);
        Assert.Contains("existed BEFORE it was opened", refusal.Message);
        Assert.Contains("published set is not at fault", refusal.Message);

        // And it must not have partially seeded on its way to refusing.
        Assert.Equal(2L, Convert.ToInt64(fx.Store.ExecuteScalar("SELECT count(*) FROM data.\"Widget\"")));
    }

    public void Dispose() => fx.Dispose();
}
