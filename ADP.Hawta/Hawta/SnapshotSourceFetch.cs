namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// What a FETCH delegate gets to work with — deliberately less than
/// <see cref="SnapshotSourceContext"/>.
///
/// <para><b>There is no store here, and that is the whole contract.</b> A fetch runs on a worker
/// thread while other fetches run on theirs and the serial drain runs on its own; the store holds
/// one DuckDB connection, is documented not thread-safe, and a DuckDB connection is locked for the
/// duration of a query anyway. Handing a fetch the store would make every deferred question about
/// cross-connection visibility and concurrent staging live again. Omitting it makes them
/// unreachable.</para>
///
/// <para><b>There is no <see cref="FileMetadataProbe"/> here either</b>, for the same reason one
/// level down: the probe is one shared, caching, explicitly not-thread-safe instance per cycle,
/// and a torn cache does not crash — it answers <c>Skipped:SourceAbsent</c> for a healthy feed.
/// File sources keep the one-phase <see cref="SnapshotSource.Ingest"/> form and keep reading the
/// probe on the drain's single thread. A file source that ever adopts the two-phase form must
/// either batch one probe read for the whole cycle before the fan-out (the batch-shaped
/// <see cref="FileMetadataProbe.Read(IReadOnlyCollection{string})"/> exists for exactly that) or
/// make the probe thread-safe — and must still leave the STAMP write on the drain, because a stamp
/// written for a merge that never ran skips that source forever.</para>
/// </summary>
public sealed class SnapshotSourceFetchContext
{
    public required CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Report rows as they accumulate, as an INCREMENT rather than a running total, so the
    /// admission window can see this source's backlog grow before the fetch finishes. Optional:
    /// a fetch that reports nothing is simply accounted in full when it completes, which is
    /// correct but lets the window admit more optimistically while it runs.
    /// </summary>
    public Action<int> RowsBuffered { get; init; } = static _ => { };
}

/// <summary>
/// What a fetch hands back to the serial drain.
///
/// <para><b>The three outcomes the design has to express</b> are <c>Staged</c> — rows are in
/// hand, the drain should stage and merge them; <c>Terminal</c> — the source settled without
/// needing the store, and the drain should write the run record that says so; and <c>Faulted</c>
/// — the fetch threw. Only the first two are values of this type: a fault is the exception
/// itself, which the dispatcher catches on the worker and re-raises in registry order on the
/// drain, exactly where the one-phase form would have raised it.</para>
///
/// <para><b>Both cases carry a delegate the DRAIN runs</b>, and that is not a formality. Nine
/// terminal paths across three ingestors end a run BEFORE the merge, and every one of them writes
/// a run record. Making the drain the only thing that ever runs those bodies is what keeps
/// <c>meta.SyncRuns</c> and <c>meta.SourceFileStamps</c> single-writer — the stamp path is a
/// non-atomic DELETE-then-INSERT pair, and a lost stamp is silent.</para>
/// </summary>
public sealed class SnapshotSourceFetch
{
    private readonly Func<SnapshotSourceContext, SnapshotMergeResult> drain;

    private SnapshotSourceFetch(long bufferedRows, Func<SnapshotSourceContext, SnapshotMergeResult> drain)
    {
        if (bufferedRows < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferedRows), "A buffered row count cannot be negative.");

        BufferedRows = bufferedRows;
        this.drain = drain;
    }

    /// <summary>
    /// Rows this fetch is holding in memory — the quantity the admission window bounds, and the
    /// quantity it releases once the drain is done. Authoritative: whatever a fetch reported
    /// through <see cref="SnapshotSourceFetchContext.RowsBuffered"/> is reconciled to this number
    /// when the fetch completes.
    /// </summary>
    public long BufferedRows { get; }

    /// <summary>The ordinary outcome: rows are buffered and the drain stages and merges them.</summary>
    public static SnapshotSourceFetch Staged(
        long bufferedRows, Func<SnapshotSourceContext, SnapshotMergeResult> drain) =>
        new(bufferedRows, drain);

    /// <summary>
    /// The source settled without the store — nothing to merge. The delegate STILL runs on the
    /// drain, because the run record that records the outcome is a write to <c>meta.SyncRuns</c>
    /// and workers never write there.
    /// </summary>
    public static SnapshotSourceFetch Terminal(Func<SnapshotSourceContext, SnapshotMergeResult> complete) =>
        new(0, complete);

    internal SnapshotMergeResult Drain(SnapshotSourceContext context) => drain(context);
}
