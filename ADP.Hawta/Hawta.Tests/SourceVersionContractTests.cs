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
        Assert.Equal("database.widgets", OwnershipSource(store, widgets, "north"));
        Assert.Equal("csv.repeated", OwnershipSource(store, gadgets, null));

        // Ownership is a per-scope map: three merged rows across two tables produce exactly
        // one ownership row per (table, scope), never one per data row.
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.SourceOwnership")));

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
    public void ScopeAdoptionAdvancesVersionOnce_ASourceKeyChangeIsAMapWriteOnly_AndARepeatDoesNothing()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var table = Table("Widget");
        store.EnsureTable(table);

        Merge(store, table, [("W1", "one")], source: "source-a", scope: "A");
        var original = Sequence(store, table, "W1");
        Assert.Equal("source-a", OwnershipSource(store, table, "A"));

        // A changed source key over an unchanged scope is a configuration rename. The map row
        // is replaced; the data row is untouched and its version does not advance. (The old
        // per-key shape re-stamped every row here — a full-table churn for an attribution-only
        // change.)
        var rename = Merge(store, table, [("W1", "one")], source: "source-b", scope: "A");
        Assert.Equal(0, rename.RowsUpdated);
        Assert.Equal(original, Sequence(store, table, "W1"));
        Assert.Equal("source-b", OwnershipSource(store, table, "A"));

        // A scope change is an adoption: the row is re-stamped, and its sequence advances
        // exactly once — one reservation above the pre-merge high watermark.
        var watermarkBefore = store.ReadChangeSequenceHighWatermark();
        var scopeAdoption = Merge(store, table, [("W1", "one")], source: "source-b", scope: "B");
        Assert.Equal(1, scopeAdoption.RowsRescoped);
        Assert.Equal(1, scopeAdoption.RowsUpdated);
        var adopted = Sequence(store, table, "W1");
        Assert.Equal(watermarkBefore + 1, adopted);
        Assert.Equal("source-b", OwnershipSource(store, table, "B"));

        var unchanged = Merge(store, table, [("W1", "one")], source: "source-b", scope: "B");
        Assert.Equal(0, unchanged.RowsUpdated);
        Assert.Equal(adopted, Sequence(store, table, "W1"));
    }

    [Fact]
    public void OwnershipMapStoresTheNullScopeAsASentinel_AndRoundTripsBothScopeShapes()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });

        store.WriteSourceOwner("Widget", null, "source-a");
        store.WriteSourceOwner("Widget", "north", "source-b");

        Assert.Equal("source-a", store.ReadSourceOwner("Widget", null));
        Assert.Equal("source-b", store.ReadSourceOwner("Widget", "north"));
        Assert.Null(store.ReadSourceOwner("Widget", "south"));
        Assert.Null(store.ReadSourceOwner("Gadget", null));

        // The single-universe scope is stored as chr(0) — NOT NULL, so DuckDB accepts it in
        // the primary key — and the sentinel never escapes the store's read/write methods.
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.SourceOwnership WHERE \"SourceScope\" = chr(0)")));
        Assert.Equal(0L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.SourceOwnership WHERE \"SourceScope\" IS NULL")));

        // Re-writing an owner replaces its row rather than accumulating one per write.
        store.WriteSourceOwner("Widget", null, "source-c");
        Assert.Equal("source-c", store.ReadSourceOwner("Widget", null));
        Assert.Equal(2L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.SourceOwnership WHERE \"TableName\" = 'Widget'")));
    }

    [Fact]
    public void ScopesDifferingOnlyInCase_AreDistinctToTheMergeSqlAndTheMapAlike()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var table = Table("Widget");
        store.EnsureTable(table);

        Merge(store, table, [("W1", "one")], source: "source-upper", scope: "North");
        var original = Sequence(store, table, "W1");

        // IS NOT DISTINCT FROM is case-sensitive, so this is an adoption, not a repeat — and
        // the map keys the two spellings separately. The registry's case-INSENSITIVE
        // construction check is what keeps such a configuration from ever going live; the
        // engine's only job here is to never disagree with its own SQL.
        var adoption = Merge(store, table, [("W1", "one")], source: "source-lower", scope: "north");
        Assert.Equal(1, adoption.RowsRescoped);
        Assert.Equal(1, adoption.RowsUpdated);
        Assert.True(Sequence(store, table, "W1") > original);

        Assert.Equal("source-upper", OwnershipSource(store, table, "North"));
        Assert.Equal("source-lower", OwnershipSource(store, table, "north"));
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

        // The failing merge runs under a DIFFERENT source key, so the ownership assertion
        // below proves a failed run can never take over a scope's attribution.
        Assert.ThrowsAny<Exception>(() => SnapshotMerge.Execute(
            store,
            table,
            new StagingTable("bad_source_version_staging", "temp.main.\"bad_source_version_staging\""),
            new SnapshotMergeOptions { Source = "database.widgets-two", DeletesEnabled = false }));

        Assert.Equal(originalSequence, Sequence(store, table, "W1"));
        Assert.Equal("one", Scalar(store,
            "SELECT \"Code\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'W1'"));
        Assert.Equal("database.widgets", OwnershipSource(store, table, null));
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
            "DELETE FROM meta.SourceOwnership WHERE \"TableName\" = 'Widget'");

        var ownershipException = Assert.Throws<InvalidOperationException>(() => fx.Publish());
        Assert.Contains("not internally owned", ownershipException.Message);
        Assert.Empty(fx.Manifests());
    }

    [Fact]
    public void PublisherRejectsRowsWhoseScopeNoCatalogEntryOwns()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1));

        // Rows resident under a scope the catalog does not declare (the fixture's widget
        // source is unscoped): every row must be attributable, or the publish refuses.
        fx.Store.Execute(
            "UPDATE data.\"Widget\" SET \"_SourceScope\" = 'unclaimed' WHERE \"_PrimaryKey\" = 'W1'");

        var exception = Assert.Throws<InvalidOperationException>(() => fx.Publish());
        Assert.Contains("not owned by exactly one source catalog entry", exception.Message);
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
    public void PublisherRejectsAStoreWithDuplicatedKeys_LeavingTheCleanSetInPlace()
    {
        // With no PRIMARY KEY index on snapshot tables, this contract is what stands between
        // a corrupt store and every consumer of the published set. The duplicate below carries
        // a properly reserved sequence, so only the key check can refuse it.
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1), ("W2", "two", 2));
        var clean = fx.Publish();

        fx.Store.Execute(
            """
            INSERT INTO data."Widget"
            SELECT * REPLACE (CAST(? AS BIGINT) AS "_ChangeSequence")
            FROM data."Widget" WHERE "_PrimaryKey" = 'W1'
            """,
            fx.Store.ReserveChangeSequences(1));

        var exception = Assert.Throws<InvalidOperationException>(() => fx.Publish());
        Assert.Contains("duplicated primary key", exception.Message);

        // Refused before any export: the clean set is still the newest manifest and the
        // table folder holds no new parquet version.
        Assert.Equal([clean.ManifestFile], fx.Manifests());
        Assert.Single(fx.Versions("Widget"));
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
        Assert.Equal(fx.WidgetSource.Key, OwnershipSource(rebuilt, fx.Widget, null));

        // The restored map must make a re-merge of identical content a complete no-op —
        // zero updates proves the rebuilt estate re-stamps nothing after DR.
        var restaged = rebuilt.CreateStagingTable(fx.Widget);
        foreach (var (key, code, quantity) in new[] { ("W1", "one", 1), ("W2", "two", 2) })
        {
            rebuilt.Execute(
                $"""
                INSERT INTO {restaged.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}, NULL
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                key, code, quantity);
        }
        var remerge = SnapshotMerge.Execute(rebuilt, fx.Widget, restaged,
            new SnapshotMergeOptions { Source = fx.WidgetSource.Key, DeletesEnabled = false });
        Assert.Equal(0, remerge.RowsUpdated);
        Assert.Equal(0, remerge.RowsInserted);

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

    [Fact]
    public void AV4PublishedSetIsStillAValidSeed_ItsCatalogRestoresTheOwnershipMap()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "one", 1));
        fx.Publish();

        // Ownership was never published, so a set stamped by the previous schema version
        // seeds the current store: the map derives from the manifest's source catalog. This
        // is why the rebuild floor (OldestRebuildableSchemaVersion) deliberately stayed at 4.
        var path = PublishedSnapshot.ResolveNewest(
            fx.PublishDirectory, PublisherFixture.SnapshotName)!;
        var manifest = PublishedSnapshot.Read(path);
        File.WriteAllText(path, JsonSerializer.Serialize(
            manifest with { SchemaVersion = SnapshotStore.OldestRebuildableSchemaVersion },
            PublishedSnapshot.SerializerOptions));

        using var rebuilt = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        var result = SnapshotRebuild.Execute(
            rebuilt, [fx.Widget, fx.Gadget], fx.PublishDirectory, PublisherFixture.SnapshotName);

        Assert.NotNull(result.ManifestFile);
        Assert.Equal(1L, Convert.ToInt64(rebuilt.ExecuteScalar("SELECT count(*) FROM data.\"Widget\"")));
        Assert.Equal(fx.WidgetSource.Key, OwnershipSource(rebuilt, fx.Widget, null));
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

    private static string? OwnershipSource(
        SnapshotStore store,
        SnapshotTableDefinition table,
        string? scope) =>
        store.ReadSourceOwner(table.Name, scope);

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
