using DuckDB.NET.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Models.Service.DuckDB;
using ShiftSoftware.ADP.SyncAgent;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using ShiftSoftware.ShiftEntity.Core;
using System.Linq.Expressions;

namespace ShiftSoftware.ADP.Menus.Sync;

/// <summary>
/// Syncs the menu catalog from its SOURCE OF TRUTH — the menus SQL database — into NORMALIZED DuckDB
/// tables through the <see cref="SyncEngine{TSource, TDestination}"/>, for menu lookups served from
/// DuckDB storage (<c>ShiftSoftware.ADP.Lookup.Services.DuckDB</c>).
///
/// <para><b>One method per table, one table per source table</b> — the same shape as the Cosmos
/// catch-up sweep beside this class (<c>MenuCatchUpReplicationExtensions</c>), so a host can sync
/// everything (<see cref="SyncAllAsync"/>) or drive individual tables from its own orchestration.
/// Unlike Cosmos, the tables are NOT denormalized: Cosmos cannot join, so its documents embed copies
/// of the reference data and replication fans out updates to keep the copies fresh — DuckDB joins,
/// so reference data lives once in its own table, the reader joins it at query time, and there are
/// no embedded copies to go stale and no fan-outs at all. The layout
/// (<see cref="ServiceMenuDuckDBTables"/>, models in <c>ADP.Models</c>) stays lookup-optimized:
/// <c>Menu.BasicModelCode</c> is the entry point and everything else is reached by id — the mirror
/// of the vehicle DuckDB tables entered by VIN.</para>
///
/// <para><b>Incremental by destination watermark, not by replication bookkeeping.</b> Each table
/// pulls source rows whose <c>LastSaveDate</c> is at or past the DESTINATION's
/// <c>MAX(LastSaveDate)</c> — the same idiom the vehicle DuckDB sync uses — so the store needs no
/// per-row replication stamps and the <c>&gt;=</c> boundary overlap is harmless (writes are keyed
/// upserts). A missing table or unreadable watermark means "cannot prove an incremental base" and
/// falls back to a full pull. Because the layout is normalized, a row changes only when ITS source
/// row changes, so per-table watermarks are consistent by construction. Hard deletes are reconciled
/// by a FULL sync (<c>fullReload: true</c>, or the automatic first run), which prunes rows whose ids
/// left the source — schedule one periodically, exactly as the vehicle sync's full mode is scheduled;
/// soft deletes are ordinary updates and flow through incrementally.</para>
///
/// <para><b>The caller owns the connection and the single-writer rule</b>: pass the WRITE database's
/// connection, run one sync at a time, and publish the written file to readers however the host
/// already does for its vehicle tables.</para>
/// </summary>
public class ServiceMenuDuckDBSyncService
{
    private const long BatchSize = 10_000;
    private const long MaxRetryCount = 3;
    private const long OperationTimeoutInSeconds = 600;

    private readonly ILogger<ServiceMenuDuckDBSyncService>? logger;

    /// <param name="logger">
    /// Optional. When present, every table's engine gets a <see cref="SyncEngineILogger"/> registered
    /// on it, so the engine's own narration — per action, per batch, per retry, exceptions included —
    /// comes through the host's logging like any other service.
    /// </param>
    public ServiceMenuDuckDBSyncService(ILogger<ServiceMenuDuckDBSyncService>? logger = null)
    {
        this.logger = logger;
    }

    // ---- one method per table ------------------------------------------------------------------------

    // Each table declares the INDEXES its readers need, alongside the table itself — the sync is what
    // owns the schema, so it is the only place that can hand the reader one. They mirror the DuckDB
    // menu lookup's access path exactly: it enters at Menu.BasicModelCode and then walks the graph by
    // FOREIGN id (menu → variants → items → parts → prices), one IN-clause query per hop, so every
    // column it joins on gets an index and nothing else does. Reads by ID need none — the row's own
    // id is the PRIMARY KEY, which DuckDB already indexes — and the small reference catalogs
    // (intervals, groups, standalone groups, the mappings) are read whole, once per reader, so an
    // index on them would be maintenance paid for a scan that happens anyway.

    public Task<ServiceMenuTableSyncResult> SyncMenusAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<Menu, MenuDuckDBModel>(database, connection, ServiceMenuDuckDBTables.Menu, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.BasicModelCode }]);

    public Task<ServiceMenuTableSyncResult> SyncVehicleModelsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<VehicleModel, MenuVehicleModelDuckDBModel>(database, connection, ServiceMenuDuckDBTables.VehicleModel, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    public Task<ServiceMenuTableSyncResult> SyncMenuVariantsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<MenuVariant, MenuVariantDuckDBModel>(database, connection, ServiceMenuDuckDBTables.MenuVariant, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.MenuID }]);

    public Task<ServiceMenuTableSyncResult> SyncMenuVariantLabourRatesAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<MenuVariantLabourRate, MenuVariantLabourRateDuckDBModel>(database, connection, ServiceMenuDuckDBTables.MenuVariantLabourRate, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.MenuVariantID }]);

    public Task<ServiceMenuTableSyncResult> SyncMenuPeriodsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<MenuPeriodicAvailability, MenuPeriodicAvailabilityDuckDBModel>(database, connection, ServiceMenuDuckDBTables.MenuPeriodicAvailability, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.MenuVariantID }]);

    public Task<ServiceMenuTableSyncResult> SyncMenuLaboursAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<MenuLabourDetails, MenuLabourDetailsDuckDBModel>(database, connection, ServiceMenuDuckDBTables.MenuLabourDetails, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.MenuVariantID }]);

    public Task<ServiceMenuTableSyncResult> SyncMenuItemsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<MenuItem, MenuItemDuckDBModel>(database, connection, ServiceMenuDuckDBTables.MenuItem, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.MenuVariantID }]);

    public Task<ServiceMenuTableSyncResult> SyncMenuItemPartsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<MenuItemPart, MenuItemPartDuckDBModel>(database, connection, ServiceMenuDuckDBTables.MenuItemPart, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.MenuItemID }]);

    public Task<ServiceMenuTableSyncResult> SyncMenuItemPartCountryPricesAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<MenuItemPartCountryPrice, MenuItemPartCountryPriceDuckDBModel>(database, connection, ServiceMenuDuckDBTables.MenuItemPartCountryPrice, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.MenuItemPartID }]);

    public Task<ServiceMenuTableSyncResult> SyncServiceIntervalsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<ServiceInterval, ServiceIntervalDuckDBModel>(database, connection, ServiceMenuDuckDBTables.ServiceInterval, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    public Task<ServiceMenuTableSyncResult> SyncServiceIntervalGroupsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<ServiceIntervalGroup, ServiceIntervalGroupDuckDBModel>(database, connection, ServiceMenuDuckDBTables.ServiceIntervalGroup, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    public Task<ServiceMenuTableSyncResult> SyncReplacementItemsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<ReplacementItem, ReplacementItemDuckDBModel>(database, connection, ServiceMenuDuckDBTables.ReplacementItem, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    public Task<ServiceMenuTableSyncResult> SyncReplacementItemServiceIntervalGroupsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<ReplacementItemServiceIntervalGroup, ReplacementItemServiceIntervalGroupDuckDBModel>(database, connection, ServiceMenuDuckDBTables.ReplacementItemServiceIntervalGroup, MenuDuckDBMappers.Map, fullReload, cancellationToken,
            [new() { Columns = row => row.ReplacementItemID }]);

    public Task<ServiceMenuTableSyncResult> SyncReplacementItemVehicleModelsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<ReplacementItemVehicleModel, ReplacementItemVehicleModelDuckDBModel>(database, connection, ServiceMenuDuckDBTables.ReplacementItemVehicleModel, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    public Task<ServiceMenuTableSyncResult> SyncStandaloneReplacementItemGroupsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<StandaloneReplacementItemGroup, StandaloneReplacementItemGroupDuckDBModel>(database, connection, ServiceMenuDuckDBTables.StandaloneReplacementItemGroup, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    public Task<ServiceMenuTableSyncResult> SyncLabourRateMappingsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<LabourRateMapping, LabourRateMappingDuckDBModel>(database, connection, ServiceMenuDuckDBTables.LabourRateMapping, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    public Task<ServiceMenuTableSyncResult> SyncBrandMappingsAsync(DbContext database, DuckDBConnection connection, bool fullReload = false, CancellationToken cancellationToken = default) =>
        SyncTableAsync<BrandMapping, BrandMappingDuckDBModel>(database, connection, ServiceMenuDuckDBTables.BrandMapping, MenuDuckDBMappers.Map, fullReload, cancellationToken);

    /// <summary>
    /// Syncs every menu table, reference tables first then the menu graph — the order is cosmetic
    /// (the tables carry no foreign-key constraints; the reader joins by id), but it means a reader
    /// opening mid-sync meets reference rows before the rows that point at them.
    /// </summary>
    public async Task<ServiceMenuDuckDBSyncResult> SyncAllAsync(
        DbContext database,
        DuckDBConnection connection,
        bool fullReload = false,
        CancellationToken cancellationToken = default)
    {
        var tables = new List<ServiceMenuTableSyncResult>
        {
            await SyncServiceIntervalGroupsAsync(database, connection, fullReload, cancellationToken),
            await SyncServiceIntervalsAsync(database, connection, fullReload, cancellationToken),
            await SyncStandaloneReplacementItemGroupsAsync(database, connection, fullReload, cancellationToken),
            await SyncReplacementItemsAsync(database, connection, fullReload, cancellationToken),
            await SyncReplacementItemServiceIntervalGroupsAsync(database, connection, fullReload, cancellationToken),
            await SyncReplacementItemVehicleModelsAsync(database, connection, fullReload, cancellationToken),
            await SyncLabourRateMappingsAsync(database, connection, fullReload, cancellationToken),
            await SyncBrandMappingsAsync(database, connection, fullReload, cancellationToken),
            await SyncVehicleModelsAsync(database, connection, fullReload, cancellationToken),
            await SyncMenusAsync(database, connection, fullReload, cancellationToken),
            await SyncMenuVariantsAsync(database, connection, fullReload, cancellationToken),
            await SyncMenuVariantLabourRatesAsync(database, connection, fullReload, cancellationToken),
            await SyncMenuPeriodsAsync(database, connection, fullReload, cancellationToken),
            await SyncMenuLaboursAsync(database, connection, fullReload, cancellationToken),
            await SyncMenuItemsAsync(database, connection, fullReload, cancellationToken),
            await SyncMenuItemPartsAsync(database, connection, fullReload, cancellationToken),
            await SyncMenuItemPartCountryPricesAsync(database, connection, fullReload, cancellationToken),
        };

        return new ServiceMenuDuckDBSyncResult(tables.All(x => x.Succeeded), tables);
    }

    // ---- the shared per-table flow -------------------------------------------------------------------

    /// <summary>
    /// One engine pass for one table, adapters on BOTH sides: <see cref="EFCoreSyncDataSource{TSource, TDestination, TDbContext}"/>
    /// reads the source (keyset-paged over the entity's id, the watermark filter applied in its
    /// <c>Query</c>), our mapper projects entity → row, and the DuckDB destination adapter creates
    /// the table and stores the batches as keyed upserts. On a FULL pull (no watermark, or an explicit
    /// full reload), rows whose ids left the source are pruned first — the hard-delete reconciler,
    /// which cannot come from the EF source because those rows no longer exist in SQL to be queried.
    /// </summary>
    /// <param name="indexes">
    /// The secondary indexes this table's readers need, created with the table by the destination
    /// adapter (see the note above the per-table methods). Null for the tables that need none.
    /// </param>
    private async Task<ServiceMenuTableSyncResult> SyncTableAsync<TEntity, TRow>(
        DbContext database,
        DuckDBConnection connection,
        string tableName,
        Func<TEntity, TRow> map,
        bool fullReload,
        CancellationToken cancellationToken,
        IReadOnlyList<DuckDBIndexDefinition<TRow>>? indexes = null)
        where TEntity : class
        where TRow : class, IServiceMenuDuckDBRow
    {
        ArgumentNullException.ThrowIfNull(connection);
        cancellationToken.ThrowIfCancellationRequested();

        var watermark = fullReload ? null : GetMaxWatermark(connection, tableName);

        var pruned = watermark is null
            ? Prune(connection, tableName, await ReadSourceIdsAsync<TEntity>(database, cancellationToken))
            : 0;

        var engine = new SyncEngine<TEntity, TRow>();

        // The ENGINE narrates its own run — every step, batch, retry and failure, exceptions
        // included — through any registered logger; SyncEngineILogger renders that narration into the
        // host's ILogger, with the table name as context.
        if (logger is not null)
            engine.RegisterLogger(new SyncEngineILogger(logger, tableName));

        engine.Configure(
            [SyncActionType.Add],
            BatchSize,
            MaxRetryCount,
            OperationTimeoutInSeconds,
            RetryAction.RetryAndStopAfterLastRetry);

        // Registered BEFORE the source adapter attaches, so the adapter's own BatchCompleted chains
        // it — the count of rows each batch actually stored, for the result.
        long upserted = 0;
        engine.SetupBatchCompleted(input =>
        {
            upserted += input.Input.StoreDataResult?.SucceededItems?.Count() ?? 0;
            return new ValueTask<bool>(true);
        });

        engine.SetupMapping((entities, _) => new ValueTask<IEnumerable<TRow?>?>(
            entities?.Select(entity => entity is null ? null : map(entity))));

        AttachSqlSource(engine, database, watermark);

        new DuckDBSyncDataDestination<TEntity, TRow, DuckDBConnection>(connection)
            .SetSyncService(engine)
            .Configure(new DuckDBSyncDataDestinationConfigurations<TEntity, TRow>
            {
                TableName = tableName,
                PrimaryKey = row => row.ID,
                Indexes = indexes,
            });

        try
        {
            var succeeded = await engine.RunAsync();

            // The per-table headline with the REAL numbers — the engine's step stream reports batches,
            // this reports what the run amounted to.
            logger?.Log(
                succeeded ? LogLevel.Information : LogLevel.Warning,
                "Menu DuckDB sync [{Table}] {Mode} sync {Outcome}: {Upserted} upserted, {Pruned} pruned (watermark: {Watermark}).",
                tableName,
                watermark is null ? "full" : "incremental",
                succeeded ? "succeeded" : "FAILED",
                upserted,
                pruned,
                watermark);

            return new ServiceMenuTableSyncResult(tableName, succeeded, (int)upserted, pruned, watermark);
        }
        finally
        {
            await engine.Reset();
        }
    }

    /// <summary>
    /// Attaches the SQL side of one table's engine: the <see cref="EFCoreSyncDataSource{TSource, TDestination, TDbContext}"/>
    /// adapter, keyset-paged over the entity's id.
    ///
    /// <para><c>SyncTimestamp</c> is left null DELIBERATELY: with it set, the adapter stamps every
    /// synced source row after each batch — replication-style bookkeeping in the source database.
    /// This sync's progress lives in the DESTINATION instead (the <c>MAX(LastSaveDate)</c> watermark
    /// this method's <c>Query</c> filters by), so the source is never written to at all.</para>
    ///
    /// <para>The <c>&gt;=</c> boundary overlap re-reads boundary rows on purpose; the keyed upsert
    /// makes it harmless. Soft-deleted rows are read deliberately (<c>IgnoreQueryFilters</c>, the
    /// same choice the replication reload makes): their rows carry <c>IsDeleted</c> and the
    /// generation layer owns every inclusion rule.</para>
    /// </summary>
    /// <remarks>
    /// Virtual as a deliberate test seam, the same idiom as the menu Cosmos reader's virtual read: it
    /// is the one place the sync touches the SQL source, so overriding it (with a fixture-fed
    /// <c>SetupGetSourceBatchItems</c>) lets the whole engine-and-DuckDB side run for real, offline.
    /// </remarks>
    protected virtual void AttachSqlSource<TEntity, TRow>(
        ISyncEngine<TEntity, TRow> engine,
        DbContext database,
        DateTimeOffset? watermark)
        where TEntity : class
        where TRow : class
    {
        ArgumentNullException.ThrowIfNull(database);

        // x => (object)x.ID, built by hand because TEntity is generic here. The adapter keysets and
        // orders its batches by this property.
        var parameter = Expression.Parameter(typeof(TEntity), "row");
        var idKey = Expression.Lambda<Func<TEntity, object>>(
            Expression.Convert(Expression.Property(parameter, "ID"), typeof(object)), parameter);

        new EFCoreSyncDataSource<TEntity, TRow, DbContext>(database)
            .SetSyncService(engine)
            .Configure(new EFCoreSyncDataSourceConfigurations<TEntity, TRow>
            {
                EntityKey = idKey,
                SourceKey = idKey,
                Query = (query, _) =>
                {
                    query = query.IgnoreQueryFilters();

                    return watermark is null
                        ? query
                        : query.Where(row =>
                            EF.Property<DateTimeOffset>(row, nameof(IShiftEntityAudit.LastSaveDate)) >= watermark.Value);
                },
            });
    }

    /// <summary>
    /// The ids currently in the source, for the full-pull prune. Its own seam because the prune is
    /// the one read the EF source adapter cannot serve — a hard-deleted row is not there to query.
    /// </summary>
    protected virtual async Task<List<long>> ReadSourceIdsAsync<TEntity>(
        DbContext database,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(database);

        return await database.Set<TEntity>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Select(row => EF.Property<long>(row, "ID"))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Deletes rows whose ids are no longer in the source. Missing table (first run) = nothing to prune.</summary>
    private static int Prune(DuckDBConnection connection, string tableName, List<long> sourceIds)
    {
        var currentIds = sourceIds.ToHashSet();
        var stale = GetExistingIds(connection, tableName)
            .Where(existingId => !currentIds.Contains(existingId))
            .ToList();

        foreach (var chunk in stale.Chunk(5000))
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {tableName} WHERE ID IN ({string.Join(",", chunk)})";
            command.ExecuteNonQuery();
        }

        return stale.Count;
    }

    /// <summary>
    /// The destination's watermark: the newest <c>LastSaveDate</c> it holds. Null — a missing table,
    /// an empty one, or any error — means "cannot prove an incremental base exists", and the caller
    /// falls back to a full pull; the same forgiving semantics the vehicle DuckDB sync's watermark
    /// reader has.
    /// </summary>
    private static DateTimeOffset? GetMaxWatermark(DuckDBConnection connection, string tableName)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT MAX(LastSaveDate) FROM {tableName}";

            return command.ExecuteScalar() switch
            {
                DateTimeOffset offset => offset,
                DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The ids currently stored, or none when the table does not exist yet (first run).</summary>
    private static List<long> GetExistingIds(DuckDBConnection connection, string tableName)
    {
        var ids = new List<long>();

        using (var tables = connection.CreateCommand())
        {
            tables.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'";

            if (Convert.ToInt64(tables.ExecuteScalar()) == 0)
                return ids;
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT ID FROM {tableName}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
                ids.Add(reader.GetInt64(0));
        }

        return ids;
    }
}

/// <summary>One table's sync outcome, and the watermark the incremental pull started from (null = full).</summary>
public record ServiceMenuTableSyncResult(
    string Table,
    bool Succeeded,
    int Upserted,
    int Pruned,
    DateTimeOffset? Watermark);

/// <summary>What one whole-catalog sync did, per table.</summary>
public record ServiceMenuDuckDBSyncResult(
    bool Succeeded,
    IReadOnlyList<ServiceMenuTableSyncResult> Tables);
