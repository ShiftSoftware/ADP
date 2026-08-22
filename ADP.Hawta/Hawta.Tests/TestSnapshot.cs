namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Shared fixture: an in-memory snapshot store with one two-column table, plus helpers to
/// stage rows and run merges the way an ingestor would.
/// </summary>
public sealed class TestSnapshot : IDisposable
{
    public SnapshotStore Store { get; }
    public SnapshotTableDefinition Table { get; }

    /// <summary>
    /// When true, every <see cref="Stage"/> call first pushes the table's current rows out to
    /// a parquet file and marks the table Deferred — so each merge in an existing test
    /// exercises the full defer → hydrate → merge path and must land on identical results.
    /// This is how the incumbent merge suite runs against both residency states.
    /// </summary>
    public bool DeferBeforeEachStage { get; init; }

    private readonly string deferDirectory;
    private int deferSequence;

    public TestSnapshot()
    {
        Store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        Table = new SnapshotTableDefinition("Widget",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
        ]);
        Store.EnsureTable(Table);
        deferDirectory = Path.Combine(Path.GetTempPath(), "hawta-defer", Guid.NewGuid().ToString("N"));
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
        if (DeferBeforeEachStage && Store.ReadResidency(Table.Name) == SnapshotResidency.Resident)
            DeferCurrentRows();

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

    /// <summary>
    /// Simulates the cold-start skip against the table's CURRENT contents: every row (live and
    /// tombstoned — the published copy is a full copy) goes out to a parquet file, the resident
    /// rows are deleted, and the table is marked Deferred pointing at that file. The next merge
    /// hydrates it back. An empty table defers too — a published entry with zero rows is
    /// legitimate, which is exactly why residency is a recorded state and not a row count.
    /// </summary>
    public void DeferCurrentRows()
    {
        Directory.CreateDirectory(deferDirectory);
        var file = Path.Combine(deferDirectory, $"defer-{deferSequence++}.parquet").Replace('\\', '/');
        Store.Execute(
            $"""
            COPY (SELECT * FROM {Table.QualifiedName} ORDER BY "_PrimaryKey")
            TO '{file.Replace("'", "''")}' (FORMAT parquet)
            """);
        var rows = Scalar<long>($"SELECT count(*) FROM {Table.QualifiedName}");
        Store.Execute($"DELETE FROM {Table.QualifiedName}");
        Store.MarkTableDeferred(Table.Name, "test-defer.json", [file], rows, contentHashes: []);
    }

    public T Scalar<T>(string sql, params object?[] parameters) =>
        (T)Convert.ChangeType(Store.ExecuteScalar(sql, parameters)!, typeof(T));

    public object? ScalarOrNull(string sql, params object?[] parameters)
    {
        var value = Store.ExecuteScalar(sql, parameters);
        return value is DBNull ? null : value;
    }

    public void Dispose()
    {
        Store.Dispose();
        try { Directory.Delete(deferDirectory, recursive: true); } catch { }
    }
}
