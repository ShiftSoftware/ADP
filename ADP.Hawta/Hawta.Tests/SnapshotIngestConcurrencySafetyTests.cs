using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Deterministic safety tests for the bounded ingest fan-out — the counterpart to
/// <see cref="CosmosReplicatorConcurrencySafetyTests"/>, whose bounded admission window this
/// dispatcher copies. Gates control fetch completion order without any real source; the real
/// DuckDB store still supplies staging, merge and run-record semantics, because "the drain is the
/// only writer" is a claim about the store and would be worth nothing against a fake one.
///
/// <para>Each fact below is one of the hazards the design review named. Nothing here sleeps for a
/// fixed interval: every wait is on a condition with a five-second ceiling, so a regression fails
/// rather than flakes.</para>
/// </summary>
public sealed class SnapshotIngestConcurrencySafetyTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    /// <summary>Sources started, in start order — the fan-out's admission decisions, observable.</summary>
    private readonly ConcurrentQueue<string> started = new();

    /// <summary>Sources drained, in drain order — must always be registry order.</summary>
    private readonly List<string> drained = [];

    private int fetchesInFlight;
    private int observedPeakFetches;
    private int drainsInFlight;
    private int observedPeakDrains;

    private readonly ConcurrentDictionary<int, byte> fetchThreads = new();
    private readonly HashSet<int> drainThreads = [];

    public void Dispose() => snapshot.Dispose();

    // ---- Fixtures ------------------------------------------------------------------------

    /// <summary>
    /// A two-phase source. The fetch reports <paramref name="rows"/> buffered rows, optionally
    /// waits on <paramref name="gate"/>, and hands the drain a body that stages and merges them.
    /// Each source owns its own <c>_SourceScope</c>, so concurrent sources on one table cannot
    /// tombstone each other and a wrong drain order would show up as lost rows.
    /// </summary>
    private SnapshotSource Fetching(
        string key,
        int rows = 1,
        Task? gate = null,
        string? group = null,
        Exception? fetchThrows = null) => new()
    {
        Key = key,
        SourceScope = key,
        ConcurrencyGroup = group,
        RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
        Table = snapshot.Table,
        Cadence = TimeSpan.FromMinutes(5),
        Fetch = context =>
        {
            fetchThreads[Environment.CurrentManagedThreadId] = 0;
            TrackPeak(ref fetchesInFlight, ref observedPeakFetches, +1);
            started.Enqueue(key);

            try
            {
                gate?.GetAwaiter().GetResult();
                context.CancellationToken.ThrowIfCancellationRequested();

                if (fetchThrows is not null)
                    throw fetchThrows;

                // Buffered BEFORE the drain sees it — this is the row the admission window
                // accounts for, and it is deliberately reported from the worker.
                var buffered = Enumerable.Range(1, rows)
                    .Select(index => ($"{key}-{index:D4}", $"code-{index}", index))
                    .ToList();
                context.RowsBuffered(buffered.Count);

                return SnapshotSourceFetch.Staged(buffered.Count, drain => Merge(key, drain, buffered));
            }
            finally
            {
                TrackPeak(ref fetchesInFlight, ref observedPeakFetches, -1);
            }
        },
    };

    /// <summary>A one-phase source — the shape file and Cosmos sources keep. Runs inline on the drain.</summary>
    private SnapshotSource Inline(string key, int rows = 1, Task? gate = null) => new()
    {
        Key = key,
        SourceScope = key,
        RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
        Table = snapshot.Table,
        Cadence = TimeSpan.FromMinutes(5),
        Ingest = context =>
        {
            gate?.GetAwaiter().GetResult();
            return Merge(key, context, Enumerable.Range(1, rows)
                .Select(index => ($"{key}-{index:D4}", $"code-{index}", index))
                .ToList());
        },
    };

    /// <summary>The serial half: staging + merge on the store's one connection.</summary>
    private SnapshotMergeResult Merge(
        string key, SnapshotSourceContext context, IReadOnlyList<(string Key, string Code, int Quantity)> rows)
    {
        lock (drainThreads)
            drainThreads.Add(Environment.CurrentManagedThreadId);
        TrackPeak(ref drainsInFlight, ref observedPeakDrains, +1);

        try
        {
            var staging = context.Store.CreateStagingTable(snapshot.Table);
            foreach (var row in rows)
            {
                context.Store.Execute(
                    $"""
                    INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
                    SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}, NULL
                    FROM (SELECT ? AS "Code", ? AS "Quantity")
                    """,
                    row.Key, row.Code, row.Quantity);
            }

            return SnapshotMerge.Execute(context.Store, snapshot.Table, staging, new SnapshotMergeOptions
            {
                Source = key,
                SourceScope = key,
                DeletesEnabled = true,
            });
        }
        finally
        {
            TrackPeak(ref drainsInFlight, ref observedPeakDrains, -1);
        }
    }

    private async Task<SnapshotIngestReport> RunAsync(
        IReadOnlyList<SnapshotSource> sources,
        int degree = 1,
        int maxBufferedRows = 100_000,
        int maxPerGroup = 2,
        CancellationToken? cancellationToken = null) =>
        await SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions
            {
                Store = snapshot.Store,
                Sources = sources,
                Degree = degree,
                MaxBufferedRows = maxBufferedRows,
                MaxPerConcurrencyGroup = maxPerGroup,
            },
            outcome => drained.Add(outcome.Source.Key),
            cancellationToken ?? TestContext.Current.CancellationToken);

    // ---- The merge drain is serial, and in registry order ---------------------------------

    [Fact]
    public async Task DrainOrder_IsRegistryOrder_WhateverOrderFetchesComplete()
    {
        // Released in reverse, so completion order is the exact opposite of registry order.
        var gates = Enumerable.Range(0, 5).Select(_ => NewGate()).ToArray();
        var sources = Enumerable.Range(0, 5)
            .Select(index => Fetching($"s{index}", rows: 2, gate: gates[index].Task))
            .ToList();

        var run = RunAsync(sources, degree: 5);
        await WaitUntilAsync(() => started.Count == 5);

        for (var index = 4; index >= 0; index--)
            gates[index].SetResult();

        var report = await run;

        Assert.Equal(["s0", "s1", "s2", "s3", "s4"], drained);
        Assert.Equal(5, report.SourcesDrained);
        Assert.False(report.StoppedEarly);

        // Every source's rows survived: a drain that ran two merges at once, or merged a buffer
        // twice, would show here rather than in a timing assertion.
        Assert.Equal(10, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    [Fact]
    public async Task TwoMergesNeverOverlap_EvenAtFullFanOutWidth()
    {
        var sources = Enumerable.Range(0, 12).Select(index => Fetching($"s{index}", rows: 3)).ToList();

        var report = await RunAsync(sources, degree: 6);

        Assert.Equal(1, observedPeakDrains);
        Assert.Equal(12, report.SourcesDrained);
        Assert.Equal(36, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    [Fact]
    public async Task FetchesRunOffTheDrainThread_SoTheStoreStaysSingleThreaded()
    {
        var sources = Enumerable.Range(0, 8).Select(index => Fetching($"s{index}", rows: 2)).ToList();

        await RunAsync(sources, degree: 4);

        // Fetches are dedicated LongRunning threads; the drain resumes on the pool. Overlap here
        // would mean a fetch could be holding the store's connection while a merge runs on it.
        Assert.NotEmpty(fetchThreads);
        Assert.NotEmpty(drainThreads);
        Assert.Empty(fetchThreads.Keys.Intersect(drainThreads));
    }

    // ---- The admission window --------------------------------------------------------------

    [Fact]
    public async Task FetchesInFlight_NeverExceedTheConfiguredDegree()
    {
        var release = NewGate();
        var sources = Enumerable.Range(0, 12)
            .Select(index => Fetching($"s{index}", gate: release.Task))
            .ToList();

        var run = RunAsync(sources, degree: 3);

        await WaitUntilAsync(() => started.Count == 3);
        // Give a wrong implementation room to over-admit before the assertion looks.
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Equal(3, started.Count);
        Assert.Equal(3, Volatile.Read(ref observedPeakFetches));

        release.SetResult();
        var report = await run;

        Assert.Equal(3, report.MaxObservedFetchesInFlight);
        Assert.Equal(12, report.SourcesFetched);
        Assert.Equal(12, report.SourcesDrained);
    }

    [Fact]
    public async Task FullWidth_IsReachedPromptly_BecauseBlockingFetchesGetDedicatedThreads()
    {
        // The hazard this pins: dispatch blocking drains onto the shared pool and the pool grows
        // only gradually once its threads are all blocked, so a fan-out over sources measured at
        // up to 100 seconds can sit at width 1 or 2 for a long time and report nothing wrong.
        var release = NewGate();
        var sources = Enumerable.Range(0, 16)
            .Select(index => Fetching($"s{index}", gate: release.Task))
            .ToList();

        var clock = Stopwatch.StartNew();
        var run = RunAsync(sources, degree: 16);

        await WaitUntilAsync(() => started.Count == 16);
        var timeToFullWidth = clock.Elapsed;

        release.SetResult();
        var report = await run;

        Assert.Equal(16, report.MaxObservedFetchesInFlight);
        Assert.True(timeToFullWidth < TimeSpan.FromSeconds(2),
            $"Sixteen blocking fetches took {timeToFullWidth.TotalMilliseconds:F0} ms to reach full width.");
        Assert.True(report.TimeToMaximumWidth <= timeToFullWidth);
    }

    [Fact]
    public async Task ABacklogOfBufferedRows_StopsAdmission_EvenWhileDegreeSlotsAreFree()
    {
        // Source 0 never finishes fetching, so the DRAIN is parked on it and can release nothing.
        // Sources 1 and 2 are admitted alongside it, and their buffered rows are then the only
        // thing that can refuse the remaining nine. The earlier "admit one whenever nothing is in
        // flight" rule failed exactly here: it would have walked all nine in, one at a time, with
        // the budget already blown.
        //
        // The two backers are gated as well, so their rows land only AFTER all three are admitted.
        // Without that gate the premise is not yet established when the window first looks: a
        // backer that buffers its hundred rows while the pump is still walking the queue closes the
        // window early and quite correctly leaves the third source unadmitted — a pass for the
        // dispatcher and a five-second timeout here. Gating separates the fact under test — the
        // window was open and admission stopped anyway — from a window that simply shut first.
        var holdTheDrain = NewGate();
        var holdTheBackers = NewGate();
        var sources = new List<SnapshotSource> { Fetching("head", gate: holdTheDrain.Task) };
        sources.AddRange(Enumerable.Range(1, 11).Select(index =>
            Fetching($"s{index}", rows: 100, gate: index <= 2 ? holdTheBackers.Task : null)));

        var run = RunAsync(sources, degree: 3, maxBufferedRows: 100);

        await WaitUntilAsync(() => started.Count == 3);   // Full degree, with the budget untouched.

        holdTheBackers.SetResult();
        await WaitUntilAsync(() => Volatile.Read(ref fetchesInFlight) == 1);   // Only the head is left.
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal(3, started.Count);   // Not 12, and not "one at a time forever".

        holdTheDrain.SetResult();
        var report = await run;

        Assert.Equal(12, report.SourcesDrained);
        Assert.Equal(12, report.SourcesFetched);
        Assert.Equal(1101, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    [Fact]
    public async Task ASourceLargerThanTheWholeRowBudget_StillRuns()
    {
        // The bound shapes memory; it must never refuse work outright. Every one of these is ten
        // times the budget, so a window without the drain's liveness admission would park forever.
        var sources = Enumerable.Range(0, 4).Select(index => Fetching($"s{index}", rows: 100)).ToList();

        var report = await RunAsync(sources, degree: 2, maxBufferedRows: 10);

        Assert.Equal(4, report.SourcesDrained);
        Assert.Equal(["s0", "s1", "s2", "s3"], drained);
        Assert.Equal(400, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    [Fact]
    public async Task TheRowBudgetRecovers_AsTheDrainReleasesEachBuffer()
    {
        // Same shape as above but many more sources than the budget could ever hold at once: this
        // only completes if every drained buffer gives its rows back to the window.
        var sources = Enumerable.Range(0, 20).Select(index => Fetching($"s{index}", rows: 50)).ToList();

        var report = await RunAsync(sources, degree: 4, maxBufferedRows: 60);

        Assert.Equal(20, report.SourcesDrained);
        Assert.Equal(1000, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    // ---- The per-remote-box cap --------------------------------------------------------------

    [Fact]
    public async Task OneConcurrencyGroup_NeverExceedsItsOwnCap()
    {
        var release = NewGate();
        // Registry order clusters a dealer's families, exactly as the real registry does.
        var sources = Enumerable.Range(0, 8)
            .Select(index => Fetching($"dealer-a/{index}", gate: release.Task, group: "dealer-a"))
            .ToList();

        var run = RunAsync(sources, degree: 8, maxPerGroup: 2);

        await WaitUntilAsync(() => started.Count == 2);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Equal(2, started.Count);

        release.SetResult();
        var report = await run;

        Assert.Equal(8, report.SourcesDrained);
        Assert.Equal(2, report.MaxObservedFetchesInFlight);
    }

    [Fact]
    public async Task ABlockedGroup_DoesNotBlockTheQueueBehindIt()
    {
        // The failure this pins: stopping at a group-blocked head. Registry order puts all four of
        // dealer A's families first, so a dispatcher that stopped rather than scanned past would
        // sit at width 1 and never touch dealer B — the exact clustering the real registry has.
        var release = NewGate();
        var sources = Enumerable.Range(0, 4)
            .Select(index => Fetching($"dealer-a/{index}", gate: release.Task, group: "dealer-a"))
            .Concat(Enumerable.Range(0, 4)
                .Select(index => Fetching($"dealer-b/{index}", gate: release.Task, group: "dealer-b")))
            .ToList();

        var run = RunAsync(sources, degree: 8, maxPerGroup: 1);

        await WaitUntilAsync(() => started.Count == 2);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal(["dealer-a/0", "dealer-b/0"], started.Order());

        release.SetResult();
        var report = await run;

        Assert.Equal(8, report.SourcesDrained);
        Assert.Equal(
            ["dealer-a/0", "dealer-a/1", "dealer-a/2", "dealer-a/3",
             "dealer-b/0", "dealer-b/1", "dealer-b/2", "dealer-b/3"],
            drained);
    }

    // ---- The kill switch ----------------------------------------------------------------------

    [Fact]
    public async Task DegreeOne_LetsNoTwoFetchesOverlap()
    {
        var sources = Enumerable.Range(0, 10).Select(index => Fetching($"s{index}", rows: 5)).ToList();

        var report = await RunAsync(sources, degree: 1);

        Assert.Equal(1, report.MaxObservedFetchesInFlight);
        Assert.Equal(1, Volatile.Read(ref observedPeakFetches));
        Assert.Equal(["s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "s8", "s9"], drained);
        Assert.Equal(50, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    // ---- Failure containment -------------------------------------------------------------------

    [Fact]
    public async Task AFaultedFetch_SurfacesAtItsRegistryPosition_AndStagesNothing()
    {
        var outcomes = new List<SnapshotIngestOutcome>();
        var sources = new List<SnapshotSource>
        {
            Fetching("ok-before", rows: 3),
            Fetching("broken", rows: 3, fetchThrows: new InvalidOperationException("the dealer box refused")),
            Fetching("ok-after", rows: 3),
        };

        var report = await SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions { Store = snapshot.Store, Sources = sources, Degree = 3 },
            outcomes.Add,
            TestContext.Current.CancellationToken);

        Assert.Equal(["ok-before", "broken", "ok-after"], outcomes.Select(o => o.Source.Key));
        Assert.Null(outcomes[1].Merge);
        Assert.Equal("the dealer box refused", outcomes[1].Failure!.Message);
        Assert.All(new[] { outcomes[0], outcomes[2] }, outcome => Assert.NotNull(outcome.Merge));
        Assert.Equal(3, report.SourcesDrained);

        // The faulted source never reached staging, so there is no half-populated staging table to
        // sweep and no run record claiming it merged. Its two neighbours are unaffected.
        Assert.Equal(6, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
        Assert.Equal(0, snapshot.Scalar<long>(
            "SELECT count(*) FROM meta.\"SyncRuns\" WHERE \"Source\" = 'broken'"));
    }

    [Fact]
    public async Task AFaultedDrain_IsContained_AndTheRemainingSourcesStillRun()
    {
        var outcomes = new List<SnapshotIngestOutcome>();
        var exploding = new SnapshotSource
        {
            Key = "explodes-on-drain",
            SourceScope = "explodes-on-drain",
            RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey("Code"),
            Table = snapshot.Table,
            Cadence = TimeSpan.FromMinutes(5),
            Fetch = _ => SnapshotSourceFetch.Staged(1, _ => throw new InvalidOperationException("merge blew up")),
        };

        await SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions
            {
                Store = snapshot.Store,
                Sources = [exploding, Fetching("survivor", rows: 4)],
                Degree = 2,
            },
            outcomes.Add,
            TestContext.Current.CancellationToken);

        Assert.Equal("merge blew up", outcomes[0].Failure!.Message);
        Assert.Equal(4, outcomes[1].Merge!.RowsInserted);
    }

    [Fact]
    public async Task AnExceptionFromTheCallback_IsNotSwallowed_AndNoFetchOutlivesTheRun()
    {
        // The callback is the caller's own assertion — a drill's DR check, the loop's bookkeeping.
        // Containing it would turn a failing drill into a passing one.
        var release = NewGate();
        var sources = Enumerable.Range(0, 6)
            .Select(index => Fetching($"s{index}", gate: index == 0 ? null : release.Task))
            .ToList();

        var run = SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions { Store = snapshot.Store, Sources = sources, Degree = 4 },
            _ => throw new InvalidOperationException("DRILL FAILED"),
            TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => started.Count >= 4);
        release.SetResult();

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => run);
        Assert.Equal("DRILL FAILED", failure.Message);
        Assert.Equal(0, Volatile.Read(ref fetchesInFlight));
    }

    // ---- Cancellation ------------------------------------------------------------------------

    [Fact]
    public async Task Cancellation_StopsTheDrain_ObservesEveryAdmittedFetch_AndReportsItStoppedEarly()
    {
        using var cancellation = new CancellationTokenSource();
        var release = NewGate();
        var sources = Enumerable.Range(0, 10)
            .Select(index => Fetching($"s{index}", gate: release.Task))
            .ToList();

        var run = RunAsync(sources, degree: 3, cancellationToken: cancellation.Token);
        await WaitUntilAsync(() => started.Count == 3);

        await cancellation.CancelAsync();
        release.SetResult();

        var report = await run;

        Assert.True(report.StoppedEarly);
        Assert.Equal(3, report.SourcesFetched);          // Admission stopped; nothing new started.
        Assert.Equal(0, Volatile.Read(ref fetchesInFlight));   // No worker outlived the cycle.

        // A cancelled fetch is not a crashed source. The serial loop reports a lost lease by
        // stopping quietly, so nothing is drained, nothing is reported, and every source keeps its
        // cadence due — which matters because the loop's `nextDue` is written from this callback.
        Assert.Equal(0, report.SourcesDrained);
        Assert.Empty(drained);
    }

    // ---- Mixed estates -------------------------------------------------------------------------

    [Fact]
    public async Task OnePhaseSources_InterleaveInRegistryOrder_AndStillRunInlineOnTheDrain()
    {
        // The real estate is mixed: five CSV file sources and one Cosmos change-feed source keep
        // the one-phase form for good reasons, and they merge in their registry positions.
        var sources = new List<SnapshotSource>
        {
            Fetching("sql-a", rows: 2),
            Inline("csv-b", rows: 2),
            Fetching("sql-c", rows: 2),
            Inline("csv-d", rows: 2),
        };

        var report = await RunAsync(sources, degree: 4);

        Assert.Equal(["sql-a", "csv-b", "sql-c", "csv-d"], drained);
        Assert.Equal(2, report.SourcesFetched);           // Only the two-phase sources fan out.
        Assert.Equal(4, report.SourcesDrained);
        Assert.Equal(1, observedPeakDrains);
        Assert.Equal(8, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    [Fact]
    public async Task ATwoPhaseSource_RunInlineByRunIngestAsync_ProducesTheSameMerge()
    {
        // The compatibility guarantee that let the contract widen without touching every caller:
        // a harness, an admin force-run or a drill that never fans out still runs both halves.
        var merge = await Fetching("inline-form", rows: 5).RunIngestAsync(new SnapshotSourceContext
        {
            Store = snapshot.Store,
            CancellationToken = TestContext.Current.CancellationToken,
        });

        Assert.True(merge.Succeeded);
        Assert.Equal(5, merge.RowsInserted);
        Assert.Empty(started.Except(["inline-form"]));
    }

    // ---- Helpers -------------------------------------------------------------------------------

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void TrackPeak(ref int current, ref int peak, int delta)
    {
        var observed = Interlocked.Add(ref current, delta);
        var previous = Volatile.Read(ref peak);
        while (observed > previous)
        {
            var replaced = Interlocked.CompareExchange(ref peak, observed, previous);
            if (replaced == previous)
                break;
            previous = replaced;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var elapsed = Stopwatch.StartNew();
        while (!predicate())
        {
            if (elapsed.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException("The deterministic test condition was not reached within five seconds.");
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
    }
    // ---- Narration -----------------------------------------------------------------------------

    /// <summary>
    /// The reason OnFetched exists: it must fire WHILE the drain is blocked, or it reports nothing
    /// the drain's own callback would not have reported later anyway.
    ///
    /// <para>The first prod-parity run made this concrete — 2.5 minutes of console silence while
    /// eight fetches ran to completion behind one 149-second source, which on screen is
    /// indistinguishable from a hang. Here the slow source sits at registry position 0, so nothing
    /// can drain until it finishes; every progress event asserted below therefore happened during
    /// the blackout.</para>
    /// </summary>
    [Fact]
    public async Task FetchesNarrate_WhileTheDrainIsStillBlockedOnTheSlowSourceAheadOfThem()
    {
        var slow = new TaskCompletionSource();
        var progress = new ConcurrentQueue<SnapshotFetchProgress>();
        var sources = new List<SnapshotSource>
        {
            Fetching("slow-head", rows: 2, gate: slow.Task),
            Fetching("quick-a", rows: 3),
            Fetching("quick-b", rows: 4),
        };

        var run = SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions
            {
                Store = snapshot.Store,
                Sources = sources,
                Degree = 3,
                OnFetched = progress.Enqueue,
            },
            outcome => drained.Add(outcome.Source.Key),
            TestContext.Current.CancellationToken);

        // Both quick sources finish and narrate; the drain cannot have moved, because the source it
        // is parked on has not been released yet.
        while (progress.Count < 2)
            await Task.Delay(10, TestContext.Current.CancellationToken);

        Assert.Empty(drained);
        var duringBlackout = progress.ToArray();
        Assert.Equal(["quick-a", "quick-b"], duringBlackout.Select(p => p.Source.Key).Order());

        // And each one names the source the CYCLE is waiting for, not itself — the difference
        // between "something finished" and "everything except slow-head has finished".
        Assert.All(duringBlackout, p => Assert.Equal("slow-head", p.DrainWaitingOn?.Key));
        Assert.All(duringBlackout, p => Assert.Null(p.Failure));
        Assert.Contains(duringBlackout, p => p.Source.Key == "quick-b" && p.BufferedRows == 4);

        slow.SetResult();
        await run;

        // The source that unblocks the drain reports itself, which is how the line reads
        // "*** this unblocks the drain ***" rather than naming some other source.
        var head = progress.Single(p => p.Source.Key == "slow-head");
        Assert.Same(head.Source, head.DrainWaitingOn);
        Assert.Equal(["slow-head", "quick-a", "quick-b"], drained);
    }

    /// <summary>
    /// A faulted fetch still narrates, carrying its exception. The drain reports it again later at
    /// the source's registry position — narration is an extra view, never the only one.
    /// </summary>
    [Fact]
    public async Task AFaultedFetch_Narrates_AndStillSurfacesOnTheDrain()
    {
        var progress = new ConcurrentQueue<SnapshotFetchProgress>();
        var outcomes = new List<SnapshotIngestOutcome>();
        var sources = new List<SnapshotSource>
        {
            Fetching("broken", rows: 3, fetchThrows: new InvalidOperationException("the dealer box refused")),
        };

        await SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions
            {
                Store = snapshot.Store,
                Sources = sources,
                Degree = 2,
                OnFetched = progress.Enqueue,
            },
            outcomes.Add,
            TestContext.Current.CancellationToken);

        var narrated = Assert.Single(progress);
        Assert.Equal("the dealer box refused", narrated.Failure!.Message);
        Assert.Equal(0, narrated.BufferedRows);
        Assert.Equal("the dealer box refused", outcomes.Single().Failure!.Message);
    }

    /// <summary>
    /// Narration must never decide the fate of a cycle — the DELIBERATE opposite of the drain's
    /// callback, where a throw is the caller's assertion failing and has to end the run. A console
    /// that cannot be written to is not a reason to lose three minutes of fetching.
    /// </summary>
    [Fact]
    public async Task AThrowingNarrator_IsSwallowed_AndEverySourceStillMerges()
    {
        var sources = new List<SnapshotSource>
        {
            Fetching("first", rows: 2),
            Fetching("second", rows: 3),
        };

        var report = await SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions
            {
                Store = snapshot.Store,
                Sources = sources,
                Degree = 2,
                OnFetched = _ => throw new InvalidOperationException("the console is gone"),
            },
            outcome => drained.Add(outcome.Source.Key),
            TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second"], drained);
        Assert.Equal(2, report.SourcesDrained);
        Assert.Equal(5, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    /// <summary>
    /// The width a narration line reports is the width that now EXISTS: this fetch retired and its
    /// replacement already admitted. Reporting before the admission pump would print a trough that
    /// never happened, which is worse than not reporting at all — it would make a healthy fan-out
    /// look like it was collapsing.
    /// </summary>
    [Fact]
    public async Task ANarrationLine_ReportsTheWidthAfterItsReplacementIsAdmitted()
    {
        var progress = new ConcurrentQueue<SnapshotFetchProgress>();
        var hold = new TaskCompletionSource();
        var sources = new List<SnapshotSource>
        {
            Fetching("held-a", rows: 1, gate: hold.Task),
            Fetching("held-b", rows: 1, gate: hold.Task),
            Fetching("quick", rows: 1),
        };

        var run = SnapshotIngestDispatcher.RunAsync(
            new SnapshotIngestDispatcherOptions
            {
                Store = snapshot.Store,
                Sources = sources,
                Degree = 3,
                OnFetched = progress.Enqueue,
            },
            outcome => drained.Add(outcome.Source.Key),
            TestContext.Current.CancellationToken);

        while (progress.IsEmpty)
            await Task.Delay(10, TestContext.Current.CancellationToken);

        // "quick" finished while both held sources are still running, so the line it printed has to
        // say two are still fetching — not one, and not zero.
        var quick = progress.Single(p => p.Source.Key == "quick");
        Assert.Equal(2, quick.FetchesInFlight);

        hold.SetResult();
        await run;
    }
}
