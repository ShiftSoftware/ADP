using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System.Diagnostics;
using System.Net.Sockets;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Write-gate tests against Azurite (the identical blob-lease code path production uses).
/// Skipped automatically when Azurite isn't listening on 127.0.0.1:10000 — start it with
/// <c>azurite --silent</c> (or the VS/VS Code Azurite task) to run these.
/// </summary>
public class SnapshotWriteGateTests
{
    private const string DevelopmentStorage = "UseDevelopmentStorage=true";

    private static bool AzuriteIsRunning()
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync("127.0.0.1", 10000).Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            return false;
        }
    }

    private static SnapshotWriteGateOptions Options(string gateName, TimeSpan? renewInterval = null) => new()
    {
        ConnectionString = DevelopmentStorage,
        GateName = gateName,
        LeaseDuration = TimeSpan.FromSeconds(15),
        RenewInterval = renewInterval ?? TimeSpan.FromSeconds(5),
    };

    /// <summary>
    /// Creates the gate container — the DEVELOPER's job, and here the test is the developer.
    /// The engine never creates a container (see <see cref="SnapshotBlobContainerMissingException"/>
    /// and the guard tests in <c>BlobContainerOwnershipTests</c>), so everything that drives it
    /// against a real service provisions one first, exactly as an operator does in production.
    /// </summary>
    private static async Task EnsureGateContainerAsync(
        SnapshotWriteGateOptions options, CancellationToken cancellationToken) =>
        await new BlobContainerClient(DevelopmentStorage, options.ContainerName)
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken);

    [Fact]
    public async Task SecondAcquirer_IsRefused_UntilTheFirstReleases()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var gateName = $"gate-{Guid.NewGuid():N}";
        await EnsureGateContainerAsync(Options(gateName), TestContext.Current.CancellationToken);

        var first = await SnapshotWriteGate.TryAcquireAsync(Options(gateName), TestContext.Current.CancellationToken);
        Assert.NotNull(first);
        Assert.False(first!.LostToken.IsCancellationRequested);

        // The whole point: a second instance fails loudly-but-gracefully, it doesn't wait.
        var second = await SnapshotWriteGate.TryAcquireAsync(Options(gateName), TestContext.Current.CancellationToken);
        Assert.Null(second);

        await first.DisposeAsync();

        var container = new BlobContainerClient(DevelopmentStorage, Options(gateName).ContainerName);
        var cleanMarker = WriteGateMarkerCodec.Parse(
            (await container.GetBlobClient(gateName).DownloadContentAsync(TestContext.Current.CancellationToken)).Value.Content);
        Assert.Equal(WriteGateMarkerState.Clean, cleanMarker.State);

        var third = await SnapshotWriteGate.TryAcquireAsync(Options(gateName), TestContext.Current.CancellationToken);
        Assert.NotNull(third);
        await third!.DisposeAsync();
    }

    [Fact]
    public async Task TwoDistinctGates_DoNotContend()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        await EnsureGateContainerAsync(Options("unused"), TestContext.Current.CancellationToken);

        var a = await SnapshotWriteGate.TryAcquireAsync(Options($"gate-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        var b = await SnapshotWriteGate.TryAcquireAsync(Options($"gate-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);

        Assert.NotNull(a);
        Assert.NotNull(b);

        await a!.DisposeAsync();
        await b!.DisposeAsync();
    }

    [Fact]
    public async Task AHeldGate_SurvivesItsOwnLeaseDuration_ByRenewing()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var gateName = $"gate-{Guid.NewGuid():N}";
        await EnsureGateContainerAsync(Options(gateName), TestContext.Current.CancellationToken);

        // 15 s lease, 5 s renew: by 20 s an unrenewed lease would have expired and the
        // second acquirer would win. Renewal must keep it out.
        var held = await SnapshotWriteGate.TryAcquireAsync(Options(gateName), TestContext.Current.CancellationToken);
        Assert.NotNull(held);

        await Task.Delay(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

        Assert.False(held!.LostToken.IsCancellationRequested);
        var contender = await SnapshotWriteGate.TryAcquireAsync(Options(gateName), TestContext.Current.CancellationToken);
        Assert.Null(contender);

        await held.DisposeAsync();
    }

    [Fact]
    public async Task InvalidOptions_AreRejectedUpFront()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => SnapshotWriteGate.TryAcquireAsync(new SnapshotWriteGateOptions
        {
            ConnectionString = DevelopmentStorage,
            GateName = "g",
            LeaseDuration = TimeSpan.FromSeconds(5),   // below Azure's 15 s floor
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => SnapshotWriteGate.TryAcquireAsync(new SnapshotWriteGateOptions
        {
            ConnectionString = DevelopmentStorage,
            GateName = "g",
            LeaseDuration = TimeSpan.FromSeconds(20),
            RenewInterval = TimeSpan.FromSeconds(30),   // renew slower than the lease expires
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => SnapshotWriteGate.TryAcquireAsync(new SnapshotWriteGateOptions
        {
            ConnectionString = DevelopmentStorage,
            GateName = "g",
            LeaseDuration = TimeSpan.FromSeconds(20),
            RenewInterval = TimeSpan.FromSeconds(10),   // no conservative validity window before first renewal
        }, TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentException>(() => SnapshotWriteGate.TryAcquireAsync(new SnapshotWriteGateOptions
        {
            ConnectionString = DevelopmentStorage,
            GateName = "g",
            LeaseDuration = TimeSpan.FromSeconds(20),
            RenewInterval = TimeSpan.FromSeconds(5),
            HandoffOperationTimeout = TimeSpan.FromSeconds(20),
        }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NewlyCreatedEmptyMarker_WithMatchingCreateETag_TransitionsActiveThenClean()
    {
        var session = new FakeLeaseSession(BinaryData.FromString(""));
        var options = Options($"gate-{Guid.NewGuid():N}");

        var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            freshEmptyMarkerETag: session.ETag,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(WriteGateMarkerState.Active, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(0, session.ReleaseCount);

        await gate.DisposeAsync();

        Assert.Equal(WriteGateMarkerState.Clean, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(1, session.ReleaseCount);
        Assert.Equal(1, session.RenewCount);
    }

    [Fact]
    public async Task ExistingLegacyEmptyMarker_RequiresOneTimeManualInitialization()
    {
        var session = new FakeLeaseSession(BinaryData.FromString(""));
        var options = Options($"gate-{Guid.NewGuid():N}");

        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(() =>
            SnapshotWriteGate.ActivateAcquiredLeaseAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                freshEmptyMarkerETag: null,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("existing empty legacy marker", exception.Message, StringComparison.Ordinal);
        Assert.Contains("mixed-version rollout", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task LegacyInitializer_ExactEmptyMarker_RequiresProofThenConfirmsCleanBeforeRelease()
    {
        var session = new FakeLeaseSession(BinaryData.FromBytes([]));
        var options = Options($"gate-{Guid.NewGuid():N}");
        var proofCount = 0;

        var result = await SnapshotWriteGate.InitializeAcquiredLegacyMarkerAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            (ensureOwnership, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ensureOwnership();
                proofCount++;
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        var marker = WriteGateMarkerCodec.Parse(session.Content);
        Assert.Equal(1, proofCount);
        Assert.Equal(WriteGateMarkerState.Clean, marker.State);
        Assert.Equal(result.HolderId, marker.HolderId);
        Assert.Equal(1, session.WriteCount);
        Assert.Equal(2, session.ReadCount); // exact-empty read, then conditioned-write confirmation
        Assert.Equal(1, session.RenewCount);
        Assert.Equal(1, session.ReleaseCount);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\r\n")]
    [InlineData("{\"version\":1,\"state\":\"clean\",\"holderId\":\"prior\",\"updatedAtUtc\":\"2026-08-03T00:00:00Z\"}")]
    public async Task LegacyInitializer_RefusesEveryNonemptyMarker_WithoutProofWriteOrRelease(string content)
    {
        var session = new FakeLeaseSession(BinaryData.FromString(content));
        var options = Options($"gate-{Guid.NewGuid():N}");
        var proofCount = 0;

        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(() =>
            SnapshotWriteGate.InitializeAcquiredLegacyMarkerAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                (_, _) =>
                {
                    proofCount++;
                    return Task.CompletedTask;
                },
                TestContext.Current.CancellationToken));

        Assert.Contains("not exactly the zero-byte legacy marker", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, proofCount);
        Assert.Equal(0, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task LegacyInitializer_ProofFailure_WritesNothingAndDoesNotRelease()
    {
        var session = new FakeLeaseSession(BinaryData.FromBytes([]));
        var options = Options($"gate-{Guid.NewGuid():N}");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SnapshotWriteGate.InitializeAcquiredLegacyMarkerAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                (_, _) => throw new InvalidOperationException("reconciliation mismatch"),
                TestContext.Current.CancellationToken));

        Assert.Equal("reconciliation mismatch", exception.Message);
        Assert.Equal(0, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task LegacyInitializer_UnconfirmedCleanWrite_DoesNotRelease()
    {
        var session = new FakeLeaseSession(BinaryData.FromBytes([]))
        {
            ConfirmationContentOverride = WriteGateMarkerCodec.Active(
                "unexpected-holder",
                DateTimeOffset.Parse("2026-08-03T00:00:00Z")),
        };
        var options = Options($"gate-{Guid.NewGuid():N}");

        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(() =>
            SnapshotWriteGate.InitializeAcquiredLegacyMarkerAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                (_, _) => Task.CompletedTask,
                TestContext.Current.CancellationToken));

        Assert.Contains("readback could not be confirmed", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task FreshEmptyMarker_ETagMismatch_RefusesBootstrapWithoutWorkOrRelease()
    {
        var session = new FakeLeaseSession(BinaryData.FromString(""));
        var options = Options($"gate-{Guid.NewGuid():N}");
        var successorWorkCount = 0;

        await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(async () =>
        {
            await using var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                freshEmptyMarkerETag: new ETag("\"different-create-etag\""),
                cancellationToken: TestContext.Current.CancellationToken);
            gate.EnsureOwnership();
            successorWorkCount++;
        });

        Assert.Equal(0, successorWorkCount);
        Assert.Equal(0, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task ExistingActiveMarker_RefusesUncleanTakeover_WithoutReleaseOrRewrite()
    {
        var recordedAt = DateTimeOffset.Parse("2026-08-03T00:00:00Z");
        var session = new FakeLeaseSession(WriteGateMarkerCodec.Active("prior-holder", recordedAt));
        var options = Options($"gate-{Guid.NewGuid():N}");

        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(() =>
            SnapshotWriteGate.ActivateAcquiredLeaseAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                freshEmptyMarkerETag: null,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(options.GateName, exception.GateName);
        Assert.Equal("prior-holder", exception.RecordedHolderId);
        Assert.Equal(recordedAt, exception.RecordedAtUtc);
        Assert.Contains("reconcile Cosmos and local state", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Theory]
    [InlineData("{\"version\":1,\"state\":1,\"holderId\":\"prior\",\"updatedAtUtc\":\"2026-08-03T00:00:00Z\"}")]
    [InlineData("{\"version\":2,\"state\":\"clean\",\"holderId\":\"prior\",\"updatedAtUtc\":\"2026-08-03T00:00:00Z\"}")]
    [InlineData("{\"version\":1,\"state\":\"clean\",\"updatedAtUtc\":\"2026-08-03T00:00:00Z\"}")]
    [InlineData("{\"version\":1,\"state\":\"clean\",\"state\":\"active\",\"holderId\":\"prior\",\"updatedAtUtc\":\"2026-08-03T00:00:00Z\"}")]
    public async Task MalformedOrUnsupportedMarker_FailsClosed_WithoutWorkRewriteOrRelease(string markerJson)
    {
        var session = new FakeLeaseSession(BinaryData.FromString(markerJson));
        var options = Options($"gate-{Guid.NewGuid():N}");
        var successorWorkCount = 0;

        await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(async () =>
        {
            await using var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                freshEmptyMarkerETag: null,
                cancellationToken: TestContext.Current.CancellationToken);
            gate.EnsureOwnership();
            successorWorkCount++;
        });

        Assert.Equal(0, successorWorkCount);
        Assert.Equal(0, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task ActiveMarkerWriteFailure_ReturnsNoGate_AndDoesNotReleaseTheLease()
    {
        var session = new FakeLeaseSession(BinaryData.FromString("")) { FailWriteNumber = 1 };
        var options = Options($"gate-{Guid.NewGuid():N}");

        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(() =>
            SnapshotWriteGate.ActivateAcquiredLeaseAsync(
                session,
                options,
                options.TimeProvider.GetTimestamp(),
                freshEmptyMarkerETag: session.ETag,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("active marker write could not be confirmed", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, session.WriteCount);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task CleanMarkerWriteFailure_LeavesActive_AndDoesNotReleaseTheLease()
    {
        var session = new FakeLeaseSession(BinaryData.FromString(""));
        var options = Options($"gate-{Guid.NewGuid():N}");
        var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            freshEmptyMarkerETag: session.ETag,
            cancellationToken: TestContext.Current.CancellationToken);
        session.FailWriteNumber = 2;

        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(
            async () => await gate.DisposeAsync());

        Assert.Contains("clean marker could not be confirmed", exception.Message, StringComparison.Ordinal);
        Assert.Equal(WriteGateMarkerState.Active, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(0, session.ReleaseCount);
    }

    [Fact]
    public async Task ReleaseFailure_AfterConfirmedCleanMarker_IsSafeToExpireWithoutManualRecovery()
    {
        var session = new FakeLeaseSession(BinaryData.FromString("")) { FailRelease = true };
        var options = Options($"gate-{Guid.NewGuid():N}");
        var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            freshEmptyMarkerETag: session.ETag,
            cancellationToken: TestContext.Current.CancellationToken);

        await gate.DisposeAsync();

        Assert.Equal(WriteGateMarkerState.Clean, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(1, session.ReleaseCount);
    }

    [Fact]
    public async Task RenewalIgnoringCancellation_BoundsDispose_LeavesActive_AndDoesNotRelease()
    {
        var session = new FakeLeaseSession(BinaryData.FromString("")) { BlockRenewalIgnoringCancellation = true };
        var options = new SnapshotWriteGateOptions
        {
            ConnectionString = DevelopmentStorage,
            GateName = $"gate-{Guid.NewGuid():N}",
            LeaseDuration = TimeSpan.FromSeconds(15),
            RenewInterval = TimeSpan.FromMilliseconds(20),
            HandoffOperationTimeout = TimeSpan.FromMilliseconds(100),
        };
        var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            freshEmptyMarkerETag: session.ETag,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.RenewalEntered.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        var started = Stopwatch.StartNew();
        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(
            async () => await gate.DisposeAsync());
        started.Stop();

        Assert.True(started.Elapsed < TimeSpan.FromSeconds(2), $"Dispose took {started.Elapsed}.");
        Assert.Contains("renewal loop did not stop", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(WriteGateMarkerState.Active, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(0, session.ReleaseCount);

        session.CompleteBlockedRenewal();
    }

    [Fact]
    public async Task FinalRenewIgnoringCancellation_BoundsDispose_LeavesActive_AndDoesNotRelease()
    {
        var session = new FakeLeaseSession(BinaryData.FromString("")) { BlockRenewalIgnoringCancellation = true };
        var options = OptionsWithShortHandoff();
        var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            freshEmptyMarkerETag: session.ETag,
            cancellationToken: TestContext.Current.CancellationToken);

        var started = Stopwatch.StartNew();
        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(
            async () => await gate.DisposeAsync());
        started.Stop();

        Assert.True(started.Elapsed < TimeSpan.FromSeconds(2), $"Dispose took {started.Elapsed}.");
        Assert.Contains("clean marker could not be confirmed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, session.RenewCount);
        Assert.Equal(1, session.WriteCount); // active only; clean was never attempted
        Assert.Equal(WriteGateMarkerState.Active, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(0, session.ReleaseCount);

        session.CompleteBlockedRenewal();
    }

    [Fact]
    public async Task CleanWriteIgnoringCancellation_BoundsDispose_LeavesActive_AndDoesNotRelease()
    {
        var session = new FakeLeaseSession(BinaryData.FromString("")) { BlockWriteNumber = 2 };
        var options = OptionsWithShortHandoff();
        var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            freshEmptyMarkerETag: session.ETag,
            cancellationToken: TestContext.Current.CancellationToken);

        var started = Stopwatch.StartNew();
        var exception = await Assert.ThrowsAsync<SnapshotWriteGateRecoveryRequiredException>(
            async () => await gate.DisposeAsync());
        started.Stop();

        Assert.True(started.Elapsed < TimeSpan.FromSeconds(2), $"Dispose took {started.Elapsed}.");
        Assert.True(
            exception.Message.Contains("clean marker could not be confirmed", StringComparison.OrdinalIgnoreCase),
            $"Dispose took a different failure path: {exception.Message}");
        Assert.Equal(WriteGateMarkerState.Active, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(0, session.ReleaseCount);

        session.CompleteBlockedWrite();
    }

    [Fact]
    public async Task ReleaseIgnoringCancellation_ReturnsWithinBound_AfterConfirmedClean()
    {
        var session = new FakeLeaseSession(BinaryData.FromString("")) { BlockReleaseIgnoringCancellation = true };
        var options = OptionsWithShortHandoff();
        var gate = await SnapshotWriteGate.ActivateAcquiredLeaseAsync(
            session,
            options,
            options.TimeProvider.GetTimestamp(),
            freshEmptyMarkerETag: session.ETag,
            cancellationToken: TestContext.Current.CancellationToken);

        var started = Stopwatch.StartNew();
        await gate.DisposeAsync();
        started.Stop();

        Assert.True(started.Elapsed < TimeSpan.FromSeconds(2), $"Dispose took {started.Elapsed}.");
        Assert.Equal(WriteGateMarkerState.Clean, WriteGateMarkerCodec.Parse(session.Content).State);
        Assert.Equal(1, session.ReleaseCount);

        session.CompleteBlockedRelease();
    }

    [Fact]
    public async Task UncleanMarker_RemainsRefused_AfterTheAzureLeaseExpires()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var options = Options($"gate-{Guid.NewGuid():N}");
        await EnsureGateContainerAsync(options, TestContext.Current.CancellationToken);
        var container = new BlobContainerClient(DevelopmentStorage, options.ContainerName);
        var blob = container.GetBlobClient(options.GateName);
        await blob.UploadAsync(BinaryData.FromString(""), overwrite: true, TestContext.Current.CancellationToken);

        var rawLease = blob.GetBlobLeaseClient();
        var acquired = await rawLease.AcquireAsync(options.LeaseDuration, cancellationToken: TestContext.Current.CancellationToken);
        var current = await blob.DownloadContentAsync(
            new BlobDownloadOptions
            {
                Conditions = new BlobRequestConditions { LeaseId = acquired.Value.LeaseId },
            },
            TestContext.Current.CancellationToken);
        await blob.UploadAsync(
            WriteGateMarkerCodec.Active("crashed-holder", DateTimeOffset.UtcNow),
            new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions
                {
                    LeaseId = acquired.Value.LeaseId,
                    IfMatch = current.Value.Details.ETag,
                },
            },
            TestContext.Current.CancellationToken);

        SnapshotWriteGateRecoveryRequiredException? refusal = null;
        var successorRemoteMutations = 0;
        var successorLocalMutations = 0;
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(25);
        while (refusal is null && DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var contender = await SnapshotWriteGate.TryAcquireAsync(
                    options,
                    TestContext.Current.CancellationToken);
                if (contender is not null)
                {
                    contender.EnsureOwnership();
                    successorRemoteMutations++;
                    successorLocalMutations++;
                    await contender.DisposeAsync();
                    Assert.Fail("An unclean marker was admitted after lease expiry.");
                }
            }
            catch (SnapshotWriteGateRecoveryRequiredException exception)
            {
                refusal = exception;
            }

            if (refusal is null)
                await Task.Delay(250, TestContext.Current.CancellationToken);
        }

        Assert.NotNull(refusal);
        Assert.Equal("crashed-holder", refusal!.RecordedHolderId);
        Assert.Equal(0, successorRemoteMutations);
        Assert.Equal(0, successorLocalMutations);
    }

    private sealed class FakeLeaseSession(BinaryData initialContent) : IWriteGateLeaseSession
    {
        private int etagVersion = 1;

        public string LeaseId { get; } = "fake-lease";
        public BinaryData Content { get; private set; } = initialContent;
        public ETag ETag { get; private set; } = new("\"etag-1\"");
        public int WriteCount { get; private set; }
        public int ReadCount { get; private set; }
        public int RenewCount { get; private set; }
        public int ReleaseCount { get; private set; }
        public int? FailWriteNumber { get; set; }
        public bool FailRelease { get; set; }
        public bool BlockRenewalIgnoringCancellation { get; set; }
        public int? BlockWriteNumber { get; set; }
        public bool BlockReleaseIgnoringCancellation { get; set; }
        public BinaryData? ConfirmationContentOverride { get; set; }
        public TaskCompletionSource RenewalEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource BlockedRenewal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource BlockedWrite { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource BlockedRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<WriteGateMarkerSnapshot> ReadMarkerAsync(CancellationToken cancellationToken)
        {
            ReadCount++;
            var content = ReadCount > 1 && ConfirmationContentOverride is not null
                ? ConfirmationContentOverride
                : Content;
            return Task.FromResult(new WriteGateMarkerSnapshot(content, ETag));
        }

        public async Task<ETag> WriteMarkerAsync(
            BinaryData content,
            ETag expectedETag,
            CancellationToken cancellationToken)
        {
            WriteCount++;
            if (FailWriteNumber == WriteCount)
                throw new InvalidOperationException("Injected marker-write failure.");
            if (BlockWriteNumber == WriteCount)
                await BlockedWrite.Task;
            Assert.Equal(ETag, expectedETag);
            Content = content;
            ETag = new ETag($"\"etag-{++etagVersion}\"");
            return ETag;
        }

        public Task RenewAsync(CancellationToken cancellationToken)
        {
            RenewCount++;
            if (BlockRenewalIgnoringCancellation)
            {
                RenewalEntered.TrySetResult();
                return BlockedRenewal.Task;
            }
            return Task.CompletedTask;
        }

        public void CompleteBlockedRenewal() => BlockedRenewal.TrySetResult();

        public async Task ReleaseAsync(CancellationToken cancellationToken)
        {
            ReleaseCount++;
            if (BlockReleaseIgnoringCancellation)
                await BlockedRelease.Task;
            if (FailRelease)
                throw new InvalidOperationException("Injected release failure.");
        }

        public void CompleteBlockedWrite() => BlockedWrite.TrySetResult();

        public void CompleteBlockedRelease() => BlockedRelease.TrySetResult();
    }

    /// <summary>
    /// Short enough that a blocked handoff operation ends dispose quickly, long enough that WHICH
    /// operation ends it is not decided by the thread-pool scheduler.
    ///
    /// <para><b>Why 500 ms and not 100.</b> DisposeAsync spends ONE budget across four operations
    /// in order: stop the renewal loop, renew, write the clean marker, release. Each of these tests
    /// blocks one of them and asserts on the failure that names it. At 100 ms the first step alone
    /// could exhaust the budget — the renewal loop is parked in
    /// <c>PeriodicTimer.WaitForNextTickAsync</c>, and cancelling it completes on a THREAD-POOL
    /// continuation. Running the full suite, where many test classes share a pool, that
    /// continuation regularly missed 100 ms, so dispose failed at step one and never reached the
    /// operation the test had blocked. It presented as a flake:
    /// <c>CleanWriteIgnoringCancellation_BoundsDispose</c> failed 3/3 full-suite runs and passed
    /// 22/22 in isolation, and neither CPU load nor pool-blocking load reproduced it in-process.</para>
    ///
    /// <para><b>The engine was right both times</b> — it bounded dispose and named the operation
    /// that actually failed. What was wrong was a test asserting on which of three correct outcomes
    /// a scheduler would pick. 500 ms leaves the injected block as by far the slowest step while
    /// keeping every dispose here an order of magnitude inside its 2-second assertion.</para>
    /// </summary>
    private static SnapshotWriteGateOptions OptionsWithShortHandoff() => new()
    {
        ConnectionString = DevelopmentStorage,
        GateName = $"gate-{Guid.NewGuid():N}",
        LeaseDuration = TimeSpan.FromSeconds(15),
        RenewInterval = TimeSpan.FromSeconds(5),
        HandoffOperationTimeout = TimeSpan.FromMilliseconds(500),
    };
}
