namespace ShiftSoftware.ADP.Hawta;

public sealed class SnapshotStoreOptions
{
    /// <summary>
    /// Path of the DuckDB write database. Must live on a real local disk — never a network
    /// mount (SMB/CIFS): DuckDB writable databases on network filesystems risk corruption.
    /// Use <c>:memory:</c> for tests.
    /// </summary>
    public required string DatabasePath { get; init; }

    /// <summary>
    /// Expected storage schema version. Opening a database whose <c>meta.schema_info</c>
    /// sentinel differs throws <see cref="SnapshotSchemaMismatchException"/> — the caller is
    /// expected to rebuild the write database from sources (idempotent under the replication
    /// stamp semantics).
    /// </summary>
    public int SchemaVersion { get; init; } = SnapshotStore.CurrentSchemaVersion;
}

/// <summary>Thrown when the write database's schema sentinel doesn't match the package's expectation.</summary>
public sealed class SnapshotSchemaMismatchException(int expected, int actual) : Exception(
    $"Snapshot write database schema version is {actual}, this package expects {expected}. " +
    "Rebuild the write database from sources (a full re-upsert is idempotent under the replication stamp semantics).")
{
    public int Expected { get; } = expected;
    public int Actual { get; } = actual;
}
