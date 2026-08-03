using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace ShiftSoftware.ADP.Menus.Sample.Functions.Functions;

/// <summary>
/// The READ side of the sample: a basic model code in, menu codes and prices out.
///
/// <para>It closes the loop the rest of this host opens. The timers and <c>replicate-all</c> project the
/// menu catalog into Cosmos; this reads one model's partition back and generates its menu from it — with
/// the SAME generator the DMS export uses, so a code served here is the code the dealer's DMS received.
/// Replicate, then look up, and compare against the export: that is the sample's whole point.</para>
///
/// <para>Everything is one call to <c>ServiceMenuLookupService</c> — one single-partition query and a
/// pure fold. There is no reference cache and no second round trip, because the documents carry
/// everything generation needs (COSMOS_REPLICATION_PLAN.md §16).</para>
/// </summary>
public class MenuLookupFunctions
{
    private readonly ServiceMenuLookupService lookup;
    private readonly ILogger<MenuLookupFunctions> logger;

    public MenuLookupFunctions(ServiceMenuLookupService lookup, ILogger<MenuLookupFunctions> logger)
    {
        this.lookup = lookup;
        this.logger = logger;
    }

    /// <summary>
    /// The service menu for one basic model code.
    ///
    /// <code>
    /// GET api/menu/ABC12
    /// GET api/menu/ABC12?language=ar&amp;countryId=2
    /// </code>
    ///
    /// <para>A model with no replicated menu answers <c>notFound: true</c> with no variants rather than
    /// a 404 — "this model has no menu" is an ordinary answer. It is also the first thing to check when
    /// the catalogue clearly HAS one: it means nothing has been replicated for that code yet, so run
    /// <c>POST api/replicate-all</c>.</para>
    ///
    /// <para><b>The route parameter is a catch-all, and has to be.</b> A basic model code is authored
    /// free-text and real catalogues contain codes with a SLASH in them (a code covering two variants,
    /// e.g. <c>ABC210/211</c>). Under an ordinary <c>{basicModelCode}</c> segment those codes are
    /// unreachable — the slash splits the segment and the request 404s — so the models that need the
    /// lookup most are exactly the ones it cannot answer for, silently. A catch-all takes the rest of
    /// the path verbatim, which is what the partition key holds.</para>
    /// </summary>
    [Function(nameof(GetMenuByBasicModelCode))]
    public async Task<IActionResult> GetMenuByBasicModelCode(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "menu/{*basicModelCode}")] HttpRequest request,
        string basicModelCode)
    {
        if (string.IsNullOrWhiteSpace(basicModelCode))
            return new BadRequestObjectResult(new { reason = "A basic model code is required." });

        var lookupRequest = new ServiceMenuLookupRequest
        {
            BasicModelCode = basicModelCode,
            Language = request.Query["language"].FirstOrDefault(),
            CountryID = long.TryParse(request.Query["countryId"].FirstOrDefault(), out var countryId) ? countryId : null,

            // All | FreeOnly | PaidOnly. Anything unparseable falls back to All rather than 400-ing:
            // this narrows a result set, so the safe failure is returning too much, not too little.
            FreeFilter = Enum.TryParse<ServiceMenuFreeFilter>(request.Query["freeFilter"].FirstOrDefault(), ignoreCase: true, out var freeFilter)
                ? freeFilter
                : ServiceMenuFreeFilter.All,
        };

        try
        {
            var menu = await lookup.GetMenuAsync(lookupRequest, request.HttpContext.RequestAborted);

            logger.LogInformation(
                "Menu lookup for {basicModelCode}: {variants} variant(s), {periodic} scheduled and {standalone} standalone service(s).",
                menu.BasicModelCode,
                menu.Variants.Count,
                menu.Variants.Sum(variant => variant.PeriodicServices.Count),
                menu.Variants.Sum(variant => variant.StandaloneServices.Count));

            return new OkObjectResult(menu);
        }
        catch (ServiceMenuContainerNotFoundException exception)
        {
            // A provisioning fault, not a data one — deliberately not folded into an empty menu, or an
            // unprovisioned host would report "no menu" for every model forever.
            logger.LogError(exception, "The ServiceMenus container is not provisioned.");

            return new ObjectResult(new { reason = exception.Message })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }
        catch (ServiceMenuGenerationException exception)
        {
            // The documents reference master data they do not carry — usually incomplete replication.
            // Preserved as a loud failure rather than a partial menu: the DMS export fails on it too.
            logger.LogError(exception, "Menu generation failed for {basicModelCode}.", basicModelCode);

            return new ObjectResult(new { reason = exception.Message, basicModelCode = exception.BasicModelCode })
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
        }
    }
}
