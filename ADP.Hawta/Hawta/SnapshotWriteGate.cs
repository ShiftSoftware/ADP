using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Hawta;

public sealed class SnapshotWriteGateOptions
{
    /// <summary>Azure Storage connection string. Locally: Azurite (<c>UseDevelopmentStorage=true</c>) — the identical code path.</summary>
    public required string ConnectionString { get; init; }

    public string ContainerName { get; init; } = "hawta-write-gate";

    /// <summary>
    /// The gate blob's name — one gate per snapshot estate (write DB + publish directory +
    /// Cosmos writer role). Typically the snapshot name.
    /// </summary>
    public required string GateName { get; init; }

    /// <summary>
    /// Azure allows 15–60 s. A crashed holder's lease expires so an operator can recover it;
    /// the durable active marker still prevents automatic takeover after an unclean exit.
    /// </summary>
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>How often the held lease renews. Must be well under <see cref="LeaseDuration"/>.</summary>
    public TimeSpan RenewInterval { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Injectable monotonic clock for deterministic lease-fence tests.</summary>
    internal TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>Bounds the complete renewal-stop, clean-marker, and release shutdown sequence.</summary>
    public TimeSpan HandoffOperationTimeout { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// The single-writer guard: a blob lease that makes any accidental second agent instance
/// (scale-out drift, slot-swap warm-up overlap, a dev machine pointed at production) fail
/// loudly instead of corrupting the publish directory or double-writing Cosmos.
/// The write DB itself is per-instance local disk — the gate protects the SHARED surfaces:
/// the publish directory and the Cosmos writer role.
/// </summary>
public static class SnapshotWriteGate
{
    /// <summary>
    /// Tries to take the gate. Null = another holder has it (the caller skips its cycle and
    /// retries later — normal during deploy overlap). Creates the gate BLOB on first use; the
    /// CONTAINER must already exist, because this engine never creates one — see
    /// <see cref="SnapshotBlobContainerMissingException"/>. Before enabling this protocol, every
    /// legacy writer must be stopped and any existing empty gate must be reconciled and
    /// initialized by an operator. The create ETag only bootstraps a blob created by this call;
    /// blob leases themselves do not change that ETag, so running old and new gate protocols
    /// concurrently is not supported.
    /// </summary>
    public static async Task<WriteGateLease?> TryAcquireAsync(
        SnapshotWriteGateOptions options, CancellationToken cancellationToken = default)
    {
        ValidateOptions(options);

        // The container is NOT created here, deliberately — an operator provisions it once, and
        // this then runs under a credential scoped to exactly that container. Creating it would
        // demand a wider credential for no gain, and it is
        // InitializeExistingLegacyMarkerAsync's posture already.
        var container = new BlobContainerClient(options.ConnectionString, options.ContainerName);

        var blob = container.GetBlobClient(options.GateName);
        ETag? freshEmptyMarkerETag = null;
        try
        {
            var createResponse = await blob.UploadAsync(
                BinaryData.FromString(""),
                new BlobUploadOptions { Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All } },
                cancellationToken);
            freshEmptyMarkerETag = createResponse.Value.ETag;
        }
        catch (RequestFailedException exception) when (IsContainerMissing(exception))
        {
            // A throw, never the null return. Null means "another holder has the gate", which
            // tells the caller to skip this cycle and come back later — so reporting a missing
            // container that way would leave a misconfigured deployment idling forever while
            // every health signal said it was fine.
            throw SnapshotBlobContainerMissingException.For(options.ContainerName, "write-gate", exception);
        }
        catch (RequestFailedException exception) when (
            exception.Status is 409 or 412) // exists (or exists-and-leased) — both fine, we only need the blob to be there
        {
        }

        var lease = blob.GetBlobLeaseClient();
        try
        {
            var acquisitionStarted = options.TimeProvider.GetTimestamp();
            var response = await lease.AcquireAsync(options.LeaseDuration, cancellationToken: cancellationToken);
            var session = new AzureWriteGateLeaseSession(blob, lease, response.Value.LeaseId);
            return await ActivateAcquiredLeaseAsync(
                session,
                options,
                acquisitionStarted,
                freshEmptyMarkerETag,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.ErrorCode == BlobErrorCode.LeaseAlreadyPresent)
        {
            return null;
        }
    }

    /// <summary>
    /// Performs the one-time, operator-controlled conversion of an existing zero-byte legacy
    /// marker to the durable v1 clean marker. This deliberately does not create either the
    /// container or blob and is not a blind reset: the caller's proof runs while the existing
    /// blob is exclusively leased and locally fenced. Only a completed proof permits the
    /// ETag-conditioned clean write; every uncertain path leaves the lease unreleased to expire.
    /// </summary>
    public static async Task<SnapshotWriteGateInitializationResult> InitializeExistingLegacyMarkerAsync(
        SnapshotWriteGateOptions options,
        Func<Action, CancellationToken, Task> proveSafeAsync,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions(options);
        ArgumentNullException.ThrowIfNull(proveSafeAsync);

        var blob = new BlobContainerClient(options.ConnectionString, options.ContainerName)
            .GetBlobClient(options.GateName);
        var lease = blob.GetBlobLeaseClient();
        try
        {
            var acquisitionStarted = options.TimeProvider.GetTimestamp();
            var response = await lease.AcquireAsync(options.LeaseDuration, cancellationToken: cancellationToken);
            return await InitializeAcquiredLegacyMarkerAsync(
                new AzureWriteGateLeaseSession(blob, lease, response.Value.LeaseId),
                options,
                acquisitionStarted,
                proveSafeAsync,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.ErrorCode == BlobErrorCode.LeaseAlreadyPresent)
        {
            throw SnapshotWriteGateRecoveryRequiredException.LegacyInitializationRefused(
                options.GateName,
                "The existing marker is leased by another holder, so the no-writer precondition is false.",
                exception);
        }
        catch (RequestFailedException exception) when (IsContainerMissing(exception))
        {
            // Same distinction as in TryAcquireAsync: a missing container is a provisioning
            // problem with a one-line fix, not a marker that could not be acquired as-is.
            throw SnapshotBlobContainerMissingException.For(options.ContainerName, "write-gate", exception);
        }
        catch (RequestFailedException exception)
        {
            throw SnapshotWriteGateRecoveryRequiredException.LegacyInitializationRefused(
                options.GateName,
                "The existing marker could not be acquired exactly as-is; it was not created or rewritten.",
                exception);
        }
    }

    internal static async Task<SnapshotWriteGateInitializationResult> InitializeAcquiredLegacyMarkerAsync(
        IWriteGateLeaseSession session,
        SnapshotWriteGateOptions options,
        long acquisitionStarted,
        Func<Action, CancellationToken, Task> proveSafeAsync,
        CancellationToken cancellationToken)
    {
        var initializer = new LegacyMarkerInitializerLease(session, options, acquisitionStarted);
        try
        {
            WriteGateMarkerSnapshot snapshot;
            try
            {
                snapshot = await session.ReadMarkerAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                throw SnapshotWriteGateRecoveryRequiredException.LegacyInitializationRefused(
                    options.GateName,
                    "The existing marker could not be read under the acquired lease.",
                    exception);
            }

            // Initialization is deliberately narrower than normal marker parsing. Whitespace,
            // malformed JSON, active, and clean markers are all nonempty and therefore refused.
            if (snapshot.Content.ToMemory().Length != 0)
            {
                throw SnapshotWriteGateRecoveryRequiredException.LegacyInitializationRefused(
                    options.GateName,
                    "The existing marker is not exactly the zero-byte legacy marker.");
            }

            using var proofCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                initializer.LostToken);
            initializer.EnsureOwnership();
            await proveSafeAsync(initializer.EnsureOwnership, proofCancellation.Token);
            proofCancellation.Token.ThrowIfCancellationRequested();
            initializer.EnsureOwnership();

            return await initializer.CompleteAsync(snapshot.ETag);
        }
        catch
        {
            await initializer.AbandonAsync();
            throw;
        }
    }

    /// <summary>
    /// Every operation inside a container that is not there answers 404 <c>ContainerNotFound</c>.
    /// Matched on the error code rather than the status, because a blob-level 404
    /// (<c>BlobNotFound</c>) is a different — and entirely normal — answer.
    /// </summary>
    private static bool IsContainerMissing(RequestFailedException exception) =>
        exception.ErrorCode == BlobErrorCode.ContainerNotFound;

    private static void ValidateOptions(SnapshotWriteGateOptions options)
    {
        if (options.LeaseDuration < TimeSpan.FromSeconds(15) || options.LeaseDuration > TimeSpan.FromSeconds(60))
            throw new ArgumentException("LeaseDuration must be between 15 and 60 seconds (Azure lease bounds).");
        if (options.RenewInterval <= TimeSpan.Zero
            || options.RenewInterval >= options.LeaseDuration - options.RenewInterval)
        {
            throw new ArgumentException(
                "RenewInterval must be positive and below half of LeaseDuration so the local ownership fence remains valid until the first renewal.");
        }
        if (options.HandoffOperationTimeout <= TimeSpan.Zero
            || options.HandoffOperationTimeout >= options.LeaseDuration)
        {
            throw new ArgumentException(
                "HandoffOperationTimeout must be positive and below LeaseDuration so clean shutdown cannot wait past the finite lease window.");
        }
    }

    /// <summary>
    /// Converts an already-acquired Azure lease into a usable write gate. A durable active
    /// marker is written before the lease can escape to shared-work callers.
    /// </summary>
    internal static async Task<WriteGateLease> ActivateAcquiredLeaseAsync(
        IWriteGateLeaseSession session,
        SnapshotWriteGateOptions options,
        long acquisitionStarted,
        ETag? freshEmptyMarkerETag,
        CancellationToken cancellationToken)
    {
        WriteGateMarkerSnapshot snapshot;
        try
        {
            snapshot = await session.ReadMarkerAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // No work was admitted. Do not release an acquisition whose marker state could
            // not be established; the finite lease may expire, but no new holder overlapped it.
            throw;
        }
        catch (Exception exception)
        {
            throw SnapshotWriteGateRecoveryRequiredException.MarkerStateUnknown(
                options.GateName, "The previous marker could not be read under the acquired lease.", exception);
        }

        WriteGateMarker previous;
        try
        {
            previous = WriteGateMarkerCodec.Parse(snapshot.Content);
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            throw SnapshotWriteGateRecoveryRequiredException.MarkerStateUnknown(
                options.GateName, "The durable marker is malformed or uses an unsupported version.", exception);
        }

        if (previous.State == WriteGateMarkerState.LegacyClean
            && (freshEmptyMarkerETag is null || freshEmptyMarkerETag.Value != snapshot.ETag))
        {
            throw SnapshotWriteGateRecoveryRequiredException.LegacyMarkerNeedsInitialization(options.GateName);
        }

        if (previous.State == WriteGateMarkerState.Active)
        {
            throw SnapshotWriteGateRecoveryRequiredException.UncleanTakeover(
                options.GateName, previous.HolderId, previous.UpdatedAtUtc);
        }

        var holderId = Guid.NewGuid().ToString("N");
        ETag activeMarkerETag;
        try
        {
            activeMarkerETag = await session.WriteMarkerAsync(
                WriteGateMarkerCodec.Active(holderId, options.TimeProvider.GetUtcNow()),
                snapshot.ETag,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The request outcome can be indeterminate. Never return the lease and never
            // release it here: either active persisted, or no shared work ever started.
            throw;
        }
        catch (Exception exception)
        {
            throw SnapshotWriteGateRecoveryRequiredException.MarkerStateUnknown(
                options.GateName, "The active marker write could not be confirmed.", exception);
        }

        return new WriteGateLease(
            session,
            options,
            holderId,
            activeMarkerETag,
            acquisitionStarted);
    }
}

/// <summary>
/// The gate's durable marker proves whether the prior lease holder stopped cleanly. This
/// exception deliberately has no automatic recovery path: first prove the old process is
/// stopped and reconcile shared/local state, then reset the marker under operator control.
/// </summary>
public sealed class SnapshotWriteGateRecoveryRequiredException : InvalidOperationException
{
    public string GateName { get; }
    public string? RecordedHolderId { get; }
    public DateTimeOffset? RecordedAtUtc { get; }

    private SnapshotWriteGateRecoveryRequiredException(
        string gateName,
        string message,
        string? recordedHolderId = null,
        DateTimeOffset? recordedAtUtc = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        GateName = gateName;
        RecordedHolderId = recordedHolderId;
        RecordedAtUtc = recordedAtUtc;
    }

    internal static SnapshotWriteGateRecoveryRequiredException UncleanTakeover(
        string gateName,
        string? holderId,
        DateTimeOffset? recordedAtUtc) => new(
            gateName,
            $"Write gate '{gateName}' records an unclean prior holder and refuses automatic takeover. " +
            "No shared work was admitted. Independently prove the prior holder is stopped, reconcile " +
            "Cosmos and local state, then reset the durable gate marker under explicit operator control.",
            holderId,
            recordedAtUtc);

    internal static SnapshotWriteGateRecoveryRequiredException MarkerStateUnknown(
        string gateName,
        string detail,
        Exception? innerException = null) => new(
            gateName,
            $"Write gate '{gateName}' cannot prove a clean handoff. {detail} No shared work was admitted. " +
            "Independently prove the prior holder is stopped, reconcile Cosmos and local state, then " +
            "inspect or reset the durable gate marker under explicit operator control.",
            innerException: innerException);

    internal static SnapshotWriteGateRecoveryRequiredException LegacyMarkerNeedsInitialization(string gateName) => new(
        gateName,
        $"Write gate '{gateName}' has an existing empty legacy marker, so a clean predecessor shutdown " +
        "cannot be proved during a mixed-version rollout. No shared work was admitted. Independently " +
        "prove every prior holder is stopped, reconcile Cosmos and local state, then initialize the " +
        "durable gate marker under explicit operator control.");

    internal static SnapshotWriteGateRecoveryRequiredException LegacyInitializationRefused(
        string gateName,
        string detail,
        Exception? innerException = null) => new(
            gateName,
            $"Write gate '{gateName}' legacy-marker initialization refused. {detail} " +
            "No shared work was admitted and no clean marker was claimed.",
            innerException: innerException);
}

public sealed record SnapshotWriteGateInitializationResult(
    string LeaseId,
    string HolderId,
    DateTimeOffset InitializedAtUtc,
    ETag CleanMarkerETag);

/// <summary>
/// Holds and renews the raw legacy-marker lease while an operator proof runs. Unlike a normal
/// write gate it admits no shared work and writes no active marker. Success writes and confirms
/// clean; every other path stops renewal without releasing, so the finite lease is the fence.
/// </summary>
internal sealed class LegacyMarkerInitializerLease
{
    private readonly IWriteGateLeaseSession session;
    private readonly string gateName;
    private readonly TimeSpan handoffOperationTimeout;
    private readonly TimeProvider timeProvider;
    private readonly CancellationTokenSource stopRenewingCts = new();
    private readonly LeaseOwnershipFence ownershipFence;
    private readonly Task renewLoop;
    private int completionStarted;

    internal LegacyMarkerInitializerLease(
        IWriteGateLeaseSession session,
        SnapshotWriteGateOptions options,
        long acquisitionStarted)
    {
        this.session = session;
        gateName = options.GateName;
        handoffOperationTimeout = options.HandoffOperationTimeout;
        timeProvider = options.TimeProvider;
        ownershipFence = new LeaseOwnershipFence(
            options.TimeProvider,
            options.LeaseDuration,
            options.RenewInterval,
            acquisitionStarted);
        renewLoop = RenewLoopAsync(options.RenewInterval);
    }

    internal CancellationToken LostToken => ownershipFence.LostToken;

    internal void EnsureOwnership() => ownershipFence.EnsureOwnership();

    internal async Task<SnapshotWriteGateInitializationResult> CompleteAsync(ETag legacyMarkerETag)
    {
        if (Interlocked.Exchange(ref completionStarted, 1) != 0)
            throw new InvalidOperationException("Legacy marker initialization has already ended.");

        var handoffBudget = Stopwatch.StartNew();
        try
        {
            await StopRenewingAsync(handoffBudget);
            if (LostToken.IsCancellationRequested)
            {
                throw SnapshotWriteGateRecoveryRequiredException.LegacyInitializationRefused(
                    gateName,
                    "Lease ownership was lost before the clean marker could be written.");
            }

            var renewalStarted = timeProvider.GetTimestamp();
            await WaitForHandoffAsync(session.RenewAsync, RemainingHandoffTime(handoffBudget));
            ownershipFence.Confirm(renewalStarted);
            ownershipFence.EnsureOwnership();

            var holderId = $"operator-initialization-{Guid.NewGuid():N}";
            var initializedAt = timeProvider.GetUtcNow();
            var cleanETag = await WaitForHandoffAsync(
                cancellationToken => session.WriteMarkerAsync(
                    WriteGateMarkerCodec.Clean(holderId, initializedAt),
                    legacyMarkerETag,
                    cancellationToken),
                RemainingHandoffTime(handoffBudget));

            var confirmation = await WaitForHandoffAsync(
                session.ReadMarkerAsync,
                RemainingHandoffTime(handoffBudget));
            var confirmedMarker = WriteGateMarkerCodec.Parse(confirmation.Content);
            if (confirmation.ETag != cleanETag
                || confirmedMarker.State != WriteGateMarkerState.Clean
                || !string.Equals(confirmedMarker.HolderId, holderId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The clean-marker readback did not exactly confirm the conditioned write.");
            }

            try
            {
                await WaitForHandoffAsync(session.ReleaseAsync, RemainingHandoffTime(handoffBudget));
            }
            catch
            {
                // The confirmed clean marker is sufficient. A failed release simply leaves a
                // finite lease that expires without admitting overlapping work.
            }

            DisposeResources();
            return new SnapshotWriteGateInitializationResult(
                session.LeaseId,
                holderId,
                initializedAt,
                cleanETag);
        }
        catch (SnapshotWriteGateRecoveryRequiredException)
        {
            ownershipFence.Lose();
            DisposeResourcesWhenSafe();
            throw;
        }
        catch (Exception exception)
        {
            ownershipFence.Lose();
            DisposeResourcesWhenSafe();
            throw SnapshotWriteGateRecoveryRequiredException.LegacyInitializationRefused(
                gateName,
                "The clean-marker write or readback could not be confirmed; the lease was deliberately not released.",
                exception);
        }
    }

    internal async Task AbandonAsync()
    {
        if (Interlocked.Exchange(ref completionStarted, 1) != 0)
            return;

        stopRenewingCts.Cancel();
        try
        {
            await renewLoop.WaitAsync(handoffOperationTimeout);
            ownershipFence.Lose();
            DisposeResources();
        }
        catch
        {
            ownershipFence.Lose();
            DisposeResourcesWhenSafe();
        }
        // Deliberately no marker write and no release. A proof/read/renew failure remains
        // fenced until Azure expires the finite lease.
    }

    private async Task RenewLoopAsync(TimeSpan interval)
    {
        try
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stopRenewingCts.Token))
            {
                var renewalStarted = timeProvider.GetTimestamp();
                await session.RenewAsync(stopRenewingCts.Token);
                ownershipFence.Confirm(renewalStarted);
            }
        }
        catch (OperationCanceledException) when (stopRenewingCts.IsCancellationRequested)
        {
        }
        catch
        {
            ownershipFence.Lose();
        }
    }

    private async Task StopRenewingAsync(Stopwatch handoffBudget)
    {
        stopRenewingCts.Cancel();
        try
        {
            await renewLoop.WaitAsync(RemainingHandoffTime(handoffBudget));
        }
        catch (TimeoutException exception)
        {
            ownershipFence.Lose();
            throw SnapshotWriteGateRecoveryRequiredException.LegacyInitializationRefused(
                gateName,
                "The renewal loop did not stop within the handoff bound; no marker was written and the lease was not released.",
                exception);
        }
    }

    private TimeSpan RemainingHandoffTime(Stopwatch handoffBudget)
    {
        var remaining = handoffOperationTimeout - handoffBudget.Elapsed;
        return remaining > TimeSpan.Zero
            ? remaining
            : throw new TimeoutException("The legacy-marker initialization handoff budget was exhausted.");
    }

    private static async Task WaitForHandoffAsync(
        Func<CancellationToken, Task> operation,
        TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var task = operation(timeoutCts.Token);
        try
        {
            await task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            timeoutCts.Cancel();
            ObserveLateFailure(task);
            throw;
        }
    }

    private static async Task<T> WaitForHandoffAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var task = operation(timeoutCts.Token);
        try
        {
            return await task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            timeoutCts.Cancel();
            ObserveLateFailure(task);
            throw;
        }
    }

    private static void ObserveLateFailure(Task task) =>
        _ = task.ContinueWith(
            completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    private void DisposeResourcesWhenSafe()
    {
        if (renewLoop.IsCompleted)
            DisposeResources();
        else
            _ = renewLoop.ContinueWith(
                _ => DisposeResources(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }

    private void DisposeResources()
    {
        stopRenewingCts.Dispose();
        ownershipFence.Dispose();
    }
}

/// <summary>
/// A held gate. Renews itself in the background until disposed; if a renewal fails (network
/// partition, lease stolen, clock trouble), <see cref="LostToken"/> fires — check it before
/// publish-critical sections and abort the cycle when it's set.
/// </summary>
public sealed class WriteGateLease : IAsyncDisposable
{
    private readonly IWriteGateLeaseSession session;
    private readonly CancellationTokenSource stopRenewingCts = new();
    private readonly LeaseOwnershipFence ownershipFence;
    private readonly string gateName;
    private readonly string holderId;
    private readonly TimeSpan handoffOperationTimeout;
    private readonly TimeProvider timeProvider;
    private readonly Task renewLoop;
    private ETag activeMarkerETag;
    private int disposeStarted;

    public string LeaseId => session.LeaseId;

    /// <summary>Fires when the lease could not be renewed — the caller no longer holds the gate.</summary>
    public CancellationToken LostToken => ownershipFence.LostToken;

    internal WriteGateLease(
        IWriteGateLeaseSession session,
        SnapshotWriteGateOptions options,
        string holderId,
        ETag activeMarkerETag,
        long acquisitionStarted)
    {
        this.session = session;
        gateName = options.GateName;
        this.holderId = holderId;
        this.activeMarkerETag = activeMarkerETag;
        handoffOperationTimeout = options.HandoffOperationTimeout;
        timeProvider = options.TimeProvider;
        ownershipFence = new LeaseOwnershipFence(
            options.TimeProvider,
            options.LeaseDuration,
            options.RenewInterval,
            acquisitionStarted);
        renewLoop = RenewLoopAsync(options.RenewInterval);
    }

    /// <summary>
    /// Fails closed unless a recent, successful Azure acquire/renew still proves this holder
    /// owns the lease. Unlike <see cref="LostToken"/>, this synchronous fence also catches a
    /// process that was suspended past lease expiry before its renewal loop can resume.
    /// </summary>
    public void EnsureOwnership() => ownershipFence.EnsureOwnership();

    private async Task RenewLoopAsync(TimeSpan interval)
    {
        try
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stopRenewingCts.Token))
            {
                // Starting the validity window before the service call is conservative: time
                // spent awaiting the successful response is never added to our local proof.
                var renewalStarted = timeProvider.GetTimestamp();
                await session.RenewAsync(stopRenewingCts.Token);
                ownershipFence.Confirm(renewalStarted);
            }
        }
        catch (OperationCanceledException) when (stopRenewingCts.IsCancellationRequested)
        {
            // Disposal — the normal exit.
        }
        catch
        {
            // Renewal failed: we no longer hold the gate. The lease will expire on its own;
            // signalling the loss is this loop's only remaining job.
            ownershipFence.Lose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposeStarted, 1) != 0)
            return;

        // Callers dispose only after all admitted shared work has stopped. Stop and observe
        // the renewal loop before recording that clean shutdown under the still-held lease.
        var handoffBudget = Stopwatch.StartNew();
        stopRenewingCts.Cancel();
        try
        {
            await renewLoop.WaitAsync(RemainingHandoffTime(handoffBudget));
        }
        catch (TimeoutException exception)
        {
            // A transport that ignores cancellation must not hold disposal open or permit a
            // clean marker. Keep the active marker and defer resource cleanup until the loop
            // really exits; it may still touch the fence after this method returns.
            ownershipFence.Lose();
            _ = renewLoop.ContinueWith(
                _ => DisposeResources(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            throw SnapshotWriteGateRecoveryRequiredException.MarkerStateUnknown(
                gateName,
                "The renewal loop did not stop within the handoff bound; the active marker was left in place and the lease was not released.",
                exception);
        }
        catch
        {
            // Renewal-loop failures were already signalled via LostToken.
        }

        if (LostToken.IsCancellationRequested)
        {
            DisposeResources();
            throw SnapshotWriteGateRecoveryRequiredException.MarkerStateUnknown(
                gateName,
                "Lease ownership was lost before a clean marker could be confirmed; the active marker was left in place.");
        }

        try
        {
            // Refresh once after work has stopped so the clean-marker/release pair has a full
            // lease window. The marker remains active if this Azure confirmation fails.
            var renewalStarted = timeProvider.GetTimestamp();
            await WaitForHandoffAsync(session.RenewAsync, RemainingHandoffTime(handoffBudget));
            ownershipFence.Confirm(renewalStarted);
            ownershipFence.EnsureOwnership();

            activeMarkerETag = await WaitForHandoffAsync(
                cancellationToken => session.WriteMarkerAsync(
                    WriteGateMarkerCodec.Clean(holderId, timeProvider.GetUtcNow()),
                    activeMarkerETag,
                    cancellationToken),
                RemainingHandoffTime(handoffBudget));
        }
        catch (Exception exception)
        {
            ownershipFence.Lose();
            DisposeResources();
            throw SnapshotWriteGateRecoveryRequiredException.MarkerStateUnknown(
                gateName,
                "The clean marker could not be confirmed; the lease was deliberately not released.",
                exception);
        }

        try
        {
            await WaitForHandoffAsync(session.ReleaseAsync, RemainingHandoffTime(handoffBudget));
        }
        catch
        {
            // Work is stopped and the durable clean marker is confirmed. Whether release
            // landed or the finite lease expires later, a successor cannot overlap this holder.
        }

        DisposeResources();
    }

    private TimeSpan RemainingHandoffTime(Stopwatch handoffBudget)
    {
        var remaining = handoffOperationTimeout - handoffBudget.Elapsed;
        return remaining > TimeSpan.Zero
            ? remaining
            : throw new TimeoutException("The write-gate handoff budget was exhausted.");
    }

    private static async Task WaitForHandoffAsync(
        Func<CancellationToken, Task> operation,
        TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var task = operation(timeoutCts.Token);
        try
        {
            await task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            timeoutCts.Cancel();
            ObserveLateFailure(task);
            throw;
        }
    }

    private static async Task<T> WaitForHandoffAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var task = operation(timeoutCts.Token);
        try
        {
            return await task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            timeoutCts.Cancel();
            ObserveLateFailure(task);
            throw;
        }
    }

    private static void ObserveLateFailure(Task task) =>
        _ = task.ContinueWith(
            completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    private void DisposeResources()
    {
        stopRenewingCts.Dispose();
        ownershipFence.Dispose();
    }
}

internal interface IWriteGateLeaseSession
{
    string LeaseId { get; }

    Task<WriteGateMarkerSnapshot> ReadMarkerAsync(CancellationToken cancellationToken);

    Task<ETag> WriteMarkerAsync(BinaryData content, ETag expectedETag, CancellationToken cancellationToken);

    Task RenewAsync(CancellationToken cancellationToken);

    Task ReleaseAsync(CancellationToken cancellationToken);
}

internal sealed record WriteGateMarkerSnapshot(BinaryData Content, ETag ETag);

internal sealed class AzureWriteGateLeaseSession(
    BlobClient blob,
    BlobLeaseClient lease,
    string leaseId) : IWriteGateLeaseSession
{
    public string LeaseId { get; } = leaseId;

    public async Task<WriteGateMarkerSnapshot> ReadMarkerAsync(CancellationToken cancellationToken)
    {
        var response = await blob.DownloadContentAsync(
            new BlobDownloadOptions
            {
                Conditions = new BlobRequestConditions { LeaseId = LeaseId },
            },
            cancellationToken);
        return new WriteGateMarkerSnapshot(response.Value.Content, response.Value.Details.ETag);
    }

    public async Task<ETag> WriteMarkerAsync(
        BinaryData content,
        ETag expectedETag,
        CancellationToken cancellationToken)
    {
        var response = await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions
                {
                    LeaseId = LeaseId,
                    IfMatch = expectedETag,
                },
            },
            cancellationToken);
        return response.Value.ETag;
    }

    public async Task RenewAsync(CancellationToken cancellationToken) =>
        await lease.RenewAsync(cancellationToken: cancellationToken);

    public async Task ReleaseAsync(CancellationToken cancellationToken) =>
        await lease.ReleaseAsync(cancellationToken: cancellationToken);
}

internal enum WriteGateMarkerState
{
    LegacyClean,
    Clean,
    Active,
}

internal sealed record WriteGateMarker(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("state")] WriteGateMarkerState State,
    [property: JsonPropertyName("holderId")] string? HolderId,
    [property: JsonPropertyName("updatedAtUtc")] DateTimeOffset? UpdatedAtUtc);

internal static class WriteGateMarkerCodec
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    internal static WriteGateMarker Parse(BinaryData content)
    {
        if (string.IsNullOrWhiteSpace(content.ToString()))
            return new WriteGateMarker(CurrentVersion, WriteGateMarkerState.LegacyClean, null, null);

        using var document = JsonDocument.Parse(content.ToMemory());
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("The write-gate marker must be a JSON object.");
        var propertyNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!propertyNames.Add(property.Name))
                throw new JsonException($"The write-gate marker repeats property '{property.Name}'.");
        }

        var marker = document.RootElement.Deserialize<WriteGateMarker>(JsonOptions)
            ?? throw new JsonException("The write-gate marker was null.");
        if (marker.Version != CurrentVersion)
            throw new FormatException($"Unsupported write-gate marker version {marker.Version}.");
        if (marker.State is not (WriteGateMarkerState.Clean or WriteGateMarkerState.Active))
            throw new FormatException($"Unsupported write-gate marker state '{marker.State}'.");
        if (string.IsNullOrWhiteSpace(marker.HolderId) || marker.UpdatedAtUtc is null)
            throw new FormatException("The write-gate marker is missing holder or timestamp evidence.");

        return marker;
    }

    internal static BinaryData Active(string holderId, DateTimeOffset updatedAtUtc) =>
        Serialize(WriteGateMarkerState.Active, holderId, updatedAtUtc);

    internal static BinaryData Clean(string holderId, DateTimeOffset updatedAtUtc) =>
        Serialize(WriteGateMarkerState.Clean, holderId, updatedAtUtc);

    private static BinaryData Serialize(
        WriteGateMarkerState state,
        string holderId,
        DateTimeOffset updatedAtUtc) =>
        BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(
            new WriteGateMarker(CurrentVersion, state, holderId, updatedAtUtc),
            JsonOptions));
}

/// <summary>
/// A conservative local proof derived from the start of the last Azure-confirmed acquire or
/// renewal. The fence expires one renewal interval early, leaving a full interval between the
/// last permitted write and Azure's lease expiry even if the holder is then suspended.
/// </summary>
internal sealed class LeaseOwnershipFence : IDisposable
{
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan maximumConfirmationAge;
    private readonly CancellationTokenSource lostCts = new();
    private long confirmationStarted;

    internal LeaseOwnershipFence(
        TimeProvider timeProvider,
        TimeSpan leaseDuration,
        TimeSpan renewInterval,
        long confirmationStarted)
    {
        this.timeProvider = timeProvider;
        maximumConfirmationAge = leaseDuration - renewInterval;
        this.confirmationStarted = confirmationStarted;
    }

    internal CancellationToken LostToken => lostCts.Token;

    internal void Confirm(long started)
    {
        if (!lostCts.IsCancellationRequested)
            Interlocked.Exchange(ref confirmationStarted, started);
    }

    internal void EnsureOwnership()
    {
        var confirmedAt = Interlocked.Read(ref confirmationStarted);
        if (lostCts.IsCancellationRequested
            || timeProvider.GetElapsedTime(confirmedAt, timeProvider.GetTimestamp()) >= maximumConfirmationAge)
        {
            Lose();
            throw new OperationCanceledException(
                "The write-gate lease is no longer proven current; replication stopped before shared or local mutation.",
                LostToken);
        }
    }

    internal void Lose()
    {
        if (!lostCts.IsCancellationRequested)
            lostCts.Cancel();
    }

    public void Dispose() => lostCts.Dispose();
}
