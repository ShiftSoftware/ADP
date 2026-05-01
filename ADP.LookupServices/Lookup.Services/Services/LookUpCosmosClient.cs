using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public sealed class LookUpCosmosClient
{
    public CosmosClient Client { get; }

    public LookUpCosmosClient(CosmosClient client)
    {
        Client = client;
    }

    public Container GetContainer(string databaseId, string containerId)
        => Client.GetContainer(databaseId, containerId);
}
