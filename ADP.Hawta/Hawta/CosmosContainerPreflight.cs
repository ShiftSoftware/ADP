using System.Net;
using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// What one family expects of its destination container: the paths Cosmos partitions the
/// container by, in level order (e.g. <c>["/Code", "/ItemType", "/CompanyID"]</c>). The
/// mapping itself cannot state these — it produces partition-key VALUES per document, and
/// the paths those values bind to live only in the container's own definition.
/// </summary>
public sealed record CosmosContainerExpectation(
    CosmosFamilyMapping Family,
    IReadOnlyList<string> PartitionKeyPaths);

/// <summary>One family's preflight verdict. <see cref="Ready"/> is the only pass.</summary>
public sealed record CosmosContainerPreflightResult(
    string Family,
    string Database,
    string Container,
    bool Exists,
    IReadOnlyList<string>? ActualPartitionKeyPaths,
    bool Ready,
    string? Problem);

/// <summary>
/// The cutover preflight: proves, BEFORE the first drain, that every family's destination
/// container exists and is partitioned by exactly the paths the mapping's documents assume.
///
/// <para>Why this exists: every drill and test creates its own containers, so container
/// definitions have never been checked against pre-provisioned ones. A partition-key-path
/// mismatch does not fail container resolution — it surfaces per document, mid-burst, as
/// 400s (or worse, as documents quietly landing under different logical partitions than the
/// incumbent writer's, which recon would read as mass divergence). One probe per container
/// before the drain turns that into a startup-shaped failure.</para>
/// </summary>
public static class CosmosContainerPreflight
{
    public static async Task<IReadOnlyList<CosmosContainerPreflightResult>> ProbeAsync(
        CosmosClient client,
        IReadOnlyList<CosmosContainerExpectation> expectations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(expectations);

        // Families sharing a container each get their own verdict, but the container is
        // read once — a mismatch must be reported against every family it would break.
        var propertiesByContainer =
            new Dictionary<CosmosContainerKey, (ContainerProperties? Properties, string? Error)>();
        var results = new List<CosmosContainerPreflightResult>(expectations.Count);

        foreach (var expectation in expectations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var family = expectation.Family;
            var key = new CosmosContainerKey(family.Database, family.Container);
            if (!propertiesByContainer.TryGetValue(key, out var read))
            {
                read = await ReadContainerAsync(client, family, cancellationToken);
                propertiesByContainer[key] = read;
            }

            if (read.Properties is null)
            {
                results.Add(new CosmosContainerPreflightResult(
                    family.Family, family.Database, family.Container,
                    Exists: false, ActualPartitionKeyPaths: null, Ready: false,
                    read.Error ?? "The container does not exist."));
                continue;
            }

            // A single-path container reports through PartitionKeyPath; hierarchical ones
            // through PartitionKeyPaths. Normalize to the list form for one comparison.
            var actual = read.Properties.PartitionKeyPaths is { Count: > 0 } hierarchical
                ? hierarchical
                : read.Properties.PartitionKeyPath is { } single ? [single] : [];

            var match = actual.SequenceEqual(expectation.PartitionKeyPaths, StringComparer.Ordinal);
            results.Add(new CosmosContainerPreflightResult(
                family.Family, family.Database, family.Container,
                Exists: true, actual, Ready: match,
                match
                    ? null
                    : $"Partition key paths disagree: the mapping assumes " +
                      $"[{string.Join(", ", expectation.PartitionKeyPaths)}], the container declares " +
                      $"[{string.Join(", ", actual)}]. Documents would land under the wrong logical " +
                      "partitions or be rejected per write."));
        }

        return results;
    }

    /// <summary>Runs the probe and throws one exception naming every not-ready family — the day-one gate shape.</summary>
    public static async Task EnsureReadyAsync(
        CosmosClient client,
        IReadOnlyList<CosmosContainerExpectation> expectations,
        CancellationToken cancellationToken = default)
    {
        var results = await ProbeAsync(client, expectations, cancellationToken);
        var notReady = results.Where(result => !result.Ready).ToList();
        if (notReady.Count == 0)
            return;

        throw new InvalidOperationException(
            "Cosmos container preflight failed — do not start the drain:\n" +
            string.Join("\n", notReady.Select(result =>
                $"  {result.Family} → {result.Database}/{result.Container}: {result.Problem}")));
    }

    private static async Task<(ContainerProperties? Properties, string? Error)> ReadContainerAsync(
        CosmosClient client,
        CosmosFamilyMapping family,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client
                .GetContainer(family.Database, family.Container)
                .ReadContainerAsync(cancellationToken: cancellationToken);
            return (response.Resource, null);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return (null, $"The container (or its database) does not exist ({exception.StatusCode}).");
        }
    }
}
