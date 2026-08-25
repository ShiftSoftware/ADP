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
    /// expected to rebuild the write database from a compatible published snapshot (or, when no
    /// seed exists, from sources).
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
    /// <para><b>Point this at instance-local storage, not a share — the same rule
    /// <see cref="DatabasePath"/> follows.</b> This documentation used to say the opposite: that
    /// the local-disk-only rule was about a <i>writable DuckDB database</i> over SMB risking
    /// corruption, and that "an extension is a read-only binary that is loaded and never written,
    /// so shared network storage is fine for it". <b>That is wrong.</b> An extension is read-only
    /// in steady state and written exactly once, at install — and DuckDB's install is not atomic
    /// (duckdb/duckdb#3947), so concurrent first touches race (duckdb/duckdb#12589, open).
    /// Measured on 1.5.5: eight concurrent cold first touches into one empty directory left one
    /// survivor and stranded a 29 MB <c>.tmp-&lt;guid&gt;</c>. On shared storage that race is
    /// cross-process AND cross-machine.</para>
    ///
    /// <para><see cref="SnapshotStore.Open"/> now pre-installs the azure extension serially, which
    /// removes the race within one process — measured 1/8 survivors before, 8/8 after. Keeping this
    /// directory instance-local is what removes it between processes and machines.</para>
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
    "Rebuild the write database from a compatible published snapshot, or from sources when no seed exists.")
{
    public int Expected { get; } = expected;
    public int Actual { get; } = actual;
}
