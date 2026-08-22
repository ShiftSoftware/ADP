using System.Text.Json;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Lazy residency end to end: a real file feed behind a real change gate, published to a real
/// directory, cold-started into fresh stores. Widget is the deferred-capable file-fed table;
/// Gadget is a declarative (non-file) sibling that stays Resident and forces manifest rewrites
/// so carry-forward has something to survive.
/// </summary>
public sealed class ResidencyFixture : IDisposable
{
    public SnapshotStore Store { get; }
    public SnapshotTableDefinition Widget { get; }
    public SnapshotTableDefinition Gadget { get; }
    public string FeedDirectory { get; }
    public string PublishDirectory { get; }
    public string FeedPath { get; }
    public SourceChangeGate Gate { get; }

    public const string SnapshotName = "resi-read";

    public ResidencyFixture()
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

        var root = Path.Combine(Path.GetTempPath(), "hawta-residency", Guid.NewGuid().ToString("N"));
        FeedDirectory = Path.Combine(root, "feeds");
        PublishDirectory = Path.Combine(root, "publish");
        Directory.CreateDirectory(FeedDirectory);
        FeedPath = Path.Combine(FeedDirectory, "widgets.csv");
        Gate = new SourceChangeGate();
    }

    public void WriteFeed(string content, string? path = null) =>
        File.WriteAllText(path ?? FeedPath, content);

    /// <summary>
    /// Rewrites a feed with IDENTICAL content but a guaranteed-different mtime — the false
    /// gate fire the content guard exists for. The mtime is pushed forward explicitly because
    /// two writes inside one filesystem timestamp granule would read as unchanged.
    /// </summary>
    public void RedeliverIdentical(string? path = null)
    {
        var target = path ?? FeedPath;
        var content = File.ReadAllText(target);
        var before = File.GetLastWriteTimeUtc(target);
        File.WriteAllText(target, content);
        File.SetLastWriteTimeUtc(target, before.AddSeconds(1));
    }

    public FileSnapshotIngestorOptions FileOptions(
        string sourceKey = "file-widget",
        string? scope = null,
        string? path = null,
        string? ingestVersion = null,
        SourceChangeGate? gate = null,
        TimeSpan? reingestAfter = null) =>
        new()
        {
            Table = Widget,
            FilePath = path ?? FeedPath,
            LogicalKey = FileLogicalKey.Single("Code"),
            ChangeGate = gate ?? Gate,
            IngestVersion = ingestVersion,
            ReingestAfter = reingestAfter,
            MergeOptions = new SnapshotMergeOptions
            {
                Source = sourceKey,
                SourceScope = scope,
                DeletesEnabled = true,
            },
        };

    public SnapshotMergeResult Ingest(SnapshotStore store, FileSnapshotIngestorOptions? options = null) =>
        FileSnapshotIngestor.Ingest(store, options ?? FileOptions(), new DirectoryListingFileMetadataProbe());

    /// <summary>A registry source wrapping one file-ingestion config (the deferred-capable shape).</summary>
    public SnapshotSource FileSource(
        FileSnapshotIngestorOptions options,
        bool pin = false,
        IReadOnlyList<CosmosFamilyMapping>? families = null,
        bool replicationEnabled = true) =>
        new()
        {
            Key = options.MergeOptions.Source,
            SourceScope = options.MergeOptions.SourceScope,
            RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
            Table = Widget,
            Cadence = TimeSpan.FromMinutes(1),
            Ingest = context => FileSnapshotIngestor.Ingest(context.Store, options, context.FileMetadata),
            FileIngestion = options,
            PinResident = pin,
            Families = families,
            ReplicationEnabled = replicationEnabled,
        };

    /// <summary>Gadget's declarative source — no file ingestion, so Gadget can never defer.</summary>
    public SnapshotSource GadgetSource() => new()
    {
        Key = "test-gadget",
        RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Label"),
        Table = Gadget,
        Cadence = TimeSpan.FromMinutes(1),
        Ingest = _ => throw new NotSupportedException("Declarative only."),
    };

    public SourceRegistry Registry(params SnapshotSource[] sources) => new(sources);

    public SnapshotPublishResult Publish(SnapshotStore store, SourceRegistry registry, bool force = false) =>
        SnapshotPublisher.Publish(store, new SnapshotPublishOptions
        {
            PublishDirectory = PublishDirectory,
            SnapshotName = SnapshotName,
            Tables = registry.Tables,
            Sources = registry.Sources,
            Force = force,
        });

    public SnapshotStore ColdStart(SourceRegistry registry, out SnapshotRebuildResult result)
    {
        var fresh = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        try
        {
            result = SnapshotRebuild.Execute(fresh, registry, PublishDirectory, SnapshotName);
            return fresh;
        }
        catch
        {
            fresh.Dispose();
            throw;
        }
    }

    public SnapshotMergeResult MergeGadgets(SnapshotStore store, params (string Key, string Label)[] rows)
    {
        var staging = store.CreateStagingTable(Gadget);
        foreach (var row in rows)
        {
            store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Label", "_PrimaryKey", "_RowHash")
                SELECT "Label", ?, {RowHash.Expression(["Label"])}
                FROM (SELECT ? AS "Label")
                """,
                row.Key, row.Label);
        }
        return SnapshotMerge.Execute(store, Gadget, staging,
            new SnapshotMergeOptions { Source = "test-gadget", DeletesEnabled = true });
    }

    public PublishedSnapshot ReadNewestManifest() =>
        PublishedSnapshot.ReadNewest(PublishDirectory, SnapshotName)
        ?? throw new InvalidOperationException("Nothing published.");

    public PublishedTableManifest Entry(string table, PublishedSnapshot? manifest = null) =>
        (manifest ?? ReadNewestManifest()).Tables.Single(t => t.Table == table);

    public string[] WidgetParquetVersions()
    {
        var folder = Path.Combine(PublishDirectory, "Widget");
        return Directory.Exists(folder)
            ? [.. Directory.GetFiles(folder, "*.parquet").Select(Path.GetFileName).Order()!]
            : [];
    }

    public static long Rows(SnapshotStore store, string table) =>
        Convert.ToInt64(store.ExecuteScalar($"SELECT count(*) FROM data.\"{table}\""));

    public void Dispose()
    {
        Store.Dispose();
        var root = Path.GetDirectoryName(FeedDirectory)!;
        try { Directory.Delete(root, recursive: true); } catch { }
    }
}

public sealed class ResidencyTests : IDisposable
{
    private readonly ResidencyFixture fx = new();

    private const string FeedV1 = "Code,Quantity\nA,1\nB,2\n";
    private const string FeedV2 = "Code,Quantity\nA,1\nB,5\nC,3\n";

    /// <summary>Ingest + publish on the primary store, then cold-start a fresh store that defers Widget.</summary>
    private SnapshotStore DeferredEstate(out SnapshotRebuildResult rebuild, out SourceRegistry registry)
    {
        fx.WriteFeed(FeedV1);
        registry = fx.Registry(fx.FileSource(fx.FileOptions()), fx.GadgetSource());
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        fx.MergeGadgets(fx.Store, ("G1", "one"));
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        var fresh = fx.ColdStart(registry, out rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
        return fresh;
    }

    // ---- The guard that is built first: a non-resident table can never be exported --------

    [Fact]
    public void DeferredTable_WithMismatchedSignature_IsCarried_NeverExported_EvenUnderForce()
    {
        using var fresh = DeferredEstate(out _, out var registry);
        var versionsBefore = fx.WidgetParquetVersions();
        var entryBefore = fx.Entry("Widget");

        // The deferred table's live signature is (0, empty) against a baseline of (2, hash) —
        // the exact mismatch that would re-export empty parquet over the copy of record if
        // the deferred branch ran after signature reading. Force must not bypass it either.
        var publish = fx.Publish(fresh, registry, force: true);

        Assert.DoesNotContain("Widget", publish.TablesExported);
        Assert.Contains("Widget", publish.TablesReused);
        Assert.Equal(versionsBefore, fx.WidgetParquetVersions());
        Assert.Equal(2, fx.Entry("Widget").RowCount);
        Assert.Equal(entryBefore.StateHash, fx.Entry("Widget").StateHash);
    }

    [Fact]
    public void DeferredTable_WithNoCommittedEntry_FailsThePublish_Loudly()
    {
        // A contradiction by construction (deferral requires a committed entry), simulated
        // directly: the publisher must refuse, never export an empty file.
        fx.Store.MarkTableDeferred("Widget", "phantom.json",
            [Path.Combine(fx.PublishDirectory, "Widget", "phantom.parquet")], 2, []);
        var registry = fx.Registry(fx.FileSource(fx.FileOptions()), fx.GadgetSource());

        var exception = Assert.Throws<InvalidOperationException>(() => fx.Publish(fx.Store, registry));

        Assert.Contains("Deferred", exception.Message);
        Assert.Empty(fx.WidgetParquetVersions());
        Assert.Equal(1L, Convert.ToInt64(fx.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.PublishRuns WHERE \"Status\" = 'Failed:Exception'")));
    }

    [Fact]
    public void DeferredTable_WithTornPublishedCopy_FailsThePublish_NeverExportsEmpty()
    {
        using var fresh = DeferredEstate(out _, out var registry);
        var parquet = fx.Entry("Widget").Resolve(fx.PublishDirectory).Single();
        File.WriteAllBytes(parquet, [1, 2, 3]);

        Assert.Throws<InvalidDataException>(() => fx.Publish(fresh, registry));

        // Exactly one (now-torn) version still on disk: nothing exported over it.
        Assert.Single(fx.WidgetParquetVersions());
    }

    [Fact]
    public void EnsureResidentForExport_IsTheBackstop()
    {
        fx.Store.MarkTableDeferred("Widget", "m.json", ["x.parquet"], 0, []);
        Assert.Throws<InvalidOperationException>(() =>
            SnapshotPublisher.EnsureResidentForExport(fx.Store, fx.Widget));

        fx.Store.MarkTableResident("Widget");
        SnapshotPublisher.EnsureResidentForExport(fx.Store, fx.Widget); // no throw
    }

    // ---- Carry-forward ---------------------------------------------------------------------

    [Fact]
    public void CarriedEntry_SurvivesAManifestRewrite_ByteForByte_IncludingTheNewFields()
    {
        using var fresh = DeferredEstate(out _, out var registry);
        var before = fx.Entry("Widget");
        Assert.NotNull(before.SourceCatalog.Single().ContentHash);

        // Gadget changed on the fresh store, so a NEW manifest must be written — and Widget's
        // entry must ride into it untouched.
        fx.MergeGadgets(fresh, ("G1", "one"), ("G2", "two"));
        var publish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, publish.Status);
        Assert.Equal(["Gadget"], publish.TablesExported);

        var after = fx.Entry("Widget");
        Assert.Equal(
            JsonSerializer.Serialize(before, PublishedSnapshot.SerializerOptions),
            JsonSerializer.Serialize(after, PublishedSnapshot.SerializerOptions));
    }

    // ---- Cold start ------------------------------------------------------------------------

    [Fact]
    public void ColdStart_SkipsADeferredCapableTable_AndTheEstateKeepsWorking()
    {
        using var fresh = DeferredEstate(out var rebuild, out var registry);

        // Rows were not loaded; the state is recorded, not inferred; Gadget loaded normally.
        Assert.Equal(0, ResidencyFixture.Rows(fresh, "Widget"));
        Assert.Equal(2, rebuild.TablesDeferred.Single().Rows);
        Assert.Equal(SnapshotResidency.Deferred, fresh.ReadResidency("Widget"));
        Assert.Equal(["Gadget"], rebuild.TablesLoaded.Select(t => t.Table));
        Assert.DoesNotContain("Widget", rebuild.TablesCreatedEmpty);

        // The restored stamp keeps the gate quiet: the deferred source still runs its tick
        // and still writes its skip run record (rule L8 — the health chain depends on it).
        var tick = fx.Ingest(fresh);
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, tick.Status);
        Assert.Equal(1L, Convert.ToInt64(fresh.ExecuteScalar(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Skipped:SourceUnchanged'")));

        // And the next publish carries the standing manifest — nothing re-exports.
        Assert.Equal(SnapshotPublishStatus.SkippedNoChanges, fx.Publish(fresh, registry).Status);

        // Ownership was restored from the catalog without rows.
        Assert.Equal("file-widget", fresh.ExecuteScalar(
            "SELECT \"SourceKey\" FROM meta.SourceOwnership WHERE \"TableName\" = 'Widget'"));
    }

    [Theory]
    [InlineData(false)] // a plain dirty row
    [InlineData(true)]  // a dead-lettered row — invisible to the pump's own predicate
    public void ColdStart_Loads_WhenRowsArePending_AndReplicationIsEnabled(bool deadLettered)
    {
        fx.WriteFeed(FeedV1);
        var families = new List<CosmosFamilyMapping>
        {
            new()
            {
                Family = "Widget", Database = "TestDb", Container = "Widgets",
                Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] } },
            },
        };
        var registry = fx.Registry(fx.FileSource(fx.FileOptions(), families: families));
        Assert.True(fx.Ingest(fx.Store).Succeeded);

        if (deadLettered)
        {
            fx.Store.Execute(
                $"UPDATE data.\"Widget\" SET \"_ReplicationAttempts\" = {SnapshotStore.MaxReplicationAttempts}");
            Assert.Equal(0, fx.Store.CountDirtyRows(fx.Widget));
            Assert.Equal(2, fx.Store.CountDeadLetteredRows(fx.Widget));
        }

        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(2, fx.Entry("Widget").ReplicationPending);

        using var fresh = fx.ColdStart(registry, out var rebuild);

        // Owed to the pump — the copy must come home so the drain can see it.
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(["Widget"], rebuild.TablesLoaded.Select(t => t.Table));
        Assert.Equal(2, ResidencyFixture.Rows(fresh, "Widget"));
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
    }

    [Fact]
    public void ColdStart_Skips_WhenRowsArePending_ButReplicationIsDisabled()
    {
        fx.WriteFeed(FeedV1);
        var families = new List<CosmosFamilyMapping>
        {
            new()
            {
                Family = "Widget", Database = "TestDb", Container = "Widgets",
                Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] } },
            },
        };
        var registry = fx.Registry(fx.FileSource(fx.FileOptions(), families: families, replicationEnabled: false));
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        // Pending recorded RAW even though the pump is off — that is what makes the later
        // flip-to-enabled safe: the restarted cold start sees this count and loads.
        Assert.Equal(2, fx.Entry("Widget").ReplicationPending);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public void ColdStart_Defers_WhenPendingIsZero_DespiteAFamily()
    {
        fx.WriteFeed(FeedV1);
        var families = new List<CosmosFamilyMapping>
        {
            new()
            {
                Family = "Widget", Database = "TestDb", Container = "Widgets",
                Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] } },
            },
        };
        var registry = fx.Registry(fx.FileSource(fx.FileOptions(), families: families));
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        foreach (var row in fx.Store.ReadDirtyRows(fx.Widget))
            fx.Store.MarkReplicated(fx.Widget, row.PrimaryKey, row.CapturedLastModified, "{\"id\":\"x\"}");
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(0, fx.Entry("Widget").ReplicationPending);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public void ColdStart_PinnedSource_StaysResident()
    {
        fx.WriteFeed(FeedV1);
        var registry = fx.Registry(fx.FileSource(fx.FileOptions(), pin: true));
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(2, ResidencyFixture.Rows(fresh, "Widget"));
    }

    [Fact]
    public void ColdStart_TableWithNoCommittedEntry_StaysResident_AndPublishesNormally()
    {
        // Publish an estate that does not know Widget yet, then cold start with Widget added:
        // no committed copy exists, so Widget must be Resident (L7) and its first publish must
        // EXPORT, never carry.
        var gadgetOnly = fx.Registry(fx.GadgetSource());
        fx.MergeGadgets(fx.Store, ("G1", "one"));
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, gadgetOnly).Status);

        fx.WriteFeed(FeedV1);
        var full = fx.Registry(fx.FileSource(fx.FileOptions()), fx.GadgetSource());
        using var fresh = fx.ColdStart(full, out var rebuild);

        Assert.Contains("Widget", rebuild.TablesCreatedEmpty);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));

        Assert.True(fx.Ingest(fresh).Succeeded);
        var publish = fx.Publish(fresh, full);
        Assert.Contains("Widget", publish.TablesExported);
        Assert.Equal(2, fx.Entry("Widget").RowCount);
    }

    /// <summary>
    /// Two committed sets: v1 (Widget 2 rows, Gadget 1 row), then v2 (Widget 3 rows,
    /// Gadget 2 rows) — the estate the fallback-seed shapes below damage in three ways. A
    /// deferral may only ever stand against the newest manifest BY NAME (the publisher's
    /// carry-forward baseline); deferring against whichever manifest happened to load would
    /// wedge every subsequent publish, with "restart the agent" rebuilding the same state.
    /// </summary>
    private SourceRegistry TwoPublishes()
    {
        fx.WriteFeed(FeedV1);
        var registry = fx.Registry(fx.FileSource(fx.FileOptions()), fx.GadgetSource());
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        fx.MergeGadgets(fx.Store, ("G1", "one"));
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        fx.WriteFeed(FeedV2);
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        fx.MergeGadgets(fx.Store, ("G1", "one"), ("G2", "two"));
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        return registry;
    }

    [Fact]
    public void ColdStart_UnreadableNewestManifest_LoadsResident_AndTheNextPublishSelfHeals()
    {
        var registry = TwoPublishes();

        // The newest manifest's JSON rots. Nobody can know what it said — and the publisher
        // resolves its baseline from that manifest by name — so nothing may stay deferred:
        // everything loads from the older seed.
        var corrupt = PublishedSnapshot.ResolveNewest(fx.PublishDirectory, ResidencyFixture.SnapshotName)!;
        File.WriteAllText(corrupt, "{ this is not a manifest");

        using var fresh = fx.ColdStart(registry, out var rebuild);

        Assert.Equal([PublishPath.FileName(corrupt)], rebuild.PublishesSkipped);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Contains("Widget", rebuild.TablesLoaded.Select(t => t.Table));
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(2, ResidencyFixture.Rows(fresh, "Widget")); // the older seed's copy

        // Resident rows restore the pre-residency self-heal: the publisher reads no baseline
        // out of the corrupt manifest, re-exports everything, and mints a manifest that
        // outranks the corrupt name — so the NEXT start is back to the good posture.
        var publish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, publish.Status);
        Assert.Contains("Widget", publish.TablesExported);

        using var healed = fx.ColdStart(registry, out var second);
        Assert.Empty(second.PublishesSkipped);
        Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public void ColdStart_OwnCopyTornInTheNewestManifest_LoadsResident_AndTheNextPublishReexports()
    {
        var registry = TwoPublishes();

        // Tear Widget's own parquet in the NEWEST manifest. Its copy of record is down to one
        // good older copy — deferring against that copy would leave the publisher throwing on
        // the torn baseline every cycle, and the recorded copy sweepable once its manifest
        // ages out. Loading resident lets the next publish mint a fresh export instead:
        // restoring redundancy is the point, not a consolation.
        var newest = fx.Entry("Widget").Resolve(fx.PublishDirectory).Single();
        File.WriteAllBytes(newest, [9, 9, 9]);

        using var fresh = fx.ColdStart(registry, out var rebuild);

        Assert.Single(rebuild.PublishesSkipped);
        Assert.Empty(rebuild.TablesDeferred);
        Assert.Contains("Widget", rebuild.TablesLoaded.Select(t => t.Table));
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(2, ResidencyFixture.Rows(fresh, "Widget")); // v1's two rows, not v2's three

        var publish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, publish.Status);
        Assert.Contains("Widget", publish.TablesExported);
        Assert.Equal(2, fx.Entry("Widget").RowCount);

        using var healed = fx.ColdStart(registry, out var second);
        Assert.Empty(second.PublishesSkipped);
        Assert.Equal(["Widget"], second.TablesDeferred.Select(t => t.Table));
    }

    [Fact]
    public void ColdStart_SiblingTear_DefersAgainstTheNewestManifestsOwnEntry_ZeroLoad()
    {
        var registry = TwoPublishes();
        var newestManifest = PublishedSnapshot.ResolveNewest(fx.PublishDirectory, ResidencyFixture.SnapshotName)!;

        // Tear the SIBLING's newest parquet only. The estate must seed from the older set,
        // but the newest manifest still supplies Widget's own entry intact — and that entry
        // is what the publisher will carry by name. Deferring against it keeps the zero-load
        // win, and record and baseline agree by construction.
        var gadgetNewest = fx.Entry("Gadget").Resolve(fx.PublishDirectory).Single();
        File.WriteAllBytes(gadgetNewest, [9, 9, 9]);

        using var fresh = fx.ColdStart(registry, out var rebuild);

        Assert.Single(rebuild.PublishesSkipped);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
        Assert.Equal(3, rebuild.TablesDeferred.Single().Rows); // v2's copy, not the seed's v1
        Assert.Equal(["Gadget"], rebuild.TablesLoaded.Select(t => t.Table));
        Assert.Equal(1, ResidencyFixture.Rows(fresh, "Gadget")); // the older intact sibling copy

        // The record IS the newest manifest's entry: name, resolved paths, row count.
        var record = fresh.ReadDeferredTableRecord("Widget")!;
        Assert.Equal(PublishPath.FileName(newestManifest), record.ManifestFile);
        Assert.Equal(fx.Entry("Widget").Resolve(fx.PublishDirectory), record.ParquetPaths);

        // The gate stamps came from the newest manifest too — the feed is unchanged since
        // the newest publish, so the deferral holds without even a content-hash read.
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, fx.Ingest(fresh).Status);

        // The next publish agrees by construction: Widget's entry carries, the torn sibling
        // re-exports, nothing throws.
        var publish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, publish.Status);
        Assert.Contains("Widget", publish.TablesReused);
        Assert.Contains("Gadget", publish.TablesExported);
        Assert.Equal(3, fx.Entry("Widget").RowCount);

        // And hydration reads the NEWEST paths: a real change merges against v2's three rows
        // (hydrating the seed's v1 copy would have to re-insert C).
        fx.WriteFeed("Code,Quantity\nA,1\nB,5\nC,4\n");
        var merge = fx.Ingest(fresh);
        Assert.True(merge.Succeeded);
        Assert.Equal(0, merge.RowsInserted);
        Assert.Equal(1, merge.RowsUpdated);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(3, ResidencyFixture.Rows(fresh, "Widget"));
    }

    [Fact]
    public void ColdStart_FromAnOlderSchemaSeed_LoadsResident_SoTheTransitionPublishCanReexport()
    {
        // Publish normally, then rewrite the manifest to claim the previous schema version —
        // a valid seed (the rebuild floor), and the exact shape every estate has on the day a
        // schema bump ships. Deferring from it would wedge the estate: the transition publish
        // refuses to reuse older-schema entries, must re-export everything, and a deferred
        // table can neither carry nor export.
        fx.WriteFeed(FeedV1);
        var registry = fx.Registry(fx.FileSource(fx.FileOptions()));
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        var manifestPath = Directory.GetFiles(fx.PublishDirectory, "resi-read-*.json").Single();
        File.WriteAllText(manifestPath, File.ReadAllText(manifestPath).Replace(
            $"\"schemaVersion\": {SnapshotStore.CurrentSchemaVersion}",
            $"\"schemaVersion\": {SnapshotStore.CurrentSchemaVersion - 1}"));

        using var fresh = fx.ColdStart(registry, out var rebuild);

        Assert.Empty(rebuild.TablesDeferred);
        Assert.Equal(2, ResidencyFixture.Rows(fresh, "Widget"));
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));

        // And the transition publish itself re-exports instead of throwing.
        var publish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, publish.Status);
        Assert.Contains("Widget", publish.TablesExported);
    }

    [Fact]
    public void WarmReopen_FindsADeferredTable_StillDeferred()
    {
        var directory = Path.Combine(Path.GetTempPath(), "hawta-residency", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var databasePath = Path.Combine(directory, "warm.duckdb");
        try
        {
            fx.WriteFeed(FeedV1);
            var registry = fx.Registry(fx.FileSource(fx.FileOptions()));
            Assert.True(fx.Ingest(fx.Store).Succeeded);
            Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

            using (var cold = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = databasePath }))
            {
                var rebuild = SnapshotRebuild.Execute(cold, registry, fx.PublishDirectory, ResidencyFixture.SnapshotName);
                Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
            }

            // The process restarts WITHOUT losing the disk: no rebuild runs, and the state
            // must come from meta — a row count would say nothing.
            using var warm = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = databasePath });
            warm.EnsureTable(fx.Widget);
            Assert.Equal(SnapshotResidency.Deferred, warm.ReadResidency("Widget"));
            Assert.NotNull(warm.ReadDeferredTableRecord("Widget"));
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    // ---- Hydration -------------------------------------------------------------------------

    [Fact]
    public void ChangedFile_OnADeferredTable_HydratesAndMerges_IdenticallyToResident()
    {
        // Resident reference run.
        fx.WriteFeed(FeedV1);
        var registry = fx.Registry(fx.FileSource(fx.FileOptions()));
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        fx.WriteFeed(FeedV2);
        var resident = fx.Ingest(fx.Store);
        Assert.True(resident.Succeeded);

        // Same scenario against a deferred estate.
        using var fx2 = new ResidencyFixture();
        fx2.WriteFeed(FeedV1);
        var registry2 = fx2.Registry(fx2.FileSource(fx2.FileOptions()));
        Assert.True(fx2.Ingest(fx2.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx2.Publish(fx2.Store, registry2).Status);
        using var fresh = fx2.ColdStart(registry2, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));

        fx2.WriteFeed(FeedV2);
        var deferred = fx2.Ingest(fresh);

        Assert.True(deferred.Succeeded);
        Assert.Equal(resident.RowsInserted, deferred.RowsInserted);
        Assert.Equal(resident.RowsUpdated, deferred.RowsUpdated);
        Assert.Equal(resident.RowsTombstoned, deferred.RowsTombstoned);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(3, ResidencyFixture.Rows(fresh, "Widget"));
    }

    [Fact]
    public void HydrationFailure_FailsTheRun_WritesNoStamp_LeavesTheTableDeferred_AndRetries()
    {
        using var fresh = DeferredEstate(out _, out _);
        var parquet = fx.Entry("Widget").Resolve(fx.PublishDirectory).Single();
        var keep = File.ReadAllBytes(parquet);
        var stampBefore = fresh.ExecuteScalar(
            "SELECT \"LastWriteUtcTicks\" FROM meta.SourceFileStamps WHERE \"SourceKey\" = 'file-widget'");

        File.WriteAllBytes(parquet, [0]); // torn copy
        fx.WriteFeed(FeedV2);

        Assert.ThrowsAny<Exception>(() => fx.Ingest(fresh));

        // No partial residency (the transaction rolled back), no stamp, a Failed run record —
        // and NO fallback to sources: the table still has zero resident rows.
        Assert.Equal(SnapshotResidency.Deferred, fresh.ReadResidency("Widget"));
        Assert.Equal(0, ResidencyFixture.Rows(fresh, "Widget"));
        Assert.Equal(stampBefore, fresh.ExecuteScalar(
            "SELECT \"LastWriteUtcTicks\" FROM meta.SourceFileStamps WHERE \"SourceKey\" = 'file-widget'"));
        Assert.Equal(1L, Convert.ToInt64(fresh.ExecuteScalar(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Failed:Exception'")));

        // The tier recovers → the next tick's retry succeeds.
        File.WriteAllBytes(parquet, keep);
        var retried = fx.Ingest(fresh);
        Assert.True(retried.Succeeded);
        Assert.Equal(3, ResidencyFixture.Rows(fresh, "Widget"));
    }

    [Fact]
    public void MergeReachedWithNoGateVerdict_StillHydratesFirst()
    {
        using var fresh = DeferredEstate(out _, out _);

        // The harness shape: an ingest with NO metadata probe produces no gate verdict at all.
        fx.WriteFeed(FeedV2);
        var merge = FileSnapshotIngestor.Ingest(fresh, fx.FileOptions(), fileMetadata: null);

        Assert.True(merge.Succeeded);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(1, merge.RowsInserted);
        Assert.Equal(1, merge.RowsUpdated);
        Assert.Equal(3, ResidencyFixture.Rows(fresh, "Widget"));
    }

    // ---- The content-identity guard ---------------------------------------------------------

    [Fact]
    public void IdenticalRedelivery_SkipsHydration_StampsTheGate_AndStaysDeferred()
    {
        using var fresh = DeferredEstate(out _, out _);

        fx.RedeliverIdentical();
        var guarded = fx.Ingest(fresh);

        Assert.Equal(SnapshotMergeStatus.SkippedContentUnchanged, guarded.Status);
        Assert.Equal(2, guarded.RowsStaged);
        Assert.Equal(SnapshotResidency.Deferred, fresh.ReadResidency("Widget"));
        Assert.Equal(0, ResidencyFixture.Rows(fresh, "Widget"));
        Assert.Equal(1L, Convert.ToInt64(fresh.ExecuteScalar(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Skipped:ContentUnchanged'")));

        // The stamp was refreshed, so the next tick skips at the metadata level again.
        var next = fx.Ingest(fresh);
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, next.Status);
    }

    [Fact]
    public void DifferingContent_Hydrates_TheGuardDoesNotMaskARealChange()
    {
        using var fresh = DeferredEstate(out _, out _);

        fx.WriteFeed(FeedV2);
        var merge = fx.Ingest(fresh);

        Assert.True(merge.Succeeded);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(3, ResidencyFixture.Rows(fresh, "Widget"));
    }

    [Fact]
    public void TheGuard_NeverEngages_OnAConfigurationChange()
    {
        using var fresh = DeferredEstate(out _, out _);

        // Identical content, but the operator bumped the ingest version: the verdict is
        // ConfigurationChanged and identical content under the OLD rules proves nothing —
        // the table must hydrate and merge.
        fx.RedeliverIdentical();
        var merge = fx.Ingest(fresh, fx.FileOptions(ingestVersion: "force-1"));

        Assert.Equal(SnapshotMergeStatus.Succeeded, merge.Status);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(0, merge.RowsUpdated); // and the merge proved the content really was identical
    }

    [Fact]
    public void TheGuard_NeverEngages_WhenTheFeedMoved()
    {
        using var fresh = DeferredEstate(out _, out _);

        // Same bytes at a different path: not a FileChanged verdict, so no guard — hydrate.
        var movedPath = Path.Combine(fx.FeedDirectory, "widgets-moved.csv");
        File.Copy(fx.FeedPath, movedPath);
        var merge = fx.Ingest(fresh, fx.FileOptions(path: movedPath));

        Assert.Equal(SnapshotMergeStatus.Succeeded, merge.Status);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
    }

    [Fact]
    public void TheGuard_NeverEngages_OnTrustExpiry()
    {
        var clock = new ManualClock();
        var gate = new SourceChangeGate(reingestAfter: null, clock);

        fx.WriteFeed(FeedV1);
        var options = fx.FileOptions(gate: gate, reingestAfter: TimeSpan.FromHours(6));
        var registry = fx.Registry(fx.FileSource(options));
        Assert.True(fx.Ingest(fx.Store, options).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));

        // Nothing about the file changed at all — but the stamp aged out, and TrustExpired
        // exists precisely because metadata identity is not content identity. No guard.
        clock.Advance(TimeSpan.FromHours(7));
        var merge = fx.Ingest(fresh, options);

        Assert.Equal(SnapshotMergeStatus.Succeeded, merge.Status);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
    }

    [Fact]
    public void TheGuard_IsPerScope_OneScopesIdenticalRedelivery_CannotMaskAnothersChange()
    {
        var pathA = Path.Combine(fx.FeedDirectory, "region-a.csv");
        var pathB = Path.Combine(fx.FeedDirectory, "region-b.csv");
        fx.WriteFeed("Code,Quantity\nA1,1\nA2,2\n", pathA);
        fx.WriteFeed("Code,Quantity\nB1,1\n", pathB);
        var optionsA = fx.FileOptions(sourceKey: "file-a", scope: "region-a", path: pathA);
        var optionsB = fx.FileOptions(sourceKey: "file-b", scope: "region-b", path: pathB);
        var registry = fx.Registry(fx.FileSource(optionsA), fx.FileSource(optionsB));

        Assert.True(fx.Ingest(fx.Store, optionsA).Succeeded);
        Assert.True(fx.Ingest(fx.Store, optionsB).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));

        // Scope A re-delivers identical content: guarded, still deferred.
        fx.RedeliverIdentical(pathA);
        var guarded = fx.Ingest(fresh, optionsA);
        Assert.Equal(SnapshotMergeStatus.SkippedContentUnchanged, guarded.Status);
        Assert.Equal(SnapshotResidency.Deferred, fresh.ReadResidency("Widget"));

        // Scope B genuinely changed: its own hash differs, so it hydrates and merges — A's
        // skip masked nothing.
        fx.WriteFeed("Code,Quantity\nB1,9\nB2,2\n", pathB);
        var merged = fx.Ingest(fresh, optionsB);
        Assert.True(merged.Succeeded);
        Assert.Equal(1, merged.RowsInserted);
        Assert.Equal(1, merged.RowsUpdated);
        Assert.Equal(SnapshotResidency.Resident, fresh.ReadResidency("Widget"));
        Assert.Equal(4, ResidencyFixture.Rows(fresh, "Widget"));
    }

    [Fact]
    public void OnAResidentTable_AnIdenticalRedelivery_MergesToZeroChanges_NoGuardInvolved()
    {
        fx.WriteFeed(FeedV1);
        Assert.True(fx.Ingest(fx.Store).Succeeded);

        fx.RedeliverIdentical();
        var merge = fx.Ingest(fx.Store);

        // Resident tables never see the guard: the merge itself is the cheap no-op.
        Assert.Equal(SnapshotMergeStatus.Succeeded, merge.Status);
        Assert.Equal(0, merge.RowsUpdated);
        Assert.Equal(0, merge.RowsInserted);
    }

    // ---- The manifest fields -----------------------------------------------------------------

    [Fact]
    public void ReplicationPending_IsRecordedRaw_AndDrainProgressForcesARecount()
    {
        fx.WriteFeed(FeedV1);
        var families = new List<CosmosFamilyMapping>
        {
            new()
            {
                Family = "Widget", Database = "TestDb", Container = "Widgets",
                Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] } },
            },
        };
        var registry = fx.Registry(fx.FileSource(fx.FileOptions(), families: families));
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(2, fx.Entry("Widget").ReplicationPending);

        // Drain progress changes bookkeeping → the signature changes → the next publish MUST
        // re-export and recompute the count. This is why the field can never go stale high.
        var row = fx.Store.ReadDirtyRows(fx.Widget).First();
        fx.Store.MarkReplicated(fx.Widget, row.PrimaryKey, row.CapturedLastModified, "{\"id\":\"x\"}");
        var second = fx.Publish(fx.Store, registry);
        Assert.Equal(SnapshotPublishStatus.Published, second.Status);
        Assert.Contains("Widget", second.TablesExported);
        Assert.Equal(1, fx.Entry("Widget").ReplicationPending);
    }

    [Fact]
    public void ReplicationPending_IsNull_ForATableWithNoCosmosFamily()
    {
        fx.WriteFeed(FeedV1);
        var registry = fx.Registry(fx.FileSource(fx.FileOptions()));
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);

        Assert.Null(fx.Entry("Widget").ReplicationPending);
        Assert.NotNull(fx.Entry("Widget").SourceCatalog.Single().ContentHash);
    }

    [Fact]
    public void ANonzeroReplicationPending_SurvivesDeferralAndAManifestRewrite_ByteForByte()
    {
        // The exact entry the flip restart depends on: a table deferred with a recorded
        // backlog (pump off, so pending is raw and nonzero). A manifest rewrite triggered by
        // a sibling must carry the count untouched — a zeroed or dropped field here would
        // wrongly defer the whole backlog at the flip restart.
        fx.WriteFeed(FeedV1);
        var families = new List<CosmosFamilyMapping>
        {
            new()
            {
                Family = "Widget", Database = "TestDb", Container = "Widgets",
                Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] } },
            },
        };
        var registry = fx.Registry(
            fx.FileSource(fx.FileOptions(), families: families, replicationEnabled: false),
            fx.GadgetSource());
        Assert.True(fx.Ingest(fx.Store).Succeeded);
        fx.MergeGadgets(fx.Store, ("G1", "one"));
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish(fx.Store, registry).Status);
        Assert.Equal(2, fx.Entry("Widget").ReplicationPending);

        using var fresh = fx.ColdStart(registry, out var rebuild);
        Assert.Equal(["Widget"], rebuild.TablesDeferred.Select(t => t.Table));
        var before = fx.Entry("Widget");

        // The residency record carries the count too — it is what the loop's flip warning
        // reads on a warm reopen, when no manifest is being consulted.
        Assert.Equal(2, fresh.ReadDeferredTableRecord("Widget")!.ReplicationPending);

        // A sibling change forces a NEW manifest; the deferred entry rides into it verbatim.
        fx.MergeGadgets(fresh, ("G1", "one"), ("G2", "two"));
        var publish = fx.Publish(fresh, registry);
        Assert.Equal(SnapshotPublishStatus.Published, publish.Status);
        Assert.Equal(["Gadget"], publish.TablesExported);

        var after = fx.Entry("Widget");
        Assert.Equal(2, after.ReplicationPending);
        Assert.Equal(
            JsonSerializer.Serialize(before, PublishedSnapshot.SerializerOptions),
            JsonSerializer.Serialize(after, PublishedSnapshot.SerializerOptions));
    }

    // ---- Callers that must refuse loudly ------------------------------------------------------

    [Fact]
    public async Task Recon_RefusesADeferredTable_WithTheRealReason_InsteadOfAllOrphans()
    {
        fx.Store.MarkTableDeferred("Widget", "m.json", ["x.parquet"], 2, []);

        var recon = new SnapshotRecon(fx.Store, cosmosClient: null);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => recon.RunAsync(
            new SnapshotReconOptions
            {
                Tables = [fx.Widget],
                Families =
                [
                    new SnapshotReconFamily
                    {
                        Mapping = new CosmosFamilyMapping
                        {
                            Family = "Widget", Database = "TestDb", Container = "Widgets",
                            Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] } },
                        },
                        EnumerationSql = "SELECT * FROM c",
                        PartitionKeyPaths = ["/id"],
                    },
                ],
            },
            (_, _) => AsyncEnumerable.Empty<System.Text.Json.Nodes.JsonObject>(),
            CancellationToken.None));

        Assert.Contains("Deferred", exception.Message);
    }

    [Fact]
    public async Task DryRunPump_RefusesADeferredTable_InsteadOfReadingVacuouslyClean()
    {
        // A deferred table's dirty queue reads as empty because the rows are not resident —
        // a dry-run that emitted zero intended ops would be read as "all settled" while the
        // manifest records a backlog. It must refuse with the real reason, like recon does,
        // and leave any previous run's recon evidence alone.
        using var snapshot = new TestSnapshot();
        snapshot.Merge([("K1", "alpha", 1), ("K2", "beta", 2)]); // a real backlog exists…
        snapshot.DeferCurrentRows();                             // …and then the table defers

        var replicator = new CosmosSnapshotReplicator(snapshot.Store, cosmosClient: null);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => replicator.RunOnceAsync(
            new CosmosSnapshotReplicatorOptions
            {
                Table = snapshot.Table,
                Families =
                [
                    new CosmosFamilyMapping
                    {
                        Family = "Widget", Database = "TestDb", Container = "Widgets",
                        Map = row => new CosmosDocument { Id = row.PrimaryKey, PartitionKey = [row.PrimaryKey], Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] } },
                    },
                ],
                DryRun = true,
            },
            TestContext.Current.CancellationToken));

        Assert.Contains("Deferred", exception.Message);
        Assert.Equal(0L, Convert.ToInt64(snapshot.Store.ExecuteScalar(
            "SELECT count(*) FROM meta.ReconOps"))); // nothing pruned, nothing emitted
    }

    // ---- Qualification ------------------------------------------------------------------------

    [Fact]
    public void Qualification_IsComputedFromTheDeclaredShape()
    {
        fx.WriteFeed(FeedV1);
        var gated = fx.FileOptions();

        // Gate-wired file source → capable.
        Assert.True(fx.Registry(fx.FileSource(gated)).IsTableDeferredCapable("Widget"));

        // A pin on the source → not capable.
        Assert.False(fx.Registry(fx.FileSource(gated, pin: true)).IsTableDeferredCapable("Widget"));

        // A file source WITHOUT a gate cannot answer "unchanged" without reading → not capable.
        var ungated = new FileSnapshotIngestorOptions
        {
            Table = fx.Widget,
            FilePath = fx.FeedPath,
            LogicalKey = FileLogicalKey.Single("Code"),
            MergeOptions = new SnapshotMergeOptions { Source = "file-widget", DeletesEnabled = true },
        };
        Assert.False(fx.Registry(fx.FileSource(ungated)).IsTableDeferredCapable("Widget"));

        // A non-file source → not capable (its ingestor merges every tick).
        Assert.False(fx.Registry(fx.GadgetSource()).IsTableDeferredCapable("Gadget"));

        // Two sources, one table: all must qualify; one pin resolves the whole table resident.
        var optionsA = fx.FileOptions(sourceKey: "file-a", scope: "region-a");
        var optionsB = fx.FileOptions(sourceKey: "file-b", scope: "region-b");
        Assert.True(fx.Registry(fx.FileSource(optionsA), fx.FileSource(optionsB))
            .IsTableDeferredCapable("Widget"));
        Assert.False(fx.Registry(fx.FileSource(optionsA), fx.FileSource(optionsB, pin: true))
            .IsTableDeferredCapable("Widget"));
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan by) => now += by;
    }

    public void Dispose() => fx.Dispose();
}
