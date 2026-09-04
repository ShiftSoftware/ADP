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
    /// Read-only directories holding extensions that SHIPPED with the deployment, searched instead
    /// of downloading. Set this and the estate never fetches an extension at runtime at all.
    ///
    /// <para><b>Layout is load-bearing</b>, and DuckDB does not guess:
    /// <c>&lt;directory&gt;/&lt;duckdb-version&gt;/&lt;platform&gt;/&lt;name&gt;.duckdb_extension</c>,
    /// e.g. <c>.../v1.5.5/windows_amd64/azure.duckdb_extension</c>. The version must track the
    /// <c>DuckDB.NET.Data.Full</c> pin and the platform must match the machine that RUNS the app,
    /// not the one that builds it.</para>
    ///
    /// <para><b>Why this is better than a writable cache, measured.</b> Under eight concurrent cold
    /// first touches, a downloading cache was 1/8 with a stranded 29 MB temp file; a shipped tree is
    /// 8/8 and the directory still holds exactly the file that was deployed. Not because a race was
    /// won — because there is no write to race. It also removes the runtime dependency on
    /// <c>extensions.duckdb.org</c>: verified by pointing the extension repository at an unreachable
    /// host and reading <c>az://</c> anyway.</para>
    ///
    /// <para><b>Setting this changes how <see cref="SnapshotStore.Open"/> provisions the extension</b>
    /// — see <see cref="SnapshotStore.ProvisionAzureExtension"/>. In particular it must disable
    /// <c>autoinstall_known_extensions</c>, because with autoinstall on DuckDB tries to DOWNLOAD
    /// before it will look here, which would make the shipped copy pointless. Measured, not
    /// assumed: <c>INSTALL azure</c> with a shipped tree present does not short-circuit, it
    /// downloads.</para>
    ///
    /// <para>Null or empty (the default) keeps the download-on-demand behaviour, which is what a
    /// dev machine wants.</para>
    /// </summary>
    public IReadOnlyList<string>? ExtensionDirectories { get; init; }

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

    /// <summary>
    /// Runs against the open connection before the schema bootstrap — the place a host registers
    /// the DuckDB scalar functions its projection SQL calls (hash-id encoders, say). A function is
    /// connection-scoped in DuckDB, so this runs on every open rather than once per process; keep
    /// it idempotent and cheap.
    /// </summary>
    public Action<DuckDB.NET.Data.DuckDBConnection>? ConfigureConnection { get; init; }
}

/// <summary>Thrown when the write database's schema sentinel doesn't match the package's expectation.</summary>
public sealed class SnapshotSchemaMismatchException(int expected, int actual) : Exception(
    $"Snapshot write database schema version is {actual}, this package expects {expected}. " +
    "Rebuild the write database from a compatible published snapshot, or from sources when no seed exists.")
{
    public int Expected { get; } = expected;
    public int Actual { get; } = actual;
}
