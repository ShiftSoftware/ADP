namespace ShiftSoftware.ADP.Menus.Sample.API.DataServices;

/// <summary>
/// The sample's two Cosmos accounts. Each name is both the <c>ConnectionStrings</c> key it is configured under
/// and the DI key its <see cref="Microsoft.Azure.Cosmos.CosmosClient"/> is registered with.
/// </summary>
public static class CosmosAccounts
{
    /// <summary>Where <see cref="CosmosService"/> reads part prices (CompanyData/Parts).</summary>
    public const string PartPrice = "PartPriceCosmos";

    /// <summary>Where the menu replication writes, and the containers it provisions (the Services database).</summary>
    public const string Replication = "ReplicationCosmos";
}
