using DuckDB.NET.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ShiftSoftware.ADP.Menus.Sync;

namespace ShiftSoftware.ADP.Menus.Sample.Functions.Functions;

/// <summary>
/// The SQL → DuckDB half of the menu data movement: ONE on-demand endpoint that syncs every menu
/// table into one DuckDB database file, one table after another, through the reusable
/// <see cref="ServiceMenuDuckDBSyncService"/> — the local-file counterpart of this host's
/// <c>POST api/replicate-all</c> Cosmos sweep, reading the same SQL database through the same
/// <see cref="MenuReplicationDB"/> context.
///
/// <para><b>Incremental by default, full on demand.</b> Each table pulls only the rows saved at or
/// past the DuckDB side's own <c>MAX(LastSaveDate)</c> watermark (a first run over a fresh file is
/// automatically a full pull). <c>?fullReload=true</c> forces every table to re-pull everything and
/// prune rows whose ids left SQL — the reconciler for hard deletes, exactly like the Cosmos sweep's
/// <c>updateAll</c>.</para>
///
/// <para><b>One writer at a time.</b> DuckDB allows a single writer per file, so run one sync call at
/// a time — this endpoint is on-demand precisely so nothing overlaps it on a schedule. The file it
/// writes is what the DuckDB menu lookup reads
/// (<c>AddDuckDBServiceMenuLookup("DataSource=…;access_mode=read_only")</c> pointed at the same
/// path).</para>
/// </summary>
public class MenuDuckDBSyncFunctions
{
    private readonly ServiceMenuDuckDBSyncService syncService;
    private readonly MenuReplicationDB database;
    private readonly IConfiguration configuration;
    private readonly ILogger<MenuDuckDBSyncFunctions> logger;

    public MenuDuckDBSyncFunctions(
        ServiceMenuDuckDBSyncService syncService,
        MenuReplicationDB database,
        IConfiguration configuration,
        ILogger<MenuDuckDBSyncFunctions> logger)
    {
        this.syncService = syncService;
        this.database = database;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// Syncs all menu tables into the DuckDB database, one by one, and reports what each table did:
    /// rows upserted, rows pruned, and the watermark the pull started from (null = a full pull).
    ///
    /// <para><b>Read the per-table results, not just the status code.</b> A table whose engine pass
    /// failed is reported with <c>succeeded: false</c> and the run carries on to the next table — the
    /// sync is idempotent, so the fix is to run it again.</para>
    /// </summary>
    [Function(nameof(DuckDBSyncAllHttp))]
    public async Task<IActionResult> DuckDBSyncAllHttp(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "duckdb-sync")] HttpRequest request)
    {
        var fullReload = bool.TryParse(request.Query["fullReload"], out var parsed) && parsed;
        var connectionString = GetDuckDBConnectionString();

        using var connection = new DuckDBConnection(connectionString);
        connection.Open();

        logger.LogInformation(
            "Menu DuckDB sync started at {start} into '{database}' (fullReload: {fullReload}).",
            DateTime.UtcNow, connection.DataSource, fullReload);

        var result = await syncService.SyncAllAsync(database, connection, fullReload, request.HttpContext.RequestAborted);

        if (!result.Succeeded)
            logger.LogWarning(
                "Menu DuckDB sync finished with failed table(s): {tables}. Run it again — the sync is idempotent.",
                string.Join(", ", result.Tables.Where(x => !x.Succeeded).Select(x => x.Table)));
        else
            logger.LogInformation("Menu DuckDB sync finished at {end}.", DateTime.UtcNow);

        return new OkObjectResult(new
        {
            synced = result.Succeeded,
            database = connection.DataSource,
            fullReload,
            tables = result.Tables,
        });
    }

    /// <summary>
    /// Reads <c>ConnectionStrings:DuckDB</c>, defaulting to a <c>menus.duckdb</c> file beside the
    /// host — DuckDB needs no server, so unlike the Cosmos half the sample works with no
    /// configuration at all.
    /// </summary>
    private string GetDuckDBConnectionString()
    {
        var connectionString = configuration.GetConnectionString("DuckDB");

        return string.IsNullOrWhiteSpace(connectionString)
            ? "DataSource=menus.duckdb"
            : connectionString;
    }
}
