using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Deterministic safety tests for bounded row-level Cosmos concurrency. The fake transport
/// controls completion and failure order without an emulator; the real DuckDB store still
/// supplies watermark, cursor, and retry-ledger semantics.
/// </summary>
public sealed class CosmosReplicatorConcurrencySafetyTests
{
    private static IEnumerable<(string, string, int)> Rows(int count) =>
        Enumerable.Range(1, count).Select(index =>
            ($"K{index:D3}", $"code-{index}", index));

    private static CosmosDocument DefaultDocument(DirtyRow row) => new()
    {
        Id = row.PrimaryKey,
        PartitionKey = [row.PrimaryKey],
        Body = new Dictionary<string, object?>
        {
            ["Code"] = row.Values["Code"],
            ["Quantity"] = row.Values["Quantity"],
        },
    };

    private static CosmosFamilyMapping Family(
        Func<DirtyRow, bool>? predicate = null,
        Func<DirtyRow, CosmosDocument>? map = null) => new()
    {
        Family = "Widget",
        Database = "TestDatabase",
        Container = "Widgets",
        Predicate = predicate,
        Map = map ?? DefaultDocument,
    };

    private static CosmosSnapshotReplicatorOptions Options(
        TestSnapshot snapshot,
        CosmosFamilyMapping family,
        int degree = 1,
        int batchSize = 1000,
        Action? ownershipGuard = null) => new()
    {
        Table = snapshot.Table,
        Families = [family],
        BatchSize = batchSize,
        MaxInFlightRows = degree,
        OwnershipGuard = ownershipGuard,
    };

    [Fact]
    public async Task DegreeOne_PreservesRemoteCommitThenNextRowSequence()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(3));

        var events = new ConcurrentQueue<string>();
        var state = new InstrumentedStateStore(snapshot.Store, events);
        var container = new FakeContainer(events: events);
        var replicator = Replicator(state, container);

        var result = await replicator.RunOnceAsync(
            Options(snapshot, Family(), degree: 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(
        [
            "remote:Upsert:K001", "commit:K001",
            "remote:Upsert:K002", "commit:K002",
            "remote:Upsert:K003", "commit:K003",
        ], events);
        Assert.Equal(1, result.MaxObservedInFlightRows);
        Assert.Equal(3, result.BookkeepingTransactions);
        Assert.Equal(3, result.BookkeepingOutcomeRows);
        Assert.Equal(1, result.MaxRowsPerBookkeepingTransaction);
        Assert.Equal([1, 1, 1], state.CommitGroups.Select(group => group.Length));
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    [Fact]
    public async Task OutOfOrderRemoteCompletion_CommitsEveryConfirmedRowExactlyOnce()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(3));

        var gates = Rows(3).ToDictionary(
            row => row.Item1,
            _ => NewGate<CosmosTransportResponse>());
        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer
        {
            Responder = (call, token) => gates[call.Id].Task.WaitAsync(token),
        };
        var run = Replicator(state, container).RunOnceAsync(
            Options(snapshot, Family(), degree: 3),
            TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => container.Calls == 3);

        gates["K003"].SetResult(Success());
        Assert.Empty(state.SuccessfulCommits); // The wave is not terminal yet.
        gates["K001"].SetResult(Success());
        Assert.Empty(state.SuccessfulCommits); // Replacements/commit wait for the whole wave.
        gates["K002"].SetResult(Success());

        var result = await run;

        Assert.Equal(["K001", "K002", "K003"], state.SuccessfulCommits.Order());
        Assert.Equal(3, state.SuccessfulCommits.Distinct().Count());
        Assert.Equal([3], state.CommitGroups.Select(group => group.Length));
        Assert.Equal(3, result.Upserted);
        Assert.Equal(3, result.MaxObservedInFlightRows);
        Assert.Equal(1, result.BookkeepingTransactions);
        Assert.Equal(3, result.BookkeepingOutcomeRows);
        Assert.Equal(3, result.MaxRowsPerBookkeepingTransaction);
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    [Fact]
    public async Task InFlightRows_NeverExceedTheConfiguredBound()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(12));

        var release = NewGate<bool>();
        var container = new FakeContainer
        {
            Responder = async (_, token) =>
            {
                await release.Task.WaitAsync(token);
                return Success();
            },
        };
        var run = Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .RunOnceAsync(Options(snapshot, Family(), degree: 3), TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => container.Active == 3);
        Assert.Equal(3, container.Calls);
        release.SetResult(true);

        var result = await run;

        Assert.Equal(3, container.MaximumActive);
        Assert.Equal(3, result.MaxObservedInFlightRows);
        Assert.Equal(12, result.Upserted);
    }

    [Fact]
    public async Task OutcomeWave_CommitsBeforeAdmittingReplacements_AndBoundsUnstampedSuccesses()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(7));
        using var commitReached = new ManualResetEventSlim();
        using var releaseCommit = new ManualResetEventSlim();
        var observedGroups = 0;
        var state = new InstrumentedStateStore(snapshot.Store)
        {
            BeforeCommit = outcomes =>
            {
                if (Interlocked.Increment(ref observedGroups) != 1)
                    return;

                Assert.Equal(3, outcomes.Count);
                commitReached.Set();
                if (!releaseCommit.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))
                    throw new TimeoutException("The deterministic bookkeeping barrier was not released.");
            },
        };
        var container = new FakeContainer();
        var run = Task.Run(() => Replicator(state, container).RunOnceAsync(
            Options(snapshot, Family(), degree: 3),
            TestContext.Current.CancellationToken));

        try
        {
            Assert.True(commitReached.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(3, container.Calls);
            Assert.Equal(3, container.Documents.Count);
            Assert.Equal(7, snapshot.Store.CountDirtyRows(snapshot.Table));
            Assert.Empty(state.CommitGroups);
        }
        finally
        {
            releaseCommit.Set();
        }

        var result = await run;

        Assert.Equal(7, container.Calls);
        Assert.Equal([3, 3, 1], state.CommitGroups.Select(group => group.Length));
        Assert.Equal(3, result.BookkeepingTransactions);
        Assert.Equal(7, result.BookkeepingOutcomeRows);
        Assert.Equal(3, result.MaxRowsPerBookkeepingTransaction);
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    [Fact]
    public async Task MixedSuccessAndFailure_StampsOnlyConfirmedRows_AndAdvancesOneLedgerEach()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(3));

        var container = new FakeContainer
        {
            Responder = (call, _) => Task.FromResult(
                call.Id == "K002"
                    ? Failure(HttpStatusCode.BadRequest, "injected poison", requestCharge: 2)
                    : Success(requestCharge: 3)),
        };
        var state = new InstrumentedStateStore(snapshot.Store);
        var result = await Replicator(state, container)
            .RunOnceAsync(Options(snapshot, Family(), degree: 3), TestContext.Current.CancellationToken);

        Assert.Equal(3, result.RemoteAttemptedRows);
        Assert.Equal(1, result.RemoteFailedRows);
        Assert.Equal(1, result.Failed);
        Assert.Equal(2, result.Upserted);
        Assert.Equal(8, result.RequestCharge);
        Assert.Equal(1, result.BookkeepingTransactions);
        Assert.Equal(3, result.BookkeepingOutcomeRows);
        Assert.Equal(3, result.MaxRowsPerBookkeepingTransaction);
        Assert.Equal([3], state.CommitGroups.Select(group => group.Length));
        Assert.Equal(["K002"], DirtyKeys(snapshot));
        Assert.Equal(1, Attempts(snapshot, "K002"));
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Equal(0, Attempts(snapshot, "K003"));
        Assert.Null(Stamp(snapshot, "K002"));
        Assert.NotNull(Stamp(snapshot, "K001"));
        Assert.NotNull(Stamp(snapshot, "K003"));
    }

    [Fact]
    public async Task NoOpAndPreflightPoison_DoNotMaskATotalRemoteOutage()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(3));

        var family = Family(
            predicate: row => row.PrimaryKey != "K002",
            map: row => row.PrimaryKey == "K003"
                ? throw new InvalidOperationException("injected mapping poison")
                : DefaultDocument(row));
        var container = new FakeContainer
        {
            Responder = (_, _) => Task.FromResult(
                Failure(HttpStatusCode.ServiceUnavailable, "injected outage")),
        };

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .DrainAsync(Options(snapshot, family, degree: 3), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ReplicationDrainStop.SystemicFailure, result.Stopped);
        Assert.False(result.Drained);
        Assert.Equal(1, result.RemoteAttemptedRows);
        Assert.Equal(1, result.RemoteFailedRows);
        Assert.Equal(2, result.Failed); // one remote failure + one preflight failure
        Assert.Equal(1, result.Excluded); // the no-op row completed locally
        Assert.Equal(["K001", "K003"], DirtyKeys(snapshot));
        Assert.Equal(1, Attempts(snapshot, "K001"));
        Assert.Equal(0, Attempts(snapshot, "K002"));
        Assert.Equal(1, Attempts(snapshot, "K003"));
    }

    [Fact]
    public async Task PreflightPoisonAlone_DoesNotMasqueradeAsASystemicCosmosFailure()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(2));

        var family = Family(map: _ => throw new InvalidOperationException("injected mapping poison"));
        var container = new FakeContainer();

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .DrainAsync(Options(snapshot, family, degree: 2), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ReplicationDrainStop.RetryPending, result.Stopped);
        Assert.False(result.Drained);
        Assert.Equal(0, result.RemoteAttemptedRows);
        Assert.Equal(0, result.RemoteFailedRows);
        Assert.Equal(2, result.Failed);
        Assert.Equal(0, container.Calls);
        Assert.All(Rows(2), row => Assert.Equal(1, Attempts(snapshot, row.Item1)));
    }

    [Fact]
    public async Task RequestPreparationFailure_IsNotAStartedRemoteAttemptOrSystemicOutage()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        var container = new FakeContainer
        {
            Responder = (_, _) => Task.FromException<CosmosTransportResponse>(
                new CosmosRequestPreparationException(
                    "injected request preparation failure",
                    new FormatException("injected invalid partition key"))),
        };

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .DrainAsync(Options(snapshot, Family()), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ReplicationDrainStop.RetryPending, result.Stopped);
        Assert.False(result.Drained);
        Assert.Equal(0, result.RemoteAttemptedRows);
        Assert.Equal(0, result.RemoteFailedRows);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, Attempts(snapshot, "K001"));
        Assert.Equal(["K001"], DirtyKeys(snapshot));
    }

    [Fact]
    public async Task ThrownCosmosException_PreservesStatusChargeAndThrottleTelemetry()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        var metrics = new ConcurrentQueue<CosmosReplicationOperationMetric>();
        var container = new FakeContainer
        {
            Responder = (_, _) => Task.FromException<CosmosTransportResponse>(
                new CosmosException(
                    "injected throttle",
                    HttpStatusCode.TooManyRequests,
                    subStatusCode: 0,
                    activityId: "test-activity",
                    requestCharge: 4.25)),
        };

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .RunOnceAsync(new CosmosSnapshotReplicatorOptions
            {
                Table = snapshot.Table,
                Families = [Family()],
                OnOperation = metrics.Enqueue,
            }, TestContext.Current.CancellationToken);

        var metric = Assert.Single(metrics);
        Assert.Equal(HttpStatusCode.TooManyRequests, metric.StatusCode);
        Assert.Equal(4.25, metric.RequestCharge);
        Assert.False(metric.Succeeded);
        Assert.False(metric.Canceled);
        Assert.Equal(1, result.RemoteAttemptedRows);
        Assert.Equal(1, result.RemoteFailedRows);
        Assert.Equal(1, result.ThrottledRequests);
        Assert.Equal(4.25, result.RequestCharge);
    }

    [Fact]
    public async Task CoordinateMove_DeletesBeforeUpsert_AndPartialFailureRetriesIdempotently()
    {
        using var snapshot = new TestSnapshot();
        var family = Family(map: row => new CosmosDocument
        {
            Id = (string)row.Values["Code"]!,
            PartitionKey = [(string)row.Values["Code"]!],
            Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] },
        });

        snapshot.Merge([("K001", "old", 1)]);
        MarkClean(snapshot, family, "K001");
        snapshot.Merge([("K001", "new", 1)]);

        var events = new ConcurrentQueue<string>();
        var newUpsertAttempts = 0;
        var container = new FakeContainer(events: events);
        container.Documents.TryAdd("old", 0);
        container.Responder = (call, _) =>
        {
            if (call.Kind == CosmosReplicationOperationKind.Upsert
                && call.Id == "new"
                && Interlocked.Increment(ref newUpsertAttempts) == 1)
            {
                return Task.FromResult(Failure(HttpStatusCode.BadRequest, "injected upsert failure"));
            }

            if (call.Kind == CosmosReplicationOperationKind.Delete)
            {
                return Task.FromResult(container.Documents.ContainsKey(call.Id)
                    ? Success(HttpStatusCode.NoContent)
                    : Failure(HttpStatusCode.NotFound, "already absent"));
            }

            return Task.FromResult(Success());
        };
        var replicator = Replicator(new InstrumentedStateStore(snapshot.Store), container);

        var first = await replicator.RunOnceAsync(
            Options(snapshot, family), TestContext.Current.CancellationToken);

        Assert.Equal(["remote:Delete:old", "remote:Upsert:new"], RemoteEvents(events));
        Assert.Equal(1, first.Deleted);
        Assert.Equal(0, first.Upserted);
        Assert.Equal(1, first.Failed);
        Assert.Equal(1, Attempts(snapshot, "K001"));
        Assert.Equal("old", ReplicationStamp.Parse((string)Stamp(snapshot, "K001")!).Families["Widget"].Id);
        Assert.False(container.Documents.ContainsKey("old"));
        Assert.False(container.Documents.ContainsKey("new"));

        events.Clear();
        var retry = await replicator.RunOnceAsync(
            Options(snapshot, family), TestContext.Current.CancellationToken);

        Assert.Equal(["remote:Delete:old", "remote:Upsert:new"], RemoteEvents(events));
        Assert.Equal(1, retry.Deleted); // idempotent 404 delete is success
        Assert.Equal(1, retry.Upserted);
        Assert.Equal(0, retry.Failed);
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
        Assert.Equal("new", ReplicationStamp.Parse((string)Stamp(snapshot, "K001")!).Families["Widget"].Id);
        Assert.True(container.Documents.ContainsKey("new"));
    }

    [Fact]
    public async Task CancellationBeforeAdmission_StartsNoRemoteOrLocalWork()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(2));
        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Replicator(state, container).RunOnceAsync(Options(snapshot, Family(), degree: 2), cancellation.Token));

        Assert.Equal(0, state.ReadCalls);
        Assert.Equal(0, container.Calls);
        Assert.Empty(state.SuccessfulCommits);
        Assert.Empty(state.FailedCommits);
        Assert.Equal(2, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    [Fact]
    public async Task CancellationDuringCalls_StopsAdmission_AndDoesNotBurnTheLedger()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(5));
        using var cancellation = new CancellationTokenSource();
        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer
        {
            Responder = async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return Success();
            },
        };
        var run = Replicator(state, container)
            .RunOnceAsync(Options(snapshot, Family(), degree: 2), cancellation.Token);

        await WaitUntilAsync(() => container.Active == 2);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(2, container.Calls);
        Assert.Empty(state.SuccessfulCommits);
        Assert.Empty(state.FailedCommits);
        Assert.Equal(5, snapshot.Store.CountDirtyRows(snapshot.Table));
        Assert.All(Rows(5), row => Assert.Equal(0, Attempts(snapshot, row.Item1)));
    }

    [Fact]
    public async Task CancellationAfterRemoteSuccessBeforeCommit_LeavesTheLandedRowDirty()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        using var cancellation = new CancellationTokenSource();
        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer
        {
            Responder = (_, _) =>
            {
                cancellation.Cancel();
                return Task.FromResult(Success());
            },
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Replicator(state, container).RunOnceAsync(Options(snapshot, Family()), cancellation.Token));

        Assert.True(container.Documents.ContainsKey("K001"));
        Assert.Empty(state.SuccessfulCommits);
        Assert.Empty(state.FailedCommits);
        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Null(Stamp(snapshot, "K001"));
    }

    [Fact]
    public async Task ExpiredHolder_AdmitsNoLaterRow_AndCannotCommitItsCompletedCall()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(3));
        var clock = new ManualFenceClock();
        using var oldFence = Fence(clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken,
            oldFence.LostToken);
        var secondCall = NewGate<CosmosTransportResponse>();
        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer
        {
            Responder = (call, token) => call.Id == "K002"
                ? secondCall.Task.WaitAsync(token)
                : Task.FromResult(Success()),
        };
        var run = Replicator(state, container).RunOnceAsync(
            Options(snapshot, Family(), ownershipGuard: oldFence.EnsureOwnership),
            linked.Token);

        await WaitUntilAsync(() => container.Calls == 2 && state.SuccessfulCommits.Contains("K001"));
        clock.Advance(TimeSpan.FromSeconds(15));
        using var successorFence = Fence(clock);
        successorFence.EnsureOwnership();
        secondCall.SetResult(Success());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.True(oldFence.LostToken.IsCancellationRequested);
        Assert.Equal(2, container.Calls); // K003 was never admitted after expiry.
        Assert.True(container.Documents.ContainsKey("K002"));
        Assert.False(container.Documents.ContainsKey("K003"));
        Assert.Equal(["K001"], state.SuccessfulCommits);
        Assert.Empty(state.FailedCommits);
        Assert.Equal(["K002", "K003"], DirtyKeys(snapshot));
        Assert.All(Rows(3), row => Assert.Equal(0, Attempts(snapshot, row.Item1)));
        Assert.Null(Stamp(snapshot, "K002"));
        Assert.Null(Stamp(snapshot, "K003"));
    }

    [Fact]
    public async Task RemoteOutcomeCompletedBeforeExpiry_CannotCommitAfterSuccessorAcquires()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        var clock = new ManualFenceClock();
        using var oldFence = Fence(clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken,
            oldFence.LostToken);
        using var commitReached = new ManualResetEventSlim();
        using var releaseCommit = new ManualResetEventSlim();
        var guardCalls = 0;

        void BlockingOwnershipGuard()
        {
            // One row invokes the fence at admission, before its upsert, then at commit.
            if (Interlocked.Increment(ref guardCalls) == 3)
            {
                commitReached.Set();
                if (!releaseCommit.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))
                    throw new TimeoutException("The deterministic commit fence was not released.");
            }
            oldFence.EnsureOwnership();
        }

        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer();
        var run = Task.Run(() => Replicator(state, container).RunOnceAsync(
            Options(snapshot, Family(), ownershipGuard: BlockingOwnershipGuard),
            linked.Token));

        try
        {
            Assert.True(commitReached.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.True(container.Documents.ContainsKey("K001")); // remote outcome is already terminal
            Assert.Empty(state.SuccessfulCommits);

            clock.Advance(TimeSpan.FromSeconds(15));
            using var successorFence = Fence(clock);
            successorFence.EnsureOwnership();
            releaseCommit.Set();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        }
        finally
        {
            releaseCommit.Set();
        }

        Assert.True(oldFence.LostToken.IsCancellationRequested);
        Assert.Equal(1, container.Calls);
        Assert.Empty(state.SuccessfulCommits);
        Assert.Empty(state.FailedCommits);
        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Null(Stamp(snapshot, "K001"));
    }

    [Fact]
    public async Task StoreFailureAfterRemoteSuccess_RestartRepairsIdempotently()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        var state = new InstrumentedStateStore(snapshot.Store) { ThrowNextSuccessfulCommit = true };
        var container = new FakeContainer();

        await Assert.ThrowsAsync<IOException>(() =>
            Replicator(state, container).RunOnceAsync(
                Options(snapshot, Family()), TestContext.Current.CancellationToken));

        Assert.True(container.Documents.ContainsKey("K001"));
        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(0, Attempts(snapshot, "K001"));

        var retry = await Replicator(state, container).RunOnceAsync(
            Options(snapshot, Family()), TestContext.Current.CancellationToken);

        Assert.Equal(1, retry.Upserted);
        Assert.Equal(2, container.Calls);
        Assert.Single(container.Documents);
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    [Fact]
    public async Task SetBasedFailureStatementError_RollsBackEarlierSuccessStatement()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(3));
        // The success UPDATE runs first. Two identical failure errors then violate this
        // index in the second set-based UPDATE, proving the first statement rolls back too.
        snapshot.Store.Execute(
            "CREATE UNIQUE INDEX \"UX_Widget_ReplicationError\" " +
            "ON data.\"Widget\" (\"_ReplicationError\")");
        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer
        {
            Responder = (call, _) => call.Id == "K001"
                ? Task.FromResult(Success())
                : Task.FromException<CosmosTransportResponse>(
                    new IOException("identical injected store constraint value")),
        };

        await Assert.ThrowsAnyAsync<Exception>(() =>
            Replicator(state, container).RunOnceAsync(
                Options(snapshot, Family(), degree: 3),
                TestContext.Current.CancellationToken));

        Assert.True(container.Documents.ContainsKey("K001"));
        Assert.Equal(["K001", "K002", "K003"], DirtyKeys(snapshot));
        Assert.All(Rows(3), row =>
        {
            Assert.Equal(0, Attempts(snapshot, row.Item1));
            Assert.Null(Stamp(snapshot, row.Item1));
        });
        Assert.Empty(state.SuccessfulCommits);
        Assert.Empty(state.FailedCommits);
        Assert.Empty(state.CommitGroups);
    }

    [Fact]
    public void SetBasedSuccess_BindsCapturedWatermarkStampAndReplicatedAtToTheirColumns()
    {
        using var snapshot = new TestSnapshot();
        var captured = new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        snapshot.Merge([("K001", "code", 1)], sourceModified: captured);
        var row = Assert.Single(snapshot.Store.ReadDirtyRows(snapshot.Table));
        var stamp = "{\"Families\":{\"Widget\":{\"Id\":\"doc-1\",\"PartitionKey\":[\"p-1\"]}}}";
        var fenceCalls = 0;

        new SnapshotReplicationStateStore(snapshot.Store).CommitReplicationOutcomes(
            snapshot.Table,
            [ReplicationStateOutcome.Replicated(row.PrimaryKey, row.CapturedLastModified, stamp)],
            () => Interlocked.Increment(ref fenceCalls));

        Assert.Equal(2, fenceCalls);
        Assert.Equal(captured, LastReplicationDate(snapshot, "K001"));
        Assert.Equal(stamp, Stamp(snapshot, "K001"));
        Assert.NotNull(snapshot.ScalarOrNull(
            "SELECT \"_ReplicatedAt\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?",
            "K001"));
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    [Fact]
    public void MalformedOutcomeGroups_FailClosedBeforeFenceOrMutation()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        var row = Assert.Single(snapshot.Store.ReadDirtyRows(snapshot.Table));
        var state = new SnapshotReplicationStateStore(snapshot.Store);
        var fenceCalls = 0;
        Action fence = () => Interlocked.Increment(ref fenceCalls);
        var invalidKind = new ReplicationStateOutcome(
            row.PrimaryKey,
            row.CapturedLastModified,
            (ReplicationStateOutcomeKind)int.MaxValue,
            ReplicationStamp: null,
            Error: null);
        var failedWithStamp = new ReplicationStateOutcome(
            row.PrimaryKey,
            row.CapturedLastModified,
            ReplicationStateOutcomeKind.Failed,
            ReplicationStamp: "invalid",
            Error: "failure");

        Assert.Throws<ArgumentException>(() =>
            state.CommitReplicationOutcomes(snapshot.Table, [invalidKind], fence));
        Assert.Throws<ArgumentException>(() =>
            state.CommitReplicationOutcomes(snapshot.Table, [failedWithStamp], fence));
        Assert.Throws<ArgumentException>(() =>
            state.CommitReplicationOutcomes(
                snapshot.Table,
                [
                    ReplicationStateOutcome.Failed(row.PrimaryKey, row.CapturedLastModified, "one"),
                    ReplicationStateOutcome.Failed(row.PrimaryKey, row.CapturedLastModified, "two"),
                ],
                fence));

        Assert.Equal(0, fenceCalls);
        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Null(Stamp(snapshot, "K001"));
    }

    [Fact]
    public async Task CancellationObservedAtPreCommit_RollsBackAppliedSuccessSet()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        using var cancellation = new CancellationTokenSource();
        var guardCalls = 0;
        void CancelAtPreCommit()
        {
            // Degree one: admission, before upsert, pre-begin, then pre-commit.
            if (Interlocked.Increment(ref guardCalls) == 4)
                cancellation.Cancel();
        }

        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Replicator(state, container).RunOnceAsync(
                Options(snapshot, Family(), ownershipGuard: CancelAtPreCommit),
                cancellation.Token));

        Assert.Equal(4, guardCalls);
        Assert.True(container.Documents.ContainsKey("K001"));
        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Null(Stamp(snapshot, "K001"));
        Assert.Empty(state.CommitGroups);
    }

    [Fact]
    public async Task DuplicateUpsertCoordinates_FailEveryImplicatedRowBeforeRemoteWork()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(2));
        var family = Family(map: _ => new CosmosDocument
        {
            Id = "shared",
            PartitionKey = ["shared"],
            Body = new Dictionary<string, object?> { ["Value"] = "shared" },
        });
        var container = new FakeContainer();

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .RunOnceAsync(Options(snapshot, family, degree: 2), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Failed);
        Assert.Equal(0, result.RemoteAttemptedRows);
        Assert.Equal(0, container.Calls);
        Assert.Equal(2, snapshot.Store.CountDirtyRows(snapshot.Table));
        Assert.All(Rows(2), row => Assert.Equal(1, Attempts(snapshot, row.Item1)));
    }

    [Fact]
    public async Task DeleteAndUpsertAtTheSameCoordinates_BothFailClosedBeforeRemoteWork()
    {
        using var snapshot = new TestSnapshot();
        var family = Family(map: _ => new CosmosDocument
        {
            Id = "shared",
            PartitionKey = ["shared"],
            Body = new Dictionary<string, object?> { ["Value"] = "shared" },
        });

        snapshot.Merge([("A", "shared", 1), ("B", "shared", 2)]);
        MarkClean(snapshot, family, "A");
        snapshot.Merge([("B", "shared", 2)], force: true);
        var container = new FakeContainer();

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .RunOnceAsync(Options(snapshot, family, degree: 2), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Failed);
        Assert.Equal(0, result.RemoteAttemptedRows);
        Assert.Equal(0, container.Calls);
        Assert.Equal(["A", "B"], DirtyKeys(snapshot));
        Assert.Equal(1, Attempts(snapshot, "A"));
        Assert.Equal(1, Attempts(snapshot, "B"));
    }

    [Fact]
    public async Task StoreCommits_RemainSerializedWhileRemoteRowsCompleteConcurrently()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(20));
        var state = new InstrumentedStateStore(snapshot.Store)
        {
            CommitDelay = TimeSpan.FromMilliseconds(2),
        };

        var result = await Replicator(state, new FakeContainer()).RunOnceAsync(
            Options(snapshot, Family(), degree: 8), TestContext.Current.CancellationToken);

        Assert.Equal(20, result.Upserted);
        Assert.Equal(1, state.MaximumConcurrentCommits);
        Assert.Equal(20, state.SuccessfulCommits.Count);
        Assert.Equal([8, 8, 4], state.CommitGroups.Select(group => group.Length));
        Assert.Equal(3, result.BookkeepingTransactions);
        Assert.Equal(20, result.BookkeepingOutcomeRows);
        Assert.Equal(8, result.MaxRowsPerBookkeepingTransaction);
    }

    [Fact]
    public async Task ConcurrentRunOnceCalls_OnOneReplicator_AreSerialized()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(1));
        var release = NewGate<bool>();
        var state = new InstrumentedStateStore(snapshot.Store);
        var container = new FakeContainer
        {
            Responder = async (_, token) =>
            {
                await release.Task.WaitAsync(token);
                return Success();
            },
        };
        var replicator = Replicator(state, container);

        var first = replicator.RunOnceAsync(Options(snapshot, Family()), TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => container.Calls == 1);
        var second = replicator.RunOnceAsync(Options(snapshot, Family()), TestContext.Current.CancellationToken);

        Assert.Equal(1, state.ReadCalls);
        Assert.Equal(1, container.Calls);
        release.SetResult(true);

        Assert.Equal(1, (await first).RowsRead);
        Assert.Equal(0, (await second).RowsRead);
        Assert.Equal(2, state.ReadCalls);
        Assert.Equal(1, container.Calls);
    }

    [Fact]
    public async Task MaxBatchCursor_MovesPastFailures_AndChargesEachOnlyOncePerDrain()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(6));
        var container = new FakeContainer
        {
            Responder = (call, _) => Task.FromResult(
                call.Id is "K001" or "K003"
                    ? Failure(HttpStatusCode.BadRequest, "injected row failure")
                    : Success()),
        };

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .DrainAsync(
                Options(snapshot, Family(), degree: 2, batchSize: 2),
                maxBatches: 2,
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ReplicationDrainStop.BatchBound, result.Stopped);
        Assert.False(result.Drained);
        Assert.Equal(2, result.Batches);
        Assert.Equal(4, result.RowsRead);
        Assert.Equal(2, result.Failed);
        Assert.Equal(2, result.BookkeepingTransactions);
        Assert.Equal(4, result.BookkeepingOutcomeRows);
        Assert.Equal(2, result.MaxRowsPerBookkeepingTransaction);
        Assert.Equal(1, Attempts(snapshot, "K001"));
        Assert.Equal(1, Attempts(snapshot, "K003"));
        Assert.Equal(0, Attempts(snapshot, "K005"));
        Assert.Equal(0, Attempts(snapshot, "K006"));
        Assert.Equal(["K001", "K003", "K005", "K006"], DirtyKeys(snapshot));
    }

    [Fact]
    public async Task ExactBatchAtBound_ReportsQueueEmptyWhenNoDirtyRowRemains()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(2));

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), new FakeContainer())
            .DrainAsync(
                Options(snapshot, Family(), degree: 2, batchSize: 2),
                maxBatches: 1,
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Drained);
        Assert.Equal(ReplicationDrainStop.QueueEmpty, result.Stopped);
        Assert.Equal(1, result.Batches);
        Assert.Equal(2, result.RowsRead);
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
    }

    [Fact]
    public async Task UnderfullMixedFailure_ReportsRetryPendingInsteadOfDrained()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(2));
        var container = new FakeContainer
        {
            Responder = (call, _) => Task.FromResult(
                call.Id == "K001"
                    ? Failure(HttpStatusCode.BadRequest, "injected row failure")
                    : Success()),
        };

        var result = await Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .DrainAsync(
                Options(snapshot, Family(), degree: 2, batchSize: 10),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Drained);
        Assert.Equal(ReplicationDrainStop.RetryPending, result.Stopped);
        Assert.Equal(1, result.Failed);
        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(1, Attempts(snapshot, "K001"));
        Assert.Equal(0, Attempts(snapshot, "K002"));
    }

    [Fact]
    public async Task ConcurrentPump_ReconcilesToZeroUnexplainedVerdicts()
    {
        using var snapshot = new TestSnapshot();
        var rows = Rows(3).ToArray();
        snapshot.Merge(rows);
        var family = Family();

        await Replicator(new InstrumentedStateStore(snapshot.Store), new FakeContainer())
            .DrainAsync(
                Options(snapshot, family, degree: 3, batchSize: 2),
                cancellationToken: TestContext.Current.CancellationToken);

        var actual = rows.Select(row => new JsonObject
        {
            ["id"] = row.Item1,
            ["Code"] = row.Item2,
            ["Quantity"] = row.Item3,
        }).ToArray();
        var recon = await new SnapshotRecon(snapshot.Store, cosmosClient: null).RunAsync(
            new SnapshotReconOptions
            {
                Tables = [snapshot.Table],
                Families =
                [
                    new SnapshotReconFamily
                    {
                        Mapping = family,
                        EnumerationSql = "SELECT * FROM c",
                        PartitionKeyPaths = ["/id"],
                    },
                ],
            },
            (_, _) => AsAsync(actual),
            TestContext.Current.CancellationToken);

        var verdict = Assert.Single(recon.Families);
        Assert.Equal(3, verdict.ExpectedDocs);
        Assert.Equal(3, verdict.ActualDocs);
        Assert.Equal(3, verdict.InSync);
        Assert.Equal(0, verdict.PendingAdd);
        Assert.Equal(0, verdict.PendingUpdate);
        Assert.Equal(0, verdict.PendingDelete);
        Assert.Equal(0, verdict.Divergent);
        Assert.Equal(0, verdict.Orphans);
        Assert.Equal(0, verdict.DuplicateExpectedCoordinates);
        Assert.Equal(0, verdict.ContendedDelete);
    }

    [Fact]
    public async Task DuplicateCoordinatesAcrossPages_AreBlockedByMandatoryPostRunRecon()
    {
        using var snapshot = new TestSnapshot();
        snapshot.Merge(Rows(2));
        var family = Family(map: _ => new CosmosDocument
        {
            Id = "shared",
            PartitionKey = ["shared"],
            Body = new Dictionary<string, object?>
            {
                ["Partition"] = "shared",
                ["Value"] = "shared",
            },
        });
        var container = new FakeContainer();

        await Replicator(new InstrumentedStateStore(snapshot.Store), container).DrainAsync(
            Options(snapshot, family, degree: 2, batchSize: 1),
            maxBatches: 3,
            cancellationToken: TestContext.Current.CancellationToken);

        // The two rows were never concurrently in one materialized page, so the bounded
        // in-flight preflight cannot see their global collision. Full recon is the required
        // acceptance backstop and must make that limitation visible.
        Assert.Equal(2, container.Calls);
        Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
        var actual = new[]
        {
            new JsonObject
            {
                ["id"] = "shared",
                ["Partition"] = "shared",
                ["Value"] = "shared",
            },
        };
        var recon = await new SnapshotRecon(snapshot.Store, cosmosClient: null).RunAsync(
            new SnapshotReconOptions
            {
                Tables = [snapshot.Table],
                Families =
                [
                    new SnapshotReconFamily
                    {
                        Mapping = family,
                        EnumerationSql = "SELECT * FROM c",
                        PartitionKeyPaths = ["/Partition"],
                    },
                ],
            },
            (_, _) => AsAsync(actual),
            TestContext.Current.CancellationToken);

        var verdict = Assert.Single(recon.Families);
        Assert.Equal(1, verdict.DuplicateExpectedCoordinates);
        Assert.Contains(recon.Samples, sample =>
            sample.Kind == "DuplicateExpectedCoordinates" && sample.CosmosId == "shared");
    }

    [Fact]
    public async Task StaleInFlightFailure_DoesNotBurnTheNewerVersionLedger()
    {
        using var snapshot = new TestSnapshot();
        var firstVersion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var secondVersion = firstVersion.AddMinutes(1);
        snapshot.Merge([("K001", "old", 1)], sourceModified: firstVersion);
        var release = NewGate<CosmosTransportResponse>();
        var container = new FakeContainer
        {
            Responder = (_, token) => release.Task.WaitAsync(token),
        };
        var run = Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .RunOnceAsync(Options(snapshot, Family()), TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => container.Calls == 1);
        snapshot.Merge([("K001", "new", 2)], sourceModified: secondVersion);
        release.SetResult(Failure(HttpStatusCode.BadRequest, "old version failed"));

        var result = await run;

        Assert.Equal(1, result.Failed);
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(secondVersion, snapshot.Store.ReadDirtyRows(snapshot.Table).Single().CapturedLastModified);
    }

    [Fact]
    public async Task SuccessForAnOlderInFlightVersion_TracksWhatLanded_AndLeavesTheNewerVersionDirty()
    {
        using var snapshot = new TestSnapshot();
        var family = Family(map: row => new CosmosDocument
        {
            Id = (string)row.Values["Code"]!,
            PartitionKey = [(string)row.Values["Code"]!],
            Body = new Dictionary<string, object?> { ["Code"] = row.Values["Code"] },
        });
        var firstVersion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var secondVersion = firstVersion.AddMinutes(1);
        snapshot.Merge([("K001", "old", 1)], sourceModified: firstVersion);
        var release = NewGate<CosmosTransportResponse>();
        var container = new FakeContainer
        {
            Responder = (_, token) => release.Task.WaitAsync(token),
        };
        var run = Replicator(new InstrumentedStateStore(snapshot.Store), container)
            .RunOnceAsync(Options(snapshot, family), TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => container.Calls == 1);
        snapshot.Merge([("K001", "new", 2)], sourceModified: secondVersion);
        release.SetResult(Success());

        await run;

        Assert.Equal(["K001"], DirtyKeys(snapshot));
        Assert.Equal(0, Attempts(snapshot, "K001"));
        Assert.Equal(firstVersion, LastReplicationDate(snapshot, "K001"));
        Assert.Equal(
            "old",
            ReplicationStamp.Parse((string)Stamp(snapshot, "K001")!).Families["Widget"].Id);
    }

    private static CosmosSnapshotReplicator Replicator(
        InstrumentedStateStore state,
        FakeContainer container) =>
        new(state, new FakeTransport(container));

    private static LeaseOwnershipFence Fence(ManualFenceClock clock) => new(
        clock,
        leaseDuration: TimeSpan.FromSeconds(15),
        renewInterval: TimeSpan.FromSeconds(5),
        confirmationStarted: clock.GetTimestamp());

    private static void MarkClean(
        TestSnapshot snapshot,
        CosmosFamilyMapping family,
        string key)
    {
        var row = snapshot.Store.ReadDirtyRows(snapshot.Table).Single(item => item.PrimaryKey == key);
        var stamp = new ReplicationStamp();
        stamp.Families[family.Family] = ReplicationStamp.ToCoordinates(family.Map(row));
        snapshot.Store.MarkReplicated(
            snapshot.Table,
            row.PrimaryKey,
            row.CapturedLastModified,
            stamp.ToJson());
    }

    private static int Attempts(TestSnapshot snapshot, string key) =>
        snapshot.Scalar<int>(
            "SELECT \"_ReplicationAttempts\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?",
            key);

    private static object? Stamp(TestSnapshot snapshot, string key) =>
        snapshot.ScalarOrNull(
            "SELECT \"_ReplicationStamp\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?",
            key);

    private static DateTime? LastReplicationDate(TestSnapshot snapshot, string key)
    {
        var value = snapshot.ScalarOrNull(
            "SELECT \"_LastReplicationDate\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?",
            key);
        return value is null ? null : DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
    }

    private static string[] DirtyKeys(TestSnapshot snapshot) =>
        snapshot.Store.ReadDirtyRows(snapshot.Table, null, 1000)
            .Select(row => row.PrimaryKey)
            .ToArray();

    private static string[] RemoteEvents(ConcurrentQueue<string> events) =>
        events.Where(value => value.StartsWith("remote:", StringComparison.Ordinal)).ToArray();

    private static CosmosTransportResponse Success(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        double requestCharge = 1) =>
        new(statusCode, IsSuccessStatusCode: true, requestCharge, RetryAfter: null, ErrorMessage: null);

    private static CosmosTransportResponse Failure(
        HttpStatusCode statusCode,
        string error,
        double requestCharge = 0) =>
        new(statusCode, IsSuccessStatusCode: false, requestCharge, RetryAfter: null, error);

    private static TaskCompletionSource<T> NewGate<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async IAsyncEnumerable<JsonObject> AsAsync(IEnumerable<JsonObject> documents)
    {
        foreach (var document in documents)
            yield return document;
        await Task.CompletedTask;
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

    private sealed record RemoteCall(
        CosmosReplicationOperationKind Kind,
        string Id,
        CosmosDocument? Document = null);

    private sealed class ManualFenceClock : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref timestamp);

        public void Advance(TimeSpan by) => Interlocked.Add(ref timestamp, by.Ticks);
    }

    private sealed class FakeTransport(FakeContainer container) : ICosmosSnapshotTransport
    {
        public ICosmosSnapshotContainer GetContainer(string database, string containerName) => container;
    }

    private sealed class FakeContainer : ICosmosSnapshotContainer
    {
        private int calls;
        private int active;
        private int maximumActive;
        private readonly ConcurrentQueue<string>? events;

        public FakeContainer(ConcurrentQueue<string>? events = null)
        {
            this.events = events;
            Responder = DefaultResponseAsync;
        }

        public Func<RemoteCall, CancellationToken, Task<CosmosTransportResponse>> Responder { get; set; }

        public ConcurrentDictionary<string, byte> Documents { get; } = new(StringComparer.Ordinal);

        public int Calls => Volatile.Read(ref calls);
        public int Active => Volatile.Read(ref active);
        public int MaximumActive => Volatile.Read(ref maximumActive);

        public Task<CosmosTransportResponse> UpsertAsync(
            CosmosDocument document,
            CancellationToken cancellationToken) =>
            InvokeAsync(new RemoteCall(CosmosReplicationOperationKind.Upsert, document.Id, document), cancellationToken);

        public Task<CosmosTransportResponse> DeleteAsync(
            string id,
            IReadOnlyList<object?> partitionKey,
            CancellationToken cancellationToken) =>
            InvokeAsync(new RemoteCall(CosmosReplicationOperationKind.Delete, id), cancellationToken);

        private async Task<CosmosTransportResponse> InvokeAsync(
            RemoteCall call,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls);
            var nowActive = Interlocked.Increment(ref active);
            UpdateMaximum(nowActive);
            events?.Enqueue($"remote:{call.Kind}:{call.Id}");

            try
            {
                var response = await Responder(call, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    if (call.Kind == CosmosReplicationOperationKind.Upsert)
                        Documents[call.Id] = 0;
                    else
                        Documents.TryRemove(call.Id, out _);
                }

                return response;
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        }

        private Task<CosmosTransportResponse> DefaultResponseAsync(
            RemoteCall call,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (call.Kind == CosmosReplicationOperationKind.Upsert)
                return Task.FromResult(Success());

            return Task.FromResult(Documents.ContainsKey(call.Id)
                ? Success(HttpStatusCode.NoContent)
                : Failure(HttpStatusCode.NotFound, "already absent"));
        }

        private void UpdateMaximum(int observed)
        {
            var previous = Volatile.Read(ref maximumActive);
            while (observed > previous)
            {
                var replaced = Interlocked.CompareExchange(ref maximumActive, observed, previous);
                if (replaced == previous)
                    return;
                previous = replaced;
            }
        }
    }

    private sealed class InstrumentedStateStore : IReplicationStateStore
    {
        private readonly IReplicationStateStore inner;
        private readonly ConcurrentQueue<string>? events;
        private int readCalls;
        private int activeCommits;
        private int maximumConcurrentCommits;
        private int throwNextSuccessfulCommit;

        public InstrumentedStateStore(SnapshotStore store, ConcurrentQueue<string>? events = null)
        {
            inner = new SnapshotReplicationStateStore(store);
            this.events = events;
        }

        public ConcurrentQueue<string> SuccessfulCommits { get; } = new();
        public ConcurrentQueue<string> FailedCommits { get; } = new();
        public ConcurrentQueue<string[]> CommitGroups { get; } = new();

        public int ReadCalls => Volatile.Read(ref readCalls);
        public int MaximumConcurrentCommits => Volatile.Read(ref maximumConcurrentCommits);
        public TimeSpan CommitDelay { get; init; }
        public Action<IReadOnlyList<ReplicationStateOutcome>>? BeforeCommit { get; init; }

        public bool ThrowNextSuccessfulCommit
        {
            init => throwNextSuccessfulCommit = value ? 1 : 0;
        }

        public IReadOnlyList<DirtyRow> ReadDirtyRows(
            SnapshotTableDefinition table,
            string? afterPrimaryKey,
            int limit)
        {
            Interlocked.Increment(ref readCalls);
            return inner.ReadDirtyRows(table, afterPrimaryKey, limit);
        }

        public void PruneReconOps(SnapshotTableDefinition table) => inner.PruneReconOps(table);

        public void AppendReconOp(ReplicationReconOperation operation) => inner.AppendReconOp(operation);

        public void CommitReplicationOutcomes(
            SnapshotTableDefinition table,
            IReadOnlyList<ReplicationStateOutcome> outcomes,
            Action ensureCommitAllowed)
        {
            EnterCommit();
            try
            {
                if (outcomes.Any(outcome => outcome.Kind == ReplicationStateOutcomeKind.Replicated)
                    && Interlocked.Exchange(ref throwNextSuccessfulCommit, 0) == 1)
                {
                    throw new IOException("Injected crash between remote success and local stamp.");
                }
                DelayCommit();
                BeforeCommit?.Invoke(outcomes);
                inner.CommitReplicationOutcomes(table, outcomes, ensureCommitAllowed);
                CommitGroups.Enqueue(outcomes.Select(outcome => outcome.PrimaryKey).ToArray());
                foreach (var outcome in outcomes)
                {
                    if (outcome.Kind == ReplicationStateOutcomeKind.Replicated)
                    {
                        SuccessfulCommits.Enqueue(outcome.PrimaryKey);
                        events?.Enqueue($"commit:{outcome.PrimaryKey}");
                    }
                    else
                    {
                        FailedCommits.Enqueue(outcome.PrimaryKey);
                        events?.Enqueue($"failure:{outcome.PrimaryKey}");
                    }
                }
            }
            finally
            {
                ExitCommit();
            }
        }

        private void EnterCommit()
        {
            var observed = Interlocked.Increment(ref activeCommits);
            var previous = Volatile.Read(ref maximumConcurrentCommits);
            while (observed > previous)
            {
                var replaced = Interlocked.CompareExchange(ref maximumConcurrentCommits, observed, previous);
                if (replaced == previous)
                    return;
                previous = replaced;
            }
        }

        private void ExitCommit() => Interlocked.Decrement(ref activeCommits);

        private void DelayCommit()
        {
            if (CommitDelay > TimeSpan.Zero)
                Thread.Sleep(CommitDelay);
        }
    }
}
