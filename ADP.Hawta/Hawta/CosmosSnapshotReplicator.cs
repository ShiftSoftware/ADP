using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Hawta;

public enum CosmosReplicationOperationKind
{
    Delete,
    Upsert,
}

/// <summary>One logical Cosmos request observed by the pump.</summary>
/// <remarks>The callback can run concurrently. Hosts that collect these metrics must be thread-safe.</remarks>
public sealed record CosmosReplicationOperationMetric(
    CosmosReplicationOperationKind Operation,
    string Family,
    string DocumentId,
    HttpStatusCode? StatusCode,
    bool Succeeded,
    bool Canceled,
    double RequestCharge,
    TimeSpan RetryAfter,
    TimeSpan Elapsed,
    string? Error);

public sealed class CosmosSnapshotReplicatorOptions
{
    public required SnapshotTableDefinition Table { get; init; }

    /// <summary>The families this table feeds (one source may fan out to several).</summary>
    public required IReadOnlyList<CosmosFamilyMapping> Families { get; init; }

    /// <summary>
    /// Dirty rows loaded per pump cycle for row mappings; distinct dirty group keys for a
    /// grouped mapping. A selected group loads all of its source rows set-wise.
    /// </summary>
    public int BatchSize { get; init; } = 1000;

    /// <summary>
    /// Maximum source rows (or aggregate groups) concurrently performing Cosmos I/O.
    /// Default 1 preserves the original remote-operation → local-stamp → next-work sequence.
    /// </summary>
    public int MaxInFlightRows { get; init; } = 1;

    /// <summary>
    /// Optional per-request metrics callback. It may run concurrently and must return
    /// quickly. Callback exceptions are ignored so observability cannot break replication.
    /// </summary>
    public Action<CosmosReplicationOperationMetric>? OnOperation { get; init; }

    /// <summary>
    /// Optional external-ownership fence. It is invoked before admitting each row, before
    /// every row-local Cosmos operation, and immediately before either successful or failed
    /// DuckDB bookkeeping. It may run concurrently and must throw when ownership is no longer
    /// valid. A thrown exception stops admission, cancels in-flight work, and is re-thrown.
    /// </summary>
    public Action? OwnershipGuard { get; init; }

    /// <summary>
    /// Dry-run: compute every intended Cosmos op and emit it to <c>meta.ReconOps</c> —
    /// nothing is pushed, nothing is stamped, the dirty state is untouched. The dual-run
    /// window's writer mode. Because nothing is stamped, a dry-run pages the ENTIRE dirty
    /// set internally (cursor on <c>_PrimaryKey</c>) and returns <c>HasMore = false</c>;
    /// draining on <c>HasMore</c> is a wet-mode protocol only.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// Dry-run only: delete the table's previous recon ops before emitting this run's
    /// (repeated dry-runs would otherwise grow <c>meta.ReconOps</c> without bound).
    /// Disable to accumulate multiple runs side by side.
    /// </summary>
    public bool PruneReconOps { get; init; } = true;

    /// <summary>Run id for recon-op association. Null = fresh GUID per run.</summary>
    public string? RunId { get; init; }

    /// <summary>
    /// Wet mode: read dirty rows with <c>_PrimaryKey</c> strictly above this cursor, or
    /// grouped work with its normalized group key strictly above this cursor. A
    /// dispatcher draining within ONE cycle passes the previous batch's
    /// <see cref="ReplicationRunResult.LastPrimaryKey"/> so a row that FAILED is not
    /// re-read (and its attempt ledger not re-burned) five times back-to-back in the same
    /// cycle — failed rows retry on the NEXT cycle, which is what the cross-run ledger was
    /// designed for. Null = start of the key space.
    /// </summary>
    public string? AfterPrimaryKey { get; init; }
}

/// <summary>Outcome of one pump batch.</summary>
public sealed record ReplicationRunResult(
    string RunId,
    bool DryRun,
    int RowsRead,
    int Upserted,
    int Deleted,
    int Excluded,
    int Failed,
    /// <summary>True when a bounded probe found another dirty row above this batch's cursor.</summary>
    bool HasMore,
    /// <summary>The batch's highest <c>_PrimaryKey</c> — the drain cursor. Null when the batch was empty.</summary>
    string? LastPrimaryKey = null,
    /// <summary>Rows that required NO Cosmos call (replication-excluded, or a tombstone that was never replicated).</summary>
    int NoOpRows = 0,
    /// <summary>Rows for which at least one Cosmos operation started.</summary>
    int RemoteAttemptedRows = 0,
    /// <summary>Remotely attempted rows whose ordered Cosmos operation sequence failed.</summary>
    int RemoteFailedRows = 0,
    /// <summary>Highest concurrently active row-level Cosmos operation sequences.</summary>
    int MaxObservedInFlightRows = 0,
    double RequestCharge = 0,
    int ThrottledRequests = 0,
    TimeSpan RetryAfter = default,
    TimeSpan CosmosOperationTime = default,
    TimeSpan BookkeepingTime = default,
    /// <summary>Committed DuckDB transactions containing terminal row outcomes.</summary>
    int BookkeepingTransactions = 0,
    /// <summary>Terminal outcomes included in committed DuckDB transactions.</summary>
    int BookkeepingOutcomeRows = 0,
    /// <summary>Largest terminal-outcome group committed in one DuckDB transaction.</summary>
    int MaxRowsPerBookkeepingTransaction = 0,
    /// <summary>Distinct aggregate keys selected in this batch (zero for row mappings).</summary>
    int GroupsRead = 0,
    /// <summary>All source rows loaded set-wise for the selected groups (zero for row mappings).</summary>
    int SourceRowsLoaded = 0,
    /// <summary>Typed reducers executed in this batch (zero for row mappings).</summary>
    int GroupsRecomputed = 0);

public enum ReplicationDrainStop
{
    /// <summary>The queue ran dry — the normal outcome.</summary>
    QueueEmpty,
    /// <summary>Every row that attempted a Cosmos op failed: treated as systemic, not per-row poison.</summary>
    SystemicFailure,
    /// <summary>The per-drain batch bound was reached; the rest rolls to the next drain.</summary>
    BatchBound,
    /// <summary>The cursor scan ended, but failed rows remain eligible for a later retry.</summary>
    RetryPending,
}

/// <summary>Outcome of a full drain (many batches).</summary>
public sealed record ReplicationDrainResult(
    string RunId,
    bool DryRun,
    int Batches,
    int RowsRead,
    int Upserted,
    int Deleted,
    int Excluded,
    int Failed,
    bool Drained,
    ReplicationDrainStop Stopped,
    int RemoteAttemptedRows = 0,
    int RemoteFailedRows = 0,
    int MaxObservedInFlightRows = 0,
    double RequestCharge = 0,
    int ThrottledRequests = 0,
    TimeSpan RetryAfter = default,
    TimeSpan CosmosOperationTime = default,
    TimeSpan BookkeepingTime = default,
    int BookkeepingTransactions = 0,
    int BookkeepingOutcomeRows = 0,
    int MaxRowsPerBookkeepingTransaction = 0,
    int GroupsRead = 0,
    int SourceRowsLoaded = 0,
    int GroupsRecomputed = 0,
    /// <summary>
    /// Rows still unreplicated but past the attempt limit — invisible to the drain's own
    /// dirty predicate, so a drain can report an empty queue while these sit permanently
    /// undelivered. Surfaced per drain because the same rows also block a cold start from
    /// deferring the table (the manifest's pending count is attempts-blind on purpose):
    /// one poison row silently costs the table its deferral until an operator resets the
    /// ledger or the source ships a content change.
    /// </summary>
    int DeadLettered = 0);

/// <summary>
/// The snapshot → Cosmos pump. Loads a batch through the dirty predicate (capturing each
/// row's <c>_LastModified</c>), maps rows through the family mappings, writes (upserts +
/// point deletes; 404 on delete = success), and stamps <c>_LastReplicationDate</c> with the
/// CAPTURED value — the framework's watermark semantics verbatim. Retry needs no queue: the
/// dirty predicate IS the queue; failures land in the per-row ledger and dead-letter at
/// <see cref="SnapshotStore.MaxReplicationAttempts"/>.
/// Coordinate changes are handled from the old stamp: delete at the old coordinates, write
/// at the new — never both docs left alive.
/// </summary>
public sealed class CosmosSnapshotReplicator
{
    private readonly IReplicationStateStore store;
    private readonly ICosmosSnapshotTransport? cosmosTransport;
    private readonly SemaphoreSlim runLock = new(1, 1);

    /// <param name="cosmosClient">May be null for dry-run-only use.</param>
    public CosmosSnapshotReplicator(SnapshotStore store, CosmosClient? cosmosClient)
        : this(
            new SnapshotReplicationStateStore(store),
            cosmosClient is null ? null : new CosmosClientSnapshotTransport(cosmosClient))
    {
    }

    internal CosmosSnapshotReplicator(
        IReplicationStateStore store,
        ICosmosSnapshotTransport? cosmosTransport)
    {
        this.store = store;
        this.cosmosTransport = cosmosTransport;
    }

    /// <summary>
    /// Drains a table's dirty queue: repeated <see cref="RunOnceAsync"/> batches, paged by
    /// primary-key cursor so a row that FAILS is not re-read (and its attempt ledger not
    /// re-burned) inside the same drain — failed rows retry on the next drain, which is what
    /// the cross-run ledger was designed for. Stops on an all-attempted-and-failed batch
    /// (systemic Cosmos failure, not per-row poison) and at <paramref name="maxBatches"/>.
    /// <para>Every caller drains through THIS method — a hand-rolled loop in a drill harness
    /// would rehearse different outage behavior than production, which is worse than no
    /// drill.</para>
    /// </summary>
    public async Task<ReplicationDrainResult> DrainAsync(
        CosmosSnapshotReplicatorOptions options,
        int maxBatches = 100,
        Action<ReplicationRunResult>? onBatch = null,
        CancellationToken cancellationToken = default)
    {
        if (maxBatches <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBatches), "The drain batch bound must be positive.");

        await runLock.WaitAsync(cancellationToken);
        try
        {
            return await DrainCoreAsync(options, maxBatches, onBatch, cancellationToken);
        }
        finally
        {
            runLock.Release();
        }
    }

    public async Task<ReplicationRunResult> RunOnceAsync(
        CosmosSnapshotReplicatorOptions options,
        CancellationToken cancellationToken = default)
    {
        await runLock.WaitAsync(cancellationToken);
        try
        {
            return await RunOnceCoreAsync(options, cancellationToken);
        }
        finally
        {
            runLock.Release();
        }
    }

    private async Task<ReplicationDrainResult> DrainCoreAsync(
        CosmosSnapshotReplicatorOptions options,
        int maxBatches,
        Action<ReplicationRunResult>? onBatch,
        CancellationToken cancellationToken)
    {
        ValidateOptions(options);

        int batches = 0, rowsRead = 0, upserted = 0, deleted = 0, excluded = 0, failed = 0;
        int remoteAttempted = 0, remoteFailed = 0, highWater = 0, throttled = 0;
        int bookkeepingTransactions = 0, bookkeepingOutcomeRows = 0, maxRowsPerBookkeepingTransaction = 0;
        int groupsRead = 0, sourceRowsLoaded = 0, groupsRecomputed = 0;
        double requestCharge = 0;
        TimeSpan retryAfter = default, cosmosTime = default, bookkeepingTime = default;
        var drained = true;
        var stopped = ReplicationDrainStop.QueueEmpty;
        var scanHasMore = false;
        string? cursor = options.AfterPrimaryKey;
        string runId = options.RunId ?? Guid.NewGuid().ToString("N");

        while (batches < maxBatches)
        {
            var result = await RunOnceCoreAsync(
                new CosmosSnapshotReplicatorOptions
                {
                    Table = options.Table,
                    Families = options.Families,
                    BatchSize = options.BatchSize,
                    MaxInFlightRows = options.MaxInFlightRows,
                    OnOperation = options.OnOperation,
                    OwnershipGuard = options.OwnershipGuard,
                    DryRun = options.DryRun,
                    PruneReconOps = options.PruneReconOps && batches == 0,
                    RunId = runId,
                    AfterPrimaryKey = cursor,
                },
                cancellationToken);

            batches++;
            rowsRead += result.RowsRead;
            upserted += result.Upserted;
            deleted += result.Deleted;
            excluded += result.Excluded;
            failed += result.Failed;
            remoteAttempted += result.RemoteAttemptedRows;
            remoteFailed += result.RemoteFailedRows;
            highWater = Math.Max(highWater, result.MaxObservedInFlightRows);
            requestCharge += result.RequestCharge;
            throttled += result.ThrottledRequests;
            retryAfter += result.RetryAfter;
            cosmosTime += result.CosmosOperationTime;
            bookkeepingTime += result.BookkeepingTime;
            bookkeepingTransactions += result.BookkeepingTransactions;
            bookkeepingOutcomeRows += result.BookkeepingOutcomeRows;
            maxRowsPerBookkeepingTransaction = Math.Max(
                maxRowsPerBookkeepingTransaction,
                result.MaxRowsPerBookkeepingTransaction);
            groupsRead += result.GroupsRead;
            sourceRowsLoaded += result.SourceRowsLoaded;
            groupsRecomputed += result.GroupsRecomputed;
            cursor = result.LastPrimaryKey;
            onBatch?.Invoke(result);
            cancellationToken.ThrowIfCancellationRequested();
            scanHasMore = result.HasMore;

            // Planning failures and no-op rows are deliberately outside this comparison.
            // A total remote outage must still break even when either appears in the batch.
            if (result.RemoteAttemptedRows > 0
                && result.RemoteFailedRows == result.RemoteAttemptedRows)
            {
                drained = false;
                stopped = ReplicationDrainStop.SystemicFailure;
                break;
            }

            if (!result.HasMore)
                break;

            if (batches >= maxBatches)
            {
                drained = false;
                stopped = ReplicationDrainStop.BatchBound;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (!options.DryRun && stopped != ReplicationDrainStop.SystemicFailure)
        {
            // HasMore describes only the forward cursor. A bounded probe from the beginning
            // distinguishes a truly empty dirty queue from failed rows intentionally left
            // behind the cursor for the next drain.
            cancellationToken.ThrowIfCancellationRequested();
            var dirtyRowRemains = store.ReadDirtyRows(options.Table, afterPrimaryKey: null, limit: 1).Count > 0;
            if (!dirtyRowRemains)
            {
                drained = true;
                stopped = ReplicationDrainStop.QueueEmpty;
            }
            else if (!scanHasMore)
            {
                drained = false;
                stopped = ReplicationDrainStop.RetryPending;
            }
        }

        // Counted at drain end so failures that just crossed the attempt limit are included.
        // One cheap scan; what it buys is the settled-vs-drained divergence being visible in
        // the run report instead of only in a cold start that unexpectedly loads the table.
        cancellationToken.ThrowIfCancellationRequested();
        var deadLettered = (int)store.CountDeadLetteredRows(options.Table);

        return new ReplicationDrainResult(
            runId, options.DryRun, batches, rowsRead, upserted, deleted, excluded, failed, drained, stopped,
            remoteAttempted, remoteFailed, highWater, requestCharge, throttled, retryAfter, cosmosTime,
            bookkeepingTime, bookkeepingTransactions, bookkeepingOutcomeRows,
            maxRowsPerBookkeepingTransaction, groupsRead, sourceRowsLoaded, groupsRecomputed,
            deadLettered);
    }

    private async Task<ReplicationRunResult> RunOnceCoreAsync(
        CosmosSnapshotReplicatorOptions options,
        CancellationToken cancellationToken)
    {
        ValidateOptions(options);

        if (!options.DryRun && cosmosTransport is null)
            throw new InvalidOperationException("A CosmosClient is required unless DryRun is set.");

        var runId = options.RunId ?? Guid.NewGuid().ToString("N");
        var groupedFamily = options.Families.SingleOrDefault(family => family.Grouping is not null);
        if (options.DryRun)
            return groupedFamily is null
                ? ExecuteDryRun(runId, options, cancellationToken)
                : ExecuteGroupedDryRun(runId, options, groupedFamily, cancellationToken);

        if (groupedFamily is not null)
            return await RunGroupedOnceAsync(runId, options, groupedFamily, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var batch = store.ReadDirtyRows(options.Table, options.AfterPrimaryKey, options.BatchSize);
        var hasMore = batch.Count == options.BatchSize
                      && store.ReadDirtyRows(options.Table, batch[^1].PrimaryKey, 1).Count > 0;
        var (work, containers) = PlanAndResolveBatch(batch, options.Families);
        var execution = await ExecuteWetBatchAsync(work, containers, options, cancellationToken);

        return new ReplicationRunResult(
            runId,
            DryRun: false,
            batch.Count,
            execution.Upserted,
            execution.Deleted,
            execution.Excluded,
            execution.Failed,
            HasMore: hasMore,
            LastPrimaryKey: batch.Count > 0 ? batch[^1].PrimaryKey : null,
            execution.NoOpRows,
            execution.RemoteAttemptedRows,
            execution.RemoteFailedRows,
            execution.MaxObservedInFlightRows,
            execution.RequestCharge,
            execution.ThrottledRequests,
            execution.RetryAfter,
            execution.CosmosOperationTime,
            execution.BookkeepingTime,
            execution.BookkeepingTransactions,
            execution.BookkeepingOutcomeRows,
            execution.MaxRowsPerBookkeepingTransaction);
    }

    private async Task<ReplicationRunResult> RunGroupedOnceAsync(
        string runId,
        CosmosSnapshotReplicatorOptions options,
        CosmosFamilyMapping family,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var grouping = family.Grouping!;
        var page = store.ReadReplicationGroups(
            options.Table,
            grouping,
            options.AfterPrimaryKey,
            options.BatchSize,
            dirtyGroupsOnly: true);
        var prepared = new List<RowWork>(page.Groups.Count);
        foreach (var group in page.Groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dirtyRows = group.DirtyRows;
            var representative = dirtyRows[0];
            try
            {
                prepared.Add(new RowWork(
                    representative,
                    PlanGroup(group, family),
                    stateRows: dirtyRows));
            }
            catch (Exception exception)
            {
                prepared.Add(new RowWork(
                    representative,
                    plan: null,
                    new InvalidOperationException(
                        $"Cosmos group mapping failed for '{group.GroupKey}': {exception.Message}",
                        exception),
                    dirtyRows));
            }
        }

        var (work, containers) = PlanAndResolveBatch([], options.Families, prepared);
        var execution = await ExecuteWetBatchAsync(work, containers, options, cancellationToken);
        var dirtyRowsRead = page.Groups.Sum(group => group.DirtyRows.Count);

        return new ReplicationRunResult(
            runId,
            DryRun: false,
            RowsRead: dirtyRowsRead,
            execution.Upserted,
            execution.Deleted,
            execution.Excluded,
            execution.Failed,
            HasMore: page.HasMore,
            LastPrimaryKey: page.LastGroupKey,
            execution.NoOpRows,
            execution.RemoteAttemptedRows,
            execution.RemoteFailedRows,
            execution.MaxObservedInFlightRows,
            execution.RequestCharge,
            execution.ThrottledRequests,
            execution.RetryAfter,
            execution.CosmosOperationTime,
            execution.BookkeepingTime,
            execution.BookkeepingTransactions,
            execution.BookkeepingOutcomeRows,
            execution.MaxRowsPerBookkeepingTransaction,
            GroupsRead: page.Groups.Count,
            SourceRowsLoaded: page.Groups.Sum(group => group.Rows.Count),
            GroupsRecomputed: page.Groups.Count);
    }

    private static RowPlan PlanGroup(ReplicationGroup group, CosmosFamilyMapping family)
    {
        var grouping = family.Grouping!;
        foreach (var sourceRow in group.Rows)
        {
            if (!string.Equals(grouping.StorageKey(sourceRow.Row), group.GroupKey, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Typed group key disagrees with DuckDB for source row '{sourceRow.Row.PrimaryKey}'.");
            }
        }

        var latestState = group.Rows
            .Where(item => item.Row.ReplicatedAt is not null)
            .OrderByDescending(item => item.Row.ReplicatedAt)
            .Select(item => ReplicationStamp.Parse(item.Row.ReplicationStamp))
            .FirstOrDefault()
            ?? new ReplicationStamp();
        latestState.Families.TryGetValue(family.Family, out var oldCoordinates);

        var document = grouping.Project(group.LiveRows);
        if (document is null)
        {
            var deletes = oldCoordinates is null
                ? new List<(CosmosFamilyMapping, ReplicationStamp.StampCoordinates)>()
                : [(family, oldCoordinates)];
            return new RowPlan(
                deletes,
                [],
                IsExcluded: oldCoordinates is null,
                NewStampJson: new ReplicationStamp().ToJson());
        }

        var documentHash = CosmosDocHash.Compute(document);
        var newStamp = new ReplicationStamp();
        newStamp.Families[family.Family] = ReplicationStamp.ToCoordinates(document, documentHash);
        var deletesForMove = oldCoordinates is not null
                             && !ReplicationStamp.CoordinatesEqual(oldCoordinates, document)
            ? new List<(CosmosFamilyMapping, ReplicationStamp.StampCoordinates)> { (family, oldCoordinates) }
            : [];
        var upserts = oldCoordinates is null
                      || !ReplicationStamp.DocumentEqual(oldCoordinates, document, documentHash)
            ? new List<(CosmosFamilyMapping, CosmosDocument)> { (family, document) }
            : [];

        return new RowPlan(deletesForMove, upserts, IsExcluded: false, newStamp.ToJson());
    }

    private sealed class RowWork(
        DirtyRow row,
        RowPlan? plan,
        Exception? preflightFailure = null,
        IReadOnlyList<DirtyRow>? stateRows = null)
    {
        public DirtyRow Row { get; } = row;
        public IReadOnlyList<DirtyRow> StateRows { get; } = stateRows ?? [row];
        public RowPlan? Plan { get; } = plan;
        public Exception? PreflightFailure { get; set; } = preflightFailure;
    }

    private sealed record RowOutcome(
        RowWork Work,
        Exception? Failure,
        bool RemoteAttempted,
        int Upserted,
        int Deleted,
        double RequestCharge,
        int ThrottledRequests,
        TimeSpan RetryAfter,
        TimeSpan CosmosOperationTime);

    private sealed record OperationAttempt(
        Exception? Failure,
        bool RemoteStarted,
        double RequestCharge,
        bool Throttled,
        TimeSpan RetryAfter,
        TimeSpan Elapsed);

    private sealed class OwnershipGuardException(Exception innerException)
        : Exception("Replication ownership could not be proven.", innerException);

    private sealed record BatchExecution(
        int Upserted,
        int Deleted,
        int Excluded,
        int Failed,
        int NoOpRows,
        int RemoteAttemptedRows,
        int RemoteFailedRows,
        int MaxObservedInFlightRows,
        double RequestCharge,
        int ThrottledRequests,
        TimeSpan RetryAfter,
        TimeSpan CosmosOperationTime,
        TimeSpan BookkeepingTime,
        int BookkeepingTransactions,
        int BookkeepingOutcomeRows,
        int MaxRowsPerBookkeepingTransaction);

    private sealed class InFlightTracker
    {
        private int current;
        private int maximum;

        public int Maximum => Volatile.Read(ref maximum);

        public void Enter()
        {
            var observed = Interlocked.Increment(ref current);
            var previous = Volatile.Read(ref maximum);
            while (observed > previous)
            {
                var replaced = Interlocked.CompareExchange(ref maximum, observed, previous);
                if (replaced == previous)
                    break;
                previous = replaced;
            }
        }

        public void Exit() => Interlocked.Decrement(ref current);
    }

    private sealed record DestinationCoordinate(
        string Database,
        string Container,
        string Id,
        string PartitionKey);

    private (IReadOnlyList<RowWork> Work, FrozenDictionary<CosmosContainerKey, ICosmosSnapshotContainer> Containers)
        PlanAndResolveBatch(
            IReadOnlyList<DirtyRow> batch,
            IReadOnlyList<CosmosFamilyMapping> families,
            IReadOnlyList<RowWork>? preparedWork = null)
    {
        var familiesByName = families.ToDictionary(family => family.Family, StringComparer.Ordinal);
        var work = preparedWork?.ToList() ?? new List<RowWork>(batch.Count);
        if (preparedWork is null)
        {
            foreach (var row in batch)
            {
                try
                {
                    work.Add(new RowWork(row, PlanRow(row, families, familiesByName)));
                }
                catch (Exception exception)
                {
                    work.Add(new RowWork(
                        row,
                        plan: null,
                        new InvalidOperationException(
                            $"Cosmos mapping failed for source row '{row.PrimaryKey}': {exception.Message}",
                            exception)));
                }
            }
        }

        // A physical destination coordinate may be touched by only one operation in this
        // materialized batch. This includes delete-vs-upsert conflicts. Every implicated
        // row fails closed before any of its remote operations start; independent rows can
        // still make progress.
        var coordinateOwners = new Dictionary<DestinationCoordinate, List<int>>();
        for (var index = 0; index < work.Count; index++)
        {
            var plan = work[index].Plan;
            if (plan is null)
                continue;

            IReadOnlyList<DestinationCoordinate> coordinates;
            try
            {
                coordinates = EnumerateCoordinates(plan).ToList();
            }
            catch (Exception exception)
            {
                work[index].PreflightFailure = new InvalidOperationException(
                    "A mapped Cosmos coordinate could not be validated. The row was not submitted remotely: " +
                    exception.Message,
                    exception);
                continue;
            }

            foreach (var coordinate in coordinates)
            {
                if (!coordinateOwners.TryGetValue(coordinate, out var owners))
                {
                    owners = [];
                    coordinateOwners.Add(coordinate, owners);
                }
                owners.Add(index);
            }
        }

        foreach (var owners in coordinateOwners.Values.Where(owners => owners.Count > 1))
        {
            foreach (var index in owners.Distinct())
            {
                work[index].PreflightFailure ??= new InvalidOperationException(
                    "A Cosmos destination coordinate is produced by more than one operation in this batch. " +
                    "The implicated row was not submitted remotely.");
            }
        }

        var containerKeys = work
            .Where(item => item.PreflightFailure is null && item.Plan is not null)
            .SelectMany(item => EnumerateContainerKeys(item.Plan!))
            .Distinct()
            .ToList();
        var resolved = new Dictionary<CosmosContainerKey, ICosmosSnapshotContainer>();

        foreach (var key in containerKeys)
        {
            try
            {
                resolved.Add(key, cosmosTransport!.GetContainer(key.Database, key.Container));
            }
            catch (Exception exception)
            {
                foreach (var item in work.Where(item =>
                             item.PreflightFailure is null
                             && item.Plan is not null
                             && EnumerateContainerKeys(item.Plan).Contains(key)))
                {
                    item.PreflightFailure = new InvalidOperationException(
                        "A Cosmos container handle could not be resolved. The row was not submitted remotely: " +
                        exception.Message,
                        exception);
                }
            }
        }

        return (work, resolved.ToFrozenDictionary());
    }

    private async Task<BatchExecution> ExecuteWetBatchAsync(
        IReadOnlyList<RowWork> work,
        FrozenDictionary<CosmosContainerKey, ICosmosSnapshotContainer> containers,
        CosmosSnapshotReplicatorOptions options,
        CancellationToken cancellationToken)
    {
        int upserted = 0, deleted = 0, excluded = 0, failed = 0, noOpRows = 0;
        int remoteAttempted = 0, remoteFailed = 0, throttled = 0;
        double requestCharge = 0;
        TimeSpan retryAfter = default, cosmosTime = default, bookkeepingTime = default;
        var tracker = new InFlightTracker();
        var active = new List<Task<RowOutcome>>(Math.Min(options.MaxInFlightRows, work.Count));
        var next = 0;
        var bookkeepingTransactions = 0;
        var bookkeepingOutcomeRows = 0;
        var maxRowsPerBookkeepingTransaction = 0;

        using var workerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var workerToken = workerCts.Token;

        void AdmitOne()
        {
            workerToken.ThrowIfCancellationRequested();
            EnsureOwnership(options);
            active.Add(ExecuteRowAsync(work[next++], containers, options, tracker, workerToken));
        }

        try
        {
            while (next < work.Count)
            {
                // Admit one bounded wave, then wait until every outcome in that wave is
                // terminal before committing it as one set. No replacement is admitted
                // until the owner has committed (or rolled back) the complete wave, so the
                // number of remote-success/local-dirty rows is never above the active degree.
                while (next < work.Count && active.Count < options.MaxInFlightRows)
                    AdmitOne();

                var outcomes = new List<RowOutcome>(active.Count);
                while (active.Count > 0)
                {
                    var completed = await Task.WhenAny(active);
                    active.Remove(completed);
                    outcomes.Add(await completed);
                }

                var stateOutcomes = outcomes.SelectMany(outcome =>
                    outcome.Work.StateRows.Select(row =>
                        outcome.Failure is null
                            ? ReplicationStateOutcome.Replicated(
                                row.PrimaryKey,
                                row.CapturedLastModified,
                                outcome.Work.Plan!.NewStampJson)
                            : ReplicationStateOutcome.Failed(
                                row.PrimaryKey,
                                row.CapturedLastModified,
                                outcome.Failure.Message)))
                    .ToList();
                var commitStarted = Stopwatch.GetTimestamp();
                store.CommitReplicationOutcomes(options.Table, stateOutcomes, EnsureCommitAllowed);
                bookkeepingTime += Stopwatch.GetElapsedTime(commitStarted);
                bookkeepingTransactions++;
                bookkeepingOutcomeRows += stateOutcomes.Count;
                maxRowsPerBookkeepingTransaction = Math.Max(
                    maxRowsPerBookkeepingTransaction,
                    stateOutcomes.Count);

                foreach (var outcome in outcomes)
                {
                    if (outcome.Failure is null)
                    {
                        if (outcome.Work.Plan!.IsExcluded)
                            excluded += outcome.Work.StateRows.Count;
                        if (!outcome.RemoteAttempted)
                            noOpRows += outcome.Work.StateRows.Count;
                    }
                    else
                    {
                        failed += outcome.Work.StateRows.Count;
                    }

                    upserted += outcome.Upserted;
                    deleted += outcome.Deleted;
                    requestCharge += outcome.RequestCharge;
                    throttled += outcome.ThrottledRequests;
                    retryAfter += outcome.RetryAfter;
                    cosmosTime += outcome.CosmosOperationTime;
                    if (outcome.RemoteAttempted)
                    {
                        remoteAttempted++;
                        if (outcome.Failure is not null)
                            remoteFailed++;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            // Stop admission and propagate the linked token into every in-flight Cosmos
            // call. Observe all tasks before returning so no worker survives its gate owner.
            workerCts.Cancel();
            try
            {
                await Task.WhenAll(active);
            }
            catch
            {
                // The original cancellation/store exception is the useful one.
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }

        return new BatchExecution(
            upserted,
            deleted,
            excluded,
            failed,
            noOpRows,
            remoteAttempted,
            remoteFailed,
            tracker.Maximum,
            requestCharge,
            throttled,
            retryAfter,
            cosmosTime,
            bookkeepingTime,
            bookkeepingTransactions,
            bookkeepingOutcomeRows,
            maxRowsPerBookkeepingTransaction);

        void EnsureCommitAllowed()
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureOwnership(options);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task<RowOutcome> ExecuteRowAsync(
        RowWork work,
        FrozenDictionary<CosmosContainerKey, ICosmosSnapshotContainer> containers,
        CosmosSnapshotReplicatorOptions options,
        InFlightTracker tracker,
        CancellationToken cancellationToken)
    {
        if (work.PreflightFailure is not null)
            return new RowOutcome(work, work.PreflightFailure, RemoteAttempted: false, 0, 0, 0, 0, default, default);

        var plan = work.Plan!;
        if (plan.Deletes.Count == 0 && plan.Upserts.Count == 0)
            return new RowOutcome(work, Failure: null, RemoteAttempted: false, 0, 0, 0, 0, default, default);

        int upserted = 0, deleted = 0, throttled = 0;
        var remoteStarted = false;
        double requestCharge = 0;
        TimeSpan retryAfter = default, cosmosTime = default;
        tracker.Enter();
        try
        {
            // Operation order is row-local and strict: old/tombstoned coordinates first,
            // then current documents. A partial failure is safe to retry because every
            // operation is an idempotent point operation.
            foreach (var (family, coordinates) in plan.Deletes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnsureOwnership(options);
                var container = containers[new CosmosContainerKey(family.Database, family.Container)];
                var attempt = await ExecuteOperationAsync(
                    options,
                    CosmosReplicationOperationKind.Delete,
                    family,
                    coordinates.Id,
                    token => container.DeleteAsync(
                        coordinates.Id,
                        coordinates.PartitionKey.Select(FromJson).ToList(),
                        token),
                    cancellationToken);
                Accumulate(attempt);
                if (attempt.Failure is not null)
                    return Outcome(attempt.Failure);
                deleted++;
            }

            foreach (var (family, document) in plan.Upserts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnsureOwnership(options);
                var container = containers[new CosmosContainerKey(family.Database, family.Container)];
                var attempt = await ExecuteOperationAsync(
                    options,
                    CosmosReplicationOperationKind.Upsert,
                    family,
                    document.Id,
                    token => container.UpsertAsync(document, token),
                    cancellationToken);
                Accumulate(attempt);
                if (attempt.Failure is not null)
                    return Outcome(attempt.Failure);
                upserted++;
            }

            return Outcome(failure: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation/gate loss is not a row failure and never touches the ledger.
            throw;
        }
        catch (OwnershipGuardException)
        {
            // Ownership is a batch-wide safety boundary, never per-row poison.
            throw;
        }
        catch (Exception exception)
        {
            return Outcome(exception);
        }
        finally
        {
            tracker.Exit();
        }

        void Accumulate(OperationAttempt attempt)
        {
            remoteStarted |= attempt.RemoteStarted;
            requestCharge += attempt.RequestCharge;
            if (attempt.Throttled) throttled++;
            retryAfter += attempt.RetryAfter;
            cosmosTime += attempt.Elapsed;
        }

        RowOutcome Outcome(Exception? failure) => new(
            work,
            failure,
            RemoteAttempted: remoteStarted,
            upserted,
            deleted,
            requestCharge,
            throttled,
            retryAfter,
            cosmosTime);
    }

    private static IEnumerable<CosmosContainerKey> EnumerateContainerKeys(RowPlan plan) =>
        plan.Deletes.Select(item => item.Family)
            .Concat(plan.Upserts.Select(item => item.Family))
            .Select(family => new CosmosContainerKey(family.Database, family.Container));

    private static IEnumerable<DestinationCoordinate> EnumerateCoordinates(RowPlan plan)
    {
        foreach (var (family, coordinates) in plan.Deletes)
        {
            yield return new DestinationCoordinate(
                family.Database,
                family.Container,
                coordinates.Id,
                PartitionKeyFingerprint(coordinates.PartitionKey.Select(FromJson)));
        }

        foreach (var (family, document) in plan.Upserts)
        {
            yield return new DestinationCoordinate(
                family.Database,
                family.Container,
                document.Id,
                PartitionKeyFingerprint(document.PartitionKey));
        }
    }

    private static string PartitionKeyFingerprint(IEnumerable<object?> levels) =>
        string.Join("|", levels.Select(level => level switch
        {
            null => "z:",
            string value => "s:" + JsonSerializer.Serialize(value),
            bool value => value ? "b:1" : "b:0",
            _ => "n:" + Convert.ToDouble(level, CultureInfo.InvariantCulture)
                .ToString("R", CultureInfo.InvariantCulture),
        }));

    private async Task<OperationAttempt> ExecuteOperationAsync(
        CosmosSnapshotReplicatorOptions options,
        CosmosReplicationOperationKind operation,
        CosmosFamilyMapping family,
        string documentId,
        Func<CancellationToken, Task<CosmosTransportResponse>> execute,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        CosmosTransportResponse response;
        try
        {
            response = await execute(cancellationToken);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            EmitMetric(options, new CosmosReplicationOperationMetric(
                operation, family.Family, documentId, null, Succeeded: false, Canceled: true,
                0, default, elapsed, exception.Message));
            throw;
        }
        catch (Exception exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            var cosmosException = exception as CosmosException;
            var statusCode = cosmosException?.StatusCode;
            var charge = cosmosException?.RequestCharge ?? 0;
            var retry = cosmosException?.RetryAfter ?? default;
            EmitMetric(options, new CosmosReplicationOperationMetric(
                operation, family.Family, documentId, statusCode, Succeeded: false, Canceled: false,
                charge, retry, elapsed, exception.Message));
            return new OperationAttempt(
                exception,
                RemoteStarted: exception is not CosmosRequestPreparationException,
                charge,
                statusCode == HttpStatusCode.TooManyRequests,
                retry,
                elapsed);
        }

        var operationElapsed = Stopwatch.GetElapsedTime(started);
        var accepted = response.IsSuccessStatusCode
                       || operation == CosmosReplicationOperationKind.Delete
                       && response.StatusCode == HttpStatusCode.NotFound;
        Exception? failure = accepted
            ? null
            : new InvalidOperationException(
                $"{operation} {family.Family}/{documentId} failed: " +
                $"{response.StatusCode} {response.ErrorMessage}");

        EmitMetric(options, new CosmosReplicationOperationMetric(
            operation,
            family.Family,
            documentId,
            response.StatusCode,
            accepted,
            Canceled: false,
            response.RequestCharge,
            response.RetryAfter ?? default,
            operationElapsed,
            failure?.Message));

        return new OperationAttempt(
            failure,
            RemoteStarted: true,
            response.RequestCharge,
            response.StatusCode == HttpStatusCode.TooManyRequests,
            response.RetryAfter ?? default,
            operationElapsed);
    }

    private static void EmitMetric(
        CosmosSnapshotReplicatorOptions options,
        CosmosReplicationOperationMetric metric)
    {
        try
        {
            options.OnOperation?.Invoke(metric);
        }
        catch
        {
            // Metrics must never change replication behavior.
        }
    }

    private static void EnsureOwnership(CosmosSnapshotReplicatorOptions options)
    {
        try
        {
            options.OwnershipGuard?.Invoke();
        }
        catch (OwnershipGuardException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new OwnershipGuardException(exception);
        }
    }

    private static void ValidateOptions(CosmosSnapshotReplicatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Table);
        ArgumentNullException.ThrowIfNull(options.Families);

        if (options.BatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.BatchSize), "Batch size must be positive.");
        if (options.MaxInFlightRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxInFlightRows), "Max in-flight rows must be positive.");

        foreach (var family in options.Families)
        {
            if ((family.Map is null) == (family.Grouping is null))
            {
                throw new ArgumentException(
                    $"Cosmos family '{family.Family}' must configure exactly one of Map or Grouping.",
                    nameof(options));
            }
        }

        var grouped = options.Families.Where(family => family.Grouping is not null).ToList();
        if (grouped.Count > 0)
        {
            if (options.Families.Count != 1)
                throw new ArgumentException(
                    "A grouped table currently maps to exactly one Cosmos family.", nameof(options));
            if (grouped[0].Predicate is not null)
                throw new ArgumentException(
                    "Grouped families express exclusion by returning null from their typed reducer; Predicate is row-oriented.",
                    nameof(options));
            if (!ReferenceEquals(grouped[0].Grouping!.Table, options.Table))
                throw new ArgumentException(
                    $"Grouped family '{grouped[0].Family}' was declared for table " +
                    $"'{grouped[0].Grouping!.Table.Name}', not '{options.Table.Name}'.",
                    nameof(options));
        }

        var duplicateFamily = options.Families
            .GroupBy(family => family.Family, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFamily is not null)
        {
            throw new ArgumentException(
                $"Cosmos family '{duplicateFamily.Key}' is declared more than once for table '{options.Table.Name}'.",
                nameof(options));
        }
    }

    /// <summary>
    /// The dry-run path: pages the ENTIRE dirty set (nothing gets stamped, so a limit-only
    /// read would return the same rows forever) and emits every intended op to
    /// <c>meta.ReconOps</c> under one run id.
    /// </summary>
    private ReplicationRunResult ExecuteDryRun(
        string runId, CosmosSnapshotReplicatorOptions options, CancellationToken cancellationToken)
    {
        if (options.PruneReconOps)
            store.PruneReconOps(options.Table);

        int rowsRead = 0, upserted = 0, deleted = 0, excluded = 0;
        string? cursor = null;
        var familiesByName = options.Families.ToDictionary(family => family.Family, StringComparer.Ordinal);

        while (true)
        {
            var batch = store.ReadDirtyRows(options.Table, cursor, options.BatchSize);
            if (batch.Count == 0)
                break;

            foreach (var row in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var plan = PlanRow(row, options.Families, familiesByName);
                EmitReconOps(runId, options.Table, row, plan);
                upserted += plan.Upserts.Count;
                deleted += plan.Deletes.Count;
                if (plan.IsExcluded) excluded++;
            }

            rowsRead += batch.Count;
            cursor = batch[^1].PrimaryKey;

            if (batch.Count < options.BatchSize)
                break;
        }

        return new ReplicationRunResult(
            runId, DryRun: true, rowsRead, upserted, deleted, excluded, Failed: 0, HasMore: false);
    }

    private ReplicationRunResult ExecuteGroupedDryRun(
        string runId,
        CosmosSnapshotReplicatorOptions options,
        CosmosFamilyMapping family,
        CancellationToken cancellationToken)
    {
        if (options.PruneReconOps)
            store.PruneReconOps(options.Table);

        int dirtyRowsRead = 0, groupsRead = 0, sourceRowsLoaded = 0;
        int upserted = 0, deleted = 0, excluded = 0;
        string? cursor = null;
        while (true)
        {
            var page = store.ReadReplicationGroups(
                options.Table,
                family.Grouping!,
                cursor,
                options.BatchSize,
                dirtyGroupsOnly: true);
            if (page.Groups.Count == 0)
                break;

            foreach (var group in page.Groups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var plan = PlanGroup(group, family);
                var dirtyRows = group.DirtyRows;
                EmitReconOps(runId, options.Table, dirtyRows[0], plan);
                dirtyRowsRead += dirtyRows.Count;
                sourceRowsLoaded += group.Rows.Count;
                groupsRead++;
                upserted += plan.Upserts.Count;
                deleted += plan.Deletes.Count;
                if (plan.IsExcluded)
                    excluded += dirtyRows.Count;
            }

            cursor = page.LastGroupKey;
            if (!page.HasMore)
                break;
        }

        return new ReplicationRunResult(
            runId,
            DryRun: true,
            RowsRead: dirtyRowsRead,
            upserted,
            deleted,
            excluded,
            Failed: 0,
            HasMore: false,
            GroupsRead: groupsRead,
            SourceRowsLoaded: sourceRowsLoaded,
            GroupsRecomputed: groupsRead);
    }

    // ---- Planning ---------------------------------------------------------------------------

    private sealed record RowPlan(
        List<(CosmosFamilyMapping Family, ReplicationStamp.StampCoordinates Coordinates)> Deletes,
        List<(CosmosFamilyMapping Family, CosmosDocument Document)> Upserts,
        bool IsExcluded,
        string? NewStampJson);

    private static RowPlan PlanRow(
        DirtyRow row,
        IReadOnlyList<CosmosFamilyMapping> families,
        IReadOnlyDictionary<string, CosmosFamilyMapping> familiesByName)
    {
        var oldStamp = ReplicationStamp.Parse(row.ReplicationStamp);

        if (row.Deleted)
        {
            // Tombstone: point-delete everything the stamp says exists. Never replicated
            // (empty stamp) = nothing to delete — the tombstone completes trivially.
            var tombstoneDeletes = oldStamp.Families
                .Where(pair => familiesByName.ContainsKey(pair.Key))
                .Select(pair => (familiesByName[pair.Key], pair.Value))
                .ToList();

            return new RowPlan(tombstoneDeletes, [], IsExcluded: false,
                NewStampJson: new ReplicationStamp().ToJson());
        }

        var matched = families
            .Where(f => f.Predicate?.Invoke(row) != false)
            .Select(f =>
            {
                var document = f.Map!(row);
                return (Family: f, Document: document, DocumentHash: CosmosDocHash.Compute(document));
            })
            .ToList();

        var newStamp = new ReplicationStamp();
        foreach (var (family, document, documentHash) in matched)
            newStamp.Families[family.Family] = ReplicationStamp.ToCoordinates(document, documentHash);

        // Old coordinates that no longer exist (family no longer matches) or that moved
        // (id/PK changed) get deleted; current documents get upserted.
        var deletes = new List<(CosmosFamilyMapping, ReplicationStamp.StampCoordinates)>();
        foreach (var (familyName, oldCoordinates) in oldStamp.Families)
        {
            if (!familiesByName.TryGetValue(familyName, out var family))
                continue;

            var current = matched.FirstOrDefault(m => m.Family.Family == familyName);
            if (current.Family is null || !ReplicationStamp.CoordinatesEqual(oldCoordinates, current.Document))
                deletes.Add((family, oldCoordinates));
        }

        var upserts = matched
            .Where(current =>
                !oldStamp.Families.TryGetValue(current.Family.Family, out var previous)
                || !ReplicationStamp.DocumentEqual(previous, current.Document, current.DocumentHash))
            .Select(current => (current.Family, current.Document))
            .ToList();

        return new RowPlan(deletes, upserts, IsExcluded: matched.Count == 0, newStamp.ToJson());
    }

    // ---- Dry-run recon ----------------------------------------------------------------------

    private void EmitReconOps(string runId, SnapshotTableDefinition table, DirtyRow row, RowPlan plan)
    {
        var emittedAt = DateTime.UtcNow;

        foreach (var (family, coordinates) in plan.Deletes)
        {
            store.AppendReconOp(new ReplicationReconOperation(
                runId,
                table.Name,
                family.Family,
                row.PrimaryKey,
                "Delete",
                coordinates.Id,
                JoinPartitionKey(coordinates.PartitionKey.Select(element => element.ToString())),
                null,
                row.CapturedLastModified,
                emittedAt));
        }

        foreach (var (family, document) in plan.Upserts)
        {
            store.AppendReconOp(new ReplicationReconOperation(
                runId,
                table.Name,
                family.Family,
                row.PrimaryKey,
                "Upsert",
                document.Id,
                JoinPartitionKey(document.PartitionKey.Select(value => value?.ToString())),
                CosmosDocHash.Compute(document),
                row.CapturedLastModified,
                emittedAt));
        }

        if (plan.IsExcluded)
        {
            store.AppendReconOp(new ReplicationReconOperation(
                runId,
                table.Name,
                "(none)",
                row.PrimaryKey,
                "Excluded",
                null,
                null,
                null,
                row.CapturedLastModified,
                emittedAt));
        }
    }

    private static string JoinPartitionKey(IEnumerable<string?> levels) => string.Join("|", levels);

    private static object? FromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.GetRawText(),
    };

}
