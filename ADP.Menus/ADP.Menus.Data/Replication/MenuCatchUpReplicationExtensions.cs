using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using ShiftSoftware.ShiftEntity.CosmosDbReplication.Services;
using ShiftSoftware.ShiftEntity.EFCore;

using NoSQLConstants = ShiftSoftware.ADP.Models.Constants.NoSQLConstants;

namespace ShiftSoftware.ADP.Menus.Data.Replication;

/// <summary>
/// Reusable Cosmos "catch-up" replication for the menu catalog — the sweep counterpart to the
/// trigger-side <c>AddMenuReplications</c>. Each <c>ReplicateXAsync</c> re-syncs ONE table through the
/// framework's <see cref="CosmosDBReplication"/> service; <see cref="ReplicateAllAsync{TDbContext}"/>
/// runs them all.
///
/// This is what the save trigger cannot do. The trigger only ever sees rows as they are saved, so a
/// catalogue that existed before replication was switched on — or any row missed while Cosmos was
/// unreachable — never reaches Cosmos at all. It is also the answer to the gaps §17 lists: a master row
/// inserted or hard-deleted never fans out (<c>UpdateReference</c> runs on Modified only), and a
/// system-wide parts-price update bypasses the MenuItem row entirely.
///
/// <para><b>Dirty-only by default.</b> <c>updateAll: false</c> syncs only rows whose
/// <c>LastReplicationDate</c> is behind their <c>LastSaveDate</c> (or absent) — the cheap incremental
/// pass an hourly timer wants. <c>updateAll: true</c> re-syncs every row: the full backfill, for first
/// switch-on or after a container is rebuilt.</para>
///
/// <para>Everything is shared with the trigger path and nothing is duplicated: the include graphs come
/// from <see cref="MenuReplicationIncludes"/>, the projections from <see cref="MenuCosmosMappers"/>,
/// and the fan-out queries from <see cref="MenuReplicationFinders"/>. That is deliberate — two
/// independently-written replication paths would drift, and the drift would be invisible until a
/// lookup returned a wrong menu code.</para>
///
/// <para><b>Containers must already exist.</b> The service warms the connection with
/// <c>CosmosClient.CreateAndInitializeAsync</c>, which fails on a missing container rather than
/// creating one. <c>MenuCosmosContainers.All</c> declares what to provision.</para>
/// </summary>
public static class MenuCatchUpReplicationExtensions
{
    /// <summary>
    /// Runs every menu catch-up in dependency order: the model-partition documents first, then the
    /// master tables — whose fan-outs refresh the denormalized copies the earlier passes just wrote.
    /// </summary>
    /// <param name="database">
    /// Used to pre-load the labour-rate and brand catalogues for the variant pass. See
    /// <see cref="ReplicateMenuVariantAsync{TDbContext}"/> for why that one needs it.
    /// </param>
    public static async Task ReplicateAllAsync<TDbContext>(
        this CosmosDBReplication cosmos,
        TDbContext database,
        string connectionString,
        string databaseId,
        bool updateAll = false)
        where TDbContext : ShiftDbContext
    {
        await cosmos.ReplicateMenuVariantAsync<TDbContext>(database, connectionString, databaseId, updateAll);
        await cosmos.ReplicateMenuPeriodAsync<TDbContext>(connectionString, databaseId, updateAll);
        await cosmos.ReplicateMenuLabourAsync<TDbContext>(connectionString, databaseId, updateAll);
        await cosmos.ReplicateMenuItemAsync<TDbContext>(connectionString, databaseId, updateAll);

        await cosmos.ReplicateServiceIntervalAsync<TDbContext>(connectionString, databaseId, updateAll);
        await cosmos.ReplicateServiceIntervalGroupAsync<TDbContext>(connectionString, databaseId, updateAll);
        await cosmos.ReplicateReplacementItemAsync<TDbContext>(connectionString, databaseId, updateAll);
        await cosmos.ReplicateStandaloneReplacementItemGroupAsync<TDbContext>(connectionString, databaseId, updateAll);
        await cosmos.ReplicateLabourRateMappingAsync<TDbContext>(connectionString, databaseId, updateAll);
        await cosmos.ReplicateBrandMappingAsync<TDbContext>(connectionString, databaseId, updateAll);
    }

    // ---- the ServiceMenus container ----------------------------------------------------------------

    /// <summary>
    /// Menu variants — the root document of each model's partition.
    ///
    /// The only pass that needs a <paramref name="database"/>: a variant document embeds the labour-rate
    /// mapping for its (brand, primary rate) pair and its brand's mapping, and a variant has no
    /// navigation to either, so no include can bring them along. The trigger resolves them with one
    /// query per row (it only ever handles one); a sweep would turn that into a query per variant, so
    /// the catalogues are loaded ONCE here and the same selection rule is applied in memory.
    /// </summary>
    public static async Task ReplicateMenuVariantAsync<TDbContext>(
        this CosmosDBReplication cosmos,
        TDbContext database,
        string connectionString,
        string databaseId,
        bool updateAll = false)
        where TDbContext : ShiftDbContext
    {
        // Both catalogues are small (a handful of rows per brand) and every variant consults them.
        var labourRateMappings = (await database.Set<LabourRateMapping>().AsNoTracking().ToListAsync()).AsQueryable();
        var brandMappings = (await database.Set<BrandMapping>().AsNoTracking().ToListAsync()).AsQueryable();

        await cosmos.SetUp<TDbContext, MenuVariant>(connectionString, databaseId, MenuReplicationIncludes.Variant)
            .Replicate<MenuVariantCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                variant => MenuCosmosMappers.Map(
                    variant,
                    MenuReplicationReload.SelectLabourRateMapping(
                        labourRateMappings, variant.Menu?.VehicleModel?.BrandID, variant.LabourRate),
                    MenuReplicationReload.SelectBrandMapping(
                        brandMappings, variant.Menu?.VehicleModel?.BrandID)))
            .RunAsync(updateAll);
    }

    public static Task ReplicateMenuPeriodAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, MenuPeriodicAvailability>(connectionString, databaseId, MenuReplicationIncludes.Period)
            .Replicate<MenuPeriodCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                period => MenuCosmosMappers.Map(period))
            .RunAsync(updateAll);

    public static Task ReplicateMenuLabourAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, MenuLabourDetails>(connectionString, databaseId, MenuReplicationIncludes.Labour)
            .Replicate<MenuLabourCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                labour => MenuCosmosMappers.Map(labour))
            .RunAsync(updateAll);

    /// <summary>
    /// Menu items, with their parts and country prices embedded. This is the pass that repairs the
    /// system-wide parts-price update (open item O2), which changes prices without touching the
    /// MenuItem row — so the trigger never fires and only a sweep brings the documents back in line.
    /// Note it needs <c>updateAll: true</c> to do so: the item rows are not dirty.
    /// </summary>
    public static Task ReplicateMenuItemAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, MenuItem>(connectionString, databaseId, MenuReplicationIncludes.Item)
            .Replicate<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                item => MenuCosmosMappers.Map(item))
            .RunAsync(updateAll);

    // ---- master containers, each with its fan-outs -------------------------------------------------

    public static Task ReplicateServiceIntervalAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, ServiceInterval>(connectionString, databaseId)
            .Replicate<ServiceIntervalCosmosModel>(
                NoSQLConstants.Containers.ServiceIntervals,
                interval => MenuCosmosMappers.Map(interval))
            .UpdateReference<MenuPeriodCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, interval) => MenuReplicationFinders.PeriodsEmbeddingInterval(query, interval.ID),
                (interval, document) => MenuCosmosMappers.ApplyTo(interval, document))
            .RunAsync(updateAll);

    public static Task ReplicateServiceIntervalGroupAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, ServiceIntervalGroup>(connectionString, databaseId, MenuReplicationIncludes.IntervalGroup)
            .Replicate<ServiceIntervalGroupCosmosModel>(
                NoSQLConstants.Containers.ServiceIntervalGroups,
                group => MenuCosmosMappers.Map(group))
            .UpdateReference<MenuLabourCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, group) => MenuReplicationFinders.LabourDetailsEmbeddingIntervalGroup(query, group.ID),
                (group, document) => MenuCosmosMappers.ApplyTo(group, document))
            .UpdateReference<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, group) => MenuReplicationFinders.MenuItemsServingIntervalGroup(query, group.ID),
                (group, document) => MenuCosmosMappers.ApplyTo(group, document))
            .RunAsync(updateAll);

    public static Task ReplicateReplacementItemAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, Entities.ReplacementItem>(connectionString, databaseId, MenuReplicationIncludes.ReplacementItem)
            .Replicate<ReplacementItemCosmosModel>(
                NoSQLConstants.Containers.ReplacementItems,
                replacementItem => MenuCosmosMappers.Map(replacementItem))
            .UpdateReference<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, replacementItem) => MenuReplicationFinders.MenuItemsEmbeddingReplacementItem(query, replacementItem.ID),
                (replacementItem, document) => MenuCosmosMappers.ApplyTo(replacementItem, document))
            .RunAsync(updateAll);

    public static Task ReplicateStandaloneReplacementItemGroupAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, StandaloneReplacementItemGroup>(connectionString, databaseId)
            .Replicate<StandaloneReplacementItemGroupCosmosModel>(
                NoSQLConstants.Containers.StandaloneReplacementItemGroups,
                group => MenuCosmosMappers.Map(group))
            .UpdateReference<MenuItemCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, group) => MenuReplicationFinders.MenuItemsEmbeddingStandaloneGroup(query, group.ID),
                (group, document) => MenuCosmosMappers.ApplyTo(group, document))
            .RunAsync(updateAll);

    public static Task ReplicateLabourRateMappingAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, LabourRateMapping>(connectionString, databaseId)
            .Replicate<LabourRateMappingCosmosModel>(
                NoSQLConstants.Containers.LabourRateMappings,
                mapping => MenuCosmosMappers.Map(mapping))
            .UpdateReference<MenuVariantCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, mapping) => MenuReplicationFinders.VariantsEmbeddingLabourRateMapping(
                    query, mapping.BrandID, mapping.LabourRate),
                (mapping, document) => MenuCosmosMappers.ApplyTo(mapping, document))
            .RunAsync(updateAll);

    public static Task ReplicateBrandMappingAsync<TDbContext>(
        this CosmosDBReplication cosmos, string connectionString, string databaseId, bool updateAll = false)
        where TDbContext : ShiftDbContext =>
        cosmos.SetUp<TDbContext, BrandMapping>(connectionString, databaseId)
            .Replicate<BrandMappingCosmosModel>(
                NoSQLConstants.Containers.BrandMappings,
                mapping => MenuCosmosMappers.Map(mapping))
            .UpdateReference<MenuVariantCosmosModel>(
                NoSQLConstants.Containers.ServiceMenus,
                (query, mapping) => MenuReplicationFinders.VariantsEmbeddingBrandMapping(query, mapping.BrandID),
                (mapping, document) => MenuCosmosMappers.ApplyTo(mapping, document))
            .RunAsync(updateAll);
}
