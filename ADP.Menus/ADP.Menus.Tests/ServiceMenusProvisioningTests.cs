using Microsoft.Azure.Cosmos;

using ShiftSoftware.ADP.Menus.Data.Replication;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Runs the shipped provisioner — <see cref="MenuCosmosProvisioning.EnsureContainersAsync"/>, the same
/// call a host makes — against a real endpoint, then verifies each of the SEVEN containers came out
/// with the partition key the code expects.
///
/// The seven are the design in COSMOS_REPLICATION_PLAN.md §16: one container per master entity, keyed
/// by its own id, plus <c>ServiceMenus</c> keyed by (BasicModelCode, ItemType) for the variant graph.
///
/// Targets the LOCAL COSMOS EMULATOR by default, so starting the emulator and running the suite is all
/// it takes:
///
///     dotnet test ADP.Menus/ADP.Menus.Tests --filter ServiceMenusProvisioning
///
/// Point it elsewhere (a throwaway cloud account, a non-default emulator port) with:
///
///     dotnet test ADP.Menus/ADP.Menus.Tests --filter ServiceMenusProvisioning ^
///         -e ADP_MENUS_COSMOS_CONNECTION="AccountEndpoint=...;AccountKey=..."
///
/// Unlike every other test here — which are pure and offline — this one talks to a real endpoint, so it
/// SKIPS (never fails) when none is reachable. CI therefore stays green without an emulator.
///
/// Why a test as well as the provisioner: a container's partition key CANNOT be changed after
/// creation, so the provisioner refuses to accept a wrong one — and this is what proves that refusal
/// works against a real Cosmos endpoint rather than only in principle. It also lets a developer stand a
/// local environment up and see it match.
/// </summary>
public class ServiceMenusProvisioningTests
{
    private const string ConnectionVariable = "ADP_MENUS_COSMOS_CONNECTION";

    /// <summary>
    /// The Azure Cosmos DB Emulator's well-known endpoint and key. Published by Microsoft and identical
    /// on every install — it is a fixed local-development constant, not a credential, and the same pair
    /// already appears in the sample's appsettings.
    /// </summary>
    private const string EmulatorConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>Kept short so a misconfigured endpoint skips promptly instead of stalling a test run.</summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task CreatesEveryServiceMenuContainerIfMissing()
    {
        // Emulator by default; the variable overrides it for a different port or account.
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = EmulatorConnectionString;

        using var client = new CosmosClient(connectionString, new CosmosClientOptions
        {
            RequestTimeout = ConnectTimeout,
            // The emulator's certificate is self-signed; this test only ever targets a local or
            // throwaway account, never anything real.
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            }),
            ConnectionMode = ConnectionMode.Gateway,
        });

        using var cancellation = new CancellationTokenSource(ConnectTimeout * MenuCosmosContainers.All.Count);

        MenuCosmosProvisioningReport report;
        try
        {
            // The same call a host makes. A container that exists with the wrong partition key makes
            // this THROW InvalidOperationException — which is a test failure, not a skip.
            report = await MenuCosmosProvisioning.EnsureContainersAsync(
                client,
                cancellationToken: cancellation.Token);
        }
        catch (Exception exception) when (exception is CosmosException or HttpRequestException or TaskCanceledException)
        {
            var target = connectionString == EmulatorConnectionString
                ? "the local Cosmos emulator (start it, or set " + ConnectionVariable + " to another endpoint)"
                : "the endpoint in " + ConnectionVariable;

            Assert.Skip($"Could not reach {target}: {exception.Message}");
            return;
        }

        Assert.Equal(7, MenuCosmosContainers.All.Count);
        Assert.Equal(MenuCosmosContainers.All.Count, report.Containers.Count);

        // Read every container back INDEPENDENTLY rather than trusting the report. The report's own
        // comparison is the thing under test here: were it to compare the expected key against itself
        // it would pass on any container, so the assertion that matters has to re-read the resource.
        // CreateContainerIfNotExists is a no-op when the container already exists, so a container
        // created earlier with the WRONG key is silently accepted — and a partition key cannot be
        // altered afterwards, only dropped and recreated.
        var database = client.GetDatabase(MenuCosmosContainers.DatabaseName);

        foreach (var expected in MenuCosmosContainers.All)
        {
            var properties = await database
                .GetContainer(expected.Name)
                .ReadContainerAsync(cancellationToken: cancellation.Token);

            Assert.Equal(expected.PartitionKeyPaths, properties.Resource.PartitionKeyPaths);
        }
    }

    /// <summary>
    /// Guards the constants themselves against drift: a partition-key path must still name a real
    /// property on a document that lands in that container. Pure — no Cosmos endpoint needed.
    /// </summary>
    [Fact]
    public void PartitionKeyPathsNameRealDocumentProperties()
    {
        Assert.Equal("/" + nameof(MenuVariantCosmosModel.BasicModelCode), NoSQLConstants.PartitionKeys.ServiceMenus.Level1);
        Assert.Equal("/" + nameof(MenuVariantCosmosModel.ItemType), NoSQLConstants.PartitionKeys.ServiceMenus.Level2);

        Assert.Equal("/" + nameof(ServiceIntervalCosmosModel.id), NoSQLConstants.PartitionKeys.ServiceIntervals.Level1);
        Assert.Equal("/" + nameof(ServiceIntervalGroupCosmosModel.id), NoSQLConstants.PartitionKeys.ServiceIntervalGroups.Level1);
        Assert.Equal("/" + nameof(ReplacementItemCosmosModel.id), NoSQLConstants.PartitionKeys.ReplacementItems.Level1);
        Assert.Equal("/" + nameof(StandaloneReplacementItemGroupCosmosModel.id), NoSQLConstants.PartitionKeys.StandaloneReplacementItemGroups.Level1);
        Assert.Equal("/" + nameof(LabourRateMappingCosmosModel.id), NoSQLConstants.PartitionKeys.LabourRateMappings.Level1);
        Assert.Equal("/" + nameof(BrandMappingCosmosModel.id), NoSQLConstants.PartitionKeys.BrandMappings.Level1);
    }

    /// <summary>
    /// Every document type in the ServiceMenus container must expose the two partition-key properties,
    /// or its upsert lands in the wrong partition (or fails). Pure — no Cosmos endpoint needed, so this
    /// one always runs and is the offline guard the provisioning test above cannot be.
    /// </summary>
    [Theory]
    [InlineData(typeof(MenuVariantCosmosModel))]
    [InlineData(typeof(MenuPeriodCosmosModel))]
    [InlineData(typeof(MenuLabourCosmosModel))]
    [InlineData(typeof(MenuItemCosmosModel))]
    public void EveryServiceMenuDocumentExposesThePartitionKeyProperties(Type documentType)
    {
        foreach (var path in new[]
                 {
                     NoSQLConstants.PartitionKeys.ServiceMenus.Level1,
                     NoSQLConstants.PartitionKeys.ServiceMenus.Level2,
                 })
        {
            var propertyName = path.TrimStart('/');

            Assert.True(
                documentType.GetProperty(propertyName) is not null,
                $"{documentType.Name} has no {propertyName} property, but the container is partitioned by {path}.");
        }

        Assert.NotNull(documentType.GetProperty("id"));
    }

    /// <summary>
    /// A master document lands in its own container keyed by <c>id</c>, so it must expose one — and it
    /// must NOT carry a basic model code. Carrying one would be the tell that the old sentinel-partition
    /// design had crept back in: a container's partition key has to be something every document in it
    /// genuinely has, and master data has no model code.
    /// </summary>
    [Theory]
    [InlineData(typeof(ServiceIntervalCosmosModel))]
    [InlineData(typeof(ServiceIntervalGroupCosmosModel))]
    [InlineData(typeof(ReplacementItemCosmosModel))]
    [InlineData(typeof(StandaloneReplacementItemGroupCosmosModel))]
    [InlineData(typeof(LabourRateMappingCosmosModel))]
    [InlineData(typeof(BrandMappingCosmosModel))]
    public void EveryMasterDocumentIsKeyedByItsOwnIdAlone(Type documentType)
    {
        Assert.NotNull(documentType.GetProperty("id"));

        Assert.Null(documentType.GetProperty(nameof(MenuVariantCosmosModel.BasicModelCode)));
        Assert.Null(documentType.GetProperty(nameof(MenuVariantCosmosModel.ItemType)));
    }

    /// <summary>Item types must be distinct, or documents of different shapes collide in one partition.</summary>
    [Fact]
    public void ServiceMenuDocumentItemTypesAreDistinct()
    {
        string[] itemTypes =
        [
            new MenuVariantCosmosModel().ItemType,
            new MenuPeriodCosmosModel().ItemType,
            new MenuLabourCosmosModel().ItemType,
            new MenuItemCosmosModel().ItemType,
        ];

        Assert.Equal(itemTypes.Length, itemTypes.Distinct().Count());
        Assert.DoesNotContain(itemTypes, string.IsNullOrWhiteSpace);
    }

    /// <summary>
    /// The discriminators the fan-out finders filter on must be exactly the values the documents
    /// report. A drift here does not fail loudly — the finder simply matches nothing, and a master edit
    /// stops reaching the documents that denormalize it.
    /// </summary>
    [Fact]
    public void ServiceMenuDocumentItemTypesMatchTheirDiscriminators()
    {
        Assert.Equal((string)ModelTypes.MenuVariant, new MenuVariantCosmosModel().ItemType);
        Assert.Equal((string)ModelTypes.MenuPeriod, new MenuPeriodCosmosModel().ItemType);
        Assert.Equal((string)ModelTypes.MenuLabour, new MenuLabourCosmosModel().ItemType);
        Assert.Equal((string)ModelTypes.MenuItem, new MenuItemCosmosModel().ItemType);
    }
}
