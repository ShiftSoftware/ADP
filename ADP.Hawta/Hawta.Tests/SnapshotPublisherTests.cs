using DuckDB.NET.Data;
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
    public Func<string, bool>? RetentionDelete { get; set; }

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

    public SnapshotPublishResult Publish(bool force = false, int keepPublishes = 3, Action? onBeforeManifestCommit = null) =>
        SnapshotPublisher.Publish(Store, new SnapshotPublishOptions
        {
            PublishDirectory = PublishDirectory,
            SnapshotName = SnapshotName,
            Tables = [Widget, Gadget],
            Force = force,
            KeepPublishes = keepPublishes,
            OnBeforeManifestCommit = onBeforeManifestCommit,
            RetentionDelete = RetentionDelete,
        });

    /// <summary>File names anywhere under the publish directory — table folders nest one level.</summary>
    public string[] Files(string pattern) =>
        Directory.Exists(PublishDirectory)
            ? Directory.GetFiles(PublishDirectory, pattern, SearchOption.AllDirectories).Select(Path.GetFileName).ToArray()!
            : [];

    /// <summary>Version file names inside one table's folder.</summary>
    public string[] Versions(string table)
    {
        var folder = Path.Combine(PublishDirectory, table);
        return Directory.Exists(folder)
            ? [.. Directory.GetFiles(folder, "*.parquet").Select(Path.GetFileName).Order()!]
            : [];
    }

    /// <summary>Manifest file names in the directory, newest first.</summary>
    public string[] Manifests() =>
        Directory.Exists(PublishDirectory)
            ? [.. Directory.GetFiles(PublishDirectory, $"{SnapshotName}-*.json")
                .Select(Path.GetFileName).OrderDescending(StringComparer.Ordinal)!]
            : [];

    public PublishedSnapshot ReadNewest() =>
        PublishedSnapshot.ReadNewest(PublishDirectory, SnapshotName)
        ?? throw new InvalidOperationException("Nothing published.");

    public PublishedTableManifest Entry(string table, PublishedSnapshot? manifest = null) =>
        (manifest ?? ReadNewest()).Tables.Single(t => t.Table == table);

    /// <summary>
    /// The consumer contract end to end: resolve the manifest, look the table up, and read its
    /// parquet through a connection that shares nothing with the write store — which is exactly
    /// what an external consumer has, now that there is no shim to open.
    /// </summary>
    public object? ReadPublished(string table, string projection, string? where = null, PublishedSnapshot? manifest = null)
    {
        var entry = Entry(table, manifest);

        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {projection} FROM {entry.ReadParquetSql(PublishDirectory)}"
                              + (where is null ? string.Empty : $" WHERE {where}");
        return command.ExecuteScalar();
    }

    /// <summary>Every value of one column in the published parquet, in stored order.</summary>
    public List<string> ReadPublishedColumn(string table, string projection)
    {
        var entry = Entry(table);

        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {projection} FROM {entry.ReadParquetSql(PublishDirectory)}";
        using var reader = command.ExecuteReader();

        var values = new List<string>();
        while (reader.Read())
            values.Add(reader.GetValue(0)?.ToString() ?? string.Empty);
        return values;
    }

    /// <summary>Column names carried by a published table's parquet.</summary>
    public List<string> PublishedColumns(string table)
    {
        var entry = Entry(table);

        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DESCRIBE SELECT * FROM {entry.ReadParquetSql(PublishDirectory)}";
        using var reader = command.ExecuteReader();

        var columns = new List<string>();
        while (reader.Read())
            columns.Add(reader.GetString(0));
        return columns;
    }

    public static bool DeleteForRetentionTest(string path)
    {
        File.Delete(path);
        return true;
    }

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
    public void FirstPublish_ExportsEveryTable_AndConsumersReadThroughTheManifest()
    {
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeGadgets(("G1", "one"));

        var result = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Equal(["Widget", "Gadget"], result.TablesExported);
        Assert.Empty(result.TablesReused);
        Assert.Single(fx.Manifests());
        Assert.Equal(2, fx.Files("*.parquet").Length);
        Assert.Empty(fx.Files("*.staging"));
        // The shim it replaced was a DuckDB database file, which is the one artifact that
        // cannot be opened over az:// — nothing may reintroduce one.
        Assert.Empty(fx.Files("*.duckdb"));

        // The consumer contract: resolve newest, read the manifest, read its parquet.
        var manifest = fx.ReadNewest();
        Assert.Equal(PublishedSnapshot.CurrentManifestVersion, manifest.ManifestVersion);
        Assert.Equal(PublisherFixture.SnapshotName, manifest.SnapshotName);
        Assert.Equal(result.PublishId, manifest.PublishId);
        Assert.Equal(SnapshotStore.CurrentSchemaVersion, manifest.SchemaVersion);
        Assert.Equal(".", manifest.PathBase);
        Assert.Equal("latest-per-table", manifest.SelectionMode);
        Assert.Equal(2, manifest.Tables.Count);

        Assert.Equal(2L, Convert.ToInt64(fx.ReadPublished("Widget", "count(*)", "\"_Deleted\" = false")));
    }

    [Fact]
    public void ManifestPathsAreFolderPerTable_ForwardSlashed_AndRelocatable()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var publish = fx.Publish();

        foreach (var entry in fx.ReadNewest().Tables)
        {
            Assert.Equal("parquet", entry.Location.Kind);
            var file = Assert.Single(entry.Location.Paths);
            Assert.True(PublishPath.IsRelativeContainedPath(file), $"'{file}' is not a contained relative path.");
            // Folder per table, forward-slashed: the layout consumers read, and the only one a
            // table growing past a single file (or becoming a Delta directory) survives.
            Assert.Equal($"{entry.Table}/{publish.PublishId}.parquet", file);
            Assert.DoesNotContain('\\', file);
            Assert.True(File.Exists(Path.Combine(fx.PublishDirectory, entry.Table, $"{publish.PublishId}.parquet")));
        }
    }

    [Fact]
    public void UnchangedData_SkipsThePublishEntirely()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var first = fx.Publish();

        var second = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, second.Status);
        Assert.Equal(first.ManifestFile, second.ManifestFile);
        Assert.Single(fx.Manifests());
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

        // Gadget's manifest entry still points at the FIRST publish's parquet, and says so.
        var manifest = fx.ReadNewest();
        Assert.Equal(first.PublishId, fx.Entry("Gadget", manifest).PublishId);
        Assert.Equal(second.PublishId, fx.Entry("Widget", manifest).PublishId);
        Assert.Contains(first.PublishId, Assert.Single(fx.Entry("Gadget", manifest).Location.Paths));

        Assert.Equal(99, Convert.ToInt32(fx.ReadPublished("Widget", "\"Quantity\"", "\"_PrimaryKey\" = 'W1'")));
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
    public void EmptyTables_ArePublishedAndNamed_NotHidden()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        // Gadget is never merged — the shape an un-wired CSV family has in production. It must
        // publish as valid, EMPTY parquet AND be named, because a consumer cannot otherwise
        // tell that from "no rows today".
        var result = fx.Publish();

        Assert.Equal(["Gadget"], result.TablesWithNoRows);
        Assert.Equal(0, fx.Entry("Gadget").RowCount);
        Assert.Equal(0L, Convert.ToInt64(fx.ReadPublished("Gadget", "count(*)")));
    }

    [Fact]
    public void RetentionKeepsThreePublishes_AndDeletesUnreferencedParquet()
    {
        fx.MergeGadgets(("G1", "one"));
        for (var i = 1; i <= 5; i++)
        {
            fx.MergeWidgets(("W1", "alpha", i));   // change Widget each round; Gadget never changes
            var result = fx.Publish();
            Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        }

        Assert.Equal(3, fx.Manifests().Length);

        // Kept: Gadget's single parquet (referenced by every manifest) + the 3 Widget parquet
        // files the kept manifests reference. The first two Widget exports are unreferenced → gone.
        Assert.Single(fx.Versions("Gadget"));
        Assert.Equal(3, fx.Versions("Widget").Length);

        // Every kept publish must still resolve to parquet that is actually there.
        foreach (var manifestFile in fx.Manifests())
        {
            var manifest = PublishedSnapshot.Read(Path.Combine(fx.PublishDirectory, manifestFile));
            foreach (var entry in manifest.Tables)
                foreach (var path in entry.Resolve(fx.PublishDirectory))
                    Assert.True(File.Exists(path), $"{manifestFile} references '{path}', which retention deleted.");

            Assert.Equal(1L, Convert.ToInt64(fx.ReadPublished("Widget", "count(*)", manifest: manifest)));
        }
    }

    [Fact]
    public void RetentionToleratesAFileHeldOpenByAConsumer()
    {
        fx.MergeGadgets(("G1", "one"));
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish(keepPublishes: 2);

        var firstWidgetParquet = Path.Combine(fx.PublishDirectory, "Widget", fx.Versions("Widget").Single()!);

        // Two more publishes push the first manifest out of the kept window, so its Widget
        // parquet becomes unreferenced and eligible for deletion.
        fx.MergeWidgets(("W1", "alpha", 2));
        fx.Publish(keepPublishes: 2);

        fx.RetentionDelete = path => string.Equals(path, firstWidgetParquet, StringComparison.OrdinalIgnoreCase)
            ? false
            : PublisherFixture.DeleteForRetentionTest(path);
        try
        {
            fx.MergeWidgets(("W1", "alpha", 3));
            var result = fx.Publish(keepPublishes: 2);

            // The old Widget parquet is unreferenced but locked — skipped, not fatal.
            Assert.Equal(SnapshotPublishStatus.Published, result.Status);
            Assert.True(result.FilesSkippedByRetention >= 1);
            Assert.True(File.Exists(firstWidgetParquet));
        }
        finally
        {
            fx.RetentionDelete = null;
        }

        // Next publish's retention pass picks it up once the consumer is gone.
        fx.MergeWidgets(("W1", "alpha", 4));
        fx.Publish(keepPublishes: 2);
        Assert.False(File.Exists(firstWidgetParquet));
    }

    [Fact]
    public void StaleStagingFilesFromACrashedRun_AreCleanedUp()
    {
        Directory.CreateDirectory(fx.PublishDirectory);
        Directory.CreateDirectory(Path.Combine(fx.PublishDirectory, "Widget"));
        var staleParquet = Path.Combine(fx.PublishDirectory, "Widget", "00000000000000000.parquet.staging");
        var staleManifest = Path.Combine(fx.PublishDirectory, $"{PublisherFixture.SnapshotName}-00000000000000000.json.staging");
        File.WriteAllText(staleParquet, "partial");
        File.WriteAllText(staleManifest, "partial");

        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        Assert.Empty(fx.Files("*.staging"));
    }

    [Fact]
    public void FaultBeforeManifestCommit_LeavesStandingSnapshotVisible_AndRecordsFailure()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        var first = fx.Publish();
        var firstManifestPath = Path.Combine(fx.PublishDirectory, first.ManifestFile!);

        fx.MergeWidgets(("W1", "alpha", 2));

        var injected = new InvalidOperationException("Injected immediately before manifest commit.");
        var observedReadyStaging = false;
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            fx.Publish(onBeforeManifestCommit: () =>
            {
                var stagingFile = Assert.Single(fx.Files("*.json.staging"));

                // The hook is after the staging manifest is fully written: it is already a
                // complete, readable snapshot whose parquet are all on disk, but its non-final
                // name keeps it invisible to resolution. That ordering IS the atomicity.
                var staged = PublishedSnapshot.Read(Path.Combine(fx.PublishDirectory, stagingFile));
                Assert.Equal(2, Convert.ToInt32(fx.ReadPublished("Widget", "\"Quantity\"", "\"_PrimaryKey\" = 'W1'", staged)));

                observedReadyStaging = true;
                throw injected;
            }));

        Assert.Same(injected, thrown);
        Assert.True(observedReadyStaging);
        Assert.Equal(firstManifestPath, PublishedSnapshot.ResolveNewest(fx.PublishDirectory, PublisherFixture.SnapshotName));
        Assert.Equal(first.ManifestFile, Assert.Single(fx.Manifests()));
        Assert.Single(fx.Files("*.json.staging"));

        // A consumer resolving now still sees the previous, complete publish.
        Assert.Equal(1, Convert.ToInt32(fx.ReadPublished("Widget", "\"Quantity\"", "\"_PrimaryKey\" = 'W1'")));

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
            fx.Publish(onBeforeManifestCommit: () => throw new InvalidOperationException("Injected failure.")));
        var staleManifest = Path.Combine(fx.PublishDirectory, Assert.Single(fx.Files("*.json.staging")));

        var recovered = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.Published, recovered.Status);
        Assert.False(File.Exists(staleManifest));
        Assert.Empty(fx.Files("*.staging"));
        Assert.Equal(2, fx.Manifests().Length);
        Assert.Equal(3, fx.Files("*.parquet").Length);   // old set + recovered Widget; failed orphan is swept
        Assert.Equal(recovered.ManifestFile, PublishPath.FileName(
            PublishedSnapshot.ResolveNewest(fx.PublishDirectory, PublisherFixture.SnapshotName)!));

        Assert.Equal(2, Convert.ToInt32(fx.ReadPublished("Widget", "\"Quantity\"", "\"_PrimaryKey\" = 'W1'")));

        Assert.Equal(1L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Failed:Exception'")));
        Assert.Equal(2L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Published'")));
    }

    [Fact]
    public void PublishStampsNeverCollide_EvenInTheSameMillisecond()
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

        Assert.Equal(["W1", "W2", "W3"], fx.ReadPublishedColumn("Widget", "\"_PrimaryKey\""));
    }

    [Fact]
    public void BookkeepingColumnsAreExported()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        var columns = fx.PublishedColumns("Widget");
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

    // ---- Manifest contract -------------------------------------------------------------------

    [Fact]
    public void AFixedNameCopyOfTheNewestManifest_GivesConsumersAStableEntryPoint()
    {
        // Committed manifests are versioned, so their names change every publish. External
        // consumers need one URL that does not.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var first = fx.Publish();

        var stablePath = Path.Combine(fx.PublishDirectory, "latest.json");
        Assert.True(File.Exists(stablePath));
        Assert.Equal(first.PublishId, PublishedSnapshot.Read(stablePath).PublishId);

        // It follows the newest commit...
        fx.MergeWidgets(("W1", "alpha", 2));
        var second = fx.Publish();
        Assert.Equal(second.PublishId, PublishedSnapshot.Read(stablePath).PublishId);

        // ...and it is a COPY, not the commit: it is never what resolve-newest, retention or
        // cold start work from, so it can never be mistaken for a published set of its own.
        Assert.DoesNotContain("latest.json", fx.Manifests());
        Assert.Equal(second.ManifestFile,
            PublishPath.FileName(PublishedSnapshot.ResolveNewest(fx.PublishDirectory, PublisherFixture.SnapshotName)!));
    }

    [Fact]
    public void ALostStablePointer_IsRestoredByTheNextCycle_EvenWhenNothingChanged()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        var first = fx.Publish();

        var stablePath = Path.Combine(fx.PublishDirectory, "latest.json");
        File.Delete(stablePath);

        // Nothing changed, so this publish skips — the pointer must still come back, or a
        // consumer bookmarked to it stays broken until the data happens to change.
        var second = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, second.Status);
        Assert.True(File.Exists(stablePath));
        Assert.Equal(first.PublishId, PublishedSnapshot.Read(stablePath).PublishId);
    }

    [Fact]
    public void KeepingFewerThanTwoPublishes_IsRefused()
    {
        // Retention depth is a recovery parameter: it is what lets cold start fall back past a
        // torn set, and what lets a consumer mid-refresh keep the files it already resolved.
        fx.MergeWidgets(("W1", "alpha", 1));

        var tooFew = Assert.Throws<ArgumentException>(() => fx.Publish(keepPublishes: 1));
        Assert.Contains("at least 2", tooFew.Message);
    }

    [Fact]
    public void AManifestNamingSomethingOtherThanABareFile_IsRefused()
    {
        // Manifests are read back from a shared location. An entry naming an absolute path or a
        // traversal would let retention fail to protect it and let a rebuild read a file the
        // publisher never wrote — so reading one is a hard failure, not a resolved path.
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var manifestPath = Path.Combine(fx.PublishDirectory, Assert.Single(fx.Manifests()));
        File.WriteAllText(manifestPath,
            File.ReadAllText(manifestPath).Replace("\"Widget/", "\"../escaped/"));

        var thrown = Assert.Throws<InvalidDataException>(() => PublishedSnapshot.Read(manifestPath));
        Assert.Contains("not a relative path", thrown.Message);
    }

    [Fact]
    public void AManifestFromANewerPublisher_IsRefusedRatherThanReadWithOlderRules()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        var manifestPath = Path.Combine(fx.PublishDirectory, Assert.Single(fx.Manifests()));
        File.WriteAllText(manifestPath, File.ReadAllText(manifestPath).Replace(
            $"\"manifestVersion\": {PublishedSnapshot.CurrentManifestVersion}",
            $"\"manifestVersion\": {PublishedSnapshot.CurrentManifestVersion + 1}"));

        var thrown = Assert.Throws<InvalidDataException>(() => PublishedSnapshot.Read(manifestPath));
        Assert.Contains("upgrade", thrown.Message);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{}")]                                    // parses fine; every member defaults
    [InlineData("{\"manifestVersion\": 2}")]              // parses fine; tables is null
    public void JsonThatIsNotAManifest_FailsAsBadData_NotAsANullReference(string content)
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.Publish();

        var manifestPath = Path.Combine(fx.PublishDirectory, Assert.Single(fx.Manifests()));
        File.WriteAllText(manifestPath, content);

        Assert.Throws<InvalidDataException>(() => PublishedSnapshot.Read(manifestPath));
    }

    [Fact]
    public void AnUnreadableBaselineManifest_DegradesToAFullReExport_NeverToAWrongPublish()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        File.WriteAllText(Path.Combine(fx.PublishDirectory, Assert.Single(fx.Manifests())), "{ not json");

        var result = fx.Publish();

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Equal(["Widget", "Gadget"], result.TablesExported);
        Assert.Empty(result.TablesReused);
    }

    [Fact]
    public void ARemotePublishLocation_FailsLoudlyRatherThanReportingNothingPublished()
    {
        // Directory.Exists("az://…") answers false without throwing, which every caller would
        // read as "nothing is published yet" — a full re-export against an empty baseline, and
        // a cold start that concludes the published set is gone.
        fx.MergeWidgets(("W1", "alpha", 1));

        var thrown = Assert.Throws<NotSupportedException>(() => SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = "az://hawta/publish",
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget],
        }));

        Assert.Contains("az://hawta/publish", thrown.Message);

        // ...and it is still recorded, because a publish that cannot reach its destination is
        // exactly the failure the run record exists to surface.
        Assert.Equal(1L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Failed:Exception'")));
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
        Assert.Equal(99, Convert.ToInt32(fx.ReadPublished("Widget", "\"Quantity\"", "\"_PrimaryKey\" = 'W2'")));
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
    public void HeldOpenManifestKeepsItsParquetAlive_UntilTheConsumerReleasesIt()
    {
        // The blocker finding: a published set whose manifest delete is skipped (a consumer is
        // mid-session against it) must keep protecting the parquet only it references — the
        // referenced set is built from manifests on disk, not just the kept window.
        fx.MergeGadgets(("G1", "one"));
        fx.MergeWidgets(("W1", "alpha", 1));
        var first = fx.Publish(keepPublishes: 2);
        var firstManifestPath = Path.Combine(fx.PublishDirectory, first.ManifestFile!);
        var firstWidgetParquet = Path.Combine("Widget", fx.Versions("Widget").Single()!);

        // A consumer that resolved the first manifest and is still reading through it.
        var consumer = PublishedSnapshot.Read(firstManifestPath);

        fx.MergeWidgets(("W1", "alpha", 2));
        fx.Publish(keepPublishes: 2);

        // Simulate the sharing violation through the retention delete hook. This avoids relying
        // on OS-specific file-lock semantics, and is the only form the condition takes for a
        // manifest that a consumer has read rather than held open.
        fx.RetentionDelete = path => string.Equals(path, firstManifestPath, StringComparison.OrdinalIgnoreCase)
            ? false
            : PublisherFixture.DeleteForRetentionTest(path);
        try
        {
            fx.MergeWidgets(("W1", "alpha", 3));
            var third = fx.Publish(keepPublishes: 2);

            Assert.Equal(SnapshotPublishStatus.Published, third.Status);
            Assert.True(third.FilesSkippedByRetention >= 1);                   // the held manifest
            Assert.True(File.Exists(Path.Combine(fx.PublishDirectory, firstWidgetParquet)));

            // The mid-session consumer still reads its own set successfully.
            Assert.Equal(1, Convert.ToInt32(
                fx.ReadPublished("Widget", "\"Quantity\"", "\"_PrimaryKey\" = 'W1'", consumer)));
        }
        finally
        {
            fx.RetentionDelete = null;
        }

        // Consumer gone → the next publish removes the old manifest AND its parquet.
        fx.MergeWidgets(("W1", "alpha", 4));
        fx.Publish(keepPublishes: 2);
        Assert.False(File.Exists(firstManifestPath));
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

        var gadgetParquet = Path.Combine(fx.PublishDirectory, "Gadget", fx.Versions("Gadget").Single()!);
        File.WriteAllBytes(gadgetParquet, [0xDE, 0xAD]);

        var result = fx.Publish();   // no data changed — only the torn file forces work

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Contains("Gadget", result.TablesExported);
        Assert.Equal(1L, Convert.ToInt64(fx.ReadPublished("Gadget", "count(*)")));
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

        Assert.Equal(["alpha", "zeta"], fx.ReadPublishedColumn("Widget", "\"Code\""));
    }

    [Fact]
    public void TombstonesFlowToTheReadTier()
    {
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeWidgets(("W1", "alpha", 1));   // W2 vanishes → tombstone
        fx.Publish();

        Assert.Equal(true, fx.ReadPublished("Widget", "\"_Deleted\"", "\"_PrimaryKey\" = 'W2'"));
    }

    public void Dispose() => fx.Dispose();
}
