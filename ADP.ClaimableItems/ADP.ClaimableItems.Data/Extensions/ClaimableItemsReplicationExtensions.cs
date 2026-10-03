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
/// <b>Every projection below (the <c>To…Model</c> methods and <see cref="UpdateServiceItemCampaign"/>) used to be
/// an AutoMapper profile map.</b> At <c>2026.8.30.1</c> the <c>mapping</c> parameter was REQUIRED - "Builds the
/// Cosmos document from the entity. Required — there is no fallback" - so each one is transcribed from the map it
/// replaces rather than inferred from the model shape. They are public so a consumer's catch-up maps through the
/// same code as the triggers. Without a delegate, a catch-up needs a ShiftMapper map that the consumer does not
/// have, and it fails.
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
                mapping: w => ToServiceItemModel(w.Entity)
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
                mapping: w => ToServiceCampaignModel(w.Entity)
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
                mapping: (w, existing) => UpdateServiceItemCampaign(w.Entity, existing)
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
                mapping: w => ToCampaignVinEntryModel(w.Entity)
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
                mapping: w => ToItemClaimModel(w.Entity)
            );

        return x;
    }

    /// <summary>
    /// The service-item document of a claimable item (Services/ServiceItems). Reads the item's Campaign, so load it.
    /// The trigger <see cref="AddClaimableItemsReplication{TDbContext}"/> registers and a consumer's catch-up
    /// (<c>replication.SetUp&lt;…, ClaimableItem&gt;(…, q =&gt; q.Include(x =&gt; x.Campaign)).Replicate(…, ToServiceItemModel)</c>)
    /// map through this one method, so the two cannot drift.
    /// </summary>
    public static ServiceItemModel ToServiceItemModel(ClaimableItem item) => new ServiceItemModel
    {
        id = item.ID.ToString(),
        // The profile mapped IntegrationID from the long ID; AutoMapper applied its
        // long -> string conversion. Made explicit here.
        IntegrationID = item.ID.ToString(),

        // EVERY READ OF item.Campaign IS NULL-SAFE. CampaignID is nullable, and the original host's AutoMapper map
        // null-propagated through the navigation: an item without a campaign (or one saved before the navigation was
        // loaded) still got its document, with default campaign fields. A bare `item.Campaign!` threw instead. The
        // trigger and the catch-up swallow per-row errors, so such an item had no document at all. The defaults
        // below are what AutoMapper wrote: default dates and enums, null scalars, empty collections and dictionaries.
        CampaignStartDate = item.Campaign?.StartDate ?? default,
        CampaignEndDate = item.Campaign?.ExpireDate ?? default,

        // The two validity modes are mutually exclusive and each nulls the other's
        // fields. Losing either condition would publish a fixed-range item as though it
        // were activation-relative.
        ActiveFor = item.ValidityMode == ClaimableItemValidityMode.RelativeToActivation
            ? (int?)item.ActiveFor
            : null,
        ActiveForDurationType = item.ValidityMode == ClaimableItemValidityMode.RelativeToActivation
            ? (DurationType?)item.ActiveForDurationType
            : null,
        ValidFrom = item.ValidityMode == ClaimableItemValidityMode.FixedDateRange
            ? item.ValidFrom
            : null,
        ValidTo = item.ValidityMode == ClaimableItemValidityMode.FixedDateRange
            ? item.ValidTo
            : null,

        CampaignUniqueReference = item.Campaign?.UniqueReference!,

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
        Name = CosmosProjectionHelpers.DeserializeDict(item.Name)
            ?? new Dictionary<string, string>(),
        CampaignName = CosmosProjectionHelpers.DeserializeDict(item.Campaign?.Name)
            ?? new Dictionary<string, string>(),
        PrintoutTitle = (item.PrintoutTitle == null
            ? null
            : CosmosProjectionHelpers.DeserializeDict(item.PrintoutTitle))
            ?? new Dictionary<string, string>(),
        PrintoutDescription = (item.PrintoutDescription == null
            ? null
            : CosmosProjectionHelpers.DeserializeDict(item.PrintoutDescription))
            ?? new Dictionary<string, string>(),

        // Scoping ids come from the CAMPAIGN, not the item.
        BrandIDs = item.Campaign?.Brands.Select(y => (long?)y) ?? Enumerable.Empty<long?>(),
        CountryIDs = item.Campaign?.Countries.Select(y => (long?)y) ?? Enumerable.Empty<long?>(),
        CompanyIDs = item.Campaign?.Companies.Select(y => (long?)y) ?? Enumerable.Empty<long?>(),

        FixedCost = item.CostingType == ClaimableItemCostingType.Fixed
            ? item.FixedCost
            : null,
        // Same coercion, and this is the one that matters most: the helper returns
        // null for EVERY Fixed-costing item (by design - its cost lives in FixedCost),
        // so without the fallback every such document flips from "ModelCosts": [] to
        // null. That is the common case for this member, not an edge case.
        ModelCosts = CosmosProjectionHelpers.DeserializeModelCosts(
            item.Costs, item.CostingType, item.ID)
            ?? new List<ShiftSoftware.ADP.Models.Vehicle.ServiceItemCostModel>(),

        CampaignActivationTrigger = item.Campaign?.ActivationTrigger ?? default,
        CampaignActivationType = item.Campaign?.ActivationType ?? default,
        AttachmentFieldBehavior = item.AttachmentFieldBehavior,
        VehicleInspectionTypeID = item.Campaign?.VehicleInspectionTypeID,

        // ---- convention: same-named members AutoMapper mapped without a ForMember ----
        // Absent from the deleted profile and therefore easy to lose. Verified by
        // matching ServiceItemModel's properties against ClaimableItem's.
        IsDeleted = item.IsDeleted,
        MaximumMileage = item.MaximumMileage,
        ProgramRole = item.ProgramRole,
        PackageCode = item.PackageCode!,
        UniqueReference = item.UniqueReference!,
        CampaignID = item.CampaignID,
        ValidityMode = item.ValidityMode,
        ClaimingMethod = item.ClaimingMethod,

        // Photo and EligibilityConditions are deliberately NOT set: ClaimableItem has no
        // source for either, so AutoMapper left them at their default too.
    };

    /// <summary>The campaign document (Services/ClaimableItemCampaigns), for the trigger and a consumer's catch-up.</summary>
    public static ServiceCampaignModel ToServiceCampaignModel(Campaign campaign) => new ServiceCampaignModel
    {
        id = campaign.ID.ToString(),
        ID = campaign.ID,
        // Same AllowNullCollections restoration as the map above - this one is also
        // ForMember-based. The three ID collections are Selects over non-nullable
        // List<long> members, so they can never be null and need no fallback.
        Name = CosmosProjectionHelpers.DeserializeDict(campaign.Name)
            ?? new Dictionary<string, string>(),
        BrandIDs = campaign.Brands.Select(y => (long?)y),
        CountryIDs = campaign.Countries.Select(y => (long?)y),
        CompanyIDs = campaign.Companies.Select(y => (long?)y),
        VehicleInspectionTypeID = campaign.VehicleInspectionTypeID,

        // ---- convention: same-named members, no ForMember in the old profile ----
        UniqueReference = campaign.UniqueReference!,
        StartDate = campaign.StartDate,
        ExpireDate = campaign.ExpireDate,
        ActivationTrigger = campaign.ActivationTrigger,
        ActivationType = campaign.ActivationType,
    };

    /// <summary>
    /// Refreshes the campaign fields embedded in an existing service-item document, leaving every item-owned member
    /// as it is: the campaign trigger's fan-out and a consumer's catch-up
    /// (<c>.UpdateReference&lt;ServiceItemModel&gt;(…, finder, UpdateServiceItemCampaign)</c>) both use it.
    /// </summary>
    public static ServiceItemModel UpdateServiceItemCampaign(Campaign campaign, ServiceItemModel existing)
    {
        existing.CampaignName = CosmosProjectionHelpers.DeserializeDict(campaign.Name)!;
        existing.CampaignUniqueReference = campaign.UniqueReference!;
        existing.CampaignStartDate = campaign.StartDate;
        existing.CampaignEndDate = campaign.ExpireDate;
        existing.CampaignActivationTrigger = campaign.ActivationTrigger;
        existing.CampaignActivationType = campaign.ActivationType;
        existing.BrandIDs = campaign.Brands.Select(x => (long?)x);
        existing.CompanyIDs = campaign.Companies.Select(y => new Nullable<long>(y));
        existing.CountryIDs = campaign.Countries.Select(y => new Nullable<long>(y));

        existing.VehicleInspectionTypeID = campaign.VehicleInspectionTypeID;

        return existing;
    }

    /// <summary>The campaign VIN entry document (CompanyData/Vehicles). Reads the entry's Campaign when it is loaded.</summary>
    public static CampaignVinEntryModel ToCampaignVinEntryModel(CampaignVinEntry entry) => new CampaignVinEntryModel
    {
        id = entry.ID.ToString(),
        VIN = entry.VIN,
        CampaignID = entry.CampaignID,
        CampaignUniqueReference = entry.Campaign != null ? entry.Campaign.UniqueReference! : null!,
        RecordedDate = entry.RecordedDate,
        CompanyID = entry.CompanyID,
        IsDeleted = entry.IsDeleted,
    };

    /// <summary>The item-claim document (CompanyData/Vehicles), for the trigger and a consumer's catch-up.</summary>
    public static ItemClaimModel ToItemClaimModel(ItemClaim claim) => new ItemClaimModel
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
        id = $"{claim.VIN}-{claim.CampaignID}-{claim.ClaimableItemID}-{claim.VehicleInspectionResultID}-{claim.CampaignVinEntryID}-{claim.ClaimableItemContractID}",

        VIN = claim.VIN,
        CompanyID = claim.CompanyID,
        BranchID = claim.CompanyBranchID,
        ClaimDate = claim.ClaimDate,

        // Null cost publishes as 0, not as null.
        Cost = claim.Cost ?? 0m,

        PackageCode = claim.PackageCode!,
        InvoiceNumber = claim.InvoiceNumber!,
        JobNumber = claim.JobNumber!,
        QRCode = claim.QRCode!,
        ServiceItemID = claim.ClaimableItemID.ToString(),

        // These two keep the explicit null check rather than `?.ToString()`: a null id
        // must publish as null, NOT as the empty string.
        VehicleInspectionID = claim.VehicleInspectionResultID == null
            ? null!
            : claim.VehicleInspectionResultID.ToString()!,
        CampaignVinEntryID = claim.CampaignVinEntryID == null
            ? null!
            : claim.CampaignVinEntryID.ToString()!,

        IsDeleted = claim.IsDeleted,

        // CompanyHashID and BranchHashID are deliberately not set - the old map did not
        // set them either.
    };

}
