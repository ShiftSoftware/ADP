using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Hawta;

public sealed class SnapshotAgentOptions
{
    public required SourceRegistry Registry { get; init; }

    /// <summary>The write DB path — instance-local disk, never a network share.</summary>
    public required string WriteDatabasePath { get; init; }

    /// <summary>
    /// Where DuckDB caches downloaded extensions. Null keeps DuckDB's own default, which is
    /// right for dev. Point it at persistent storage on any host whose default is ephemeral —
    /// see <see cref="SnapshotStoreOptions.ExtensionDirectory"/> for why that matters.
    /// </summary>
    public string? ExtensionDirectory { get; init; }

    /// <summary>
    /// Read-only directories holding extensions that shipped with the deployment. Set this and the
    /// agent never fetches an extension at runtime.
    /// See <see cref="SnapshotStoreOptions.ExtensionDirectories"/> for the required layout.
    /// </summary>
    public IReadOnlyList<string>? ExtensionDirectories { get; init; }

    /// <summary>
    /// Azure Storage connection string for DuckDB's own <c>az://</c> access, when
    /// <see cref="PublishStore"/> is a container. Set it from the same configuration value as the
    /// store's — the two halves authenticate independently and configuring only one fails at the
    /// first export, not at startup.
    /// </summary>
    public string? AzureConnectionString { get; init; }

    /// <summary>
    /// Runs against every write-DB connection the loop opens, before the schema bootstrap — where a
    /// host registers the DuckDB scalar functions its projection sources call. See
    /// <see cref="SnapshotStoreOptions.ConfigureConnection"/>.
    /// </summary>
    public Action<DuckDB.NET.Data.DuckDBConnection>? ConfigureStoreConnection { get; init; }

    /// <summary>The read tier's location (local folder in dev, the blob container in prod).</summary>
    public required string PublishDirectory { get; init; }

    /// <summary>
    /// Where the publish tier reads and writes. Null means a plain local directory at
    /// <see cref="PublishDirectory"/> — the incumbent behaviour. Set it to a
    /// <see cref="BlobPublishStore"/> to publish into a container.
    ///
    /// <para>One store serves the whole cycle: the publisher, the in-process retention sweep and
    /// the cold-start rebuild all take THIS instance. They must never resolve their own, or a
    /// deployment could end up publishing to one location and rebuilding from another — which
    /// presents as an empty estate after a swap, not as a configuration error.</para>
    /// </summary>
    public PublishStore? PublishStore { get; init; }

    public required string SnapshotName { get; init; }

    /// <summary>Minimum interval between publishes. The publisher itself skips unchanged sets.</summary>
    public TimeSpan PublishCadence { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Dark-launch mode: ingest + publish only — the Cosmos pump does not run at all.
    /// (Dry-run PUMPS are an on-demand recon operation via
    /// <see cref="CosmosSnapshotReplicatorOptions.DryRun"/>, not something the loop repeats
    /// every cycle: nothing gets stamped, so each cycle would re-plan the entire dirty set.)
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// Null = ungated: nothing serialises two agent instances writing the same estate. Safe
    /// only for a deployment that provably runs one process. Any multi-instance deployment —
    /// including slot swaps, whose old and new instances overlap — must configure a gate.
    /// </summary>
    public SnapshotWriteGateOptions? WriteGate { get; init; }

    /// <summary>Per-table parquet export sort, passed through to the publisher.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? SortColumns { get; init; }

    /// <summary>Published sets kept by retention — a recovery window, not a storage knob. See <see cref="SnapshotPublishOptions.KeepPublishes"/>.</summary>
    public int KeepPublishes { get; init; } = 3;

    /// <summary>Drain bound: pump batches per table per cycle (a stuck-failing batch dead-letters after 5 attempts anyway).</summary>
    public int MaxPumpBatchesPerCycle { get; init; } = 100;

    /// <summary>
    /// Concurrent FETCHES per cycle, for sources that declare <see cref="SnapshotSource.Fetch"/>.
    /// The merge drain is serial and in registry order at every degree.
    ///
    /// <para><b>1 — the default — is exactly today's behaviour</b>, so adopting this package
    /// changes nothing until a host opts in, and setting the host's knob back to 1 is the kill
    /// switch: production drops to serial fetching with a config change and no redeploy.</para>
    /// </summary>
    public int IngestDegree { get; init; } = 1;

    /// <summary>Fetched-but-unmerged rows above which admission stops. See <see cref="SnapshotIngestDispatcherOptions.MaxBufferedRows"/>.</summary>
    public int IngestMaxBufferedRows { get; init; } = 100_000;

    /// <summary>Concurrent fetches per <see cref="SnapshotSource.ConcurrencyGroup"/> — the per-remote-box cap.</summary>
    public int IngestMaxPerConcurrencyGroup { get; init; } = 2;

    /// <summary>Injectable clock for tests.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>Upper bound on idle sleep between scheduler checks.</summary>
    public TimeSpan MaxIdleWait { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>How long to back off when the gate is held elsewhere or a cycle crashed at store level.</summary>
    public TimeSpan GateRetryWait { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Observability callback (the host adapts to its logger). Exceptions in the callback are swallowed.</summary>
    public Action<SnapshotAgentEvent>? OnEvent { get; init; }

    /// <summary>
    /// The run-log exporter. Null keeps it off, the incumbent behaviour. When set, the loop copies
    /// the run tables (<c>meta.SyncRuns</c>, <c>meta.PublishRuns</c>, <c>meta.CycleRuns</c>,
    /// <c>meta.PumpRuns</c>, <c>meta.FetchRuns</c>) to parquet on <see cref="SnapshotRunLogOptions.Cadence"/>, once more
    /// at shutdown, and never per cycle. Facts only, into a location of its own, never into the
    /// published set. A flush that fails is a warning, never a failed cycle. Once a day, after a
    /// cadence flush, the loop also prunes flushed rows of earlier days out of the write DB
    /// (<see cref="SnapshotRunLog.Prune"/>); with the run log off nothing is ever pruned, because
    /// nothing was ever copied out. See <see cref="SnapshotRunLog"/>.
    /// </summary>
    public SnapshotRunLogOptions? RunLog { get; init; }

    /// <summary>
    /// What the pump writes through instead of a Cosmos client: an in-memory container for the
    /// engine's own tests and for the host repository's drills, which need a wet cycle with no
    /// Cosmos anywhere. Null in production, where the client given to the loop is the transport.
    /// </summary>
    internal ICosmosSnapshotTransport? ReplicationTransport { get; init; }
}

public enum SnapshotAgentEventLevel { Info, Warning, Error }

public sealed record SnapshotAgentEvent(
    SnapshotAgentEventLevel Level,
    string Message,
    string? SourceKey = null,
    Exception? Exception = null);

/// <summary>One ingest attempt within a cycle.</summary>
public sealed record SnapshotAgentSourceRun(string SourceKey, SnapshotMergeResult? Merge, Exception? Error);

/// <summary>One table's pump drain within a cycle (accumulated across batches).</summary>
/// <param name="StopReason">Why the drain stopped, as <see cref="ReplicationDrainStop"/> names it. Null when the caller did not record one.</param>
/// <param name="StartedAt">System-clock UTC time the drain started. Default when the caller did not record one.</param>
/// <param name="FinishedAt">System-clock UTC time the drain finished. Default when the caller did not record one.</param>
public sealed record SnapshotAgentPumpRun(
    string Table,
    int Batches,
    int RowsRead,
    int Upserted,
    int Deleted,
    int Excluded,
    int Failed,
    bool Drained,
    int RemoteAttemptedRows = 0,
    int RemoteFailedRows = 0,
    int MaxObservedInFlightRows = 0,
    double RequestCharge = 0,
    int ThrottledRequests = 0,
    TimeSpan CosmosOperationTime = default,
    TimeSpan BookkeepingTime = default,
    int GroupsRead = 0,
    int SourceRowsLoaded = 0,
    int GroupsRecomputed = 0,
    int DeadLettered = 0,
    string? StopReason = null,
    DateTime StartedAt = default,
    DateTime FinishedAt = default);

/// <summary>What one cycle did.</summary>
public sealed record SnapshotAgentCycle(
    bool GateAcquired,
    bool ColdStartRebuild,
    IReadOnlyList<SnapshotAgentSourceRun> Sources,
    IReadOnlyList<SnapshotAgentPumpRun> Pumps,
    SnapshotPublishResult? Publish)
{
    public static readonly SnapshotAgentCycle Idle = new(true, false, [], [], null);
    public static readonly SnapshotAgentCycle GateUnavailable = new(false, false, [], [], null);

    /// <summary>The id of this cycle's row in <c>meta.CycleRuns</c>. Null for an idle cycle, which records nothing.</summary>
    public string? CycleId { get; init; }

    /// <summary>
    /// What the run log did at the end of this cycle. Null when no flush was due, when the run log
    /// is off, and when the flush failed (that is reported as a warning event instead).
    /// </summary>
    public SnapshotRunLogFlushResult? RunLog { get; init; }
}

/// <summary>
/// The dispatcher: per-source cadences, each cycle bracketed by the write gate. A cycle is:
/// gate → cold-start rebuild if the write DB is fresh → ingest every due source → pump the
/// affected tables (wet mode) → publish on its own cadence → release.
///
/// <para><b>One sequential worker for everything that touches the store</b> — it is
/// single-connection and single-writer, and merges are ≤ 60 ms outside one 8.2 s outlier, so
/// there is nothing there worth parallelising. What IS worth parallelising is the wait in front
/// of it: fetch is 95.4–97.9 % of a cycle. Sources that declare
/// <see cref="SnapshotSource.Fetch"/> have that half dispatched ahead by
/// <see cref="SnapshotIngestDispatcher"/>, bounded by <see cref="SnapshotAgentOptions.IngestDegree"/>
/// and by rows in flight; the merge drain stays serial and in registry order, and every run
/// record, source stamp and piece of scheduler state is still written by it.</para>
/// Not thread-safe by contract: one loop instance, one caller at a time
/// (<see cref="RunAsync"/> is the caller in production; <see cref="RunSourceOnceAsync"/>
/// is for admin force-runs while the loop is NOT running the same instance).
/// </summary>
public sealed class SnapshotAgentLoop : IDisposable
{
    private readonly SnapshotAgentOptions options;
    private readonly CosmosClient? cosmosClient;
    private readonly Dictionary<string, DateTimeOffset> nextDue = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset nextPublish = DateTimeOffset.MinValue;

    // The run log's state. The boot time comes from the SYSTEM clock, not the TimeProvider: the
    // run tables are stamped with DateTime.UtcNow, and the "since boot" filter has to speak the
    // same clock. The first flush is due at once, so the first cycle after a boot leaves a trace;
    // after that the cadence applies. Cadence arithmetic uses the TimeProvider like the rest.
    private readonly DateTime bootStartedAt = DateTime.UtcNow;
    private readonly string bootId;
    private DateTimeOffset nextRunLogFlush = DateTimeOffset.MinValue;
    private DateTime? runLogFlushedThrough;
    private DateOnly? runLogPrunedOn;
    private long cycleSequence;

    private SnapshotStore? store;
    private CosmosSnapshotReplicator? replicator;

    public SnapshotAgentLoop(SnapshotAgentOptions options, CosmosClient? cosmosClient)
    {
        if (options.MaxPumpBatchesPerCycle <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.MaxPumpBatchesPerCycle),
                "Max pump batches per cycle must be positive.");

        options.RunLog?.Validate();

        this.options = options;
        this.cosmosClient = cosmosClient;
        bootId = SnapshotRunLog.NewBootId(bootStartedAt);

        if (!options.DryRun
            && cosmosClient is null
            && options.ReplicationTransport is null
            && options.Registry.Sources.Any(s => s is { Families.Count: > 0, ReplicationEnabled: true }))
        {
            throw new InvalidOperationException(
                "Wet mode with replicated tables requires a CosmosClient (set DryRun for dark-launch).");
        }

        // A Cosmos-READING source fails at STARTUP without a client, never per cadence tick, and
        // DryRun does not excuse it — DryRun darkens the pump, not the reads.
        //
        // Without this guard such a source has Families = null, so it slips past the check above:
        // the host starts clean, then throws on every tick, and its table publishes as well-formed
        // EMPTY parquet. A consumer cannot tell that from "this table genuinely has no rows", and
        // for a table whose absent rows mean "the dealer did not do it", the empty version is a
        // report that accuses everyone. Dark DMS dealers are allowed to ship dark because their
        // table has live siblings keeping it real; a Cosmos-only table has none.
        var cosmosReaders = options.Registry.Sources.Where(s => s.CosmosRead is not null).ToList();
        if (cosmosClient is null && cosmosReaders.Count > 0)
        {
            throw new InvalidOperationException(
                "Cosmos-reading source(s) require a CosmosClient: " +
                string.Join(", ", cosmosReaders.Select(s => $"'{s.Key}' ({s.CosmosRead})")) +
                ". A source that cannot reach its container must not be registered at all — turning it " +
                "off with Enabled still ensures, publishes and rebuilds an empty table.");
        }
    }

    /// <summary>The store, once a cycle has opened it (exposed for hosts' diagnostics endpoints).</summary>
    public SnapshotStore? Store => store;

    /// <summary>
    /// This process lifetime's id. It is the run log's file name, so a host that prints it in its
    /// startup line lets an operator match the log stream to the files.
    /// </summary>
    public string BootId => bootId;

    /// <summary>When this loop was constructed, by the system clock. Run-log rows older than this belong to an earlier boot.</summary>
    public DateTime BootStartedAt => bootStartedAt;

    /// <summary>Runs cycles until cancelled. Contains its own failures: a crashed cycle is an event + backoff, not a dead agent.</summary>
    /// <summary>
    /// True once a cycle has completed <b>with the gate held</b> — meaning the write DB opened,
    /// any cold-start rebuild from the published set finished, and the estate is serviceable.
    ///
    /// <para>This is the signal a slot-swap warm-up needs (D3). Every swap hands a new instance an
    /// empty local disk, so the incoming instance must rebuild before it can publish; pinging an
    /// endpoint that reports merely "the process started" would complete the swap during exactly
    /// the window the warm-up exists to cover, which is worse than not warming at all.</para>
    ///
    /// <para>A cycle that could not take the gate does NOT count: it did no work and proves
    /// nothing about this instance's readiness.</para>
    /// </summary>
    public bool HasCompletedAServiceableCycle => Volatile.Read(ref serviceableCycles) > 0;

    private int serviceableCycles;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SnapshotAgentCycle cycle;
            try
            {
                cycle = await RunCycleAsync(cancellationToken);
                if (cycle.GateAcquired)
                    Interlocked.Increment(ref serviceableCycles);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Emit(SnapshotAgentEventLevel.Error, "Cycle failed at store level; backing off.", exception: exception);
                if (!await WaitAsync(options.GateRetryWait, cancellationToken)) break;
                continue;
            }

            var wait = cycle.GateAcquired ? ComputeWait() : options.GateRetryWait;
            if (!await WaitAsync(wait, cancellationToken)) break;
        }

        // The final flush, once the loop has stopped and before the host disposes it. No gate is
        // held and none is needed: run-log files are per boot. One attempt, no retry. A lost tail
        // is bounded by the cadence and is visible in the log itself, because the next boot's rows
        // start a new file.
        FlushRunLog("shutdown");
    }

    /// <summary>
    /// One scheduler cycle: due sources only. Returns without work (and without touching
    /// the gate) when nothing is due.
    /// </summary>
    public async Task<SnapshotAgentCycle> RunCycleAsync(CancellationToken cancellationToken)
    {
        var now = options.TimeProvider.GetUtcNow();
        var due = options.Registry.Sources
            .Where(s => s.Enabled && NextDueFor(s.Key) <= now)
            .ToList();
        var publishDue = nextPublish <= now;

        if (due.Count == 0 && !publishDue)
            return SnapshotAgentCycle.Idle;

        return await RunAsync(due, publishDue, cancellationToken);
    }

    /// <summary>Admin force-run: one source now, cadence ignored, then pump + publish.</summary>
    public async Task<SnapshotAgentCycle> RunSourceOnceAsync(string key, CancellationToken cancellationToken)
    {
        var source = options.Registry[key];
        if (!source.Enabled)
            throw new InvalidOperationException(
                $"Source '{key}' is disabled (not onboarded / excluded by the host's allowlist) — a force-run " +
                "would bypass exactly the guard that keeps it dark.");

        return await RunAsync([source], publishDue: true, cancellationToken);
    }

    private async Task<SnapshotAgentCycle> RunAsync(
        IReadOnlyList<SnapshotSource> due, bool publishDue, CancellationToken cancellationToken)
    {
        // The cycle's row in meta.CycleRuns starts here, before the gate, so a cycle that could not
        // take the gate is recorded too (when a store is already open to record it in).
        var cycleStartedAt = DateTime.UtcNow;
        var cycleId = $"{bootId}-{Interlocked.Increment(ref cycleSequence):D6}";

        WriteGateLease? gate = null;
        if (options.WriteGate is not null)
        {
            gate = await SnapshotWriteGate.TryAcquireAsync(options.WriteGate, cancellationToken);
            if (gate is null)
            {
                Emit(SnapshotAgentEventLevel.Warning,
                    "Write gate held elsewhere — skipping this cycle (normal during deploy overlap).");
                RecordCycle(cycleId, cycleStartedAt, "GateUnavailable", coldStart: false, due.Count,
                    [], 0, 0, [], publish: null, error: null);
                return SnapshotAgentCycle.GateUnavailable;
            }
        }

        // Declared outside the try, so the cycle's row can be written from the catch blocks with
        // whatever the cycle managed to do before it failed.
        var coldStart = false;
        var sourceRuns = new List<SnapshotAgentSourceRun>();
        var pumpRuns = new List<SnapshotAgentPumpRun>();
        var ingestFetchedAhead = 0;
        var ingestPeakWidth = 0;
        SnapshotPublishResult? publish = null;

        try
        {
            using var linkedCts = gate is null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gate.LostToken);
            var token = linkedCts.Token;
            Action? ownershipGuard = gate is null ? null : new Action(gate.EnsureOwnership);

            coldStart = EnsureStore();

            // One probe per cycle, always — it is lazy, so a registry with no gated source never
            // touches the file system through it. Sharing it across the cycle is the point: every
            // source sees the same picture of the share, and no cycle inherits another's cache.
            var fileMetadata = new DirectoryListingFileMetadataProbe();

            var pumpTables = new Dictionary<string, (SnapshotTableDefinition Table, IReadOnlyList<CosmosFamilyMapping> Families, int BatchSize, int MaxInFlightRows)>(
                StringComparer.OrdinalIgnoreCase);

            // The fan-out. Sources that declare Fetch have their external half dispatched ahead,
            // bounded by degree AND by rows in flight; everything else runs inline exactly as it
            // always has. EVERY callback below lands on the drain's single thread, in registry
            // order, which is what keeps nextDue, sourceRuns and pumpTables plain unsynchronised
            // state — nextDue in particular is a loop FIELD whose corruption would be permanent
            // for the process, not for the cycle.
            var ingest = await SnapshotIngestDispatcher.RunAsync(
                new SnapshotIngestDispatcherOptions
                {
                    Store = store!,
                    Sources = due,
                    FileMetadata = fileMetadata,
                    Degree = options.IngestDegree,
                    MaxBufferedRows = options.IngestMaxBufferedRows,
                    MaxPerConcurrencyGroup = options.IngestMaxPerConcurrencyGroup,
                },
                outcome =>
                {
                    var source = outcome.Source;
                    nextDue[source.Key] = options.TimeProvider.GetUtcNow() + source.Cadence;

                    if (outcome.Failure is { } exception)
                    {
                        // The merge already wrote its Failed:Exception run record; the loop's job
                        // is to contain the failure so the other sources still run. A fetch that
                        // threw has a Failed:Fetch run record, and a drain that threw before its
                        // merge a Failed:Exception one, both written by the dispatcher's drain. A
                        // one-phase source that threw before reaching a merge wrote nothing, as
                        // it always has.
                        sourceRuns.Add(new SnapshotAgentSourceRun(source.Key, null, exception));
                        Emit(SnapshotAgentEventLevel.Error, "Ingest crashed.", source.Key, exception);
                        return;
                    }

                    var merge = outcome.Merge!;
                    sourceRuns.Add(new SnapshotAgentSourceRun(source.Key, merge, null));

                    // The gate and guard skips are the system working, not a problem — warning
                    // on them would make the healthy steady state the noisiest thing in the log.
                    if (!merge.Succeeded
                        && merge.Status is not (SnapshotMergeStatus.SkippedSourceAbsent
                            or SnapshotMergeStatus.SkippedSourceEmpty
                            or SnapshotMergeStatus.SkippedSourceUnchanged
                            or SnapshotMergeStatus.SkippedContentUnchanged))
                    {
                        Emit(SnapshotAgentEventLevel.Warning,
                            $"Ingest finished {merge.Status} (run {merge.RunId}).", source.Key);
                    }

                    if (merge.RowsRescoped > 0)
                    {
                        Emit(SnapshotAgentEventLevel.Warning,
                            $"{merge.RowsRescoped} row(s) adopted from another _SourceScope — fine once " +
                            "(scope migration), a config error if it repeats (two sources claiming the same keys).",
                            source.Key);
                    }

                    if (source is { Families.Count: > 0, ReplicationEnabled: true })
                        pumpTables.TryAdd(source.Table.Name, (
                            source.Table,
                            source.Families,
                            source.ReplicationBatchSize,
                            source.ReplicationMaxInFlightRows));
                },
                token);

            ingestFetchedAhead = ingest.SourcesFetched;
            ingestPeakWidth = ingest.MaxObservedFetchesInFlight;

            // Width is the thing that silently fails here: a fan-out whose blocking work cannot
            // get threads reaches degree 1 and reports nothing. Say it once per cycle that
            // actually fanned out, so a regression is visible in the log rather than in the clock.
            if (ingest.SourcesFetched > 1)
            {
                Emit(SnapshotAgentEventLevel.Info,
                    $"Ingest fan-out: {ingest.SourcesFetched} source(s) fetched ahead, peak width " +
                    $"{ingest.MaxObservedFetchesInFlight}/{options.IngestDegree} reached after " +
                    $"{ingest.TimeToMaximumWidth.TotalMilliseconds:F0} ms, peak buffer " +
                    $"{ingest.MaxObservedBufferedRows}/{options.IngestMaxBufferedRows} row(s).");
            }

            // Degrading to per-file probing is correct behaviour, not a failure — but it means a
            // folder would not enumerate, which is worth knowing before it becomes an outage.
            if (fileMetadata.FoldersDegradedToPerFileProbing > 0)
            {
                Emit(SnapshotAgentEventLevel.Warning,
                    $"{fileMetadata.FoldersDegradedToPerFileProbing} source folder(s) would not enumerate this " +
                    "cycle; fell back to per-file metadata probing. Feeds still ingest — check share health.");
            }

            // The flip trap's surviving flavor: the load/skip decision runs ONLY at cold
            // start, so a table deferred while replication was OFF stays Deferred through a
            // WARM restart that turns replication ON. The pump below then drains an empty
            // table every cycle — honestly reporting QueueEmpty — while the rows the manifest
            // recorded as owed sit in the published copy indefinitely. Nothing here can fix
            // that safely (hydrating outside a merge is a second residency decision point);
            // what it can do is refuse to be quiet about it. Fires every cycle on purpose,
            // and in dark-launch (DryRun) too: the state is wrong regardless of whether the
            // pump runs. Remediation is a cold start. A host that wipes its write DB at
            // every process start makes each start cold and this state unreachable — there
            // this warning is the tripwire that should never fire; on hosts that reuse
            // estates (local workflows), replication flips must ship as cold starts, never
            // as bare config edits on a warm process.
            foreach (var (table, _, _, _) in pumpTables.Values)
            {
                if (store!.ReadResidency(table.Name) != SnapshotResidency.Deferred)
                    continue;
                var record = store.ReadDeferredTableRecord(table.Name);
                if (record?.ReplicationPending is > 0)
                {
                    Emit(SnapshotAgentEventLevel.Warning,
                        $"Table {table.Name} is Deferred with {record.ReplicationPending} row(s) recorded as " +
                        "owed to the replication pump, and replication is now enabled. The pump cannot see " +
                        "deferred rows, so they will never drain from here. Cold-start the agent (deploy or " +
                        "swap): the restart will load the table and the pump will perform the full drain.");
                }
            }

            if (!options.DryRun)
            {
                foreach (var (table, families, batchSize, maxInFlightRows) in pumpTables.Values)
                {
                    if (token.IsCancellationRequested)
                        break;

                    pumpRuns.Add(await PumpAsync(
                        table,
                        families,
                        batchSize,
                        maxInFlightRows,
                        ownershipGuard,
                        token));
                }
            }

            if (publishDue && !token.IsCancellationRequested)
            {
                // The gate is what makes publishing to the shared location safe — if the lease
                // was lost mid-cycle, another instance may already be publishing there.
                ownershipGuard?.Invoke();
                publish = SnapshotPublisher.Publish(store!, new SnapshotPublishOptions
                {
                    PublishDirectory = options.PublishDirectory,
                    Store = options.PublishStore,
                    SnapshotName = options.SnapshotName,
                    Tables = options.Registry.Tables,
                    Sources = options.Registry.Sources,
                    SortColumns = options.SortColumns,
                    KeepPublishes = options.KeepPublishes,
                });
                nextPublish = options.TimeProvider.GetUtcNow() + options.PublishCadence;

                if (publish.Status == SnapshotPublishStatus.Published)
                    Emit(SnapshotAgentEventLevel.Info,
                        $"Published {publish.ManifestFile} (exported: {string.Join(", ", publish.TablesExported)}).");

                // An un-wired family publishes as valid, EMPTY parquet, which a consumer cannot
                // tell from "no rows today". The manifest carries rowCount for that reason; this
                // says it where operators actually look.
                if (publish.TablesWithNoRows.Count > 0)
                {
                    Emit(SnapshotAgentEventLevel.Warning,
                        $"Published set carries empty table(s): {string.Join(", ", publish.TablesWithNoRows)} — " +
                        "consumers cannot distinguish an un-wired feed from a genuinely empty one.");
                }
            }
            else if (publishDue)
            {
                Emit(SnapshotAgentEventLevel.Warning, "Skipped publish — gate lost or cancellation requested mid-cycle.");
            }

            // This cycle's facts, written before the run log flushes so the flush can carry them.
            RecordCycle(cycleId, cycleStartedAt, "Ran", coldStart, due.Count, sourceRuns,
                ingestFetchedAhead, ingestPeakWidth, pumpRuns, publish, error: null);

            // The run log, on its own clock and on this thread. It reads the run tables this cycle
            // just wrote, which is why it lives here and nowhere else: the store is single-connection
            // and the loop is one caller at a time. Skipped while the process is shutting down,
            // because the final flush after the loop covers that. Contained when it fails, because
            // observability must never fail the agent.
            var runLog = cancellationToken.IsCancellationRequested ? null : FlushRunLogIfDue();

            return new SnapshotAgentCycle(true, coldStart, sourceRuns, pumpRuns, publish)
            {
                CycleId = cycleId,
                RunLog = runLog,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A shutdown mid-cycle is not a failure, and the row says so.
            RecordCycle(cycleId, cycleStartedAt, "Cancelled", coldStart, due.Count, sourceRuns,
                ingestFetchedAhead, ingestPeakWidth, pumpRuns, publish, error: null);
            throw;
        }
        catch (Exception exception)
        {
            var error = exception is OperationCanceledException && gate?.LostToken.IsCancellationRequested == true
                ? "The write gate was lost mid-cycle."
                : exception.Message;
            RecordCycle(cycleId, cycleStartedAt, "Failed", coldStart, due.Count, sourceRuns,
                ingestFetchedAhead, ingestPeakWidth, pumpRuns, publish, error);
            throw;
        }
        finally
        {
            if (gate is not null)
                await gate.DisposeAsync();
        }
    }

    private async Task<SnapshotAgentPumpRun> PumpAsync(
        SnapshotTableDefinition table,
        IReadOnlyList<CosmosFamilyMapping> families,
        int batchSize,
        int maxInFlightRows,
        Action? ownershipGuard,
        CancellationToken cancellationToken)
    {
        // The drain (cursor paging + systemic-failure breaker + batch bound) lives in the
        // replicator so every caller — this loop and the dev harness alike — rehearses the
        // SAME outage behavior. Timed by the system clock, like every run record.
        var startedAt = DateTime.UtcNow;
        var drain = await replicator!.DrainAsync(
            new CosmosSnapshotReplicatorOptions
            {
                Table = table,
                Families = families,
                BatchSize = batchSize,
                MaxInFlightRows = maxInFlightRows,
                OwnershipGuard = ownershipGuard,
            },
            maxBatches: options.MaxPumpBatchesPerCycle,
            onBatch: batch =>
            {
                if (batch.Failed > 0)
                    Emit(SnapshotAgentEventLevel.Warning,
                        $"Pump {table.Name}: {batch.Failed} row(s) failed this batch (per-row ledger; dead-letters at {SnapshotStore.MaxReplicationAttempts}).");
            },
            cancellationToken);

        if (drain.Stopped == ReplicationDrainStop.SystemicFailure)
            Emit(SnapshotAgentEventLevel.Error,
                $"Pump {table.Name}: every row that attempted a Cosmos op failed — treating as a systemic " +
                "Cosmos failure and stopping this cycle's drain (retries next cycle).");
        else if (drain.Stopped == ReplicationDrainStop.BatchBound)
            Emit(SnapshotAgentEventLevel.Warning,
                $"Pump {table.Name}: drain bound reached ({drain.Batches} batches) — the rest rolls to the next cycle.");
        else if (drain.Stopped == ReplicationDrainStop.RetryPending)
            Emit(SnapshotAgentEventLevel.Warning,
                $"Pump {table.Name}: cursor scan complete with retryable failed row(s) still dirty — retries next cycle.");

        // A drained queue with dead-letters is the settled-vs-drained divergence: the pump's
        // own predicate no longer sees these rows, so without this line nothing anywhere says
        // they exist — until a cold start declines to defer the table and nobody knows why.
        if (drain.DeadLettered > 0)
            Emit(SnapshotAgentEventLevel.Warning,
                $"Pump {table.Name}: {drain.DeadLettered} row(s) are dead-lettered — unreplicated but past " +
                $"{SnapshotStore.MaxReplicationAttempts} attempts, invisible to the drain. They also block " +
                "this table's cold-start deferral. Reset the failure ledger (or ship a content change) to retry.");

        if (drain.RowsRead > 0)
        {
            var groupShape = drain.GroupsRead > 0
                ? $", groups {drain.GroupsRead}, source rows loaded {drain.SourceRowsLoaded}"
                : string.Empty;
            Emit(SnapshotAgentEventLevel.Info,
                $"Pump {table.Name}: read {drain.RowsRead} row(s), remote attempted {drain.RemoteAttemptedRows}, " +
                $"failed {drain.Failed}, max in flight {drain.MaxObservedInFlightRows}/{maxInFlightRows}, " +
                $"request charge {drain.RequestCharge:F2}, bookkeeping {drain.BookkeepingTime.TotalMilliseconds:F1} ms" +
                $"{groupShape}.");
        }

        return new SnapshotAgentPumpRun(
            table.Name, drain.Batches, drain.RowsRead, drain.Upserted, drain.Deleted,
            drain.Excluded, drain.Failed, drain.Drained, drain.RemoteAttemptedRows,
            drain.RemoteFailedRows, drain.MaxObservedInFlightRows, drain.RequestCharge,
            drain.ThrottledRequests, drain.CosmosOperationTime, drain.BookkeepingTime,
            drain.GroupsRead, drain.SourceRowsLoaded, drain.GroupsRecomputed,
            drain.DeadLettered, drain.Stopped.ToString(), startedAt, DateTime.UtcNow);
    }

    /// <summary>Opens (or rebuilds) the write DB. Returns true when this was a cold start that restored from the published set.</summary>
    private SnapshotStoreOptions StoreOptions() => new()
    {
        DatabasePath = options.WriteDatabasePath,
        ExtensionDirectory = options.ExtensionDirectory,
        ExtensionDirectories = options.ExtensionDirectories,
        // The run log's credential stands in when the publish tier is local but the run log is a
        // container, so the store still provisions the azure extension and holds a credential.
        AzureConnectionString = options.AzureConnectionString ?? options.RunLog?.AzureConnectionString,
        ConfigureConnection = options.ConfigureStoreConnection,
    };

    private bool EnsureStore()
    {
        if (store is not null)
            return false;

        var existed = File.Exists(options.WriteDatabasePath);
        try
        {
            store = SnapshotStore.Open(StoreOptions());
        }
        catch (SnapshotSchemaMismatchException exception)
        {
            Emit(SnapshotAgentEventLevel.Warning,
                $"Write DB schema v{exception.Actual} != v{exception.Expected} — rebuilding from the published set.",
                exception: exception);
            DeleteWriteDatabase();
            store = SnapshotStore.Open(StoreOptions());
            existed = false;
        }
        catch (DuckDB.NET.Data.DuckDBException exception) when (existed)
        {
            // A write DB that won't OPEN (corruption after a hard crash, torn WAL replay) is
            // exactly what rebuild-from-published exists for — wedging in retry-forever
            // while a clean seed sits in the publish directory would be absurd. IO-level
            // errors (file locked by another process, permissions) are NOT this: File.Delete
            // will throw on those and the cycle fails loudly instead of deleting a live file.
            Emit(SnapshotAgentEventLevel.Warning,
                "Write DB failed to open — presuming corruption; deleting and rebuilding from the published set.",
                exception: exception);
            DeleteWriteDatabase();
            store = SnapshotStore.Open(StoreOptions());
            existed = false;
        }

        // The run log's own credential, when it has one. Scoped to the run-log root, so it changes
        // nothing for the publish tier.
        if (options.RunLog is { } runLog)
            SnapshotRunLog.ApplyCredential(store, runLog);

        foreach (var table in options.Registry.Tables)
            store.EnsureTable(table);

        replicator = options.ReplicationTransport is { } transport
            ? new CosmosSnapshotReplicator(new SnapshotReplicationStateStore(store), transport)
            : new CosmosSnapshotReplicator(store, cosmosClient);

        var coldStart = !existed;
        if (coldStart)
        {
            // The slot-swap / new-instance story: local disk is empty, the published set is
            // the seed. Bookkeeping columns are published, so replication state survives and
            // the next pump writes zero Cosmos ops for unchanged data.
            // The registry overload, so qualified quiet tables stay deferred to the published
            // copy instead of being downloaded just to sit unread until the next deploy.
            try
            {
                var rebuild = SnapshotRebuild.Execute(store, options.Registry, options.PublishDirectory,
                    options.SnapshotName, options.PublishStore);
                Emit(SnapshotAgentEventLevel.Info,
                    rebuild.ManifestFile is null
                        ? rebuild.PublishesSkipped.Count > 0
                            ? $"Cold start: no compatible v4 seed (ignored {rebuild.PublishesSkipped.Count} pre-v4 publish(es)) — rebuilding from sources."
                            : "Cold start: nothing published yet — starting from an empty write DB."
                        : $"Cold start: rebuilt {rebuild.TotalRows} row(s) across {rebuild.TablesLoaded.Count} table(s) from {rebuild.ManifestFile}."
                          + (rebuild.TablesDeferred.Count > 0
                              ? $" {rebuild.TablesDeferred.Count} table(s) stay deferred to the published copy" +
                                $" ({rebuild.TablesDeferred.Sum(t => t.Rows)} row(s) not downloaded)."
                              : string.Empty));
            }
            catch
            {
                // Never half-start. A failed cold-start rebuild (publish tier unreachable,
                // every kept set torn) must not leave this fresh, EMPTY store behind: the
                // next cycle would find the file, skip the rebuild, run sources against an
                // empty all-resident estate, and publish that over the real set. Drop the
                // store AND the just-created file so the next cycle retries the FULL cold
                // start — with the loop's backoff, a tier that is briefly unreachable at
                // boot means the boot waits until it is back, nothing less.
                DeleteWriteDatabase();
                throw;
            }
        }

        return coldStart;
    }

    private void DeleteWriteDatabase()
    {
        store?.Dispose();
        store = null;
        replicator = null;
        // WAL first: a crash between the two deletes must never leave an orphaned WAL for
        // DuckDB to replay against the NEXT fresh database file. (DB-without-WAL is safe —
        // it is about to be deleted or rebuilt either way.)
        var wal = options.WriteDatabasePath + ".wal";
        if (File.Exists(wal)) File.Delete(wal);
        if (File.Exists(options.WriteDatabasePath)) File.Delete(options.WriteDatabasePath);
    }

    /// <summary>Flushes the run log when its cadence has elapsed. Null when it is off or not due.</summary>
    private SnapshotRunLogFlushResult? FlushRunLogIfDue()
    {
        if (options.RunLog is null || nextRunLogFlush > options.TimeProvider.GetUtcNow())
            return null;
        var result = FlushRunLog("cadence");
        if (result is not null)
            PruneRunLogIfDue();
        return result;
    }

    /// <summary>
    /// Once per UTC day, right after a cadence flush that succeeded: deletes the flushed rows of
    /// earlier days from the write DB's run tables (<see cref="SnapshotRunLog.Prune"/>). Never
    /// before the first flush that wrote, so nothing is deleted until the run log has proven it can
    /// copy rows out; never at shutdown, which does the minimum. Contained: a prune that fails is a
    /// warning, and the day is marked before the attempt so it is retried tomorrow, not per flush.
    /// </summary>
    private void PruneRunLogIfDue()
    {
        if (store is null || runLogFlushedThrough is not { } flushedThrough)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (runLogPrunedOn == today)
            return;

        runLogPrunedOn = today;
        try
        {
            var pruned = SnapshotRunLog.Prune(store, flushedThrough, today);
            if (pruned.Total > 0)
            {
                var detail = string.Join(", ", pruned.RowsDeleted
                    .Where(entry => entry.Value > 0)
                    .Select(entry => $"{entry.Key} {entry.Value}"));
                Emit(SnapshotAgentEventLevel.Info,
                    $"Run log: pruned {pruned.Total} flushed row(s) from days before {today:yyyy-MM-dd} out of the " +
                    $"write DB ({detail}) in {pruned.Elapsed.TotalMilliseconds:F0} ms; the newest row per source stays.");
            }
        }
        catch (Exception exception)
        {
            Emit(SnapshotAgentEventLevel.Warning,
                "Run log: pruning the write DB's run tables failed. The cycle and the run log are unaffected; " +
                "the first cadence flush of the next day retries.",
                exception: exception);
        }
    }

    /// <summary>
    /// One flush attempt, contained. The next due time moves BEFORE the attempt, so a destination
    /// that keeps failing is retried at the cadence and warns at the cadence, never every cycle:
    /// a missing container is loud every few minutes and heals itself once the container appears.
    /// </summary>
    private SnapshotRunLogFlushResult? FlushRunLog(string reason)
    {
        var runLog = options.RunLog;
        if (runLog is null || store is null)
            return null;

        nextRunLogFlush = options.TimeProvider.GetUtcNow() + runLog.Cadence;
        try
        {
            var result = SnapshotRunLog.Flush(
                store, runLog, options.SnapshotName, bootId, bootStartedAt, runLogFlushedThrough);
            runLogFlushedThrough = result.FlushedThrough;
            if (!result.Skipped)
            {
                Emit(SnapshotAgentEventLevel.Info,
                    $"Run log: {result.RowsWritten} row(s) written to {result.FilesWritten.Count} file(s) under " +
                    $"{runLog.Store.Root} in {result.Elapsed.TotalMilliseconds:F0} ms ({reason}).");
            }
            return result;
        }
        catch (Exception exception)
        {
            // A warning, never a failed cycle. The destination is named; the credential never is.
            Emit(SnapshotAgentEventLevel.Warning,
                $"Run log: flush to {runLog.Store.Root} failed ({reason}). The cycle is unaffected and the " +
                "next due flush retries. If the destination is a container, an operator must create it first.",
                exception: exception);
            return null;
        }
    }

    /// <summary>
    /// Writes the cycle's row in <c>meta.CycleRuns</c>: what the cycle did, as recorded facts.
    /// <c>SourcesRun</c> counts sources that reached a merge (their own outcome is in
    /// <c>meta.SyncRuns</c>); <c>SourcesFailed</c> counts sources that crashed before one. A
    /// source whose fetch crashed is counted as failed, and its <c>Failed:Fetch</c> row in
    /// <c>meta.SyncRuns</c> names it; one whose drain crashed after a good fetch has a
    /// <c>Failed:Exception</c> row. Then one
    /// row per pumped table in <c>meta.PumpRuns</c>: the facts the cycle row sums, and the ones a
    /// sum cannot keep. Only when the store is open, which it is not for a cycle that failed to
    /// open it. Contained: a row that cannot be written is a warning, and the cycle's own outcome
    /// stands.
    /// </summary>
    private void RecordCycle(
        string cycleId, DateTime startedAt, string outcome, bool coldStart, int sourcesDue,
        IReadOnlyList<SnapshotAgentSourceRun> sources, int ingestFetchedAhead, int ingestPeakWidth,
        IReadOnlyList<SnapshotAgentPumpRun> pumps, SnapshotPublishResult? publish, string? error)
    {
        if (store is null)
            return;

        try
        {
            store.Execute(
                """
                INSERT INTO meta.CycleRuns
                ("CycleId", "StartedAt", "FinishedAt", "Outcome", "ColdStart",
                 "SourcesDue", "SourcesRun", "SourcesFailed", "IngestFetchedAhead", "IngestPeakWidth",
                 "PumpTables", "PumpBatches", "PumpRowsRead", "PumpUpserted", "PumpDeleted", "PumpFailed",
                 "PumpDeadLettered", "PumpRequestCharge", "PumpThrottledRequests", "PumpCosmosMs", "PumpBookkeepingMs",
                 "PublishStatus", "PublishId", "Error")
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                cycleId, startedAt, DateTime.UtcNow, outcome, coldStart,
                sourcesDue,
                sources.Count(run => run.Merge is not null),
                sources.Count(run => run.Error is not null),
                ingestFetchedAhead, ingestPeakWidth,
                pumps.Count,
                pumps.Sum(pump => pump.Batches),
                pumps.Sum(pump => (long)pump.RowsRead),
                pumps.Sum(pump => (long)pump.Upserted),
                pumps.Sum(pump => (long)pump.Deleted),
                pumps.Sum(pump => (long)pump.Failed),
                pumps.Sum(pump => (long)pump.DeadLettered),
                pumps.Sum(pump => pump.RequestCharge),
                pumps.Sum(pump => pump.ThrottledRequests),
                pumps.Sum(pump => pump.CosmosOperationTime.TotalMilliseconds),
                pumps.Sum(pump => pump.BookkeepingTime.TotalMilliseconds),
                publish?.Status.ToString(), publish?.PublishId, error);

            // A pump run that carries no time of its own (a caller built the record by hand) is
            // placed at the cycle's start, so it always lands in the cycle's day file.
            foreach (var pump in pumps)
            {
                store.Execute(
                    """
                    INSERT INTO meta.PumpRuns
                    ("CycleId", "Table", "StartedAt", "FinishedAt",
                     "Batches", "RowsRead", "Upserted", "Deleted", "Excluded", "Failed", "DeadLettered",
                     "Drained", "StopReason", "RemoteAttemptedRows", "RemoteFailedRows", "MaxObservedInFlightRows",
                     "RequestCharge", "ThrottledRequests", "CosmosMs", "BookkeepingMs",
                     "GroupsRead", "SourceRowsLoaded", "GroupsRecomputed")
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """,
                    cycleId, pump.Table,
                    pump.StartedAt == default ? startedAt : pump.StartedAt,
                    pump.FinishedAt == default ? null : pump.FinishedAt,
                    pump.Batches, (long)pump.RowsRead, (long)pump.Upserted, (long)pump.Deleted, (long)pump.Excluded,
                    (long)pump.Failed, (long)pump.DeadLettered,
                    pump.Drained, pump.StopReason, (long)pump.RemoteAttemptedRows, (long)pump.RemoteFailedRows,
                    pump.MaxObservedInFlightRows,
                    pump.RequestCharge, pump.ThrottledRequests,
                    pump.CosmosOperationTime.TotalMilliseconds, pump.BookkeepingTime.TotalMilliseconds,
                    pump.GroupsRead, (long)pump.SourceRowsLoaded, pump.GroupsRecomputed);
            }
        }
        catch (Exception exception)
        {
            Emit(SnapshotAgentEventLevel.Warning,
                $"The cycle record {cycleId} could not be written in full to meta.CycleRuns and meta.PumpRuns.",
                exception: exception);
        }
    }

    private DateTimeOffset NextDueFor(string key) => nextDue.GetValueOrDefault(key, DateTimeOffset.MinValue);

    private TimeSpan ComputeWait()
    {
        var now = options.TimeProvider.GetUtcNow();
        var next = nextPublish;
        foreach (var source in options.Registry.Sources)
        {
            if (!source.Enabled) continue;
            var dueAt = NextDueFor(source.Key);
            if (dueAt < next) next = dueAt;
        }

        var wait = next - now;
        if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
        return wait > options.MaxIdleWait ? options.MaxIdleWait : wait;
    }

    private async Task<bool> WaitAsync(TimeSpan wait, CancellationToken cancellationToken)
    {
        try
        {
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, options.TimeProvider, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private void Emit(SnapshotAgentEventLevel level, string message, string? sourceKey = null, Exception? exception = null)
    {
        try
        {
            options.OnEvent?.Invoke(new SnapshotAgentEvent(level, message, sourceKey, exception));
        }
        catch
        {
            // An observability callback must never take the agent down.
        }
    }

    public void Dispose()
    {
        store?.Dispose();
        store = null;
        replicator = null;
    }
}
