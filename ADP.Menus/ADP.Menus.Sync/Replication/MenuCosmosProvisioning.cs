using Microsoft.Azure.Cosmos;

using System.Net;

namespace ShiftSoftware.ADP.Menus.Sync.Replication;

/// <summary>What provisioning did to one container, and whether it is usable.</summary>
/// <param name="Name">The container id.</param>
/// <param name="Created">True when this run created it; false when it was already there.</param>
/// <param name="ExpectedPartitionKeyPaths">The key <see cref="MenuCosmosContainers"/> declares.</param>
/// <param name="ActualPartitionKeyPaths">The key the container really has.</param>
public sealed record MenuCosmosContainerProvisioning(
    string Name,
    bool Created,
    IReadOnlyList<string> ExpectedPartitionKeyPaths,
    IReadOnlyList<string> ActualPartitionKeyPaths)
{
    /// <summary>
    /// False when the container exists with a DIFFERENT partition key than the replication writes
    /// under. Not repairable in place — see <see cref="MenuCosmosProvisioning"/>.
    /// </summary>
    public bool PartitionKeyMatches =>
        ActualPartitionKeyPaths.SequenceEqual(ExpectedPartitionKeyPaths, StringComparer.Ordinal);
}

/// <summary>The outcome of one <see cref="MenuCosmosProvisioning.EnsureContainersAsync"/> call.</summary>
/// <param name="DatabaseName">The database the containers live in.</param>
/// <param name="DatabaseCreated">True when this run created the database.</param>
/// <param name="Containers">One entry per container, in <see cref="MenuCosmosContainers.All"/> order.</param>
public sealed record MenuCosmosProvisioningReport(
    string DatabaseName,
    bool DatabaseCreated,
    IReadOnlyList<MenuCosmosContainerProvisioning> Containers)
{
    /// <summary>Containers this run created. Zero on every run after the first.</summary>
    public int CreatedCount => Containers.Count(container => container.Created);

    /// <summary>Containers that exist with the wrong partition key. Empty is the only good value.</summary>
    public IReadOnlyList<MenuCosmosContainerProvisioning> Mismatches =>
        Containers.Where(container => !container.PartitionKeyMatches).ToList();
}

/// <summary>
/// Creates the Cosmos database and the seven containers <c>AddMenuReplications</c> writes to, and
/// verifies each one's partition key — the deployment-side half of the menu replication.
///
/// <para><b>Every host needs this and nothing else provides it.</b> Replication is fire-and-forget: a
/// write to a container that does not exist is logged and dropped, never surfaced to the save that
/// triggered it. A host that registers the trigger but never provisions therefore looks wired up and
/// quietly writes nothing. The catch-up sweep is only marginally louder — it warms the connection with
/// <c>CreateAndInitializeAsync</c>, which fails on a missing container rather than creating one.</para>
///
/// <para><b>Why it verifies rather than just creating.</b> A partition key CANNOT be changed after a
/// container is created, and <c>CreateContainerIfNotExists</c> is a no-op when the container already
/// exists — so a container created earlier with the wrong key is silently accepted, and every document
/// then lands in the wrong partition (or fails to write at all). That is the one provisioning mistake
/// with no cheap recovery: it means dropping and recreating the container once real data exists. So a
/// mismatch throws here, at startup, where it is a boot failure with a name in it rather than a lookup
/// that returns the wrong menu six months later.</para>
///
/// <para>Idempotent: safe to call on every start. <see cref="MenuCosmosContainers.All"/> is the single
/// declaration of what to create and with which key, so the list is never retyped — this method, the
/// sample host and <c>ServiceMenusProvisioningTests</c> all read it.</para>
/// </summary>
public static class MenuCosmosProvisioning
{
    /// <summary>
    /// Ensures the database and all seven containers exist with the expected partition keys.
    /// </summary>
    /// <param name="cosmosClient">The same client <c>AddMenuReplications</c> is given.</param>
    /// <param name="databaseThroughput">
    /// Applied only if the database has to be created; <see langword="null"/> leaves it to the account
    /// default (shared throughput off, or serverless).
    /// </param>
    /// <param name="containerThroughput">
    /// Dedicated throughput for containers this call creates; <see langword="null"/> means the
    /// container inherits the database's shared throughput where there is one.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>What was created and what was already there.</returns>
    /// <exception cref="InvalidOperationException">
    /// A container exists with a partition key other than the one the replication writes under.
    /// </exception>
    public static async Task<MenuCosmosProvisioningReport> EnsureContainersAsync(
        CosmosClient cosmosClient,
        ThroughputProperties? databaseThroughput = null,
        ThroughputProperties? containerThroughput = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);

        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            MenuCosmosContainers.DatabaseName,
            databaseThroughput,
            cancellationToken: cancellationToken);

        var results = new List<MenuCosmosContainerProvisioning>(MenuCosmosContainers.All.Count);

        foreach (var expected in MenuCosmosContainers.All)
        {
            var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(expected.Name, expected.PartitionKeyPaths),
                containerThroughput,
                cancellationToken: cancellationToken);

            // When the container already exists the SDK returns ITS properties, not the ones just
            // passed in — which is exactly what makes the comparison below meaningful.
            results.Add(new MenuCosmosContainerProvisioning(
                expected.Name,
                Created: containerResponse.StatusCode == HttpStatusCode.Created,
                expected.PartitionKeyPaths,
                containerResponse.Resource.PartitionKeyPaths));
        }

        var report = new MenuCosmosProvisioningReport(
            MenuCosmosContainers.DatabaseName,
            DatabaseCreated: databaseResponse.StatusCode == HttpStatusCode.Created,
            results);

        // Checked after every container, so one call names all of the wrong ones rather than making
        // the operator rediscover them one restart at a time.
        if (report.Mismatches.Count > 0)
            throw new InvalidOperationException(
                "The Cosmos containers the menu replication writes to exist with the wrong partition key, "
                + "and a partition key cannot be changed after creation — each one has to be dropped and "
                + "recreated: "
                + string.Join("; ", report.Mismatches.Select(container =>
                    $"'{container.Name}' is [{string.Join(", ", container.ActualPartitionKeyPaths)}] "
                    + $"but must be [{string.Join(", ", container.ExpectedPartitionKeyPaths)}]"))
                + ".");

        return report;
    }
}
