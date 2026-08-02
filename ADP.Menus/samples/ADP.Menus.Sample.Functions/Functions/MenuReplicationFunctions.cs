using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ShiftSoftware.ADP.Menus.Data.Replication;
using ShiftSoftware.ShiftEntity.CosmosDbReplication.Services;

using System.Runtime.CompilerServices;

namespace ShiftSoftware.ADP.Menus.Sample.Functions.Functions;

/// <summary>
/// Scheduled Cosmos catch-up replication for the menu catalog: ONE hourly timer per table (an
/// incremental, dirty-only re-sync), an on-demand HTTP endpoint that re-syncs EVERYTHING (a full
/// backfill), and one that reports how far replication has got. All of it runs through the reusable
/// <c>MenuCatchUpReplicationExtensions</c>, so the documents are byte-identical to the ones the sample
/// API's save trigger writes.
///
/// <para><b>Timers are disabled in local dev, two ways.</b> (1) The host-honoured
/// <c>AzureWebJobs.&lt;FunctionName&gt;.Disabled</c> settings in <c>local.settings.json</c> stop them
/// firing locally at all; a deployed host never reads that file, so replication runs there. (2)
/// <see cref="RunTimerAsync"/> also no-ops when the host environment is Development — a code-level
/// backstop for any Development host. Either way <c>POST api/replicate-all</c> works while developing.</para>
///
/// <para>The timers all fire on the hour and therefore overlap. That is safe here: each pass derives
/// its documents from SQL independently, so two passes touching one document write the same values.
/// (<c>ReplicateAllAsync</c> still orders model documents before master tables, because in a single
/// sequential run the master fan-outs should land last.)</para>
/// </summary>
public class MenuReplicationFunctions
{
    /// <summary>Top of every hour. Change here to re-schedule every per-table timer at once.</summary>
    private const string HourlySchedule = "0 0 * * * *";

    private readonly CosmosDBReplication cosmos;
    private readonly MenuReplicationDB database;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<MenuReplicationFunctions> logger;

    public MenuReplicationFunctions(
        CosmosDBReplication cosmos,
        MenuReplicationDB database,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<MenuReplicationFunctions> logger)
    {
        this.cosmos = cosmos;
        this.database = database;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    // ─────────────────────── the ServiceMenus container (per model-code partition) ───────────────────────

    [Function(nameof(ReplicateMenuVariantsTimer))]
    public Task ReplicateMenuVariantsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateMenuVariantAsync(database, connection, databaseId));

    [Function(nameof(ReplicateMenuPeriodsTimer))]
    public Task ReplicateMenuPeriodsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateMenuPeriodAsync<MenuReplicationDB>(connection, databaseId));

    [Function(nameof(ReplicateMenuLabourDetailsTimer))]
    public Task ReplicateMenuLabourDetailsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateMenuLabourAsync<MenuReplicationDB>(connection, databaseId));

    [Function(nameof(ReplicateMenuItemsTimer))]
    public Task ReplicateMenuItemsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateMenuItemAsync<MenuReplicationDB>(connection, databaseId));

    // ─────────────────────── master tables (own container each, plus fan-outs) ───────────────────────

    [Function(nameof(ReplicateServiceIntervalsTimer))]
    public Task ReplicateServiceIntervalsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateServiceIntervalAsync<MenuReplicationDB>(connection, databaseId));

    [Function(nameof(ReplicateServiceIntervalGroupsTimer))]
    public Task ReplicateServiceIntervalGroupsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateServiceIntervalGroupAsync<MenuReplicationDB>(connection, databaseId));

    [Function(nameof(ReplicateReplacementItemsTimer))]
    public Task ReplicateReplacementItemsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateReplacementItemAsync<MenuReplicationDB>(connection, databaseId));

    [Function(nameof(ReplicateStandaloneReplacementItemGroupsTimer))]
    public Task ReplicateStandaloneReplacementItemGroupsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateStandaloneReplacementItemGroupAsync<MenuReplicationDB>(connection, databaseId));

    [Function(nameof(ReplicateLabourRateMappingsTimer))]
    public Task ReplicateLabourRateMappingsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateLabourRateMappingAsync<MenuReplicationDB>(connection, databaseId));

    [Function(nameof(ReplicateBrandMappingsTimer))]
    public Task ReplicateBrandMappingsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((replication, connection, databaseId) =>
            replication.ReplicateBrandMappingAsync<MenuReplicationDB>(connection, databaseId));

    // ─────────────────────── on-demand: replicate EVERYTHING (full backfill) ───────────────────────

    /// <summary>
    /// Re-syncs every menu table with <c>updateAll: true</c> — every row, not just the dirty ones —
    /// then reports what is still outstanding.
    ///
    /// This is what to run when switching replication on for an existing catalogue, after rebuilding a
    /// container, or after the system-wide parts-price update (which changes prices without touching
    /// the MenuItem rows, so they are never dirty and only a full pass repairs their documents).
    ///
    /// <para><b>Read the response, not just the status code.</b> A row the sweep failed to write is
    /// marked unsuccessful and skipped silently — no exception, no log line — so a run that wrote 1060
    /// of 1188 documents finishes exactly like one that wrote all of them. The sweep is idempotent and
    /// self-healing, so the fix is to run it again; the counts below are how you know you need to.</para>
    /// </summary>
    [Function(nameof(ReplicateAllHttp))]
    public async Task<IActionResult> ReplicateAllHttp(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "replicate-all")] HttpRequest request)
    {
        if (!TryGetCosmos(out var connectionString, out var databaseId))
            return new OkObjectResult(new { replicated = false, reason = "Cosmos replication is unconfigured." });

        logger.LogInformation("Menu full replication started at {start}.", DateTime.UtcNow);
        await cosmos.ReplicateAllAsync(database, connectionString, databaseId, updateAll: true);
        logger.LogInformation("Menu full replication finished at {end}.", DateTime.UtcNow);

        var status = await MenuReplicationStatus.ReadAsync(database, request.HttpContext.RequestAborted);

        if (!status.IsUpToDate)
            logger.LogWarning(
                "Menu full replication finished with {pending} of {total} rows still outstanding "
                + "({neverReplicated} never written). Run it again — the sweep picks up exactly those rows.",
                status.Pending,
                status.Total,
                status.NeverReplicated);

        return new OkObjectResult(new { replicated = true, status });
    }

    // ─────────────────────── on-demand: how far replication has got ───────────────────────

    /// <summary>
    /// Per-table counts of what is replicated and what is not — the check that a backfill actually
    /// finished, and the cheap way to watch the hourly timers keep up.
    ///
    /// Reads SQL only: it reports what the pipeline believes, so it cannot see a document staled by an
    /// edit that bypassed its owning row. That case is what <c>replicate-all</c> is for.
    /// </summary>
    [Function(nameof(ReplicationStatusHttp))]
    public async Task<IActionResult> ReplicationStatusHttp(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "replication-status")] HttpRequest request)
        => new OkObjectResult(await MenuReplicationStatus.ReadAsync(database, request.HttpContext.RequestAborted));

    // ─────────────────────── helpers ───────────────────────

    /// <summary>
    /// Runs one table's catch-up. Backstop to the host-level disable in <c>local.settings.json</c>: also
    /// skips in a Development host, then no-ops when Cosmos is unconfigured. <paramref name="caller"/>
    /// is the compiler-supplied function name.
    /// </summary>
    private async Task RunTimerAsync(
        Func<CosmosDBReplication, string, string, Task> replicate,
        [CallerMemberName] string caller = "")
    {
        if (environment.IsDevelopment())
        {
            logger.LogInformation("{caller} skipped: replication timers are disabled in Development.", caller);
            return;
        }

        if (!TryGetCosmos(out var connectionString, out var databaseId))
        {
            logger.LogInformation("{caller} skipped: Cosmos replication is unconfigured.", caller);
            return;
        }

        logger.LogInformation("{caller} started at {start}.", caller, DateTime.UtcNow);
        await replicate(cosmos, connectionString, databaseId);
        logger.LogInformation("{caller} finished at {end}.", caller, DateTime.UtcNow);
    }

    /// <summary>
    /// Reads the same <c>ConnectionStrings:Cosmos</c> the sample API uses, so both halves of the sample
    /// are configured in one place. A missing connection string means "replication off", exactly as it
    /// does there.
    /// </summary>
    private bool TryGetCosmos(out string connectionString, out string databaseId)
    {
        databaseId = MenuCosmosContainers.DatabaseName;
        connectionString = configuration.GetConnectionString("Cosmos") ?? string.Empty;

        return !string.IsNullOrWhiteSpace(connectionString);
    }
}
