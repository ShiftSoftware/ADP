using System.Text.Json.Nodes;
using DuckDB.NET.Data;

namespace ShiftSoftware.ADP.Hawta;

public sealed class CosmosChangeFeedIngestorOptions
{
    /// <summary>The snapshot table this source feeds.</summary>
    public required SnapshotTableDefinition Table { get; init; }

    /// <summary>The read seam. Closed over by the ingest delegate, like a SQL connection string.</summary>
    public required ICosmosSnapshotReader Reader { get; init; }

    /// <summary>Which container. Also the cursor's addressing check.</summary>
    public required CosmosSourceRead Source { get; init; }

    /// <summary>
    /// The registry source key. It keys <c>meta.SourceCosmosCursors</c>, so it must be the same
    /// string across restarts and rebuilds or the source silently re-reads from the beginning.
    /// </summary>
    public required string SourceKey { get; init; }

    /// <summary>
    /// Projects one Cosmos document onto the table's source columns. A column the projection
    /// omits stages NULL. The projection owns every shape decision — flattening, canonicalizing a
    /// nested array into a JSON string, normalizing a key — because that is where the document's
    /// semantics live, not in the framework.
    /// </summary>
    public required Func<JsonObject, IReadOnlyDictionary<string, object?>> Project { get; init; }

    /// <summary>Projected column whose (trimmed) text becomes <c>_PrimaryKey</c>.</summary>
    public required string PrimaryKeyColumn { get; init; }

    /// <summary>
    /// Optional projected column carrying the document's own modification time (becomes
    /// <c>_SourceModified</c>).
    ///
    /// <para><b>Do not point this at a creation stamp.</b> A field written once at creation and
    /// never touched on mutation would pin <c>_LastModified</c> to creation time and make the
    /// freshness signal meaningless — leave it null (the merge stamps the run clock) or project
    /// Cosmos's own <c>_ts</c>.</para>
    /// </summary>
    public string? SourceModifiedColumn { get; init; }

    /// <summary>
    /// Operator lever: change it to discard the persisted cursor and re-read the whole container
    /// once, with no deploy.
    ///
    /// <para><b>It is the only automatic full-re-read trigger, deliberately.</b> The file gate's
    /// fingerprint also folds in Hawta's assembly version and the declarative options, because
    /// re-reading one file costs one file. Here a re-read costs the entire container, and the
    /// standing directive is that a full read happens only when someone wants one. So a projection
    /// change that needs a backfill is an explicit version bump, not a side effect of shipping.</para>
    /// </summary>
    public string? IngestVersion { get; init; }

    /// <summary>Documents per change-feed response the service should aim for.</summary>
    public int PageSizeHint { get; init; } = 1000;

    /// <summary>
    /// Documents accumulated before a merge runs. <b>Page-and-merge, never accumulate-then-merge</b>:
    /// ingest holds the single-threaded write gate on a renewing lease, so an 18-month bootstrap
    /// read into one giant staging table blocks every other source for its duration and loses
    /// everything if the lease drops. Merging in bounded batches makes progress durable and lets
    /// the cursor advance with it.
    /// </summary>
    public int MergeBatchSize { get; init; } = 2000;

    /// <summary>Runaway guard on one drain. Reached only if a container never reports caught-up.</summary>
    public int MaxPagesPerRun { get; init; } = 10_000;

    /// <summary>
    /// Observability callback for the two things an operator must not miss: a discarded cursor,
    /// and a bootstrap starting. Facts only — the host decides what they mean.
    /// </summary>
    public Action<string>? OnDiagnostic { get; init; }

    public required SnapshotMergeOptions MergeOptions { get; init; }
}

/// <summary>
/// Incremental ingestion from a Cosmos container's change feed (pull model, LatestVersion),
/// resumed from a continuation token persisted in <c>meta.SourceCosmosCursors</c>.
///
/// <para><b>The shape of one run:</b> resume from the stored token (or from the beginning if there
/// is none), drain pages until the feed reports 304, merging every
/// <see cref="CosmosChangeFeedIngestorOptions.MergeBatchSize"/> documents and persisting the token
/// of the last MERGED page after each. Persisting after the merge, never before, is what keeps the
/// failure direction safe: a crash between the two re-reads a bounded delta the hash diff absorbs,
/// while the reverse would skip documents that are in no table.</para>
///
/// <para><b>Deletes are off and must stay off.</b> A change feed presents only what changed, so a
/// delete-enabled merge would read "absent" as "gone" and tombstone the entire table on the first
/// run. The consequence is stated rather than discovered: hard deletes and TTL expiry in Cosmos are
/// invisible to the snapshot, which for an audit record is the desirable direction.</para>
/// </summary>
public static class CosmosChangeFeedSnapshotIngestor
{
    public static async Task<SnapshotMergeResult> IngestAsync(
        SnapshotStore store,
        CosmosChangeFeedIngestorOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SourceKey);

        // A guard that must act inside the operation, not a health check: an incremental feed
        // merged with deletes enabled tombstones every row it did not happen to see this tick.
        if (options.MergeOptions.DeletesEnabled)
        {
            throw new ArgumentException(
                $"Source '{options.SourceKey}' reads a change feed, which presents only CHANGED documents. " +
                "DeletesEnabled must be false — absence means 'unchanged', not 'gone'.",
                nameof(options));
        }

        if (options.MergeBatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MergeBatchSize must be positive.");
        if (options.MaxPagesPerRun <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxPagesPerRun must be positive.");

        var resume = ResolveResumeToken(store, options);

        try
        {
            return await DrainAsync(store, options, resume, cancellationToken);
        }
        catch (CosmosChangeFeedTokenException exception) when (resume is not null)
        {
            // Safe by construction rather than by care: re-reading documents that have not changed
            // produces no row versions, because the merge diffs content hashes. Expensive enough to
            // be loud about, though — this is a full container read nobody asked for.
            store.ClearSourceCosmosCursor(options.SourceKey);
            options.OnDiagnostic?.Invoke(
                $"Cosmos rejected the stored change-feed cursor for '{options.SourceKey}' on {options.Source}; " +
                $"discarding it and re-reading the container from the beginning. ({exception.Message})");

            return await DrainAsync(store, options, resume: null, cancellationToken);
        }
    }

    /// <summary>
    /// What the stored cursor is worth this run. A cursor is only meaningful against the container
    /// that issued it and the ingest version it was taken under; anything else re-reads.
    /// </summary>
    private static string? ResolveResumeToken(SnapshotStore store, CosmosChangeFeedIngestorOptions options)
    {
        var cursor = store.ReadSourceCosmosCursor(options.SourceKey);
        if (cursor is null)
        {
            options.OnDiagnostic?.Invoke(
                $"No change-feed cursor for '{options.SourceKey}': reading {options.Source} from the beginning. " +
                "This is the bootstrap — the only routine full read of the container.");
            return null;
        }

        if (!string.Equals(cursor.Database, options.Source.Database, StringComparison.Ordinal)
            || !string.Equals(cursor.Container, options.Source.Container, StringComparison.Ordinal))
        {
            options.OnDiagnostic?.Invoke(
                $"The stored cursor for '{options.SourceKey}' addresses {cursor.Database}/{cursor.Container} but the " +
                $"source now reads {options.Source}; discarding it and reading from the beginning.");
            return null;
        }

        if (!string.Equals(Normalize(cursor.IngestVersion), Normalize(options.IngestVersion), StringComparison.Ordinal))
        {
            options.OnDiagnostic?.Invoke(
                $"IngestVersion changed for '{options.SourceKey}' " +
                $"({Describe(cursor.IngestVersion)} -> {Describe(options.IngestVersion)}); " +
                "re-reading the container from the beginning, as asked.");
            return null;
        }

        return cursor.ContinuationToken;

        static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        static string Describe(string? value) => Normalize(value) ?? "<none>";
    }

    private static async Task<SnapshotMergeResult> DrainAsync(
        SnapshotStore store,
        CosmosChangeFeedIngestorOptions options,
        string? resume,
        CancellationToken cancellationToken)
    {
        var request = new CosmosChangeFeedRequest
        {
            Database = options.Source.Database,
            Container = options.Source.Container,
            ContinuationToken = resume,
            PageSizeHint = options.PageSizeHint,
        };

        var batch = new List<IReadOnlyDictionary<string, object?>>(options.MergeBatchSize);
        var pages = 0;
        SnapshotMergeResult? aggregate = null;
        var mergedAnything = false;

        // ONE run record for the whole drain, not one per merge transaction. The drain merges every
        // MergeBatchSize rows so the cursor never advances past data that did not land — which is
        // correct, and which made the 49,532-document bootstrap write 25 rows into meta.SyncRuns
        // where every other source writes one. The publisher takes the NEWEST row per source into
        // the manifest, so `sourceRuns` reported the last batch's 1,532 as the run.
        var drainRunId = options.MergeOptions.RunId ?? Guid.NewGuid().ToString("N");
        var drainStartedAt = DateTime.UtcNow;
        var batchOptions = options.MergeOptions.WithSuppressedRunRecord();

        // The token that describes exactly what is in `batch`. Persisted only once the batch has
        // merged, so the write DB never claims to have ingested documents it does not hold.
        string? pendingToken = null;
        string? caughtUpToken = null;

        await foreach (var page in options.Reader.ReadChangeFeedAsync(request, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var document in page.Documents)
                batch.Add(options.Project(document));

            pendingToken = page.ContinuationToken;
            pages++;

            var atBound = pages >= options.MaxPagesPerRun;

            if (page.CaughtUp)
                caughtUpToken = page.ContinuationToken;

            if (batch.Count >= options.MergeBatchSize || page.CaughtUp || atBound)
            {
                if (batch.Count > 0)
                {
                    var result = MergeBatch(store, options, batchOptions, batch);
                    aggregate = Combine(aggregate, result);
                    mergedAnything = true;
                    batch.Clear();

                    // Stop the drain on anything short of success. Advancing the cursor past a
                    // merge that did not land is the one mistake this design cannot absorb.
                    if (!result.Succeeded)
                    {
                        return RecordDrain(store, options, drainRunId, drainStartedAt, aggregate!,
                            $"Batch merge finished {result.Status}; the drain stopped and the cursor was not advanced past it.");
                    }
                }

                PersistCursor(store, options, pendingToken);
            }

            if (page.CaughtUp || atBound)
            {
                if (atBound && !page.CaughtUp)
                {
                    options.OnDiagnostic?.Invoke(
                        $"Change-feed drain for '{options.SourceKey}' stopped at its {options.MaxPagesPerRun}-page " +
                        "bound without reaching the end of the feed; the next cycle resumes from the stored cursor.");
                }

                break;
            }
        }

        if (mergedAnything)
            return RecordDrain(store, options, drainRunId, drainStartedAt, aggregate!, error: null);

        // Nothing changed upstream. That still has to be a run record: a source whose reads stop
        // producing must stay VISIBLE in meta.SyncRuns, or "caught up" and "crashing before
        // staging" become the same absence — and only one of them is healthy.
        var skipped = new SnapshotMergeResult(drainRunId, SnapshotMergeStatus.SkippedSourceUnchanged, 0, 0, 0, 0);
        SnapshotMerge.InsertRunRecord(
            store, options.Table, options.MergeOptions, drainRunId, drainStartedAt, skipped,
            caughtUpToken is null
                ? "The change feed returned no documents. Nothing merged."
                : $"The change feed is caught up on {options.Source}. No documents since the stored cursor.");

        return skipped;
    }

    private static void PersistCursor(SnapshotStore store, CosmosChangeFeedIngestorOptions options, string? token)
    {
        if (string.IsNullOrEmpty(token))
            return;

        store.WriteSourceCosmosCursor(new SourceCosmosCursor(
            options.SourceKey,
            options.Source.Database,
            options.Source.Container,
            token,
            string.IsNullOrWhiteSpace(options.IngestVersion) ? null : options.IngestVersion.Trim(),
            DateTime.UtcNow));
    }

    /// <summary>
    /// Writes the single <c>meta.SyncRuns</c> row for this drain and returns the aggregate under
    /// the drain's own run id, so what the caller prints and what the manifest publishes agree.
    /// </summary>
    private static SnapshotMergeResult RecordDrain(
        SnapshotStore store,
        CosmosChangeFeedIngestorOptions options,
        string runId,
        DateTime startedAt,
        SnapshotMergeResult aggregate,
        string? error)
    {
        var drain = aggregate with { RunId = runId };
        SnapshotMerge.InsertRunRecord(
            store, options.Table, options.MergeOptions, runId, startedAt, drain, error);
        return drain;
    }

    private static SnapshotMergeResult MergeBatch(
        SnapshotStore store,
        CosmosChangeFeedIngestorOptions options,
        SnapshotMergeOptions mergeOptions,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var staging = store.CreateStagingTable(options.Table, persistent: true);

        // One document can appear more than once in a single drain — it is re-emitted when it is
        // mutated while we are paging. The merge refuses duplicate keys outright, so the LAST
        // version wins here, which is also the newest.
        var byKey = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal);
        var keyless = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var row in rows)
        {
            var key = row.TryGetValue(options.PrimaryKeyColumn, out var value)
                ? value?.ToString()?.Trim()
                : null;

            // A document with no usable identity is NOT dropped. It goes to staging with a NULL
            // key so the merge fails the whole run loudly, instead of the source quietly ingesting
            // 99% of a container forever.
            if (string.IsNullOrEmpty(key))
                keyless.Add(row);
            else
                byKey[key] = row;
        }

        using (var appender = store.Connection.CreateAppender("stage", staging.Name))
        {
            foreach (var (key, row) in byKey)
                AppendRow(appender.CreateRow(), options, row, key);

            foreach (var row in keyless)
                AppendRow(appender.CreateRow(), options, row, key: null);
        }

        store.Execute(
            $"""
            UPDATE {staging.QualifiedName}
            SET "{BookkeepingColumns.RowHash}" = {RowHash.Expression(options.Table.Columns.Select(c => c.Name))},
                "{BookkeepingColumns.ReplicationHash}" = {RowHash.Expression(options.Table.ReplicationColumns.Select(c => c.Name))}
            """);

        return SnapshotMerge.Execute(store, options.Table, staging, mergeOptions);
    }

    private static void AppendRow(
        IDuckDBAppenderRow row,
        CosmosChangeFeedIngestorOptions options,
        IReadOnlyDictionary<string, object?> values,
        string? key)
    {
        foreach (var column in options.Table.Columns)
            AppendValue(row, values.TryGetValue(column.Name, out var value) ? value : null);

        AppendValue(row, key);

        // Content and replication hashes are filled in-database after the load, so
        // canonicalization stays uniform across every source format.
        AppendValue(row, null);
        AppendValue(row, null);

        AppendValue(row, options.SourceModifiedColumn is not null
                         && values.TryGetValue(options.SourceModifiedColumn, out var modified)
            ? modified
            : null);

        row.EndRow();
    }

    private static void AppendValue(IDuckDBAppenderRow row, object? value)
    {
        switch (value)
        {
            case null or DBNull: row.AppendNullValue(); break;
            case string s: row.AppendValue(s); break;
            case bool b: row.AppendValue(b); break;
            case byte by: row.AppendValue(by); break;
            case short sh: row.AppendValue(sh); break;
            case int i: row.AppendValue(i); break;
            case long l: row.AppendValue(l); break;
            case float f: row.AppendValue(f); break;
            case double d: row.AppendValue(d); break;
            case decimal m: row.AppendValue(m); break;
            case DateTime dt: row.AppendValue(dt); break;
            case DateTimeOffset dto: row.AppendValue(dto.UtcDateTime); break;
            case Guid g: row.AppendValue(g.ToString()); break;
            case byte[] bytes: row.AppendValue(bytes); break;
            default: row.AppendValue(value.ToString()); break;
        }
    }

    /// <summary>
    /// One ingest call reports one result even though it ran several merges. Counts add up; the
    /// run id is the last batch's, and any non-success status wins over Succeeded because the
    /// drain stops there.
    /// </summary>
    private static SnapshotMergeResult Combine(SnapshotMergeResult? left, SnapshotMergeResult right) =>
        left is null
            ? right
            : new SnapshotMergeResult(
                right.RunId,
                right.Succeeded ? left.Status : right.Status,
                left.RowsStaged + right.RowsStaged,
                left.RowsInserted + right.RowsInserted,
                left.RowsUpdated + right.RowsUpdated,
                left.RowsTombstoned + right.RowsTombstoned,
                left.PendingDeletes + right.PendingDeletes,
                left.RowsRescoped + right.RowsRescoped);
}
