namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// One registry entry: everything the dispatcher needs to run a source — what to ingest
/// (the delegate closes over connections, paths, and per-source config), how often, and
/// where the table's rows replicate (if anywhere). Adding a source to the agent is adding
/// one of these to the registry.
/// </summary>
public sealed class SnapshotSource
{
    /// <summary>
    /// Stable source key, recorded on <c>meta.SyncRuns</c> via the ingest delegate's merge
    /// options (e.g. <c>dms-order-lines/AAD</c>, <c>csv-jpm</c>). Unique within a registry.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The table this source feeds. Sources sharing a table (e.g. one view family across
    /// eight dealers) must share the SAME definition instance — and distinguish their rows
    /// via <see cref="SnapshotMergeOptions.SourceScope"/> inside the ingest delegate.
    /// </summary>
    public required SnapshotTableDefinition Table { get; init; }

    /// <summary>How often the source is due. Independent per source — this is what makes freshness per-family honest.</summary>
    public required TimeSpan Cadence { get; init; }

    /// <summary>
    /// Runs the full pull → stage → merge for this source and returns the merge result.
    /// Called under the write gate, on the dispatcher's single thread. The delegate owns
    /// source connectivity (SQL connection, file path) and its own merge options.
    /// </summary>
    public required Func<SnapshotSourceContext, SnapshotMergeResult> Ingest { get; init; }

    /// <summary>
    /// The declarative file configuration when this is a common-path file source. It is the
    /// same instance closed over by <see cref="Ingest"/>, exposed for diagnostics and tests so
    /// callers can prove which sources use direct typed binding versus a custom projection.
    /// Null for non-file sources.
    /// </summary>
    public FileSnapshotIngestorOptions? FileIngestion { get; init; }

    /// <summary>
    /// The Cosmos families this source's TABLE feeds, or null for snapshot-only tables.
    /// Sources sharing a table must reference the SAME families list instance — the pump
    /// runs per table, and mappings resolve per-scope values (CompanyID etc.) from
    /// <see cref="DirtyRow.SourceScope"/>, never from per-source closures.
    /// </summary>
    public IReadOnlyList<CosmosFamilyMapping>? Families { get; init; }

    /// <summary>Dirty rows, or distinct aggregate keys for grouped mappings, per pump batch.</summary>
    public int ReplicationBatchSize { get; init; } = 1000;

    /// <summary>
    /// Maximum rows or aggregate groups from this table concurrently performing Cosmos I/O. DuckDB
    /// bookkeeping remains serialized by the pump owner. Default 1 is compatibility mode.
    /// </summary>
    public int ReplicationMaxInFlightRows { get; init; } = 1;

    /// <summary>
    /// False keeps this table's pump OFF while the mappings stay wired (recon can still use
    /// them) — the migration posture for a family whose Cosmos documents the INCUMBENT still
    /// owns: pumping it would double-write. Flip per family at its cutover. Sources sharing
    /// a table must agree (validated by the registry).
    /// </summary>
    public bool ReplicationEnabled { get; init; } = true;

    /// <summary>
    /// False ships the source dark (roster entry exists; nothing runs). The table is still
    /// ensured, published, and rebuilt — "not enabled" must never mean "missing from the
    /// publish set", or the table would silently lose its DR seed.
    /// </summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>What an ingest delegate gets to work with.</summary>
public sealed class SnapshotSourceContext
{
    public required SnapshotStore Store { get; init; }
    public required CancellationToken CancellationToken { get; init; }
}
