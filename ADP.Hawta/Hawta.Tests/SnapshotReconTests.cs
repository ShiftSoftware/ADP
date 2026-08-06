using System.Text.Json.Nodes;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Recon classification over the full intended state, via the test seam (caller-supplied
/// "actual" documents instead of a live Cosmos). Every verdict class gets a concrete row.
/// </summary>
public class SnapshotReconTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    public void Dispose() => snapshot.Dispose();

    private static CosmosDocument MapDocument(DirtyRow row) => new()
    {
        Id = row.PrimaryKey,
        PartitionKey = [(string)row.Values["Code"]!, "Widget", 7],
        Body = new Dictionary<string, object?>
        {
            ["Code"] = row.Values["Code"],
            ["Quantity"] = row.Values["Quantity"],
            ["ItemType"] = "Widget",
            ["CompanyID"] = 7,
        },
    };

    private static SnapshotReconFamily Family() => new()
    {
        Mapping = new CosmosFamilyMapping
        {
            Family = "Widget",
            Database = "TestDb",
            Container = "Widgets",
            Map = MapDocument,
        },
        EnumerationSql = "SELECT * FROM c WHERE c.ItemType = 'Widget'",
        PartitionKeyPaths = ["/Code", "/ItemType", "/CompanyID"],
    };

    /// <summary>An actual document exactly as the pump would have written the given row.</summary>
    private static JsonObject ActualDocFor(string key, string code, int quantity) => new()
    {
        ["id"] = key,
        ["Code"] = code,
        ["Quantity"] = quantity,
        ["ItemType"] = "Widget",
        ["CompanyID"] = 7,
        ["_rid"] = "system-noise",       // recon must strip Cosmos system properties
        ["_ts"] = 1754000000,
    };

    private void MarkCleanAt(string key)
    {
        var row = snapshot.Store.ReadRows(snapshot.Table, null, 1000).Single(r => r.PrimaryKey == key);
        var stamp = new ReplicationStamp();
        if (!row.Deleted)
            stamp.Families["Widget"] = ReplicationStamp.ToCoordinates(MapDocument(row.AsDirtyRow()));
        snapshot.Store.MarkReplicated(snapshot.Table, key, row.CapturedLastModified, stamp.ToJson());
    }

    private void MarkCleanWithStampOnly(string key, CosmosDocument stampedAs)
    {
        var row = snapshot.Store.ReadRows(snapshot.Table, null, 1000).Single(r => r.PrimaryKey == key);
        var stamp = new ReplicationStamp();
        stamp.Families["Widget"] = ReplicationStamp.ToCoordinates(stampedAs);
        snapshot.Store.MarkReplicated(snapshot.Table, key, row.CapturedLastModified, stamp.ToJson());
    }

    [Fact]
    public async Task EveryVerdictClass_LandsWhereItBelongs()
    {
        // Snapshot state:
        //   SYNCED   clean, actual matches                     → InSync
        //   NEWROW   dirty, no actual                          → PendingAdd
        //   CHANGED  dirty (edited after replication), stale actual → PendingUpdate
        //   MISSING  clean, no actual                          → DivergentMissing
        //   DRIFTED  clean, actual differs                     → DivergentContent
        //   TOMBDIRTY  dirty tombstone, actual still there     → PendingDelete
        //   TOMBCLEAN  clean tombstone, actual still there     → DivergentGhost
        //   (actual-only ORPHAN doc)                           → Orphans
        snapshot.Merge(
        [
            ("SYNCED", "alpha", 1),
            ("NEWROW", "beta", 2),
            ("CHANGED", "gamma", 3),
            ("MISSING", "delta", 4),
            ("DRIFTED", "epsilon", 5),
            ("TOMBDIRTY", "zeta", 6),
            ("TOMBCLEAN", "eta", 7),
        ]);

        foreach (var key in new[] { "SYNCED", "MISSING", "DRIFTED" })
            MarkCleanAt(key);

        // CHANGED: replicate at quantity 3, then edit to 33 → dirty again, actual stays at 3.
        MarkCleanAt("CHANGED");
        snapshot.Merge(
        [
            ("SYNCED", "alpha", 1),
            ("NEWROW", "beta", 2),
            ("CHANGED", "gamma", 33),
            ("MISSING", "delta", 4),
            ("DRIFTED", "epsilon", 5),
            ("TOMBDIRTY", "zeta", 6),
            ("TOMBCLEAN", "eta", 7),
        ]);

        // Tombstone rows: stamp them as replicated first so the tombstones have coordinates.
        MarkCleanAt("TOMBDIRTY");
        MarkCleanAt("TOMBCLEAN");
        snapshot.Merge(
        [
            ("SYNCED", "alpha", 1),
            ("NEWROW", "beta", 2),
            ("CHANGED", "gamma", 33),
            ("MISSING", "delta", 4),
            ("DRIFTED", "epsilon", 5),
        ], force: true);

        // TOMBCLEAN's tombstone already replicated (empty stamp, watermark current) — but the
        // document is still in "Cosmos": the ghost.
        MarkCleanAt("TOMBCLEAN");

        var actualDocuments = new List<JsonObject>
        {
            ActualDocFor("SYNCED", "alpha", 1),
            ActualDocFor("CHANGED", "gamma", 3),      // stale content, row dirty → PendingUpdate
            ActualDocFor("DRIFTED", "epsilon", 55),   // wrong content, row clean → DivergentContent
            ActualDocFor("TOMBDIRTY", "zeta", 6),     // pending delete
            ActualDocFor("TOMBCLEAN", "eta", 7),      // ghost
            ActualDocFor("ORPHAN", "omega", 9),       // nothing accounts for it
        };

        var recon = new SnapshotRecon(snapshot.Store, cosmosClient: null);
        var result = await recon.RunAsync(
            new SnapshotReconOptions { Tables = [snapshot.Table], Families = [Family()] },
            (_, _) => actualDocuments.ToAsyncEnumerable(),
            TestContext.Current.CancellationToken);

        var family = Assert.Single(result.Families);
        Assert.Equal(5, family.ExpectedDocs);    // live rows
        Assert.Equal(6, family.ActualDocs);
        Assert.Equal(1, family.InSync);
        Assert.Equal(1, family.PendingAdd);
        Assert.Equal(1, family.PendingUpdate);
        Assert.Equal(1, family.PendingDelete);
        Assert.Equal(1, family.DivergentMissing);
        Assert.Equal(1, family.DivergentContent);
        Assert.Equal(1, family.DivergentGhost);
        Assert.Equal(1, family.Orphans);
        Assert.Equal(0, family.DuplicateExpectedCoordinates);
        Assert.Equal(3, family.Divergent);

        Assert.Contains(result.Samples, s => s.Kind == "DivergentMissing" && s.CosmosId == "MISSING");
        Assert.Contains(result.Samples, s => s.Kind == "DivergentContent" && s.CosmosId == "DRIFTED");
        Assert.Contains(result.Samples, s => s.Kind == "DivergentGhost" && s.CosmosId == "TOMBCLEAN");
        Assert.Contains(result.Samples, s => s.Kind == "Orphan" && s.CosmosId == "ORPHAN");

        Assert.Equal(7, result.RowsScanned);
        Assert.Equal(2, result.TombstonedRows);
    }

    [Fact]
    public async Task APendingCoordinateMove_TracksTheOldDocument_InsteadOfCallingItAnOrphan()
    {
        snapshot.Merge([("MOVER", "old-code", 1)]);

        // Replicated under OLD coordinates (PK level 1 = "old-code")…
        MarkCleanAt("MOVER");

        // …then the row changes so the mapping now produces different coordinates.
        snapshot.Merge([("MOVER", "new-code", 1)]);

        var actualDocuments = new List<JsonObject> { ActualDocFor("MOVER", "old-code", 1) };

        var recon = new SnapshotRecon(snapshot.Store, cosmosClient: null);
        var result = await recon.RunAsync(
            new SnapshotReconOptions { Tables = [snapshot.Table], Families = [Family()] },
            (_, _) => actualDocuments.ToAsyncEnumerable(),
            TestContext.Current.CancellationToken);

        var family = Assert.Single(result.Families);
        Assert.Equal(0, family.Orphans);          // the old doc is a tracked pending delete…
        Assert.Equal(1, family.PendingDelete);
        Assert.Equal(1, family.PendingAdd);       // …and the new coordinates are a pending add.
    }

    [Fact]
    public async Task ASharedDocumentSpace_SeparatesASettledHandoverFromAnImpendingDestructiveDelete()
    {
        // Two tables, one document space (the JPM + NonJPM shape): table B's live row and
        // table A's tombstone can name the SAME coordinates. What that means depends on
        // whether A's delete has already happened.
        var tableA = new SnapshotTableDefinition("FeedA",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Quantity", "INTEGER")]);
        var tableB = new SnapshotTableDefinition("FeedB",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Quantity", "INTEGER")]);
        snapshot.Store.EnsureTable(tableA);
        snapshot.Store.EnsureTable(tableB);

        void Stage(SnapshotTableDefinition table, string key, string code, int quantity)
        {
            var staging = snapshot.Store.CreateStagingTable(table);
            snapshot.Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}, NULL
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                key, code, quantity);
            SnapshotMerge.Execute(snapshot.Store, table, staging,
                new SnapshotMergeOptions { Source = table.Name, DeletesEnabled = true });
        }

        SnapshotReconFamily SharedFamily(SnapshotTableDefinition table) => new()
        {
            Mapping = new CosmosFamilyMapping
            {
                Family = "Shared",
                Database = "TestDb",
                Container = "Shared",
                Map = row => new CosmosDocument
                {
                    Id = (string)row.Values["Code"]!,
                    PartitionKey = [(string)row.Values["Code"]!],
                    Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] },
                },
            },
            EnumerationSql = "SELECT * FROM c",
            PartitionKeyPaths = ["/Code"],
            SourceTable = table.Name,
        };

        // Both feeds carried part P; A replicated it, then P left feed A.
        Stage(tableA, "P", "shared-part", 1);
        Stage(tableB, "P", "shared-part", 1);

        var rowA = snapshot.Store.ReadRows(tableA, null, 10).Single();
        var stampA = new ReplicationStamp();
        stampA.Families["Shared"] = ReplicationStamp.ToCoordinates(SharedFamily(tableA).Mapping.Map(rowA.AsDirtyRow()));
        snapshot.Store.MarkReplicated(tableA, "P", rowA.CapturedLastModified, stampA.ToJson());

        SnapshotMerge.Execute(snapshot.Store, tableA, snapshot.Store.CreateStagingTable(tableA),
            new SnapshotMergeOptions { Source = "FeedA", DeletesEnabled = true, ForceDeletes = true });

        var actual = new List<JsonObject>
        {
            new() { ["id"] = "shared-part", ["Code"] = "shared-part" },
        };

        var recon = new SnapshotRecon(snapshot.Store, cosmosClient: null);
        var options = new SnapshotReconOptions
        {
            Tables = [tableA, tableB],
            Families = [SharedFamily(tableA), SharedFamily(tableB)],
        };

        // A's delete is still QUEUED while B's live row owns the document: when the pump runs
        // it destroys a document B will never re-create (B is clean). That is the alarm — and
        // suppressing it as "another table expects these coordinates" would hide it.
        var pending = (await recon.RunAsync(options, (_, _) => actual.ToAsyncEnumerable(), TestContext.Current.CancellationToken))
            .Families.Single();
        Assert.Equal(1, pending.ContendedDelete);
        Assert.Equal(0, pending.PendingDelete);
        Assert.Equal(0, pending.Orphans);
        Assert.True(pending.Divergent >= 1);

        // Once A's delete has SETTLED and B's document is established, the end state is
        // correct — not a ghost, not a contended delete.
        snapshot.Store.MarkReplicated(tableA, "P",
            snapshot.Store.ReadRows(tableA, null, 10).Single().CapturedLastModified,
            new ReplicationStamp().ToJson());
        var rowB = snapshot.Store.ReadRows(tableB, null, 10).Single();
        var stampB = new ReplicationStamp();
        stampB.Families["Shared"] = ReplicationStamp.ToCoordinates(SharedFamily(tableB).Mapping.Map(rowB.AsDirtyRow()));
        snapshot.Store.MarkReplicated(tableB, "P", rowB.CapturedLastModified, stampB.ToJson());

        var settled = (await recon.RunAsync(options, (_, _) => actual.ToAsyncEnumerable(), TestContext.Current.CancellationToken))
            .Families.Single();
        Assert.Equal(0, settled.ContendedDelete);
        Assert.Equal(0, settled.DivergentGhost);
        Assert.Equal(0, settled.Divergent);
        Assert.Equal(1, settled.InSync);
    }

    [Fact]
    public async Task TwoRowsMappingToOneDocument_AreReportedAsDuplicateCoordinates()
    {
        var duplicateFamily = new SnapshotReconFamily
        {
            Mapping = new CosmosFamilyMapping
            {
                Family = "Widget",
                Database = "TestDb",
                Container = "Widgets",
                // Deliberately collision-prone: id = Code, and two rows share a Code.
                Map = row => new CosmosDocument
                {
                    Id = (string)row.Values["Code"]!,
                    PartitionKey = [(string)row.Values["Code"]!, "Widget", 7],
                    Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] },
                },
            },
            EnumerationSql = "SELECT * FROM c",
            PartitionKeyPaths = ["/Code", "/ItemType", "/CompanyID"],
        };

        snapshot.Merge([("R1", "same", 1), ("R2", "same", 2)]);

        var recon = new SnapshotRecon(snapshot.Store, cosmosClient: null);
        var result = await recon.RunAsync(
            new SnapshotReconOptions { Tables = [snapshot.Table], Families = [duplicateFamily] },
            (_, _) => new List<JsonObject>().ToAsyncEnumerable(),
            TestContext.Current.CancellationToken);

        var family = Assert.Single(result.Families);
        Assert.Equal(1, family.DuplicateExpectedCoordinates);
        Assert.Contains(result.Samples, s => s.Kind == "DuplicateExpectedCoordinates" && s.CosmosId == "same");
    }
}

file static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return item;
        await Task.CompletedTask;
    }
}
