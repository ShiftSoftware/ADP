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
    /// The row scope this source owns, matching <see cref="SnapshotMergeOptions.SourceScope"/>.
    /// Hawta needs this declaratively (not only inside the ingest closure) to publish the v4
    /// table catalog and restore internal ownership from a compatible manifest, including for
    /// disabled sources. Null means the source owns the table's unscoped universe.
    /// </summary>
    public string? SourceScope { get; init; }

    /// <summary>
    /// Exact derivation and meaning of this source's canonical <c>_PrimaryKey</c>. Published once
    /// per table/scope in the manifest; it is deliberately not repeated on every parquet row.
    /// </summary>
    public required SourceRecordIdentityDescriptor RecordIdentity { get; init; }

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
    ///
    /// <para>Set this <b>or</b> <see cref="IngestAsync"/>, never both — the registry validates it.
    /// Reading it on a source that declared the async sibling throws rather than returning null,
    /// so the mistake names itself instead of arriving as a NullReferenceException.</para>
    /// </summary>
    public Func<SnapshotSourceContext, SnapshotMergeResult> Ingest
    {
        get => ingest ?? throw new InvalidOperationException(
            $"Source '{Key}' declares IngestAsync, not the synchronous Ingest delegate. " +
            $"Call {nameof(RunIngestAsync)} to run a source without knowing which it uses.");
        init
        {
            // An explicit null reads as "not set", so a caller can pick a delegate in a
            // conditional expression without silently claiming to have one.
            ingest = value;
            HasSynchronousIngest = value is not null;
        }
    }

    private readonly Func<SnapshotSourceContext, SnapshotMergeResult>? ingest;

    /// <summary>
    /// The asynchronous sibling, for sources whose pull is genuinely I/O-bound and has no
    /// synchronous API — a Cosmos change-feed read is the first.
    ///
    /// <para><b>Why a sibling and not a widened <see cref="Ingest"/>.</b> That delegate is a
    /// <c>public required</c> member of a public type in a published package, invoked at thirteen
    /// sites and constructed at nineteen across three repositories, from enclosing methods that are
    /// synchronous by signature. Changing its type is a source AND binary break for every consumer
    /// compiled against an earlier release, and cascades <c>void</c> to <c>async Task</c> through a
    /// whole test suite and harness — or forces sync-over-async, which this codebase forbids
    /// elsewhere for good reason. A sibling costs one branch in the dispatcher and changes zero
    /// existing call sites.</para>
    /// </summary>
    public Func<SnapshotSourceContext, Task<SnapshotMergeResult>>? IngestAsync { get; init; }

    /// <summary>True when this source was built with the synchronous <see cref="Ingest"/> delegate.</summary>
    public bool HasSynchronousIngest { get; private set; }

    /// <summary>
    /// Runs whichever ingest delegate this source declares. The dispatcher calls this, and so
    /// should any harness that fans out over a registry — it is the only form that works for every
    /// source kind.
    /// </summary>
    public Task<SnapshotMergeResult> RunIngestAsync(SnapshotSourceContext context) =>
        IngestAsync is { } asynchronous
            ? asynchronous(context)
            : Task.FromResult(Ingest(context));

    /// <summary>
    /// Non-null when this source READS a Cosmos container, naming which one. Declarative on
    /// purpose: the reader itself is closed over by the ingest delegate (the connection-string
    /// precedent), but the agent has to be able to see that a Cosmos read exists BEFORE it starts
    /// running cycles — see <see cref="CosmosSourceRead"/>.
    /// </summary>
    public CosmosSourceRead? CosmosRead { get; init; }

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

    /// <summary>
    /// This cycle's file-metadata probe, or null when the host does not gate on source changes.
    /// <b>One instance per cycle</b>: it caches, so every source in a cycle sees one consistent
    /// picture of the share and no cycle inherits another cycle's view of it.
    /// </summary>
    public FileMetadataProbe? FileMetadata { get; init; }
}
