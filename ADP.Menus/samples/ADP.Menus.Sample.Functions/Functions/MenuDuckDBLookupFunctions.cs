using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace ShiftSoftware.ADP.Menus.Sample.Functions.Functions;

/// <summary>
/// The menu lookup served from the DUCKDB storage — the same read <c>GET api/menu/{code}</c> answers
/// from Cosmos, answered instead from the local file <c>POST api/duckdb-sync</c> writes. Sync, then
/// call both endpoints for one code: the menus must be identical, because everything above the
/// storage seam IS identical — the same <c>ServiceMenuLookupService</c>, the same evaluators, the
/// same generator; only <c>IServiceMenuLookupStorageService</c> differs.
///
/// <para><b>Why this host builds the DuckDB lookup per request instead of registering it:</b> storage
/// choice is normally a DEPLOYMENT decision — a real DuckDB-backed host calls
/// <c>AddDuckDBServiceMenuLookup("DataSource=…;ACCESS_MODE=READ_ONLY")</c> once and its ordinary menu
/// endpoint serves from DuckDB, with no Cosmos storage registered at all. This sample deliberately
/// keeps BOTH backends alive to let you compare them, so the DuckDB side is composed here, per
/// request, from the same parts the registration would wire: the reader over its own read-only
/// connection, and the host's registered <see cref="ServiceMenuGenerationEvaluator"/> so both
/// endpoints resolve country and transfer-rate configuration identically.</para>
///
/// <para>Opening read-only per request also respects the single-writer rule: the lookup never blocks
/// (or corrupts) a concurrently running <c>duckdb-sync</c> longer than one request, and a store that
/// was never synced fails loudly here rather than answering "no menu".</para>
/// </summary>
public class MenuDuckDBLookupFunctions
{
    private readonly ServiceMenuGenerationEvaluator generationEvaluator;
    private readonly IConfiguration configuration;
    private readonly ILogger<MenuDuckDBLookupFunctions> logger;

    public MenuDuckDBLookupFunctions(
        ServiceMenuGenerationEvaluator generationEvaluator,
        IConfiguration configuration,
        ILogger<MenuDuckDBLookupFunctions> logger)
    {
        this.generationEvaluator = generationEvaluator;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// The service menu for one basic model code, out of the DuckDB store.
    ///
    /// <code>
    /// GET api/menu-duckdb/ABC12
    /// GET api/menu-duckdb/ABC12?language=ar&amp;countryId=2
    /// </code>
    ///
    /// Same query parameters, same response shape and same answers as <c>GET api/menu/{code}</c> —
    /// including <c>notFound: true</c> for a model the store holds no menu for. The catch-all route
    /// exists for the same reason as there: authored codes can contain a slash.
    /// </summary>
    [Function(nameof(GetMenuByBasicModelCodeFromDuckDB))]
    public async Task<IActionResult> GetMenuByBasicModelCodeFromDuckDB(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "menu-duckdb/{*basicModelCode}")] HttpRequest request,
        string basicModelCode)
    {
        if (string.IsNullOrWhiteSpace(basicModelCode))
            return new BadRequestObjectResult(new { reason = "A basic model code is required." });

        var lookupRequest = new ServiceMenuLookupRequest
        {
            BasicModelCode = basicModelCode,
            Language = request.Query["language"].FirstOrDefault(),
            CountryID = long.TryParse(request.Query["countryId"].FirstOrDefault(), out var countryId) ? countryId : null,
            FreeFilter = Enum.TryParse<ServiceMenuFreeFilter>(request.Query["freeFilter"].FirstOrDefault(), ignoreCase: true, out var freeFilter)
                ? freeFilter
                : ServiceMenuFreeFilter.All,
        };

        try
        {
            // The reader opens (and owns) its own read-only connection for exactly this request —
            // see the class remarks for why this sample composes rather than registers it.
            using var storage = new DuckDBServiceMenuLookupStorageService(GetReadOnlyDuckDBConnectionString());
            var lookup = new ServiceMenuLookupService(storage, generationEvaluator);

            var menu = await lookup.GetMenuAsync(lookupRequest, request.HttpContext.RequestAborted);

            logger.LogInformation(
                "DuckDB menu lookup for {basicModelCode}: {variants} variant(s), {periodic} scheduled and {standalone} standalone service(s).",
                menu.BasicModelCode,
                menu.Variants.Count,
                menu.Variants.Sum(variant => variant.PeriodicServices.Count),
                menu.Variants.Sum(variant => variant.StandaloneServices.Count));

            return new OkObjectResult(menu);
        }
        catch (ServiceMenuContainerNotFoundException exception)
        {
            // The file exists but holds no menu tables — the store was never synced.
            logger.LogError(exception, "The DuckDB menu store is not populated.");

            return new ObjectResult(new { reason = exception.Message })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }
        catch (ServiceMenuGenerationException exception)
        {
            // Same contract as the Cosmos endpoint: rows referencing master data the store does not
            // hold fail loudly rather than producing a partial menu.
            logger.LogError(exception, "Menu generation failed for {basicModelCode} over DuckDB.", basicModelCode);

            return new ObjectResult(new { reason = exception.Message, basicModelCode = exception.BasicModelCode })
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
        }
        catch (Exception exception) when (exception is ServiceMenuStorageException or global::DuckDB.NET.Data.DuckDBException)
        {
            // The store could not be opened or read — most commonly the file does not exist yet
            // (read-only mode never creates one), i.e. the sync has never run.
            logger.LogError(exception, "The DuckDB menu store could not be read.");

            return new ObjectResult(new
            {
                reason = "The DuckDB menu store could not be opened or read. Run POST api/duckdb-sync first, "
                    + "then retry. Details: " + exception.Message,
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }
    }

    /// <summary>
    /// The same database <c>duckdb-sync</c> writes (<c>ConnectionStrings:DuckDB</c>, defaulting to
    /// <c>menus.duckdb</c> beside the host), forced read-only: a reader must never hold a write claim
    /// on the sync's file — and read-only also refuses to CREATE a missing file, so an unsynced store
    /// fails loudly here instead of materializing as an empty database that answers "no menu".
    /// </summary>
    private string GetReadOnlyDuckDBConnectionString()
    {
        var connectionString = configuration.GetConnectionString("DuckDB");

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = "DataSource=menus.duckdb";

        return connectionString.Contains("ACCESS_MODE", StringComparison.OrdinalIgnoreCase)
            ? connectionString
            : connectionString.TrimEnd(';') + ";ACCESS_MODE=READ_ONLY";
    }
}
