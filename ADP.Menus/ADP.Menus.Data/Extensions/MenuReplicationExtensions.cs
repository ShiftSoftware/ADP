using Microsoft.Azure.Cosmos;

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Replication;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using ShiftSoftware.ShiftEntity.CosmosDbReplication;
using ShiftSoftware.ShiftEntity.EFCore;

using NoSQLConstants = ShiftSoftware.ADP.Models.Constants.NoSQLConstants;

namespace ShiftSoftware.ADP.Menus.Data.Extensions;

/// <summary>
/// Opt-in Cosmos replication for the menu catalog, following the container design in
/// COSMOS_REPLICATION_PLAN.md §16 (the ShiftIdentity <c>CompanyBranches</c> pattern):
///
///  • Each MASTER entity — service interval, interval group, replacement item, standalone
///    replacement-item group, labour-rate mapping, brand mapping — is replicated into its OWN
///    container, partitioned by its own id.
///
///  • The variant and its three link tables are replicated into <c>ServiceMenus</c>, partitioned by
///    (BasicModelCode, ItemType), FULLY DENORMALIZED. So a lookup is one partition query by basic model
///    code and nothing else: no reference cache, no second round trip, no reader-side staleness.
///
///  • Keeping those denormalized copies fresh is replication's job. Every master registration therefore
///    carries <c>UpdateReference</c> fan-outs onto the menu documents that embed it.
///
/// Call <see cref="AddMenuReplications{TDbContext}"/> from inside the consumer's
/// <c>AddShiftEntityCosmosDbReplicationTrigger</c> callback, passing the Cosmos client. A read-only
/// consumer — one fed by its own sync agent — simply does not call it, so the module replicates
/// nothing. That is the extension point that keeps the module usable both ways, matching
/// <c>AddClaimableItemsReplication</c> and <c>AddWarrantyClaimsReplication</c>.
///
/// Two framework behaviours shape everything below:
///
///  • Register each entity type EXACTLY ONCE. The framework keeps only the last registration per type,
///    silently. That is why a master entity's own document and all of its fan-outs are chained off a
///    single <c>SetUpReplication</c> call.
///
///  • <c>UpdateReference</c> runs on <c>ChangeType.Modified</c> ONLY — never on Added or Deleted. An
///    EDIT to a master row therefore reaches every document that embeds it, but a newly INSERTED master
///    row reaches none (nothing references it yet, so that is correct), and a HARD-DELETED master row
///    leaves its embedded copies behind. Soft delete is an ordinary edit, so it does fan out, carrying
///    <c>IsDeleted</c> onto the copies for readers to honour.
/// </summary>
public static class MenuReplicationExtensions
{
    /// <summary>Wires every menu table. The consumer's single entry point.</summary>
    public static ShiftEntityCosmosDbOptions AddMenuReplications<TDbContext>(
        this ShiftEntityCosmosDbOptions options,
        CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext =>
        options
            .AddMenuVariantReplication<TDbContext>(cosmosClient)
            .AddMenuPeriodReplication<TDbContext>(cosmosClient)
            .AddMenuLabourReplication<TDbContext>(cosmosClient)
            .AddMenuItemReplication<TDbContext>(cosmosClient)
            .AddServiceIntervalReplication<TDbContext>(cosmosClient)
            .AddServiceIntervalGroupReplication<TDbContext>(cosmosClient)
            .AddReplacementItemReplication<TDbContext>(cosmosClient)
            .AddStandaloneReplacementItemGroupReplication<TDbContext>(cosmosClient)
            .AddLabourRateMappingReplication<TDbContext>(cosmosClient)
            .AddBrandMappingReplication<TDbContext>(cosmosClient);

    // ---- the ServiceMenus container ----------------------------------------------------------------

    /// <summary>
    /// The root document of a model's menu graph. Its labour-rate and brand mappings are resolved at
    /// map time (there is no navigation to them) and kept fresh afterwards by those tables' fan-outs.
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddMenuVariantReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        options.SetUpReplication<TDbContext, MenuVariant>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                MenuReplicationReload.Variant<TDbContext>())
            .Replicate<MenuVariantCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                // Partition-key expressions are over the COSMOS MODEL, not the entity.
                partitionKeyLevel1Expression: document => document.BasicModelCode,
                partitionKeyLevel2Expression: document => document.ItemType,
                mapping: wrapper =>
                {
                    var master = MenuReplicationReload.VariantMasterData<TDbContext>(wrapper);
                    return MenuCosmosMappers.Map(wrapper.Entity, master.LabourRateMapping, master.BrandMapping);
                });

        return options;
    }

    public static ShiftEntityCosmosDbOptions AddMenuPeriodReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        options.SetUpReplication<TDbContext, MenuPeriodicAvailability>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                MenuReplicationReload.Period<TDbContext>())
            .Replicate<MenuPeriodCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                partitionKeyLevel1Expression: document => document.BasicModelCode,
                partitionKeyLevel2Expression: document => document.ItemType,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity));

        return options;
    }

    public static ShiftEntityCosmosDbOptions AddMenuLabourReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        options.SetUpReplication<TDbContext, MenuLabourDetails>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                MenuReplicationReload.Labour<TDbContext>())
            .Replicate<MenuLabourCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                partitionKeyLevel1Expression: document => document.BasicModelCode,
                partitionKeyLevel2Expression: document => document.ItemType,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity));

        return options;
    }

    /// <summary>
    /// Menu items, with their parts and country prices embedded.
    ///
    /// NOTE: because parts live inside this document, anything that edits a part or a price WITHOUT
    /// touching the MenuItem row leaves the document stale. The variant save path and the propagation
    /// path both stamp the item, so they are safe; the system-wide parts-price update is not. See the
    /// plan's open item O2.
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddMenuItemReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        options.SetUpReplication<TDbContext, MenuItem>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                MenuReplicationReload.Item<TDbContext>())
            .Replicate<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                partitionKeyLevel1Expression: document => document.BasicModelCode,
                partitionKeyLevel2Expression: document => document.ItemType,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity));

        return options;
    }

    // ---- master containers, each with its fan-outs -------------------------------------------------
    // Which documents a fan-out reaches is decided by MenuReplicationFinders — see the remarks there
    // for why those queries are named methods rather than inline predicates.

    /// <summary>Service intervals, plus the periodic-availability documents that embed them.</summary>
    public static ShiftEntityCosmosDbOptions AddServiceIntervalReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        // Scalars only — no reload needed.
        options.SetUpReplication<TDbContext, ServiceInterval>(cosmosClient, NoSQLConstants.Databases.Services)
            .Replicate<ServiceIntervalCosmosModel>(
                NoSQLConstants.Containers.ServiceIntervals,
                partitionKeyLevel1Expression: document => document.id,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity))
            .UpdateReference<MenuPeriodCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, wrapper) => MenuReplicationFinders.PeriodsEmbeddingInterval(query, wrapper.Entity.ID),
                (wrapper, document) => MenuCosmosMappers.ApplyTo(wrapper.Entity, document));

        return options;
    }

    /// <summary>
    /// Service-interval groups, plus every menu document that embeds one: the labour details keyed to
    /// the group, and the menu items whose replacement item serves it.
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddServiceIntervalGroupReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        options.SetUpReplication<TDbContext, ServiceIntervalGroup>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                MenuReplicationReload.IntervalGroup<TDbContext>())
            .Replicate<ServiceIntervalGroupCosmosModel>(
                NoSQLConstants.Containers.ServiceIntervalGroups,
                partitionKeyLevel1Expression: document => document.id,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity))
            .UpdateReference<MenuLabourCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, wrapper) => MenuReplicationFinders.LabourDetailsEmbeddingIntervalGroup(query, wrapper.Entity.ID),
                (wrapper, document) => MenuCosmosMappers.ApplyTo(wrapper.Entity, document))
            .UpdateReference<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, wrapper) => MenuReplicationFinders.MenuItemsServingIntervalGroup(query, wrapper.Entity.ID),
                (wrapper, document) => MenuCosmosMappers.ApplyTo(wrapper.Entity, document));

        return options;
    }

    /// <summary>
    /// Replacement items, plus the menu items that embed them. The fan-out refreshes the menu item's
    /// WHOLE replacement-item slice — the item, its standalone group and the interval groups it serves
    /// — because a re-pointed group or a changed set of interval links would otherwise leave the
    /// document describing the old ones.
    ///
    /// This is also the closest thing to replication for <c>ReplacementItemServiceIntervalGroup</c>:
    /// those link rows have no basic model code of their own, so they cannot be replicated per row into
    /// a model partition. Their effect reaches Cosmos only when the parent replacement item is saved.
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddReplacementItemReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        options.SetUpReplication<TDbContext, ReplacementItem>(
                cosmosClient,
                NoSQLConstants.Databases.Services,
                MenuReplicationReload.ReplacementItem<TDbContext>())
            .Replicate<ReplacementItemCosmosModel>(
                NoSQLConstants.Containers.ReplacementItems,
                partitionKeyLevel1Expression: document => document.id,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity))
            .UpdateReference<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, wrapper) => MenuReplicationFinders.MenuItemsEmbeddingReplacementItem(query, wrapper.Entity.ID),
                (wrapper, document) => MenuCosmosMappers.ApplyTo(wrapper.Entity, document));

        return options;
    }

    /// <summary>
    /// Standalone replacement-item groups, plus the menu items that fold into one. Which items those
    /// are is decided by the replacement item, so re-pointing an item at a different group is that
    /// table's fan-out; this one only refreshes the group's own fields.
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddStandaloneReplacementItemGroupReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        // Scalars only — no reload needed.
        options.SetUpReplication<TDbContext, StandaloneReplacementItemGroup>(cosmosClient, NoSQLConstants.Databases.Services)
            .Replicate<StandaloneReplacementItemGroupCosmosModel>(
                NoSQLConstants.Containers.StandaloneReplacementItemGroups,
                partitionKeyLevel1Expression: document => document.id,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity))
            .UpdateReference<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, wrapper) => MenuReplicationFinders.MenuItemsEmbeddingStandaloneGroup(query, wrapper.Entity.ID),
                (wrapper, document) => MenuCosmosMappers.ApplyTo(wrapper.Entity, document));

        return options;
    }

    /// <summary>
    /// Labour-rate mappings, plus the variant documents that embed one. A mapping is keyed by
    /// (brand, primary labour rate), so the finder matches on both — a variant whose rate differs keeps
    /// its own mapping.
    /// </summary>
    public static ShiftEntityCosmosDbOptions AddLabourRateMappingReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        // Scalars only — no reload needed.
        options.SetUpReplication<TDbContext, LabourRateMapping>(cosmosClient, NoSQLConstants.Databases.Services)
            .Replicate<LabourRateMappingCosmosModel>(
                NoSQLConstants.Containers.LabourRateMappings,
                partitionKeyLevel1Expression: document => document.id,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity))
            .UpdateReference<MenuVariantCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, wrapper) => MenuReplicationFinders.VariantsEmbeddingLabourRateMapping(
                    query, wrapper.Entity.BrandID, wrapper.Entity.LabourRate),
                (wrapper, document) => MenuCosmosMappers.ApplyTo(wrapper.Entity, document));

        return options;
    }

    /// <summary>Brand mappings, plus every variant document for that brand.</summary>
    public static ShiftEntityCosmosDbOptions AddBrandMappingReplication<TDbContext>(
        this ShiftEntityCosmosDbOptions options, CosmosClient cosmosClient)
        where TDbContext : ShiftDbContext
    {
        // Scalars only — no reload needed.
        options.SetUpReplication<TDbContext, BrandMapping>(cosmosClient, NoSQLConstants.Databases.Services)
            .Replicate<BrandMappingCosmosModel>(
                NoSQLConstants.Containers.BrandMappings,
                partitionKeyLevel1Expression: document => document.id,
                mapping: wrapper => MenuCosmosMappers.Map(wrapper.Entity))
            .UpdateReference<MenuVariantCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, wrapper) => MenuReplicationFinders.VariantsEmbeddingBrandMapping(query, wrapper.Entity.BrandID),
                (wrapper, document) => MenuCosmosMappers.ApplyTo(wrapper.Entity, document));

        return options;
    }
}
