using Azure.Storage.Blobs;
using System.Net.Sockets;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The standing rule, pinned so nobody re-adds the convenience: <b>the engine never creates or
/// deletes a blob container.</b> It takes a container name and manages the blobs inside it.
/// Creating the container is a one-time provisioning act by whoever owns the storage account, and
/// that does not change with the credential — a connection string that COULD create one still must
/// not be used to, because the container is the unit a credential is scoped to.
///
/// <para>Two engine surfaces touch a container: the write gate and the blob publish store. Both
/// must refuse a missing one with an error naming the container and saying an operator has to
/// create it — never a raw 404, never silence, and (the sharp one) never the write gate's
/// "another holder has it" answer, which tells the caller to skip its cycle and retry, so a
/// misconfigured deployment would idle forever looking healthy.</para>
///
/// <para>Runs against Azurite (the identical code path production uses), and skips when it is not
/// listening — start it with <c>azurite --silent</c>.</para>
/// </summary>
public sealed class BlobContainerOwnershipTests
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

    /// <summary>A name nothing has ever created, so "missing" is a fact rather than an assumption.</summary>
    private static string UncreatedContainerName() => $"hawta-absent-{Guid.NewGuid():N}";

    private static SnapshotWriteGateOptions GateOptions(string containerName) => new()
    {
        ConnectionString = DevelopmentStorage,
        ContainerName = containerName,
        GateName = $"gate-{Guid.NewGuid():N}",
        LeaseDuration = TimeSpan.FromSeconds(15),
        RenewInterval = TimeSpan.FromSeconds(5),
    };

    /// <summary>
    /// The developer/operator role, which the engine deliberately does not perform for itself.
    /// Tests own the containers they use, and clean them up.
    /// </summary>
    private static BlobContainerClient CreateContainer(string containerName)
    {
        var container = new BlobContainerClient(DevelopmentStorage, containerName);
        container.CreateIfNotExists();
        return container;
    }

    private static void AssertNamesTheContainerAndTheFix(
        SnapshotBlobContainerMissingException exception, string containerName)
    {
        Assert.Equal(containerName, exception.ContainerName);

        // Naming it is the whole point: the operator has to know WHICH container to create, and a
        // wrong container name in configuration looks exactly like a container nobody made yet.
        Assert.Contains(containerName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not exist", exception.Message, StringComparison.Ordinal);
        Assert.Contains("An operator must create it", exception.Message, StringComparison.Ordinal);
        Assert.Contains("never creates or deletes containers", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteGate_RefusesAMissingContainer_AndDoesNotCreateOne()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var containerName = UncreatedContainerName();

        var exception = await Assert.ThrowsAsync<SnapshotBlobContainerMissingException>(() =>
            SnapshotWriteGate.TryAcquireAsync(GateOptions(containerName), TestContext.Current.CancellationToken));

        AssertNamesTheContainerAndTheFix(exception, containerName);
        Assert.Contains("write-gate container", exception.Message, StringComparison.Ordinal);

        // The refusal is only half the rule. The other half is that the attempt left nothing
        // behind — a created container would make the SECOND start succeed against an estate
        // nobody provisioned, which is the failure this whole convention exists to prevent.
        Assert.False(new BlobContainerClient(DevelopmentStorage, containerName).Exists());
    }

    [Fact]
    public async Task WriteGate_MissingContainer_IsNeverReportedAsAnotherHolderHavingTheGate()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var containerName = UncreatedContainerName();
        var container = CreateContainer(containerName);
        try
        {
            // Null has one meaning and it is a healthy one: somebody else holds the gate, skip this
            // cycle, come back later. Established here against a container that exists, so the
            // contrast below is a real distinction and not an artefact of the setup.
            var options = GateOptions(containerName);
            await using var held = await SnapshotWriteGate.TryAcquireAsync(
                options, TestContext.Current.CancellationToken);
            Assert.NotNull(held);
            Assert.Null(await SnapshotWriteGate.TryAcquireAsync(options, TestContext.Current.CancellationToken));
        }
        finally
        {
            container.DeleteIfExists();
        }

        // Same call, same options shape, container gone: this must NOT come back as that same
        // benign null. A caller told to skip and retry would skip and retry forever, and every
        // health signal would keep saying the agent was fine.
        var refused = await Record.ExceptionAsync(() =>
            SnapshotWriteGate.TryAcquireAsync(GateOptions(containerName), TestContext.Current.CancellationToken));

        Assert.IsType<SnapshotBlobContainerMissingException>(refused);
    }

    [Fact]
    public async Task WriteGateLegacyInitialization_RefusesAMissingContainer_WithoutRunningTheProof()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var containerName = UncreatedContainerName();
        var proofRuns = 0;

        var exception = await Assert.ThrowsAsync<SnapshotBlobContainerMissingException>(() =>
            SnapshotWriteGate.InitializeExistingLegacyMarkerAsync(
                GateOptions(containerName),
                (_, _) =>
                {
                    proofRuns++;
                    return Task.CompletedTask;
                },
                TestContext.Current.CancellationToken));

        AssertNamesTheContainerAndTheFix(exception, containerName);

        // A missing container is a provisioning mistake, not a marker that could not be acquired
        // as-is, and it is answered before an operator is asked to prove anything.
        Assert.Equal(0, proofRuns);
        Assert.False(new BlobContainerClient(DevelopmentStorage, containerName).Exists());
    }

    [Fact]
    public void PublishStore_RefusesAMissingContainer_AndDoesNotCreateOne()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var containerName = UncreatedContainerName();

        var exception = Assert.Throws<SnapshotBlobContainerMissingException>(
            () => new BlobPublishStore(DevelopmentStorage, containerName, "snapshots").EnsureReady());

        AssertNamesTheContainerAndTheFix(exception, containerName);
        Assert.Contains("publish container", exception.Message, StringComparison.Ordinal);
        Assert.False(new BlobContainerClient(DevelopmentStorage, containerName).Exists());
    }

    [Fact]
    public void PublishStore_AcceptsAnEmptyContainerAnOperatorCreated()
    {
        Assert.SkipWhen(!AzuriteIsRunning(), "Azurite is not running on 127.0.0.1:10000.");
        var containerName = UncreatedContainerName();
        var container = CreateContainer(containerName);
        try
        {
            // The pairing that makes the refusal above meaningful. An empty container is a
            // legitimate "nothing published yet" and must pass — a probe that refused it would
            // block every first-ever publish, and one that could not tell it from a missing
            // container would re-seed the estate from source on a configuration typo.
            new BlobPublishStore(DevelopmentStorage, containerName, "snapshots").EnsureReady();
        }
        finally
        {
            container.DeleteIfExists();
        }
    }
}
