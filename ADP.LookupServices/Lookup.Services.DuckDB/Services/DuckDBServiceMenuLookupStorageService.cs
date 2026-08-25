using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using ShiftSoftware.ADP.Models.Service.DuckDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// The DuckDB implementation of <see cref="IServiceMenuLookupStorageService"/> — reads the NORMALIZED
/// menu tables that <c>ServiceMenuDuckDBSyncService</c> (in <c>ShiftSoftware.ADP.Menus.Sync</c>)
/// produces, and assembles them into the same <see cref="ServiceMenuDocuments"/> shape the Cosmos
/// reader returns, so everything above the storage seam is identical on both backends.
///
/// <para><b>Joins instead of embedded copies.</b> Cosmos cannot join, so its documents embed the
/// reference data (intervals, groups, replacement items, mappings) and replication fans out updates
/// to keep the copies fresh. Here the reference data lives once in its own tables and is joined at
/// read time — always current as of the snapshot, with no copies to go stale. The assembly rules
/// mirror <c>MenuCosmosMappers</c> (and the mapping-selection rules mirror
/// <c>MenuReplicationReload</c>) — the differential test that generates the same menu through both
/// backends is what pins the two implementations together.</para>
///
/// <para><b>Entered by basic model code, then everything by id</b> — the mirror of the vehicle DuckDB
/// tables entered by VIN. A whole set of codes resolves with one <c>IN</c>-clause query per
/// menu-graph table per 500-code chunk, and the small reference catalogs (intervals, groups,
/// standalone groups, mappings) load ONCE per reader instance and serve every lookup from memory.</para>
///
/// <para><b>A per-code cache, scoped to this instance's lifetime.</b> Hosts point DuckDB readers at
/// published snapshot files and register them scoped — the vehicle DuckDB service caches on the same
/// reasoning — and the cache is what makes the vehicle lookup's bulk flow cheap here: many VINs share
/// few basic model codes. Cached <see cref="ServiceMenuDocuments"/> instances are SHARED between
/// calls, so treat them as read-only; and a host that syncs into the same database this instance is
/// reading should use a fresh scope (a fresh reader) afterwards.</para>
///
/// <para><b>Faults are translated, not swallowed, and never invent an empty menu.</b> Missing menu
/// tables throw <see cref="ServiceMenuContainerNotFoundException"/> (this store was never synced);
/// any other storage failure throws <see cref="ServiceMenuStorageException"/>, which the vehicle
/// lookup's menu section contains exactly like a <c>CosmosException</c>.</para>
/// </summary>
public class DuckDBServiceMenuLookupStorageService : IServiceMenuLookupStorageService, IDisposable
{
    private const int CodeChunkSize = 500;

    private static readonly string[] MenuTableNames =
    [
        ServiceMenuDuckDBTables.Menu,
        ServiceMenuDuckDBTables.VehicleModel,
        ServiceMenuDuckDBTables.MenuVariant,
        ServiceMenuDuckDBTables.MenuVariantLabourRate,
        ServiceMenuDuckDBTables.MenuPeriodicAvailability,
        ServiceMenuDuckDBTables.MenuLabourDetails,
        ServiceMenuDuckDBTables.MenuItem,
        ServiceMenuDuckDBTables.MenuItemPart,
        ServiceMenuDuckDBTables.MenuItemPartCountryPrice,
        ServiceMenuDuckDBTables.ServiceInterval,
        ServiceMenuDuckDBTables.ServiceIntervalGroup,
        ServiceMenuDuckDBTables.ReplacementItem,
        ServiceMenuDuckDBTables.ReplacementItemServiceIntervalGroup,
        ServiceMenuDuckDBTables.ReplacementItemVehicleModel,
        ServiceMenuDuckDBTables.StandaloneReplacementItemGroup,
        ServiceMenuDuckDBTables.LabourRateMapping,
        ServiceMenuDuckDBTables.BrandMapping,
    ];

    private readonly global::DuckDB.NET.Data.DuckDBConnection connection;
    private readonly bool ownsConnection;

    private readonly ConcurrentDictionary<string, ServiceMenuDocuments> menuCache = new(StringComparer.Ordinal);
    private bool tablesVerified;
    private ReferenceData referenceData;

    /// <summary>
    /// Over a connection the HOST owns: the host opens it, shares it (typically with the vehicle
    /// DuckDB services), and disposes it. This reader never closes it.
    /// </summary>
    public DuckDBServiceMenuLookupStorageService(global::DuckDB.NET.Data.DuckDBConnection connection)
    {
        this.connection = connection;
    }

    /// <summary>
    /// Over the reader's OWN connection, opened here from a connection string and disposed with the
    /// reader. For hosts that keep no shared <c>DuckDBConnection</c> registration — each instance
    /// (each scope, under DI) opens the file itself, so point the string at a published read snapshot
    /// and prefer <c>access_mode=read_only</c>.
    /// </summary>
    public DuckDBServiceMenuLookupStorageService(string connectionString)
    {
        connection = new global::DuckDB.NET.Data.DuckDBConnection(connectionString);
        connection.Open();
        ownsConnection = true;
    }

    public void Dispose()
    {
        if (ownsConnection)
            connection.Dispose();
    }

    public async Task<ServiceMenuDocuments> GetMenuDocumentsAsync(string basicModelCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(basicModelCode))
            return new ServiceMenuDocuments { BasicModelCode = basicModelCode };

        var results = await GetMenuDocumentsAsync(new[] { basicModelCode }, cancellationToken);

        return results.Count > 0
            ? results[0]
            : new ServiceMenuDocuments { BasicModelCode = basicModelCode.Trim() };
    }

    public async Task<IReadOnlyList<ServiceMenuDocuments>> GetMenuDocumentsAsync(IEnumerable<string> basicModelCodes, CancellationToken cancellationToken = default)
    {
        var codes = ServiceMenuCosmosService.NormalizeCodes(basicModelCodes);

        if (codes.Count == 0)
            return new List<ServiceMenuDocuments>();

        var missing = codes.Where(code => !menuCache.ContainsKey(code)).ToList();

        if (missing.Count > 0)
        {
            EnsureMenuTablesExist();
            var reference = await GetReferenceDataAsync(cancellationToken);

            foreach (var chunk in Chunk(missing, CodeChunkSize))
            {
                // Every chunk code gets an entry up front, so a code with no rows caches as an EMPTY
                // document set — "no menu" is an answer worth caching too, or bulk vehicle lookups
                // would re-query the same missing model for every VIN that maps to it.
                var sets = chunk.ToDictionary(
                    code => code,
                    code => new ServiceMenuDocuments { BasicModelCode = code },
                    StringComparer.Ordinal);

                await AssembleChunkAsync(chunk, sets, reference, cancellationToken);

                foreach (var entry in sets)
                    menuCache[entry.Key] = entry.Value;
            }
        }

        return codes.Select(code => menuCache[code]).ToList();
    }

    // ---- the join-and-assemble read ------------------------------------------------------------------

    /// <summary>
    /// One chunk of codes: the menu-graph tables are fetched with one <c>IN</c>-clause query each,
    /// then assembled into per-code document sets. The assembly reproduces <c>MenuCosmosMappers</c>'
    /// projections field for field — what Cosmos embeds at write time, this composes at read time.
    /// </summary>
    private async Task AssembleChunkAsync(
        List<string> chunk,
        Dictionary<string, ServiceMenuDocuments> sets,
        ReferenceData reference,
        CancellationToken cancellationToken)
    {
        var codeInClause = string.Join(",", chunk.Select(code => $"'{code.Replace("'", "''")}'"));

        var menus = await QueryAsync<MenuDuckDBModel>(
            $"SELECT * FROM {ServiceMenuDuckDBTables.Menu} WHERE BasicModelCode IN ({codeInClause})", cancellationToken);

        if (menus.Count == 0)
            return;

        var vehicleModels = (await QueryByIdsAsync<MenuVehicleModelDuckDBModel>(
                ServiceMenuDuckDBTables.VehicleModel, "ID",
                menus.Where(x => x.VehicleModelID.HasValue).Select(x => x.VehicleModelID.Value), cancellationToken))
            .ToDictionary(x => x.ID);

        var variants = await QueryByIdsAsync<MenuVariantDuckDBModel>(
            ServiceMenuDuckDBTables.MenuVariant, "MenuID", menus.Select(x => x.ID), cancellationToken);

        var variantIds = variants.Select(x => x.ID).ToList();

        var variantRates = (await QueryByIdsAsync<MenuVariantLabourRateDuckDBModel>(
                ServiceMenuDuckDBTables.MenuVariantLabourRate, "MenuVariantID", variantIds, cancellationToken))
            .ToLookup(x => x.MenuVariantID);

        var periods = await QueryByIdsAsync<MenuPeriodicAvailabilityDuckDBModel>(
            ServiceMenuDuckDBTables.MenuPeriodicAvailability, "MenuVariantID", variantIds, cancellationToken);

        var labours = await QueryByIdsAsync<MenuLabourDetailsDuckDBModel>(
            ServiceMenuDuckDBTables.MenuLabourDetails, "MenuVariantID", variantIds, cancellationToken);

        var items = await QueryByIdsAsync<MenuItemDuckDBModel>(
            ServiceMenuDuckDBTables.MenuItem, "MenuVariantID", variantIds, cancellationToken);

        var parts = (await QueryByIdsAsync<MenuItemPartDuckDBModel>(
                ServiceMenuDuckDBTables.MenuItemPart, "MenuItemID", items.Select(x => x.ID), cancellationToken))
            .ToLookup(x => x.MenuItemID);

        var prices = (await QueryByIdsAsync<MenuItemPartCountryPriceDuckDBModel>(
                ServiceMenuDuckDBTables.MenuItemPartCountryPrice, "MenuItemPartID",
                parts.SelectMany(group => group).Select(x => x.ID), cancellationToken))
            .ToLookup(x => x.MenuItemPartID);

        var itemLinks = (await QueryByIdsAsync<ReplacementItemVehicleModelDuckDBModel>(
                ServiceMenuDuckDBTables.ReplacementItemVehicleModel, "ID",
                items.Where(x => x.ReplacementItemVehicleModelID.HasValue).Select(x => x.ReplacementItemVehicleModelID.Value),
                cancellationToken))
            .ToDictionary(x => x.ID);

        var replacementItems = (await QueryByIdsAsync<ReplacementItemDuckDBModel>(
                ServiceMenuDuckDBTables.ReplacementItem, "ID",
                itemLinks.Values.Select(x => x.ReplacementItemID), cancellationToken))
            .ToDictionary(x => x.ID);

        // The replacement-item ↔ interval-group links, LIVE ones only — the one soft-delete filter,
        // mirroring MenuCosmosMappers.IntervalGroupLinks: a link contributes only its group id to a
        // flat list, so a deleted link that is projected would be indistinguishable from a live one.
        var liveGroupLinks = (await QueryByIdsAsync<ReplacementItemServiceIntervalGroupDuckDBModel>(
                ServiceMenuDuckDBTables.ReplacementItemServiceIntervalGroup, "ReplacementItemID",
                replacementItems.Keys, cancellationToken))
            .Where(link => !link.IsDeleted)
            .OrderBy(link => link.ID)
            .ToLookup(link => link.ReplacementItemID);

        // ---- assemble, per menu, into its code's document set ----------------------------------------

        var menusById = menus.ToDictionary(x => x.ID);
        var variantCode = new Dictionary<long, string>();

        foreach (var variant in variants.OrderBy(x => x.ID))
        {
            if (!menusById.TryGetValue(variant.MenuID, out var menu) || !sets.TryGetValue(menu.BasicModelCode?.Trim() ?? string.Empty, out var documents))
                continue;

            variantCode[variant.ID] = documents.BasicModelCode;
            var vehicleModel = menu.VehicleModelID.HasValue && vehicleModels.TryGetValue(menu.VehicleModelID.Value, out var vm) ? vm : null;
            var brandId = vehicleModel?.BrandID;

            documents.Variants.Add(new MenuVariantCosmosModel
            {
                id = variant.ID.ToString(),
                BasicModelCode = documents.BasicModelCode,
                VariantID = variant.ID,
                BrandID = brandId,
                Model = vehicleModel?.Name,
                VariantName = variant.Name,
                MenuPrefix = variant.MenuPrefix,
                MenuPostfix = variant.MenuPostfix,
                StandaloneMenuPrefix = variant.StandaloneMenuPrefix,
                StandaloneMenuPostfix = variant.StandaloneMenuPostfix,
                LabourRate = variant.LabourRate,
                DiscountPercentage = variant.DiscountPercentage,
                IsFree = variant.IsFree,
                HasStandaloneItems = variant.HasStandaloneItems,

                // Joined live from the Menu row — in Cosmos this flag must be flattened onto the
                // variant document because deletes do not cascade; here the join IS the freshness.
                MenuIsDeleted = menu.IsDeleted,

                CountryLabourRates = variantRates[variant.ID]
                    .OrderBy(rate => rate.ID)
                    .Select(rate => new MenuCountryLabourRateCosmosModel
                    {
                        CountryID = rate.CountryID,
                        LabourRate = rate.LabourRate,
                        IsDeleted = rate.IsDeleted,
                    })
                    .ToList(),

                LabourRateMapping = reference.SelectLabourRateMapping(brandId, variant.LabourRate),
                BrandMapping = reference.SelectBrandMapping(brandId),

                IsDeleted = variant.IsDeleted,
            });
        }

        foreach (var period in periods.OrderBy(x => x.ID))
        {
            if (!variantCode.TryGetValue(period.MenuVariantID, out var code))
                continue;

            sets[code].Periods.Add(new MenuPeriodCosmosModel
            {
                id = period.ID.ToString(),
                BasicModelCode = code,
                VariantID = period.MenuVariantID,
                ServiceIntervalID = period.ServiceIntervalID,
                ServiceInterval = reference.IntervalDocument(period.ServiceIntervalID),
                IsDeleted = period.IsDeleted,
            });
        }

        foreach (var labour in labours.OrderBy(x => x.ID))
        {
            if (!variantCode.TryGetValue(labour.MenuVariantID, out var code))
                continue;

            sets[code].Labours.Add(new MenuLabourCosmosModel
            {
                id = labour.ID.ToString(),
                BasicModelCode = code,
                VariantID = labour.MenuVariantID,
                ServiceIntervalGroupID = labour.ServiceIntervalGroupID,
                AllowedTime = labour.AllowedTime,
                Consumable = labour.Consumable,
                ServiceIntervalGroup = reference.GroupDocument(labour.ServiceIntervalGroupID),
                IsDeleted = labour.IsDeleted,
            });
        }

        foreach (var item in items.OrderBy(x => x.ID))
        {
            if (!variantCode.TryGetValue(item.MenuVariantID, out var code))
                continue;

            var link = item.ReplacementItemVehicleModelID.HasValue
                && itemLinks.TryGetValue(item.ReplacementItemVehicleModelID.Value, out var foundLink)
                ? foundLink
                : null;

            var replacementItem = link is not null && replacementItems.TryGetValue(link.ReplacementItemID, out var foundItem)
                ? foundItem
                : null;

            var groupLinks = replacementItem is null
                ? Enumerable.Empty<ReplacementItemServiceIntervalGroupDuckDBModel>()
                : liveGroupLinks[replacementItem.ID];

            sets[code].Items.Add(new MenuItemCosmosModel
            {
                id = item.ID.ToString(),
                BasicModelCode = code,
                MenuItemID = item.ID,
                VariantID = item.MenuVariantID,
                StandaloneAllowedTime = item.StandaloneAllowedTime,

                // About the LINK row, not the replacement item's own flag — MenuCosmosMappers.Map(MenuItem).
                HasReplacementItem = link is not null,
                ReplacementItemDeleted = link?.IsDeleted ?? false,

                ReplacementItem = replacementItem is null
                    ? null
                    : new ReplacementItemCosmosModel
                    {
                        id = replacementItem.ID.ToString(),
                        ReplacementItemID = replacementItem.ID,
                        FriendlyName = replacementItem.FriendlyName,
                        StandaloneOperationCode = replacementItem.StandaloneOperationCode,
                        StandaloneLabourCode = replacementItem.StandaloneLabourCode,
                        StandaloneReplacementItemGroupID = replacementItem.StandaloneReplacementItemGroupID,
                        ServiceIntervalGroupIDs = groupLinks.Select(x => x.ServiceIntervalGroupID).ToList(),
                        IsDeleted = replacementItem.IsDeleted,
                    },

                ServiceIntervalGroups = groupLinks
                    .Select(x => reference.GroupDocument(x.ServiceIntervalGroupID))
                    .Where(group => group is not null)
                    .ToList(),

                StandaloneGroup = reference.StandaloneGroupDocument(replacementItem?.StandaloneReplacementItemGroupID),

                Parts = parts[item.ID]
                    .OrderBy(part => part.ID)
                    .Select(part => new MenuItemPartCosmosModel
                    {
                        PartNumber = part.PartNumber,
                        SortOrder = part.SortOrder,
                        PeriodicQuantity = part.PeriodicQuantity,
                        StandaloneQuantity = part.StandaloneQuantity,
                        IsDeleted = part.IsDeleted,
                        CountryPrices = prices[part.ID]
                            .OrderBy(price => price.ID)
                            .Select(price => new MenuPartCountryPriceCosmosModel
                            {
                                CountryID = price.CountryID,
                                PartPrice = price.PartPrice,
                                PartFinalPrice = price.PartFinalPrice,
                                IsDeleted = price.IsDeleted,
                            })
                            .ToList(),
                    })
                    .ToList(),

                IsDeleted = item.IsDeleted,
            });
        }
    }

    // ---- the reference catalogs, loaded once per instance --------------------------------------------

    /// <summary>
    /// The master catalogs — intervals, groups, standalone groups and the two mapping tables. Small,
    /// read whole, and cached for this instance's lifetime (the snapshot cannot change under it), so
    /// assembly joins them from memory. Group membership derives from the intervals themselves
    /// (<c>ServiceIntervalGroupID</c>), UNFILTERED, exactly as the Cosmos projection copies it; the
    /// mapping selections reproduce <c>MenuReplicationReload</c>'s live-only, ordered-by-id rules.
    /// </summary>
    private sealed class ReferenceData
    {
        public Dictionary<long, ServiceIntervalDuckDBModel> Intervals;
        public Dictionary<long, ServiceIntervalGroupDuckDBModel> Groups;
        public ILookup<long, long> GroupMemberIntervalIds;
        public Dictionary<long, StandaloneReplacementItemGroupDuckDBModel> StandaloneGroups;
        public List<LabourRateMappingDuckDBModel> LabourRateMappings;
        public List<BrandMappingDuckDBModel> BrandMappings;

        public ServiceIntervalCosmosModel IntervalDocument(long id) =>
            Intervals.TryGetValue(id, out var interval)
                ? new ServiceIntervalCosmosModel
                {
                    id = interval.ID.ToString(),
                    ServiceIntervalID = interval.ID,
                    Code = interval.Code,
                    Description = interval.Description,
                    ValueInMeter = interval.ValueInMeter,
                    ServiceIntervalGroupID = interval.ServiceIntervalGroupID,
                    IsDeleted = interval.IsDeleted,
                }
                : null;

        public ServiceIntervalGroupCosmosModel GroupDocument(long id) =>
            Groups.TryGetValue(id, out var group)
                ? new ServiceIntervalGroupCosmosModel
                {
                    id = group.ID.ToString(),
                    ServiceIntervalGroupID = group.ID,
                    LabourCode = group.LabourCode,
                    ServiceIntervalIDs = GroupMemberIntervalIds[group.ID].ToList(),
                    IsDeleted = group.IsDeleted,
                }
                : null;

        public StandaloneReplacementItemGroupCosmosModel StandaloneGroupDocument(long? id) =>
            id.HasValue && StandaloneGroups.TryGetValue(id.Value, out var group)
                ? new StandaloneReplacementItemGroupCosmosModel
                {
                    id = group.ID.ToString(),
                    StandaloneReplacementItemGroupID = group.ID,
                    Name = group.Name,
                    MenuCode = group.MenuCode,
                    LabourCode = group.LabourCode,
                    IsDeleted = group.IsDeleted,
                }
                : null;

        public LabourRateMappingCosmosModel SelectLabourRateMapping(long? brandId, decimal labourRate)
        {
            var mapping = LabourRateMappings
                .Where(x => !x.IsDeleted && x.BrandID == brandId && x.LabourRate == labourRate)
                .OrderBy(x => x.ID)
                .FirstOrDefault();

            return mapping is null
                ? null
                : new LabourRateMappingCosmosModel
                {
                    id = mapping.ID.ToString(),
                    BrandID = mapping.BrandID,
                    LabourRate = mapping.LabourRate,
                    Code = mapping.Code,
                    IsDeleted = mapping.IsDeleted,
                };
        }

        public BrandMappingCosmosModel SelectBrandMapping(long? brandId)
        {
            var mapping = BrandMappings
                .Where(x => !x.IsDeleted && x.BrandID == brandId)
                .OrderBy(x => x.ID)
                .FirstOrDefault();

            return mapping is null
                ? null
                : new BrandMappingCosmosModel
                {
                    id = mapping.ID.ToString(),
                    BrandID = mapping.BrandID,
                    Code = mapping.Code,
                    BrandAbbreviation = mapping.BrandAbbreviation,
                    IsDeleted = mapping.IsDeleted,
                };
        }
    }

    private async Task<ReferenceData> GetReferenceDataAsync(CancellationToken cancellationToken)
    {
        if (referenceData is not null)
            return referenceData;

        var intervals = await QueryAsync<ServiceIntervalDuckDBModel>(
            $"SELECT * FROM {ServiceMenuDuckDBTables.ServiceInterval}", cancellationToken);

        referenceData = new ReferenceData
        {
            Intervals = intervals.ToDictionary(x => x.ID),
            GroupMemberIntervalIds = intervals.OrderBy(x => x.ID).ToLookup(x => x.ServiceIntervalGroupID, x => x.ID),
            Groups = (await QueryAsync<ServiceIntervalGroupDuckDBModel>(
                $"SELECT * FROM {ServiceMenuDuckDBTables.ServiceIntervalGroup}", cancellationToken)).ToDictionary(x => x.ID),
            StandaloneGroups = (await QueryAsync<StandaloneReplacementItemGroupDuckDBModel>(
                $"SELECT * FROM {ServiceMenuDuckDBTables.StandaloneReplacementItemGroup}", cancellationToken)).ToDictionary(x => x.ID),
            LabourRateMappings = await QueryAsync<LabourRateMappingDuckDBModel>(
                $"SELECT * FROM {ServiceMenuDuckDBTables.LabourRateMapping}", cancellationToken),
            BrandMappings = await QueryAsync<BrandMappingDuckDBModel>(
                $"SELECT * FROM {ServiceMenuDuckDBTables.BrandMapping}", cancellationToken),
        };

        return referenceData;
    }

    // ---- plumbing ------------------------------------------------------------------------------------

    /// <summary>
    /// The missing-table check, done ONCE per instance against information_schema rather than by
    /// sniffing error messages: a store that was never synced must fail loudly as "not provisioned",
    /// not per-query and not as an empty menu.
    /// </summary>
    private void EnsureMenuTablesExist()
    {
        if (tablesVerified)
            return;

        List<string> existing;

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT table_name FROM information_schema.tables";

            existing = new List<string>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
                existing.Add(reader.GetString(0));
        }
        catch (Exception ex)
        {
            throw new ServiceMenuStorageException(
                "The DuckDB menu store could not be inspected (querying information_schema failed). " +
                "The connection may be closed, or the file unreadable.",
                ex);
        }

        var missing = MenuTableNames
            .Where(name => !existing.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count > 0)
            throw new ServiceMenuContainerNotFoundException(
                connection.Database,
                string.Join(", ", missing),
                $"The DuckDB menu table(s) '{string.Join(", ", missing)}' do not exist in this database. " +
                "This store has never been populated with menu data (or the connection points at the wrong " +
                "file). Run ServiceMenuDuckDBSyncService (ShiftSoftware.ADP.Menus.Sync) against this " +
                "database, then look up again.",
                null);

        tablesVerified = true;
    }

    private static IEnumerable<List<string>> Chunk(IReadOnlyList<string> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
            yield return items.Skip(i).Take(size).ToList();
    }

    private Task<List<T>> QueryByIdsAsync<T>(string tableName, string idColumn, IEnumerable<long> ids, CancellationToken cancellationToken)
        where T : new()
    {
        var idList = ids.Distinct().ToList();

        if (idList.Count == 0)
            return Task.FromResult(new List<T>());

        return QueryAsync<T>(
            $"SELECT * FROM {tableName} WHERE {idColumn} IN ({string.Join(",", idList)})", cancellationToken);
    }

    /// <summary>
    /// Reads rows onto the row model: columns by name (case-insensitive), scalars only — the
    /// normalized tables have no JSON columns. This does NOT swallow: a row that cannot materialize
    /// means the store and the reader disagree about the schema, and a menu quietly missing its parts
    /// is a wrong price, not a degraded one.
    /// </summary>
    private async Task<List<T>> QueryAsync<T>(string sql, CancellationToken cancellationToken) where T : new()
    {
        try
        {
            var results = new List<T>();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columnOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                columnOrdinals[reader.GetName(i)] = i;

            var properties = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                .ToList();

            while (await reader.ReadAsync(cancellationToken))
            {
                var model = new T();

                foreach (var property in properties)
                {
                    if (!columnOrdinals.TryGetValue(property.Name, out var ordinal) || reader.IsDBNull(ordinal))
                        continue;

                    property.SetValue(model, ReadValue(reader, ordinal, property.PropertyType));
                }

                results.Add(model);
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ServiceMenuStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ServiceMenuStorageException(
                $"The DuckDB menu store could not be read (query failed or a row did not match the " +
                $"expected schema): {sql}",
                ex);
        }
    }

    private static object ReadValue(DbDataReader reader, int ordinal, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
            return reader.GetValue(ordinal)?.ToString();

        if (underlyingType == typeof(bool))
            return reader.GetBoolean(ordinal);

        if (underlyingType == typeof(int))
            return Convert.ToInt32(reader.GetValue(ordinal));

        if (underlyingType == typeof(long))
            return Convert.ToInt64(reader.GetValue(ordinal));

        if (underlyingType == typeof(decimal))
            return Convert.ToDecimal(reader.GetValue(ordinal));

        if (underlyingType == typeof(double))
            return Convert.ToDouble(reader.GetValue(ordinal));

        if (underlyingType == typeof(DateTime))
            return reader.GetDateTime(ordinal);

        if (underlyingType == typeof(DateTimeOffset))
        {
            var value = reader.GetValue(ordinal);
            return value is DateTimeOffset offset ? offset : new DateTimeOffset(Convert.ToDateTime(value), TimeSpan.Zero);
        }

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, Convert.ToInt32(reader.GetValue(ordinal)));

        return reader.GetValue(ordinal);
    }
}
