using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.SyncAgent;

public class SyncCosmosClient
{
    internal CosmosClient Client { get; private set; }

    internal SyncCosmosClient(CosmosClient client)
    {
        this.Client = client;
    }
}
