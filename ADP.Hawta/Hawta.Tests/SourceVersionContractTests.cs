using System.Text.Json;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public sealed class SourceVersionContractTests
{
    private static readonly string[] RemovedRowIdentityColumns =
    [
        "_SourceKey",
        "_SourceRecordId",
        "_SourceRecordIdentityKind",
    ];

    [Fact]
    public void VersionsAreStoreWideIncreasing_AndSourceOwnershipIsPrivate()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var widgets = Table("Widget");
        var gadgets = Table("Gadget");
        store.EnsureTable(widgets);
        store.EnsureTable(gadgets);

        Merge(store, widgets, [("W2", "two"), ("W1", "one")],
            source: "database.widgets", scope: "north",
            kind: SourceRecordIdentityKind.DatabaseKey);
        Merge(store, gadgets, [("G1", "one")],
            source: "csv.repeated", kind: SourceRecordIdentityKind.OccurrenceOrdinal);

        Assert.Equal([1L, 2L, 3L], ReadSequences(store, widgets, gadgets));
        Assert.Equal("north", Scalar(store,
            "SELECT \"_SourceScope\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));
        Assert.NotEqual(DBNull.Value, Scalar(store,
            "SELECT \"_ChangeRecordedAt\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));
        Assert.Equal("database.widgets", OwnershipSource(store, widgets, "W1"));
        Assert.Equal("csv.repeated", OwnershipSource(store, gadgets, "G1"));

        var columns = TableColumns(store, widgets);
        Assert.Contains(BookkeepingColumns.PrimaryKey, columns);
        Assert.Contains(BookkeepingColumns.ChangeSequence, columns);
        Assert.Contains(BookkeepingColumns.ChangeRecordedAt, columns);
        Assert.DoesNotContain(RemovedRowIdentityColumns, columns.Contains);
    }

    [Fact]
    public void UnchangedReingestRetainsVersion_WhileUpdateTombstoneAndResurrectionAdvanceIt()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var table = Table("Widget");
        store.EnsureTable(table);

        Merge(store, table, [("W1", "one"), ("W2", "two")], deletesEnabled: true);
        var insertedSequence = Sequence(store, table, "W1");
        var insertedAt = ChangeRecordedAt(store, table, "W1");

        var unchanged = Merge(store, table, [("W1", "one"), ("W2", "two")], deletesEnabled: true);
        Assert.Equal(0, unchanged.RowsUpdated);
        Assert.Equal(insertedSequence, Sequence(store, table, "W1"));
        Assert.Equal(insertedAt, ChangeRecordedAt(store, table, "W1"));

        Merge(store, table, [("W1", "changed"), ("W2", "two")], deletesEnabled: true);
        var updatedSequence = Sequence(store, table, "W1");
        Assert.True(updatedSequence > insertedSequence);

        Merge(store, table, [("W2", "two")], deletesEnabled: true);
        var tombstoneSequence = Sequence(store, table, "W1");
        Assert.True(tombstoneSequence > updatedSequence);
        Assert.Equal(true, Scalar(store,
            "SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));

        Merge(store, table, [("W1", "changed"), ("W2", "two")], deletesEnabled: true);
        Assert.True(Sequence(store, table, "W1") > tombstoneSequence);
        Assert.Equal(false, Scalar(store,
            "SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));
    }

    [Fact]
    public void SourceAndScopeAdoptionAdvanceVersion_ButARepeatDoesNot()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var table = Table("Widget");
        store.EnsureTable(table);

        Merge(store, table, [("W1", "one")], source: "source-a", scope: "A");
        var original = Sequence(store, table, "W1");

        var sourceAdoption = Merge(store, table, [("W1", "one")], source: "source-b", scope: "A");
        Assert.Equal(1, sourceAdoption.RowsUpdated);
        var afterSource = Sequence(store, table, "W1");
        Assert.True(afterSource > original);
        Assert.Equal("source-b", OwnershipSource(store, table, "W1"));

        var scopeAdoption = Merge(store, table, [("W1", "one")], source: "source-b", scope: "B");
        Assert.Equal(1, scopeAdoption.RowsRescoped);
        var adopted = Sequence(store, table, "W1");
        Assert.True(adopted > afterSource);

        var unchanged = Merge(store, table, [("W1", "one")], source: "source-b", scope: "B");
        Assert.Equal(0, unchanged.RowsUpdated);
        Assert.Equal(adopted, Sequence(store, table, "W1"));
    }

    [Fact]
    public void FailedMergeRollsBackRowOwnershipAndSequenceReservation()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var table = new SnapshotTableDefinition("Widget",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
        ]);
        store.EnsureTable(table);
        MergeQuantity(store, table, [("W1", "one", 1)]);
        var originalSequence = Sequence(store, table, "W1");

        store.Execute(
            """
            CREATE OR REPLACE TEMP TABLE "bad_source_version_staging" (
                "Code" VARCHAR, "Quantity" VARCHAR,
                "_PrimaryKey" VARCHAR, "_RowHash" VARCHAR,
                "_ReplicationHash" VARCHAR, "_SourceModified" TIMESTAMP
            )
            """);
        store.Execute(
            "INSERT INTO \"bad_source_version_staging\" VALUES ('changed', '2', 'W1', 'changed-hash', NULL, NULL)");
        store.Execute(
            "INSERT INTO \"bad_source_version_staging\" VALUES ('bad', 'not-an-integer', 'W2', 'new-hash', NULL, NULL)");

        Assert.ThrowsAny<Exception>(() => SnapshotMerge.Execute(
            store,
            table,
            new StagingTable("bad_source_version_staging", "temp.main.\"bad_source_version_staging\""),
            new SnapshotMergeOptions { Source = "database.widgets", DeletesEnabled = false }));

        Assert.Equal(originalSequence, Sequence(store, table, "W1"));
        Assert.Equal("one", Scalar(store,
            "SELECT \"Code\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));
        Assert.Equal("database.widgets", OwnershipSource(store, table, "W1"));
        Assert.Equal(originalSequence, store.ReadChangeSequenceHighWatermark());

        MergeQuantity(store, table, [("W2", "two", 2)]);
        Assert.Equal(originalSequence + 1, Sequence(store, table, "W2"));
    }

    [Fact]
    public void PublishUsesManifestCatalogAndTwoColumnRowContract_WithoutMutatingForcedRepublish()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1));
        var sequence = Sequence(fx.Store, fx.Widget, "W1");
        var recordedAt = ChangeRecordedAt(fx.Store, fx.Widget, "W1");

        fx.Publish();
        var forced = fx.Publish(force: true);
        var manifest = fx.ReadNewest();

        Assert.Equal(SnapshotPublishStatus.Published, forced.Status);
        Assert.Equal(SnapshotStore.CurrentSchemaVersion, manifest.SchemaVersion);
        Assert.Equal(sequence, manifest.ChangeSequenceHighWatermark);

        var source = Assert.Single(fx.Entry("Widget", manifest).SourceCatalog);
        Assert.Equal(fx.WidgetSource.Key, source.SourceKey);
        Assert.Null(source.SourceScope);
        Assert.Equal(SourceRecordIdentityKind.LogicalKey, source.RecordIdentity.IdentityKind);
        Assert.Equal(BookkeepingColumns.PrimaryKey, source.RecordIdentity.CanonicalColumn);
        Assert.Equal("Code", source.RecordIdentity.PrimaryKeyColumn);
        Assert.Equal(SourcePrimaryKeySemantics.SourceColumn, source.RecordIdentity.Semantics);
        var keyPart = Assert.Single(source.RecordIdentity.KeyParts);
        Assert.Equal("Code", keyPart.Column);
        Assert.Equal(FileKeyNormalization.Trim, keyPart.Normalization);

        var publishedColumns = fx.PublishedColumns("Widget");
        Assert.Contains(BookkeepingColumns.PrimaryKey, publishedColumns);
        Assert.Contains(BookkeepingColumns.ChangeSequence, publishedColumns);
        Assert.Contains(BookkeepingColumns.ChangeRecordedAt, publishedColumns);
        Assert.DoesNotContain(RemovedRowIdentityColumns, publishedColumns.Contains);
        Assert.Equal(sequence, Convert.ToInt64(fx.ReadPublished(
            "Widget", "\"_ChangeSequence\"", "\"_PrimaryKey\" = 'W1'")));

        Assert.Equal(sequence, Sequence(fx.Store, fx.Widget, "W1"));
        Assert.Equal(recordedAt, ChangeRecordedAt(fx.Store, fx.Widget, "W1"));

        var manifestText = File.ReadAllText(PublishedSnapshot.ResolveNewest(
            fx.PublishDirectory, PublisherFixture.SnapshotName)!);
        Assert.Contains("\"identityKind\": \"LogicalKey\"", manifestText, StringComparison.Ordinal);
        Assert.Contains("\"semantics\": \"SourceColumn\"", manifestText, StringComparison.Ordinal);
        Assert.Contains("\"normalization\": \"Trim\"", manifestText, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogOnlyChangePublishesManifestAndReusesParquetWithoutAdvancingRows()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1));
        fx.Publish();
        var before = fx.ReadNewest();
        var sequence = Sequence(fx.Store, fx.Widget, "W1");

        var changedWidgetSource = Source(
            fx.WidgetSource.Key,
            fx.Widget,
            descriptor: SourceRecordIdentityDescriptor.DatabaseKey("Code"));
        var result = SnapshotPublisher.Publish(fx.Store, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget, fx.Gadget],
            Sources = [changedWidgetSource, fx.GadgetSource],
        });
        var after = fx.ReadNewest();

        Assert.Equal(SnapshotPublishStatus.Published, result.Status);
        Assert.Empty(result.TablesExported);
        Assert.Equal(["Widget", "Gadget"], result.TablesReused);
        var beforeLocation = before.Tables.Single(entry => entry.Table == "Widget").Location;
        var afterLocation = after.Tables.Single(entry => entry.Table == "Widget").Location;
        Assert.Equal(beforeLocation.Kind, afterLocation.Kind);
        Assert.Equal(beforeLocation.Paths, afterLocation.Paths);
        Assert.Equal("Code", Assert.Single(after.Tables.Single(
            entry => entry.Table == "Widget").SourceCatalog).RecordIdentity.PrimaryKeyColumn);
        Assert.Equal(SourceRecordIdentityKind.DatabaseKey, Assert.Single(after.Tables.Single(
            entry => entry.Table == "Widget").SourceCatalog).RecordIdentity.IdentityKind);
        Assert.Equal(sequence, Sequence(fx.Store, fx.Widget, "W1"));
        Assert.Equal(sequence, after.ChangeSequenceHighWatermark);
    }

    [Fact]
    public void PublisherRejectsInvalidSequenceAndMissingPrivateOwnership()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1));
        fx.Store.Execute(
            "UPDATE data.\"Widget\" SET \"_ChangeSequence\" = 0 WHERE \"_PrimaryKey\" = 'W1'");

        var sequenceException = Assert.Throws<InvalidOperationException>(() => fx.Publish());
        Assert.Contains("without complete durable change-sequence metadata", sequenceException.Message);
        Assert.Empty(fx.Manifests());

        fx.Store.Execute(
            "UPDATE data.\"Widget\" SET \"_ChangeSequence\" = 1 WHERE \"_PrimaryKey\" = 'W1'");
        fx.Store.Execute(
            "DELETE FROM meta.SourceOwnership WHERE \"TableName\" = 'Widget' AND \"PrimaryKey\" = 'W1'");

        var ownershipException = Assert.Throws<InvalidOperationException>(() => fx.Publish());
        Assert.Contains("not internally owned", ownershipException.Message);
        Assert.Empty(fx.Manifests());
    }

    [Fact]
    public void PublisherRejectsDuplicateSequencesAndValuesAboveAllocatorHighWatermark()
    {
        using (var duplicate = new PublisherFixture())
        {
            duplicate.MergeWidgets(("W1", "one", 1), ("W2", "two", 2));
            duplicate.Store.Execute(
                "UPDATE data.\"Widget\" SET \"_ChangeSequence\" = 1 WHERE \"_PrimaryKey\" = 'W2'");

            var exception = Assert.Throws<InvalidOperationException>(() => duplicate.Publish());
            Assert.Contains("duplicate durable change-sequence", exception.Message);
            Assert.Empty(duplicate.Manifests());
        }

        using (var aboveAllocator = new PublisherFixture())
        {
            aboveAllocator.MergeWidgets(("W1", "one", 1));
            aboveAllocator.Store.Execute(
                "UPDATE data.\"Widget\" SET \"_ChangeSequence\" = 2 WHERE \"_PrimaryKey\" = 'W1'");

            var exception = Assert.Throws<InvalidOperationException>(() => aboveAllocator.Publish());
            Assert.Contains("above allocator high watermark", exception.Message);
            Assert.Empty(aboveAllocator.Manifests());
        }
    }

    [Fact]
    public void SchemaV4ManifestRequiresHighWatermarkAndSourceCatalog()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1));
        fx.Publish();

        var path = PublishedSnapshot.ResolveNewest(
            fx.PublishDirectory, PublisherFixture.SnapshotName)!;
        var manifest = PublishedSnapshot.Read(path);

        File.WriteAllText(path, JsonSerializer.Serialize(
            manifest with { ChangeSequenceHighWatermark = null },
            PublishedSnapshot.SerializerOptions));
        var highWatermarkException = Assert.Throws<SnapshotSequenceContractException>(
            () => PublishedSnapshot.Read(path));
        Assert.Contains("no change-sequence high watermark", highWatermarkException.Message);

        var tablesWithoutCatalog = manifest.Tables.Select(entry =>
            entry with { SourceCatalog = [] }).ToList();
        File.WriteAllText(path, JsonSerializer.Serialize(
            manifest with { Tables = tablesWithoutCatalog },
            PublishedSnapshot.SerializerOptions));
        var catalogException = Assert.Throws<InvalidDataException>(() => PublishedSnapshot.Read(path));
        Assert.Contains("has no source catalog", catalogException.Message);
    }

    [Fact]
    public void RebuildPreservesVersionsAndOwnership_AndReseedsAbovePublishedHighWatermark()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1), ("W2", "two", 2));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();
        var published = fx.ReadNewest();

        using var rebuilt = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        SnapshotRebuild.Execute(
            rebuilt, [fx.Widget, fx.Gadget], fx.PublishDirectory, PublisherFixture.SnapshotName);

        Assert.Equal(1L, Sequence(rebuilt, fx.Widget, "W1"));
        Assert.Equal(fx.WidgetSource.Key, OwnershipSource(rebuilt, fx.Widget, "W1"));

        var staging = rebuilt.CreateStagingTable(fx.Widget);
        rebuilt.Execute(
            $"""
            INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
            SELECT "Code", "Quantity", 'W3', {RowHash.Expression(["Code", "Quantity"])}, NULL
            FROM (SELECT 'three' AS "Code", 3 AS "Quantity")
            """);
        SnapshotMerge.Execute(rebuilt, fx.Widget, staging,
            new SnapshotMergeOptions { Source = fx.WidgetSource.Key, DeletesEnabled = false });

        Assert.Equal(published.ChangeSequenceHighWatermark!.Value + 1,
            Sequence(rebuilt, fx.Widget, "W3"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void PreV4ManifestIsIgnoredForACleanSourceRegeneration(int oldSchemaVersion)
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1));
        fx.Publish();

        var path = PublishedSnapshot.ResolveNewest(
            fx.PublishDirectory, PublisherFixture.SnapshotName)!;
        var manifest = PublishedSnapshot.Read(path);
        var oldTables = manifest.Tables.Select(entry =>
            entry with { SourceCatalog = [] }).ToList();
        File.WriteAllText(path, JsonSerializer.Serialize(
            manifest with
            {
                SchemaVersion = oldSchemaVersion,
                ChangeSequenceHighWatermark = oldSchemaVersion == 2
                    ? null
                    : manifest.ChangeSequenceHighWatermark,
                Tables = oldTables,
            },
            PublishedSnapshot.SerializerOptions));

        using var rebuilt = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var result = SnapshotRebuild.Execute(
            rebuilt, [fx.Widget, fx.Gadget], fx.PublishDirectory, PublisherFixture.SnapshotName);

        Assert.Null(result.ManifestFile);
        Assert.Empty(result.TablesLoaded);
        Assert.Equal(["Widget", "Gadget"], result.TablesCreatedEmpty);
        Assert.Single(result.PublishesSkipped);
        Assert.Equal(0L, Convert.ToInt64(rebuilt.ExecuteScalar("SELECT count(*) FROM data.\"Widget\"")));
        Assert.Equal(0L, rebuilt.ReadChangeSequenceHighWatermark());
    }

    private static SnapshotTableDefinition Table(string name) => new(name,
        [new SnapshotColumn("Code", "VARCHAR")]);

    private static SnapshotMergeResult Merge(
        SnapshotStore store,
        SnapshotTableDefinition table,
        IReadOnlyList<(string Key, string Code)> rows,
        string source = "source",
        string? scope = null,
        SourceRecordIdentityKind kind = SourceRecordIdentityKind.LogicalKey,
        bool deletesEnabled = false)
    {
        var staging = store.CreateStagingTable(table);
        foreach (var row in rows)
        {
            store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", ?, {RowHash.Expression(["Code"])}, NULL
                FROM (SELECT ? AS "Code")
                """,
                row.Key, row.Code);
        }

        return SnapshotMerge.Execute(store, table, staging, new SnapshotMergeOptions
        {
            Source = source,
            SourceScope = scope,
            RecordIdentityKind = kind,
            DeletesEnabled = deletesEnabled,
        });
    }

    private static SnapshotMergeResult MergeQuantity(
        SnapshotStore store,
        SnapshotTableDefinition table,
        IReadOnlyList<(string Key, string Code, int Quantity)> rows)
    {
        var staging = store.CreateStagingTable(table);
        foreach (var row in rows)
        {
            store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}, NULL
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                row.Key, row.Code, row.Quantity);
        }

        return SnapshotMerge.Execute(store, table, staging,
            new SnapshotMergeOptions { Source = "database.widgets", DeletesEnabled = false });
    }

    private static SnapshotSource Source(
        string key,
        SnapshotTableDefinition table,
        string? scope = null,
        SourceRecordIdentityDescriptor? descriptor = null) => new()
    {
        Key = key,
        SourceScope = scope,
        RecordIdentity = descriptor ?? SourceRecordIdentityDescriptor.LogicalKey(BookkeepingColumns.PrimaryKey),
        Table = table,
        Cadence = TimeSpan.FromMinutes(1),
        Ingest = _ => throw new InvalidOperationException("Not run by this contract test."),
    };

    private static long Sequence(SnapshotStore store, SnapshotTableDefinition table, string key) =>
        Convert.ToInt64(store.ExecuteScalar(
            $"SELECT \"_ChangeSequence\" FROM {table.QualifiedName} WHERE \"_PrimaryKey\" = ?", key));

    private static DateTime ChangeRecordedAt(
        SnapshotStore store,
        SnapshotTableDefinition table,
        string key) =>
        Convert.ToDateTime(store.ExecuteScalar(
            $"SELECT \"_ChangeRecordedAt\" FROM {table.QualifiedName} WHERE \"_PrimaryKey\" = ?", key));

    private static string OwnershipSource(
        SnapshotStore store,
        SnapshotTableDefinition table,
        string key) =>
        Convert.ToString(store.ExecuteScalar(
            "SELECT \"SourceKey\" FROM meta.SourceOwnership WHERE \"TableName\" = ? AND \"PrimaryKey\" = ?",
            table.Name, key))!;

    private static object Scalar(SnapshotStore store, string sql) => store.ExecuteScalar(sql)!;

    private static HashSet<string> TableColumns(
        SnapshotStore store,
        SnapshotTableDefinition table)
    {
        using var command = store.Connection.CreateCommand();
        command.CommandText =
            "SELECT column_name FROM information_schema.columns WHERE table_schema = 'data' AND table_name = ?";
        var parameter = command.CreateParameter();
        parameter.Value = table.Name;
        command.Parameters.Add(parameter);

        using var reader = command.ExecuteReader();
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            result.Add(reader.GetString(0));
        return result;
    }

    private static List<long> ReadSequences(
        SnapshotStore store,
        params SnapshotTableDefinition[] tables)
    {
        var selects = tables.Select(table =>
            $"SELECT \"_ChangeSequence\" AS sequence FROM {table.QualifiedName}");
        using var command = store.Connection.CreateCommand();
        command.CommandText = $"SELECT sequence FROM ({string.Join(" UNION ALL ", selects)}) ORDER BY sequence";
        using var reader = command.ExecuteReader();
        var result = new List<long>();
        while (reader.Read())
            result.Add(reader.GetInt64(0));
        return result;
    }
}
