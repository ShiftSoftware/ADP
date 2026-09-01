using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Mapping;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.CosmosDbReplication;
using ShiftSoftware.ShiftEntity.EFCore;
using NoSQLConstants = ShiftSoftware.ADP.Models.Constants.NoSQLConstants;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Extensions;

/// <summary>
/// Cosmos replication for this module.
///
/// <para>
/// <b>Every <c>mapping:</c> delegate below used to be an AutoMapper profile map.</b> At
/// <c>2026.8.30.1</c> the <c>mapping</c> parameter is REQUIRED - "Builds the Cosmos document from
/// the entity. Required — there is no fallback" - so each one is transcribed from the map it
/// replaces rather than inferred from the model shape.
/// </para>
///
/// <para>
/// <b>Two of these maps carry members the old profile never mentioned.</b> The
/// <c>ClaimableItem → ServiceItemModel</c> and <c>Campaign → ServiceCampaignModel</c> maps were
/// <c>ForMember</c>-based, so AutoMapper ALSO auto-mapped every same-named property on top of what
/// the profile listed. Those members are marked "convention" below and are just as load-bearing as
/// the explicit ones - writing only the profile's <c>ForMember</c> lines would have silently
/// dropped eight fields from the service-item document and five from the campaign document. The
/// other three maps used <c>ConvertUsing</c>, which bypasses convention entirely, so those are
/// complete exactly as written.
/// </para>
///
/// <para>
/// <b>Nothing verifies this code at runtime.</b> Replication is disabled during parity runs and
/// failures on this path are swallowed - they surface as permanently-dirty rows under a clean
/// watermark, never as an exception or an HTTP error. Line-by-line review against the old profile
/// is the only control there is.
/// </para>
/// </summary>
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
            // Transcribed from ClaimableItemProfile's CreateMap<ClaimableItem, ServiceItemModel>.
            // The largest map in the group; the previous call passed partitionKeyLevel2Expression:
            // null, which is what the two-key overload here means.
            .Replicate<ServiceItemModel>(
                NoSQLConstants.Containers.ServiceItems,
                partitionKeyLevel1Expression: document => document.id,
                mapping: w => new ServiceItemModel
                {
                    id = w.Entity.ID.ToString(),
                    // The profile mapped IntegrationID from the long ID; AutoMapper applied its
                    // long -> string conversion. Made explicit here.
                    IntegrationID = w.Entity.ID.ToString(),

                    CampaignStartDate = w.Entity.Campaign!.StartDate,
                    CampaignEndDate = w.Entity.Campaign!.ExpireDate,

                    // The two validity modes are mutually exclusive and each nulls the other's
                    // fields. Losing either condition would publish a fixed-range item as though it
                    // were activation-relative.
                    ActiveFor = w.Entity.ValidityMode == ClaimableItemValidityMode.RelativeToActivation
                        ? (int?)w.Entity.ActiveFor
                        : null,
                    ActiveForDurationType = w.Entity.ValidityMode == ClaimableItemValidityMode.RelativeToActivation
                        ? (DurationType?)w.Entity.ActiveForDurationType
                        : null,
                    ValidFrom = w.Entity.ValidityMode == ClaimableItemValidityMode.FixedDateRange
                        ? w.Entity.ValidFrom
                        : null,
                    ValidTo = w.Entity.ValidityMode == ClaimableItemValidityMode.FixedDateRange
                        ? w.Entity.ValidTo
                        : null,

                    CampaignUniqueReference = w.Entity.Campaign!.UniqueReference!,

                    // EVERY DICTIONARY AND COLLECTION MEMBER IN THIS MAP ENDS IN A `?? empty`,
                    // AND THAT IS NOT DEFENSIVE PADDING - it restores a coercion the framework used
                    // to perform invisibly.
                    //
                    // This map was ForMember-based, so AutoMapper ran each resolved value through
                    // its member/collection mapper, and AllowNullCollections defaults to FALSE -
                    // nothing in ShiftEntity or this repo overrides it. A resolver returning null
                    // for a dictionary- or collection-typed member therefore reached Cosmos as an
                    // EMPTY one. Reproduced directly against AutoMapper 14.0.0 (the version the
                    // 2026.7.31.1 replication package pins): a null PrintoutTitle serialized as
                    // "PrintoutTitle": {}, never as null.
                    //
                    // Transcribing the profile's `== null ? null : ...` branch literally therefore
                    // does NOT reproduce the old document - it writes null where production has {}.
                    // The branch is kept because it is what the profile said, and the coercion is
                    // restated on top because it is what the profile DID.
                    //
                    // This is the same AllowNullCollections trap already documented in ADP.Surveys
                    // (BankQuestionRepository / ScreenTemplateRepository, `SplitTags(...) ?? new
                    // List<string>()`). It bites here and NOT in the three ConvertUsing maps below,
                    // because ConvertUsing bypasses the member mapper entirely - those return the
                    // object as-is, so their nulls really were nulls.
                    Name = CosmosProjectionHelpers.DeserializeDict(w.Entity.Name)
                        ?? new Dictionary<string, string>(),
                    CampaignName = CosmosProjectionHelpers.DeserializeDict(w.Entity.Campaign!.Name)
                        ?? new Dictionary<string, string>(),
                    PrintoutTitle = (w.Entity.PrintoutTitle == null
                        ? null
                        : CosmosProjectionHelpers.DeserializeDict(w.Entity.PrintoutTitle))
                        ?? new Dictionary<string, string>(),
                    PrintoutDescription = (w.Entity.PrintoutDescription == null
                        ? null
                        : CosmosProjectionHelpers.DeserializeDict(w.Entity.PrintoutDescription))
                        ?? new Dictionary<string, string>(),

                    // Scoping ids come from the CAMPAIGN, not the item.
                    BrandIDs = w.Entity.Campaign!.Brands.Select(y => (long?)y),
                    CountryIDs = w.Entity.Campaign!.Countries.Select(y => (long?)y),
                    CompanyIDs = w.Entity.Campaign!.Companies.Select(y => (long?)y),

                    FixedCost = w.Entity.CostingType == ClaimableItemCostingType.Fixed
                        ? w.Entity.FixedCost
                        : null,
                    // Same coercion, and this is the one that matters most: the helper returns
                    // null for EVERY Fixed-costing item (by design - its cost lives in FixedCost),
                    // so without the fallback every such document flips from "ModelCosts": [] to
                    // null. That is the common case for this member, not an edge case.
                    ModelCosts = CosmosProjectionHelpers.DeserializeModelCosts(
                        w.Entity.Costs, w.Entity.CostingType, w.Entity.ID)
                        ?? new List<ShiftSoftware.ADP.Models.Vehicle.ServiceItemCostModel>(),

                    CampaignActivationTrigger = w.Entity.Campaign!.ActivationTrigger,
                    CampaignActivationType = w.Entity.Campaign!.ActivationType,
                    AttachmentFieldBehavior = w.Entity.AttachmentFieldBehavior,
                    VehicleInspectionTypeID = w.Entity.Campaign!.VehicleInspectionTypeID,

                    // ---- convention: same-named members AutoMapper mapped without a ForMember ----
                    // Absent from the deleted profile and therefore easy to lose. Verified by
                    // matching ServiceItemModel's properties against ClaimableItem's.
                    IsDeleted = w.Entity.IsDeleted,
                    MaximumMileage = w.Entity.MaximumMileage,
                    ProgramRole = w.Entity.ProgramRole,
                    PackageCode = w.Entity.PackageCode!,
                    UniqueReference = w.Entity.UniqueReference!,
                    CampaignID = w.Entity.CampaignID,
                    ValidityMode = w.Entity.ValidityMode,
                    ClaimingMethod = w.Entity.ClaimingMethod,

                    // Photo and EligibilityConditions are deliberately NOT set: ClaimableItem has no
                    // source for either, so AutoMapper left them at their default too.
                }
            );

        x.SetUpReplication<TDbContext, Campaign>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                null
            )
            // Transcribed from CampaignProfile's CreateMap<Campaign, ServiceCampaignModel>.
            .Replicate<ServiceCampaignModel>(
                cosmosContainerId: NoSQLConstants.Containers.ClaimableItemCampaigns,
                partitionKeyLevel1Expression: document => document.id,
                mapping: w => new ServiceCampaignModel
                {
                    id = w.Entity.ID.ToString(),
                    ID = w.Entity.ID,
                    // Same AllowNullCollections restoration as the map above - this one is also
                    // ForMember-based. The three ID collections are Selects over non-nullable
                    // List<long> members, so they can never be null and need no fallback.
                    Name = CosmosProjectionHelpers.DeserializeDict(w.Entity.Name)
                        ?? new Dictionary<string, string>(),
                    BrandIDs = w.Entity.Brands.Select(y => (long?)y),
                    CountryIDs = w.Entity.Countries.Select(y => (long?)y),
                    CompanyIDs = w.Entity.Companies.Select(y => (long?)y),
                    VehicleInspectionTypeID = w.Entity.VehicleInspectionTypeID,

                    // ---- convention: same-named members, no ForMember in the old profile ----
                    UniqueReference = w.Entity.UniqueReference!,
                    StartDate = w.Entity.StartDate,
                    ExpireDate = w.Entity.ExpireDate,
                    ActivationTrigger = w.Entity.ActivationTrigger,
                    ActivationType = w.Entity.ActivationType,
                }
            )
            // Transcribed from CampaignProfile's SECOND map,
            // CreateMap<Campaign, ServiceItemModel>().ConvertUsing((src, dest) => { ...; return dest; }).
            //
            // That map mutated an EXISTING destination and returned it, which is exactly this
            // delegate's shape - the fan-out refreshes the campaign fields embedded in every
            // service-item document without touching anything else on them. ConvertUsing bypasses
            // AutoMapper's convention, so these ten assignments are the complete map: adding any
            // other member here would overwrite item-owned data with campaign data.
            .UpdateReference<ServiceItemModel>(
                cosmosContainerId: NoSQLConstants.Containers.ServiceItems,
                (q, e) => q.Where(si => si.CampaignID == e.Entity.ID),
                mapping: (w, existing) =>
                {
                    existing.CampaignName = CosmosProjectionHelpers.DeserializeDict(w.Entity.Name)!;
                    existing.CampaignUniqueReference = w.Entity.UniqueReference!;
                    existing.CampaignStartDate = w.Entity.StartDate;
                    existing.CampaignEndDate = w.Entity.ExpireDate;
                    existing.CampaignActivationTrigger = w.Entity.ActivationTrigger;
                    existing.CampaignActivationType = w.Entity.ActivationType;
                    existing.BrandIDs = w.Entity.Brands.Select(x => (long?)x);
                    existing.CompanyIDs = w.Entity.Companies.Select(y => new Nullable<long>(y));
                    existing.CountryIDs = w.Entity.Countries.Select(y => new Nullable<long>(y));

                    existing.VehicleInspectionTypeID = w.Entity.VehicleInspectionTypeID;

                    return existing;
                }
            );

        x.SetUpReplication<TDbContext, CampaignVinEntry>(
                cosmosClient,
                NoSQLConstants.Databases.CompanyData,
                null
            )
            // Transcribed from CampaignVinEntryProfile's ConvertUsing object initializer - complete
            // as written. CompanyHashID is deliberately not set; the old map did not set it either.
            .Replicate<CampaignVinEntryModel>(
                NoSQLConstants.Containers.Vehicles,
                partitionKeyLevel1Expression: document => document.VIN,
                partitionKeyLevel2Expression: document => document.ItemType,
                mapping: w => new CampaignVinEntryModel
                {
                    id = w.Entity.ID.ToString(),
                    VIN = w.Entity.VIN,
                    CampaignID = w.Entity.CampaignID,
                    CampaignUniqueReference = w.Entity.Campaign != null ? w.Entity.Campaign.UniqueReference! : null!,
                    RecordedDate = w.Entity.RecordedDate,
                    CompanyID = w.Entity.CompanyID,
                    IsDeleted = w.Entity.IsDeleted,
                }
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
            // Transcribed from ItemClaimProfile's ConvertUsing object initializer.
            .Replicate<ItemClaimModel>(
                NoSQLConstants.Containers.Vehicles,
                partitionKeyLevel1Expression: document => document.VIN,
                partitionKeyLevel2Expression: document => document.ItemType,
                mapping: w => new ItemClaimModel
                {
                    // ── FROZEN DOCUMENT IDENTITY - DO NOT REFORMAT ───────────────────────────
                    // A live production document-identity contract, byte-frozen. Six fields, five
                    // separators, this exact order. It deliberately INCLUDES CampaignVinEntryID
                    // even though the entity's SQL unique hash EXCLUDES it (see ItemClaim's own
                    // remarks) - the two are not interchangeable and this one is not derived from
                    // that one.
                    //
                    // A null long? interpolates to the empty string, so a claim with no inspection
                    // and no vin-entry yields "VIN-1-2---6". That is the existing production key
                    // shape and must stay exactly so; "fixing" it re-keys live documents and orphans
                    // every claim already written.
                    id = $"{w.Entity.VIN}-{w.Entity.CampaignID}-{w.Entity.ClaimableItemID}-{w.Entity.VehicleInspectionResultID}-{w.Entity.CampaignVinEntryID}-{w.Entity.ClaimableItemContractID}",

                    VIN = w.Entity.VIN,
                    CompanyID = w.Entity.CompanyID,
                    BranchID = w.Entity.CompanyBranchID,
                    ClaimDate = w.Entity.ClaimDate,

                    // Null cost publishes as 0, not as null.
                    Cost = w.Entity.Cost ?? 0m,

                    PackageCode = w.Entity.PackageCode!,
                    InvoiceNumber = w.Entity.InvoiceNumber!,
                    JobNumber = w.Entity.JobNumber!,
                    QRCode = w.Entity.QRCode!,
                    ServiceItemID = w.Entity.ClaimableItemID.ToString(),

                    // These two keep the explicit null check rather than `?.ToString()`: a null id
                    // must publish as null, NOT as the empty string.
                    VehicleInspectionID = w.Entity.VehicleInspectionResultID == null
                        ? null!
                        : w.Entity.VehicleInspectionResultID.ToString()!,
                    CampaignVinEntryID = w.Entity.CampaignVinEntryID == null
                        ? null!
                        : w.Entity.CampaignVinEntryID.ToString()!,

                    IsDeleted = w.Entity.IsDeleted,

                    // CompanyHashID and BranchHashID are deliberately not set - the old map did not
                    // set them either.
                }
            );

        return x;
    }
}
