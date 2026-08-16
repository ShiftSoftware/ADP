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

    /// <summary>
    /// Where DuckDB keeps downloaded extensions. Null (the default) leaves DuckDB's own default,
    /// <c>~/.duckdb/extensions</c>, which is correct for dev machines.
    ///
    /// <para><b>Set this on any host whose default location is ephemeral.</b> Reading
    /// <c>az://</c> needs the <c>azure</c> extension, which is <b>not</b> compiled into
    /// DuckDB.NET — DuckDB downloads it (~28 MB) on first use, because
    /// <c>autoinstall_known_extensions</c> defaults to true. On App Service the default
    /// directory does not survive an instance move, so that download repeats on <b>every</b>
    /// cold start, and every one of those silently depends on <c>extensions.duckdb.org</c>
    /// being reachable at that moment. Pointing this at persistent storage makes it happen
    /// once.</para>
    ///
    /// <para>This deliberately does <b>not</b> conflict with <see cref="DatabasePath"/>'s
    /// local-disk-only rule. That rule exists because a <i>writable DuckDB database</i> over
    /// SMB risks corruption; an extension is a read-only binary that is loaded and never
    /// written, so shared network storage is fine for it and wrong for the database.</para>
    ///
    /// <para>Deployment note, since it is easy to get wrong: on Azure App Service use the
    /// <c>%HOME%</c> environment variable rather than a literal <c>D:\home</c>. New apps
    /// resolve <c>%HOME%</c> to <c>C:\home</c>; only older ones get <c>D:</c>, and Microsoft's
    /// guidance is explicitly not to hardcode the drive letter.</para>
    /// </summary>
    public string? ExtensionDirectory { get; init; }

    /// <summary>
    /// Azure Storage connection string, when the published tier lives in a blob container. Null for
    /// a local or SMB publish location.
    ///
    /// <para>This is the DuckDB half of the blob credential and is deliberately separate from the
    /// SDK half held by <see cref="BlobPublishStore"/>: parquet moves through DuckDB, everything
    /// else through Azure.Storage.Blobs, and they authenticate independently. Set both from the
    /// same configuration value.</para>
    ///
    /// <para><b>Never log it.</b> It carries the account key or SAS.</para>
    /// </summary>
    public string? AzureConnectionString { get; init; }
}

/// <summary>Thrown when the write database's schema sentinel doesn't match the package's expectation.</summary>
public sealed class SnapshotSchemaMismatchException(int expected, int actual) : Exception(
    $"Snapshot write database schema version is {actual}, this package expects {expected}. " +
    "Rebuild the write database from sources (a full re-upsert is idempotent under the replication stamp semantics).")
{
    public int Expected { get; } = expected;
    public int Actual { get; } = actual;
}
