using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public sealed class LookUpCosmosClient
{
    public CosmosClient Client { get; }
    private readonly string databaseNameSuffix;

    public LookUpCosmosClient(CosmosClient client, string databaseNameSuffix = null)
    {
        Client = client;
        this.databaseNameSuffix = databaseNameSuffix ?? string.Empty;
    }

    /// <summary>Resolves the container, applying <see cref="LookupOptions.CosmosDatabaseNameSuffix"/>
    /// to the platform-standard database name when configured (shared-emulator dev scenarios;
    /// production tenants normally keep the standard names on their own account).</summary>
    public Container GetContainer(string databaseId, string containerId)
        => Client.GetContainer(databaseId + databaseNameSuffix, containerId);
}
