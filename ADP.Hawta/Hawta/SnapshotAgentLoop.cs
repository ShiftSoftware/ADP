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
    /// Azure Storage connection string for DuckDB's own <c>az://</c> access, when
    /// <see cref="PublishStore"/> is a container. Set it from the same configuration value as the
    /// store's — the two halves authenticate independently and configuring only one fails at the
    /// first export, not at startup.
    /// </summary>
    public string? AzureConnectionString { get; init; }

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

    /// <summary>Null = ungated (single-process local runs). Production always gates.</summary>
    public SnapshotWriteGateOptions? WriteGate { get; init; }

    /// <summary>Per-table parquet export sort, passed through to the publisher.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? SortColumns { get; init; }

    /// <summary>Published sets kept by retention — a recovery window, not a storage knob. See <see cref="SnapshotPublishOptions.KeepPublishes"/>.</summary>
    public int KeepPublishes { get; init; } = 3;

    /// <summary>Drain bound: pump batches per table per cycle (a stuck-failing batch dead-letters after 5 attempts anyway).</summary>
    public int MaxPumpBatchesPerCycle { get; init; } = 100;

    /// <summary>Injectable clock for tests.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>Upper bound on idle sleep between scheduler checks.</summary>
    public TimeSpan MaxIdleWait { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>How long to back off when the gate is held elsewhere or a cycle crashed at store level.</summary>
    public TimeSpan GateRetryWait { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Observability callback (the host adapts to its logger). Exceptions in the callback are swallowed.</summary>
    public Action<SnapshotAgentEvent>? OnEvent { get; init; }
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
    int GroupsRecomputed = 0);

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
}

/// <summary>
/// The dispatcher: per-source cadences over ONE sequential worker (the store is
/// single-connection and single-writer — concurrency here would buy nothing and break
/// everything), each cycle bracketed by the write gate. A cycle is: gate → cold-start
/// rebuild if the write DB is fresh → ingest every due source → pump the affected tables
/// (wet mode) → publish on its own cadence → release.
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

    private SnapshotStore? store;
    private CosmosSnapshotReplicator? replicator;

    public SnapshotAgentLoop(SnapshotAgentOptions options, CosmosClient? cosmosClient)
    {
        if (options.MaxPumpBatchesPerCycle <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.MaxPumpBatchesPerCycle),
                "Max pump batches per cycle must be positive.");

        this.options = options;
        this.cosmosClient = cosmosClient;

        if (!options.DryRun
            && cosmosClient is null
            && options.Registry.Sources.Any(s => s is { Families.Count: > 0, ReplicationEnabled: true }))
        {
            throw new InvalidOperationException(
                "Wet mode with replicated tables requires a CosmosClient (set DryRun for dark-launch).");
        }
    }

    /// <summary>The store, once a cycle has opened it (exposed for hosts' diagnostics endpoints).</summary>
    public SnapshotStore? Store => store;

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
        WriteGateLease? gate = null;
        if (options.WriteGate is not null)
        {
            gate = await SnapshotWriteGate.TryAcquireAsync(options.WriteGate, cancellationToken);
            if (gate is null)
            {
                Emit(SnapshotAgentEventLevel.Warning,
                    "Write gate held elsewhere — skipping this cycle (normal during deploy overlap).");
                return SnapshotAgentCycle.GateUnavailable;
            }
        }

        try
        {
            using var linkedCts = gate is null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gate.LostToken);
            var token = linkedCts.Token;
            Action? ownershipGuard = gate is null ? null : new Action(gate.EnsureOwnership);

            var coldStart = EnsureStore();

            // One probe per cycle, always — it is lazy, so a registry with no gated source never
            // touches the file system through it. Sharing it across the cycle is the point: every
            // source sees the same picture of the share, and no cycle inherits another's cache.
            var fileMetadata = new DirectoryListingFileMetadataProbe();

            var sourceRuns = new List<SnapshotAgentSourceRun>();
            var pumpTables = new Dictionary<string, (SnapshotTableDefinition Table, IReadOnlyList<CosmosFamilyMapping> Families, int BatchSize, int MaxInFlightRows)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var source in due)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    var merge = source.Ingest(new SnapshotSourceContext
                    {
                        Store = store!,
                        CancellationToken = token,
                        FileMetadata = fileMetadata,
                    });
                    sourceRuns.Add(new SnapshotAgentSourceRun(source.Key, merge, null));

                    // SkippedSourceUnchanged is the gate working, not a problem — warning on it
                    // would make the healthy steady state the noisiest thing in the log.
                    if (!merge.Succeeded
                        && merge.Status is not (SnapshotMergeStatus.SkippedSourceAbsent
                            or SnapshotMergeStatus.SkippedSourceEmpty
                            or SnapshotMergeStatus.SkippedSourceUnchanged))
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
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    // The merge already wrote its Failed:Exception run record; the loop's job is
                    // to contain the failure so the other sources still run.
                    sourceRuns.Add(new SnapshotAgentSourceRun(source.Key, null, exception));
                    Emit(SnapshotAgentEventLevel.Error, "Ingest crashed.", source.Key, exception);
                }
                finally
                {
                    nextDue[source.Key] = options.TimeProvider.GetUtcNow() + source.Cadence;
                }

                if (source is { Families.Count: > 0, ReplicationEnabled: true })
                    pumpTables.TryAdd(source.Table.Name, (
                        source.Table,
                        source.Families,
                        source.ReplicationBatchSize,
                        source.ReplicationMaxInFlightRows));
            }

            // Degrading to per-file probing is correct behaviour, not a failure — but it means a
            // folder would not enumerate, which is worth knowing before it becomes an outage.
            if (fileMetadata.FoldersDegradedToPerFileProbing > 0)
            {
                Emit(SnapshotAgentEventLevel.Warning,
                    $"{fileMetadata.FoldersDegradedToPerFileProbing} source folder(s) would not enumerate this " +
                    "cycle; fell back to per-file metadata probing. Feeds still ingest — check share health.");
            }

            var pumpRuns = new List<SnapshotAgentPumpRun>();
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

            SnapshotPublishResult? publish = null;
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

            return new SnapshotAgentCycle(true, coldStart, sourceRuns, pumpRuns, publish);
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
        // SAME outage behavior.
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
            drain.GroupsRead, drain.SourceRowsLoaded, drain.GroupsRecomputed);
    }

    /// <summary>Opens (or rebuilds) the write DB. Returns true when this was a cold start that restored from the published set.</summary>
    private bool EnsureStore()
    {
        if (store is not null)
            return false;

        var existed = File.Exists(options.WriteDatabasePath);
        try
        {
            store = SnapshotStore.Open(new SnapshotStoreOptions
            {
                DatabasePath = options.WriteDatabasePath,
                ExtensionDirectory = options.ExtensionDirectory,
                AzureConnectionString = options.AzureConnectionString,
            });
        }
        catch (SnapshotSchemaMismatchException exception)
        {
            Emit(SnapshotAgentEventLevel.Warning,
                $"Write DB schema v{exception.Actual} != v{exception.Expected} — rebuilding from the published set.",
                exception: exception);
            DeleteWriteDatabase();
            store = SnapshotStore.Open(new SnapshotStoreOptions
            {
                DatabasePath = options.WriteDatabasePath,
                ExtensionDirectory = options.ExtensionDirectory,
                AzureConnectionString = options.AzureConnectionString,
            });
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
            store = SnapshotStore.Open(new SnapshotStoreOptions
            {
                DatabasePath = options.WriteDatabasePath,
                ExtensionDirectory = options.ExtensionDirectory,
                AzureConnectionString = options.AzureConnectionString,
            });
            existed = false;
        }

        foreach (var table in options.Registry.Tables)
            store.EnsureTable(table);

        replicator = new CosmosSnapshotReplicator(store, cosmosClient);

        if (!existed)
        {
            // The slot-swap / new-instance story: local disk is empty, the published set is
            // the seed. Bookkeeping columns are published, so replication state survives and
            // the next pump writes zero Cosmos ops for unchanged data.
            var rebuild = SnapshotRebuild.Execute(store, options.Registry.Tables, options.PublishDirectory,
                options.SnapshotName, options.PublishStore);
            Emit(SnapshotAgentEventLevel.Info,
                rebuild.ManifestFile is null
                    ? "Cold start: nothing published yet — starting from an empty write DB."
                    : $"Cold start: rebuilt {rebuild.TotalRows} row(s) across {rebuild.TablesLoaded.Count} table(s) from {rebuild.ManifestFile}.");
            return true;
        }

        return false;
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
