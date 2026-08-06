namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Shared fixture: an in-memory snapshot store with one two-column table, plus helpers to
/// stage rows and run merges the way an ingestor would.
/// </summary>
public sealed class TestSnapshot : IDisposable
{
    public SnapshotStore Store { get; }
    public SnapshotTableDefinition Table { get; }

    public TestSnapshot()
    {
        Store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        Table = new SnapshotTableDefinition("Widget",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
        ]);
        Store.EnsureTable(Table);
    }

    /// <summary>Stages rows (key, code, quantity) with the uniform row-hash recipe, then merges.</summary>
    public SnapshotMergeResult Merge(
        IEnumerable<(string Key, string Code, int Quantity)> rows,
        bool deletesEnabled = true,
        string? scope = null,
        bool force = false,
        DateTime? sourceModified = null,
        double maxDeletedPercent = 0.20,
        int minDeletedRowsAbsolute = 50)
    {
        var staging = Stage(rows, sourceModified);

        return SnapshotMerge.Execute(Store, Table, staging, new SnapshotMergeOptions
        {
            Source = "test-source",
            SourceScope = scope,
            DeletesEnabled = deletesEnabled,
            ForceDeletes = force,
            MaxDeletedPercent = maxDeletedPercent,
            MinDeletedRowsAbsolute = minDeletedRowsAbsolute,
        });
    }

    /// <summary>Stages rows without merging (for tests that drive the merge/replicator directly).</summary>
    public StagingTable Stage(
        IEnumerable<(string Key, string Code, int Quantity)> rows,
        DateTime? sourceModified = null)
    {
        var staging = Store.CreateStagingTable(Table);

        foreach (var row in rows)
        {
            Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}, ?
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                row.Key, sourceModified, row.Code, row.Quantity);
        }

        return staging;
    }

    public T Scalar<T>(string sql, params object?[] parameters) =>
        (T)Convert.ChangeType(Store.ExecuteScalar(sql, parameters)!, typeof(T));

    public object? ScalarOrNull(string sql, params object?[] parameters)
    {
        var value = Store.ExecuteScalar(sql, parameters);
        return value is DBNull ? null : value;
    }

    public void Dispose() => Store.Dispose();
}
