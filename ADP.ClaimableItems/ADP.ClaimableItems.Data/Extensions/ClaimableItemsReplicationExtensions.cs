using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.CosmosDbReplication;
using ShiftSoftware.ShiftEntity.EFCore;
using NoSQLConstants = ShiftSoftware.ADP.Models.Constants.NoSQLConstants;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Extensions;

public static class ClaimableItemsReplicationExtensions
{
    /// <summary>
    /// Opt-in Cosmos replication for the claimable-items catalog: ClaimableItem → ServiceItemModel,
    /// Campaign → ServiceCampaignModel (+ fan-out UpdateReference onto ServiceItemModel), and
    /// CampaignVinEntry → CampaignVinEntryModel. Call this from inside the consumer's
    /// <c>AddShiftEntityCosmosDbReplicationTrigger</c> callback, passing the org Cosmos client.
    /// A read-only consumer (e.g. a consumer that is fed by its sync agent) simply does NOT call this,
    /// so the module replicates nothing — the extension point that keeps the module usable both ways.
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddClaimableItemsReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions x,
        CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        x.SetUpReplication<TDbContext, ClaimableItem>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                null
            )
            .Replicate<ServiceItemModel>(
                NoSQLConstants.Containers.ServiceItems,
                partitionKeyLevel1Expression: e => e.id,
                partitionKeyLevel2Expression: null
            );

        x.SetUpReplication<TDbContext, Campaign>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                null
            )
            .Replicate<ServiceCampaignModel>(
                cosmosContainerId: NoSQLConstants.Containers.ClaimableItemCampaigns,
                partitionKeyLevel1Expression: e => e.id
            )
            .UpdateReference<ServiceItemModel>(
                cosmosContainerId: NoSQLConstants.Containers.ServiceItems,
                (q, e) => q.Where(si => si.CampaignID == e.Entity.ID)
            );

        x.SetUpReplication<TDbContext, CampaignVinEntry>(
                cosmosClient,
                NoSQLConstants.Databases.CompanyData,
                null
            )
            .Replicate<CampaignVinEntryModel>(
                NoSQLConstants.Containers.Vehicles,
                partitionKeyLevel1Expression: e => e.VIN,
                partitionKeyLevel2Expression: e => e.ItemType
            );

        return x;
    }

    /// <summary>
    /// Opt-in Cosmos replication for the claim record: ItemClaim → ItemClaimModel into
    /// CompanyData/Vehicles (Phase 2 Slice 5 — moved verbatim from the original host's
    /// SetUpReplication block). Registered separately from the catalog replication because a
    /// consumer may author the catalog without hosting the claim flow (or vice versa).
    /// NOTE: partition keys are registered 2-level (VIN, ItemType) exactly as the original host always
    /// did, although NoSQLConstants defines the Vehicles container as 3-level — pre-existing
    /// behavior, reproduced deliberately (see goldens-phase2.md §3).
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddItemClaimReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions x,
        CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        x.SetUpReplication<TDbContext, ItemClaim>(
                cosmosClient,
                NoSQLConstants.Databases.CompanyData,
                null
            )
            .Replicate<ItemClaimModel>(
                NoSQLConstants.Containers.Vehicles,
                partitionKeyLevel1Expression: e => e.VIN,
                partitionKeyLevel2Expression: e => e.ItemType
            );

        return x;
    }
}
