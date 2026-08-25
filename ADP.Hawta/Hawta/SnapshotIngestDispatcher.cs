using System.Diagnostics;

namespace ShiftSoftware.ADP.Hawta;

public sealed class SnapshotIngestDispatcherOptions
{
    public required SnapshotStore Store { get; init; }

    /// <summary>
    /// The sources to run, in the order they must MERGE. The dispatcher never reorders them —
    /// registry order is the merge order, and this list is what defines it.
    /// </summary>
    public required IReadOnlyList<SnapshotSource> Sources { get; init; }

    /// <summary>This cycle's shared probe. Only the serial drain ever sees it — see <see cref="SnapshotSourceFetchContext"/>.</summary>
    public FileMetadataProbe? FileMetadata { get; init; }

    /// <summary>
    /// Maximum fetches in flight at once. <b>1 is the kill switch</b> — no two fetches ever
    /// overlap, without a redeploy. (A fetch may still overlap the DRAIN of an earlier source;
    /// that is the pipelining this design exists for, and it touches no shared state.)
    ///
    /// <para>Sized against the END STATE, not today's estate: 28 sources are parallelisable now
    /// (12 DMS at three operational dealers + 16 app tables) and 48 at eight dealers, and
    /// enabling a dealer is a config change nobody would think to re-benchmark.</para>
    /// </summary>
    public int Degree { get; init; } = 1;

    /// <summary>
    /// Rows held in fetched-but-not-yet-merged buffers, above which admission stops.
    ///
    /// <para><b>Rows, not tasks, because rows are what fetch-ahead risks.</b> Per-source universes
    /// differ by ~100× across this estate (13,355 rows vs 2), so a degree cap alone does not bound
    /// memory — which is exactly why this is a hand-rolled admission window and not
    /// <c>Parallel.ForEachAsync(MaxDegreeOfParallelism)</c>. Both terms live in ONE admission test
    /// rather than in two mechanisms, so there is no separate semaphore to keep in step with it.</para>
    ///
    /// <para><b>What it actually bounds, stated precisely — it is not the whole buffer.</b> A
    /// fetch's size is unknowable until it has run, so this can only refuse the NEXT source, never
    /// resize the ones already fetching. The peak is therefore
    /// <c>MaxBufferedRows + (Degree + 1) × largest-single-universe</c>: this value bounds the
    /// BACKLOG the fan-out may build in front of the drain — the term that would otherwise grow
    /// with the number of sources — while <see cref="Degree"/> bounds the concurrent term, and the
    /// <c>+1</c> is the one source the drain force-admits to guarantee liveness. Claiming a
    /// tighter bound would be claiming the estate had told us row counts it has not.</para>
    ///
    /// <para><b>Sizing.</b> Against the measured estate — ~30 K rows across all sixteen app tables,
    /// and 9,254 / 12,567 / 6,210 / 5,224 across the four DMS families at three dealers, so roughly
    /// 120 K rows in total at the eight-dealer end state — the default is around four fifths of
    /// everything the fan-out could ever hold. So it does not bite while the estate behaves, and it
    /// stops admission hard the moment one source starts returning millions of rows.</para>
    /// </summary>
    public int MaxBufferedRows { get; init; } = 100_000;

    /// <summary>
    /// Maximum concurrent fetches sharing one <see cref="SnapshotSource.ConcurrencyGroup"/> — the
    /// per-remote-box cap.
    ///
    /// <para><b>Why it is not just <see cref="Degree"/>.</b> Registry order groups a dealer's four
    /// view families adjacently, so a plain degree cap of 4 or more would hit ONE dealer 1C box
    /// four ways by construction. Those boxes being slow is the entire premise of the fan-out;
    /// making them slower is not a trade this build is willing to make silently.</para>
    ///
    /// <para>1 means "no remote box ever sees more concurrency than the serial loop gave it".
    /// A blocked group does not block the queue — admission scans past it to the next group,
    /// which is what keeps the degree usable.</para>
    /// </summary>
    public int MaxPerConcurrencyGroup { get; init; } = 2;

    /// <summary>
    /// Narration, called once per fetch as it FINISHES — optional, and null means silence.
    ///
    /// <para><b>Why this exists at all.</b> Every visible line a caller prints comes from
    /// <c>onDrained</c>, which runs on the serial drain in registry order. So while the drain is
    /// parked on a slow source, a fan-out running at full width produces no output whatsoever: the
    /// first real cycle spent 2.5 minutes fetching eight sources concurrently and looked, on
    /// screen, exactly like a wedged process. The information was all recorded and none of it was
    /// visible until afterwards. A fetch-completion event is the only thing that can show progress
    /// during the window, because the drain by definition is not making any.</para>
    ///
    /// <para><b>Called on the WORKER thread, concurrently with other workers</b> — unlike
    /// <c>onDrained</c>. A handler that writes anywhere shared must do its own locking.</para>
    ///
    /// <para><b>Exceptions are swallowed</b>, and this is the deliberate opposite of
    /// <c>onDrained</c>, where a throw is a caller's assertion failing and must end the run.
    /// Narration is not an assertion; a console that cannot be written to must not decide the fate
    /// of a three-minute cycle.</para>
    /// </summary>
    public Action<SnapshotFetchProgress>? OnFetched { get; init; }
}

/// <summary>
/// One fetch, as it completes — the fan-out's only signal of life while the drain is blocked.
/// </summary>
/// <param name="Elapsed">Wall time of this fetch alone.</param>
/// <param name="BufferedRows">Rows this fetch put in memory. Zero when it failed.</param>
/// <param name="Failure">Null on success. The drain reports it again, later, at the source's registry position.</param>
/// <param name="FetchesInFlight">Fetches still running AFTER this one finished and admission topped up.</param>
/// <param name="TotalBufferedRows">Rows held across every fetched-but-not-yet-drained buffer.</param>
/// <param name="DrainWaitingOn">
/// The source the serial drain is parked on, which is what makes this line diagnostic rather than
/// decorative: it names the one source the cycle is actually waiting for. Null before the drain
/// starts waiting, and equal to <paramref name="Source"/> when this fetch is the one that unblocks it.
/// </param>
public sealed record SnapshotFetchProgress(
    SnapshotSource Source,
    TimeSpan Elapsed,
    long BufferedRows,
    Exception? Failure,
    int FetchesInFlight,
    long TotalBufferedRows,
    SnapshotSource? DrainWaitingOn);

/// <summary>One source's outcome, handed to the caller ON THE DRAIN THREAD, in registry order.</summary>
/// <param name="Merge">Null when the source threw; see <paramref name="Failure"/>.</param>
/// <param name="Failure">Null on success. A fetch fault surfaces here, at the source's registry position.</param>
/// <param name="Fetch">Wall time of the parallel half. Zero for a one-phase source.</param>
/// <param name="Drain">Wall time of the serial half — for a one-phase source, the whole ingest.</param>
public sealed record SnapshotIngestOutcome(
    SnapshotSource Source,
    SnapshotMergeResult? Merge,
    Exception? Failure,
    TimeSpan Fetch,
    TimeSpan Drain);

/// <summary>
/// What the fan-out actually did. The width figures are the ones worth watching: a blocking
/// fan-out that never reaches its configured degree is the failure mode this shape was chosen to
/// avoid, and it is invisible without measuring it.
/// </summary>
/// <param name="TimeToMaximumWidth">
/// How long after the first admission the dispatcher reached
/// <paramref name="MaxObservedFetchesInFlight"/>. Long here on a wide estate means the fetches are
/// not getting threads — the reason they run on dedicated threads rather than the pool.
/// </param>
public sealed record SnapshotIngestReport(
    int SourcesDrained,
    int SourcesFetched,
    int MaxObservedFetchesInFlight,
    long MaxObservedBufferedRows,
    TimeSpan TimeToMaximumWidth,
    bool StoppedEarly);

/// <summary>
/// Runs a cycle's sources with a bounded fetch fan-out and a strictly serial merge drain.
///
/// <para><b>The shape, and why each half is where it is.</b> Fetch is 95.4–97.9 % of a cycle
/// (99.8 % attributing the Cosmos drain correctly) and the largest true merge in the estate is
/// 8.2 s against 1.44 M rows, with every other merge ever recorded at ≤ 60 ms. So the fan-out
/// overlaps the external wait and nothing else: workers open their own source connection, drain
/// their own reader into their own buffer, and touch no DuckDB at all. The drain then stages,
/// hashes and merges each buffer on the store's one connection, in registry order.</para>
///
/// <para><b>What that buys, beyond the wall clock.</b> The store's threading contract is
/// untouched, so cross-connection visibility and concurrent staging never become live questions.
/// Every run record and every source stamp is written by the drain, so <c>meta.SyncRuns</c> and
/// the non-atomic DELETE-then-INSERT stamp path stay single-writer. Staging is created by the
/// drain from a COMPLETE buffer, so a faulted fetch cannot leave a half-populated staging table
/// for the mass-delete guardrail to mistake for a real universe. And the source connection closes
/// after the drain of the reader instead of after the merge, which is strictly gentler on the
/// dealer box than today.</para>
///
/// <para><b>Blocking work runs on dedicated threads, never the shared pool.</b> Every ingest body
/// in this engine is fully blocking, fetches were measured up to 100 seconds, and the write gate's
/// renewal is a thread-pool continuation with a 45-second proof budget. Handing the pool a wave of
/// 100-second blocking work is how a lease gets starved, and a lost lease leaves the ACTIVE marker
/// in blob and wedges the agent. <see cref="TaskCreationOptions.LongRunning"/> keeps the pool free
/// for the renewal and removes the pool's ramp-up from time-to-full-width.</para>
///
/// <para><b>Not thread-safe, one call at a time</b>, exactly like the loop that owns it. The
/// callback runs on the drain, and the drain is one thread's worth of work at a time — which is
/// what lets a caller fold results into plain, unsynchronised collections.</para>
/// </summary>
public static class SnapshotIngestDispatcher
{
    /// <summary>
    /// Fetches ahead within the bounds, drains in registry order, and calls
    /// <paramref name="onDrained"/> once per source ON THE DRAIN THREAD.
    ///
    /// <para>An ingest that throws is CONTAINED and reported through
    /// <see cref="SnapshotIngestOutcome.Failure"/>, so one bad source cannot decide the fate of
    /// the others. An exception from <paramref name="onDrained"/> is NOT contained — that is the
    /// caller's own assertion failing, and swallowing it would turn a drill into a pass.</para>
    /// </summary>
    public static async Task<SnapshotIngestReport> RunAsync(
        SnapshotIngestDispatcherOptions options,
        Action<SnapshotIngestOutcome> onDrained,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Degree, 1, nameof(options.Degree));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxBufferedRows, 1, nameof(options.MaxBufferedRows));
        ArgumentOutOfRangeException.ThrowIfLessThan(
            options.MaxPerConcurrencyGroup, 1, nameof(options.MaxPerConcurrencyGroup));

        var sources = options.Sources;
        var run = new Run(options, cancellationToken);

        try
        {
            for (var index = 0; index < sources.Count; index++)
            {
                // FIRST, before admission can start a single worker for this iteration. A worker
                // narrates from its own thread the moment it finishes, and the whole value of that
                // line is that it names the source the cycle is parked on — so the marker has to be
                // in place before any worker exists to read it. Setting it after PumpAdmission left
                // a scheduling window in which the earliest fetches reported "drain not waiting
                // yet", which is not merely imprecise: it is worthless exactly when the fan-out is
                // widest and the reader most needs to know what the hold-up is. Volatile rather
                // than locked — the drain writes, workers read, and a narration line must never
                // contend for the admission lock.
                run.DrainWaitingOnIndex = index;

                // Top up before every wait, so a fetch starts the moment the budget allows —
                // workers also pump on completion, which is what keeps width up while the drain
                // is parked on a slow source.
                run.PumpAdmission();

                if (cancellationToken.IsCancellationRequested)
                {
                    run.StoppedEarly = true;
                    break;
                }

                var source = sources[index];
                var context = new SnapshotSourceContext
                {
                    Store = options.Store,
                    CancellationToken = cancellationToken,
                    FileMetadata = options.FileMetadata,
                };

                SnapshotMergeResult? merge = null;
                Exception? failure = null;
                var fetchElapsed = TimeSpan.Zero;
                var drainClock = new Stopwatch();

                try
                {
                    if (run.Slots[index] is { } slot)
                    {
                        // Liveness: the bounds may have refused this source outright, so start it
                        // here rather than wait on something nothing will start.
                        run.EnsureAdmitted(index);

                        // Awaited without WaitAsync(token) on purpose: abandoning a running worker
                        // would leave a thread filling a buffer nobody owns. Its own token check
                        // (one per row) is what makes it stop.
                        var fetched = await slot.Task.ConfigureAwait(false);
                        fetchElapsed = fetched.Elapsed;

                        // The buffer is accounted until the drain is DONE with it — it is still in
                        // memory while it stages — and released whichever way the drain ends, or
                        // the window would shrink by one source's universe for the rest of the cycle.
                        try
                        {
                            if (fetched.Failure is not null)
                            {
                                failure = fetched.Failure;
                            }
                            else
                            {
                                drainClock.Start();
                                merge = fetched.Fetch!.Drain(context);
                                drainClock.Stop();
                            }
                        }
                        finally
                        {
                            run.ReleaseBufferedRows(fetched.AccountedRows);
                        }

                        // A worker captures its exception as a RESULT rather than faulting its
                        // task, so cancellation has to be recognised here instead of by the catch
                        // below. Without this a lost write-gate lease would arrive as "Ingest
                        // crashed" against every source that was mid-fetch — an Error event and a
                        // failed run for something the serial loop reports by quietly stopping.
                        if (failure is OperationCanceledException && cancellationToken.IsCancellationRequested)
                        {
                            run.StoppedEarly = true;
                            break;
                        }
                    }
                    else
                    {
                        drainClock.Start();
                        merge = await source.RunIngestAsync(context).ConfigureAwait(false);
                        drainClock.Stop();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    run.StoppedEarly = true;
                    break;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                drainClock.Stop();
                run.SourcesDrained++;
                onDrained(new SnapshotIngestOutcome(source, merge, failure, fetchElapsed, drainClock.Elapsed));
            }
        }
        finally
        {
            await run.StopAndObserveAsync().ConfigureAwait(false);
        }

        return run.ToReport();
    }

    /// <summary>A completed fetch, or the exception that replaced it. The task carrying this never faults.</summary>
    private sealed record FetchSlot(
        SnapshotSourceFetch? Fetch,
        Exception? Failure,
        TimeSpan Elapsed,
        long AccountedRows);

    private sealed class Run
    {
        private readonly SnapshotIngestDispatcherOptions options;
        private readonly CancellationToken cancellationToken;
        private readonly Stopwatch clock = Stopwatch.StartNew();

        /// <summary>Guards admission and every counter admission reads. Held only while starting threads.</summary>
        private readonly object admission = new();

        /// <summary>Fetchable indices not yet admitted, in registry order. A group-blocked head is skipped, not popped.</summary>
        private readonly LinkedList<int> pending = new();

        private readonly Dictionary<string, int> groupsInFlight = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<TaskCompletionSource<FetchSlot>> admitted = [];

        private int fetchesInFlight;
        private long bufferedRows;
        private bool stopped;

        /// <summary>Registry index the drain is parked on; -1 before it waits on anything.</summary>
        public volatile int DrainWaitingOnIndex = -1;

        public Run(SnapshotIngestDispatcherOptions options, CancellationToken cancellationToken)
        {
            this.options = options;
            this.cancellationToken = cancellationToken;

            Slots = new TaskCompletionSource<FetchSlot>?[options.Sources.Count];
            for (var index = 0; index < options.Sources.Count; index++)
            {
                if (options.Sources[index].Fetch is null)
                    continue;

                // RunContinuationsAsynchronously keeps the drain's continuation off the worker's
                // dedicated thread: without it, SetResult would inline the whole staging + merge
                // onto a LongRunning thread that is meant to be finishing.
                Slots[index] = new TaskCompletionSource<FetchSlot>(TaskCreationOptions.RunContinuationsAsynchronously);
                pending.AddLast(index);
            }
        }

        public TaskCompletionSource<FetchSlot>?[] Slots { get; }

        public int SourcesDrained { get; set; }
        public bool StoppedEarly { get; set; }

        private int sourcesFetched;
        private int maxFetchesInFlight;
        private long maxBufferedRows;
        private TimeSpan timeToMaximumWidth;

        public SnapshotIngestReport ToReport() => new(
            SourcesDrained,
            sourcesFetched,
            maxFetchesInFlight,
            Interlocked.Read(ref maxBufferedRows),
            timeToMaximumWidth,
            StoppedEarly);

        /// <summary>
        /// Admits every source the bounds currently allow.
        ///
        /// <para>Called from the drain before each wait AND from each worker as it finishes, so
        /// width recovers the moment a slot frees rather than at the next drain boundary. Both
        /// callers take the same lock; the body only starts threads, so it is never held long.</para>
        /// </summary>
        public void PumpAdmission()
        {
            lock (admission)
            {
                var node = pending.First;
                while (node is not null)
                {
                    if (stopped || cancellationToken.IsCancellationRequested)
                        return;
                    if (fetchesInFlight >= options.Degree)
                        return;
                    if (Volatile.Read(ref bufferedRows) >= options.MaxBufferedRows)
                        return;

                    var index = node.Value;
                    var group = options.Sources[index].ConcurrencyGroup;
                    var next = node.Next;

                    if (group is null || groupsInFlight.GetValueOrDefault(group) < options.MaxPerConcurrencyGroup)
                    {
                        // Scanning PAST a blocked group rather than stopping at it is what makes the
                        // per-box cap compatible with the degree: registry order clusters a dealer's
                        // families together, so stopping would pin the whole fan-out at the group cap.
                        pending.Remove(node);
                        Start(index, group);
                    }

                    node = next;
                }
            }
        }

        /// <summary>
        /// Admits the source the drain is about to wait on, whatever the bounds say — and this is
        /// the ONLY place a bound is overridden.
        ///
        /// <para><b>It is what makes liveness a one-line argument.</b> The bounds above refuse work
        /// freely: a source whose universe alone exceeds the row budget, or whose group is already
        /// full of its own siblings, may never be admitted by them. Since the drain calls this
        /// immediately before every wait, the slot it waits on is always started, so no bound can
        /// park the cycle. The earlier "admit one whenever nothing is in flight" version also
        /// guaranteed liveness, but it did so by walking the ENTIRE remaining list one source at a
        /// time whenever the drain was busy — which quietly defeated the row budget it was meant to
        /// coexist with.</para>
        /// </summary>
        public void EnsureAdmitted(int index)
        {
            lock (admission)
            {
                if (stopped)
                    return;

                var node = pending.First;
                while (node is not null && node.Value != index)
                    node = node.Next;

                if (node is null)
                    return;

                pending.Remove(node);
                Start(index, options.Sources[index].ConcurrencyGroup);
            }
        }

        private void Start(int index, string? group)
        {
            // Caller holds `admission`.
            fetchesInFlight++;
            sourcesFetched++;
            if (group is not null)
                groupsInFlight[group] = groupsInFlight.GetValueOrDefault(group) + 1;

            if (fetchesInFlight > maxFetchesInFlight)
            {
                maxFetchesInFlight = fetchesInFlight;
                timeToMaximumWidth = clock.Elapsed;
            }

            var source = options.Sources[index];
            var completion = Slots[index]!;
            admitted.Add(completion);

            try
            {
                _ = Task.Factory.StartNew(
                    () => Fetch(source, completion, group),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }
            catch (Exception exception)
            {
                // The thread itself could not be created. The slot MUST still complete or the
                // drain waits on it forever.
                completion.TrySetResult(new FetchSlot(null, exception, TimeSpan.Zero, 0));
                ReleaseWorker(group);
            }
        }

        private void Fetch(SnapshotSource source, TaskCompletionSource<FetchSlot> completion, string? group)
        {
            var elapsed = Stopwatch.StartNew();
            long reported = 0;
            long accounted = 0;
            Exception? failure = null;

            try
            {
                var fetch = source.Fetch!(new SnapshotSourceFetchContext
                {
                    CancellationToken = cancellationToken,
                    RowsBuffered = count =>
                    {
                        reported += count;
                        Track(Interlocked.Add(ref bufferedRows, count));
                    },
                });

                // BufferedRows is the authority; incremental reports are an early view of it. A
                // fetch that reported nothing is accounted in full right here.
                Track(Interlocked.Add(ref bufferedRows, fetch.BufferedRows - reported));
                accounted = fetch.BufferedRows;
                completion.TrySetResult(new FetchSlot(fetch, null, elapsed.Elapsed, fetch.BufferedRows));
            }
            catch (Exception exception)
            {
                // Nothing survives a faulted fetch, so nothing stays accounted for it.
                failure = exception;
                Interlocked.Add(ref bufferedRows, -reported);
                completion.TrySetResult(new FetchSlot(null, exception, elapsed.Elapsed, 0));
            }
            finally
            {
                ReleaseWorker(group);
                PumpAdmission();

                // AFTER both, so the width this line reports is the width that now exists — this
                // fetch retired and its replacement already started. Reporting before the pump
                // would print a trough that never happened.
                Narrate(source, elapsed.Elapsed, accounted, failure);
            }
        }

        private void Narrate(SnapshotSource source, TimeSpan elapsed, long rowsFetched, Exception? failure)
        {
            if (options.OnFetched is not { } onFetched)
                return;

            int inFlight;
            lock (admission)
                inFlight = fetchesInFlight;

            var waitingOn = DrainWaitingOnIndex;

            try
            {
                onFetched(new SnapshotFetchProgress(
                    source,
                    elapsed,
                    rowsFetched,
                    failure,
                    inFlight,
                    Interlocked.Read(ref bufferedRows),
                    waitingOn >= 0 && waitingOn < options.Sources.Count ? options.Sources[waitingOn] : null));
            }
            catch
            {
                // Documented on OnFetched: narration never decides the fate of a cycle. The drain's
                // own callback is the opposite, and deliberately so.
            }
        }

        private void ReleaseWorker(string? group)
        {
            lock (admission)
            {
                fetchesInFlight--;
                if (group is not null && groupsInFlight.TryGetValue(group, out var held))
                    groupsInFlight[group] = held - 1;
            }
        }

        public void ReleaseBufferedRows(long rows)
        {
            if (rows > 0)
                Interlocked.Add(ref bufferedRows, -rows);
        }

        private void Track(long observed)
        {
            var previous = Interlocked.Read(ref maxBufferedRows);
            while (observed > previous)
            {
                var replaced = Interlocked.CompareExchange(ref maxBufferedRows, observed, previous);
                if (replaced == previous)
                    break;
                previous = replaced;
            }
        }

        /// <summary>
        /// Stops admission and observes every worker already admitted, so no fetch outlives the
        /// cycle — and therefore the write-gate lease — that started it. The wait is bounded by the
        /// slowest single fetch, which is the same bound the serial loop has always had.
        /// </summary>
        public async Task StopAndObserveAsync()
        {
            TaskCompletionSource<FetchSlot>[] outstanding;
            lock (admission)
            {
                stopped = true;
                outstanding = admitted.ToArray();
            }

            foreach (var slot in outstanding)
                await slot.Task.ConfigureAwait(false);   // Never faults: the worker always sets a result.
        }
    }
}
