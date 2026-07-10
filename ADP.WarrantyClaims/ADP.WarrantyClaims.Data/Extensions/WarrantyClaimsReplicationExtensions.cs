using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ShiftEntity.CosmosDbReplication;
using ShiftSoftware.ShiftEntity.EFCore;
using NoSQLConstants = ShiftSoftware.ADP.Models.Constants.NoSQLConstants;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;

public static class WarrantyClaimsReplicationExtensions
{
    /// <summary>
    /// Opt-in Cosmos replication for warranty claims: WarrantyClaim → WarrantyClaimModel into
    /// CompanyData/Vehicles with the 2-level partition key (VIN, ItemType) — moved verbatim from the original
    /// host application's inline SetUpReplication block. Call this from inside the consumer's
    /// <c>AddShiftEntityCosmosDbReplicationTrigger</c> callback, passing the org Cosmos client. A
    /// read-only consumer simply does NOT call this, so the module replicates nothing.
    /// NOTE: the labor-lines-only <c>PrepareForReplicationAsync</c> refetch stays on
    /// <c>WarrantyClaimRepository</c>, which the framework resolves (registered via RegisterShiftRepositories).
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddWarrantyClaimsReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions x,
        CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        x.SetUpReplication<TDbContext, WarrantyClaim>(
                cosmosClient,
                NoSQLConstants.Databases.CompanyData,
                null
            )
            .Replicate<WarrantyClaimModel>(
                NoSQLConstants.Containers.Vehicles,
                partitionKeyLevel1Expression: e => e.VIN,
                partitionKeyLevel2Expression: e => e.ItemType
            );

        return x;
    }
}
