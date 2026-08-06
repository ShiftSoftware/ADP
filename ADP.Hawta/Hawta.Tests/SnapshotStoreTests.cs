using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public class SnapshotStoreTests
{
    [Fact]
    public void Open_CreatesSchemas_Sentinel_AndSyncRuns()
    {
        using var store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });

        Assert.Equal(SnapshotStore.CurrentSchemaVersion,
            Convert.ToInt32(store.ExecuteScalar("SELECT \"SchemaVersion\" FROM meta.schema_info")));
        Assert.Equal(0L, Convert.ToInt64(store.ExecuteScalar("SELECT count(*) FROM meta.SyncRuns")));
        store.Execute("CREATE TABLE src.probe (x INTEGER)");
    }

    [Fact]
    public void Open_OnAnOlderSchema_Throws_WithRebuildGuidance()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hawta-test-{Guid.NewGuid():N}.duckdb");
        try
        {
            using (SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = path, SchemaVersion = 1 }))
            {
            }

            var mismatch = Assert.Throws<SnapshotSchemaMismatchException>(() =>
                SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = path, SchemaVersion = 2 }));

            Assert.Equal(2, mismatch.Expected);
            Assert.Equal(1, mismatch.Actual);
            Assert.Contains("Rebuild", mismatch.Message);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EnsureTable_IsIdempotent_AndAddsBookkeepingColumns()
    {
        using var snapshot = new TestSnapshot();

        snapshot.Store.EnsureTable(snapshot.Table);

        foreach (var column in BookkeepingColumns.All)
        {
            var count = Convert.ToInt64(snapshot.Store.ExecuteScalar(
                """
                SELECT count(*) FROM information_schema.columns
                WHERE table_schema = 'data' AND table_name = 'Widget' AND column_name = ?
                """,
                column));
            Assert.Equal(1L, count);
        }
    }

    [Fact]
    public void EnsureTable_AddsNewSourceColumns_ToAnExistingTable()
    {
        using var snapshot = new TestSnapshot();

        var widened = new SnapshotTableDefinition("Widget",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
            new SnapshotColumn("Colour", "VARCHAR"),
        ]);

        snapshot.Store.EnsureTable(widened);

        var count = Convert.ToInt64(snapshot.Store.ExecuteScalar(
            """
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema = 'data' AND table_name = 'Widget' AND column_name = 'Colour'
            """));
        Assert.Equal(1L, count);
    }

    [Fact]
    public void TableDefinition_RejectsUnsafeIdentifiers()
    {
        Assert.Throws<ArgumentException>(() =>
            new SnapshotTableDefinition("bad name; DROP TABLE x", [new SnapshotColumn("A", "VARCHAR")]));
        Assert.Throws<ArgumentException>(() =>
            new SnapshotTableDefinition("Good", [new SnapshotColumn("_PrimaryKey", "VARCHAR")]));
        Assert.Throws<ArgumentException>(() =>
            new SnapshotTableDefinition("Good", [new SnapshotColumn("A", "VARCHAR); DROP TABLE x; --")]));
        Assert.Throws<ArgumentException>(() =>
            new SnapshotTableDefinition("Good", [new SnapshotColumn("A", "VARCHAR"), new SnapshotColumn("a", "VARCHAR")]));
    }
}
