using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;
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
            // Transcribed from the deleted WarrantyClaim profile's
            // CreateMap<WarrantyClaim, WarrantyClaimModel>. At 2026.8.30.1 the `mapping` parameter is
            // REQUIRED - "Builds the Cosmos document from the entity. Required - there is no
            // fallback" - so this delegate replaces the AutoMapper fallback the call relied on.
            //
            // FIVE MEMBERS ARE RENAMED between entity and document and NO convention derives any of
            // them; each is written out below and marked. Nothing else on the model is
            // convention-mapped either: every member the old ForMember map did not name has no
            // source on the entity (BrandHashID, CompanyHashID) or is computed (ItemType), so this
            // list is the complete map rather than a partial one on top of a convention.
            //
            // ZERO HARNESS COVERAGE. Replication is disabled during parity runs and its failures are
            // swallowed - they surface as permanently-dirty rows under a clean watermark, never as an
            // exception. Line-by-line review against the old profile is the only control.
            .Replicate<WarrantyClaimModel>(
                NoSQLConstants.Containers.Vehicles,
                partitionKeyLevel1Expression: document => document.VIN,
                partitionKeyLevel2Expression: document => document.ItemType,
                mapping: w => ToWarrantyClaimModel(w.Entity)
            );

        return x;
    }

    /// <summary>
    /// The warranty-claim document (CompanyData/Vehicles). Reads the claim's labor lines, so load them. The trigger
    /// <see cref="AddWarrantyClaimsReplication{TDbContext}"/> registers and a consumer's catch-up
    /// (<c>replication.SetUp&lt;…, WarrantyClaim&gt;(…, q =&gt; q.Include(x =&gt; x.WarrantyClaimLaborLines)).Replicate(…, ToWarrantyClaimModel)</c>)
    /// map through this one method.
    /// </summary>
    public static WarrantyClaimModel ToWarrantyClaimModel(WarrantyClaim claim) => new WarrantyClaimModel
    {
        id = claim.ID.ToString(),
        VIN = claim.VIN,
        IsDeleted = claim.IsDeleted,
        ClaimNumber = claim.ClaimNumber,
        CompanyID = claim.CompanyID,
        WarrantyType = claim.WarrantyType,
        DateOfReceipt = claim.DateOfReceipt,
        DeliveryDate = claim.DeliveryDate,
        RepairDate = claim.RepairDate,
        RepairCompletionDate = claim.RepairCompletionDate,
        Odometer = claim.Odometer,
        ProcessDate = claim.ProcessDate,
        DistributorProcessDate = claim.DistributorProcessDate,

        // Both enums are NULLABLE on the entity and NON-nullable on the document, so the
        // old map used `!.Value`. Preserved exactly: a null still throws here rather
        // than silently publishing default(ClaimStatus), which would be a wrong status
        // on a live document instead of a loud failure.
        ClaimStatus = claim.ClaimStatus!.Value,
        ManufacturerStatus = claim.ManufacturerStatus!.Value,

        // ---- the five renames; convention derives NONE of these ----
        DealerClaimNumber = claim.DealerClaimNo!,          // DealerClaimNo
        InvoiceNumber = claim.InvoiceNo!,                  // InvoiceNo
        RepairOrderNumber = claim.RepairOrderNo,           // RepairOrderNo
        LaborOperationNumberMain = claim.LaborOperationNoMain!, // LaborOperationNoMain
        DistributorComment = claim.DistComment1!,          // DistComment1

        // Franchise key compared against a numeric literal. Carried over verbatim - not
        // rewritten into an enum lookup, which would be a behaviour change smuggled into
        // a migration. (The profile also carried a COMMENTED-OUT `Brand` mapping; it is
        // not live behaviour and is deliberately not resurrected here.)
        BrandID = claim.Franchise == Franchises.Toyota.Key ? 2 : 3,

        // Nested collection with its own rename: OperationNumber -> LaborCode.
        LaborLines = claim.WarrantyClaimLaborLines.Select(y => new WarrantyClaimLaborLineModel
        {
            DistributorHour = y.DistributorHour,
            Hour = y.Hour,
            ID = y.ID,
            LaborCode = y.OperationNumber,
            MainOperation = y.MainOperation,
            PayCode = y.PayCode,
        }),

        // BrandHashID and CompanyHashID are deliberately not set - the entity has no
        // source for either, so the old map left them at their default too.
    };

}
