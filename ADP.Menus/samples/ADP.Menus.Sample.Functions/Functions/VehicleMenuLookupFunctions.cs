using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace ShiftSoftware.ADP.Menus.Sample.Functions.Functions;

/// <summary>
/// The same menu, reached from a VIN instead of a model code — the join the vehicle lookup performs.
///
/// <para><c>GET api/menu/{basicModelCode}</c> already proves the read path. What this adds is the one step
/// that can go wrong on real data and cannot be tested from here: the vehicle lookup does not receive a
/// basic model code, it <b>derives</b> one from the vehicle's Katashiki, and matches it against a code an
/// author typed into the menus database. Nothing guarantees the two agree
/// (COSMOS_REPLICATION_PLAN.md open item O3), so the response echoes the Katashiki, the code derived from
/// it, and the resulting <c>status</c> — which is how a deployment measures its own hit rate.</para>
///
/// <para><b>This endpoint needs the VEHICLE containers, which this sample does not provision.</b> Menu
/// replication fills the <c>Services</c> database; a vehicle lookup reads <c>CompanyData</c>/<c>Vehicles</c>,
/// which comes from an entirely different pipeline. Against a menus-only emulator this answers 503 saying
/// so. An unknown VIN, on a host that HAS those containers, is an ordinary answer:
/// <c>status: "NoBasicModelCode"</c>, because there is no Katashiki to derive a key from.</para>
/// </summary>
public class VehicleMenuLookupFunctions
{
    private readonly VehicleLookupService vehicleLookup;
    private readonly ILogger<VehicleMenuLookupFunctions> logger;

    public VehicleMenuLookupFunctions(VehicleLookupService vehicleLookup, ILogger<VehicleMenuLookupFunctions> logger)
    {
        this.vehicleLookup = vehicleLookup;
        this.logger = logger;
    }

    /// <summary>
    /// A vehicle lookup with the model's service menu attached.
    ///
    /// <code>
    /// GET api/vehicle/JTMHX01J8L4198293
    /// GET api/vehicle/JTMHX01J8L4198293?language=ar&amp;countryId=2
    /// </code>
    ///
    /// <para>The response is trimmed to the join and the menu — a real host returns the whole
    /// <see cref="VehicleLookupDTO"/>, of which the menu is one property. Everything else about the vehicle
    /// is beside the point here and would bury what this endpoint exists to show.</para>
    /// </summary>
    [Function(nameof(GetVehicleWithServiceMenu))]
    public async Task<IActionResult> GetVehicleWithServiceMenu(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "vehicle/{vin}")] HttpRequest request,
        string vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return new BadRequestObjectResult(new { reason = "A VIN is required." });

        var requestOptions = new VehicleLookupRequestOptions
        {
            // The menu generates in the request's language — there is no separate menu language, because a
            // lookup rendering in one language with menu codes in another would be a bug.
            LanguageCode = request.Query["language"].FirstOrDefault() ?? "en",

            ServiceMenuOptions = new VehicleServiceMenuRequestOptions
            {
                // Without this the response has no ServiceMenu at all: the section is opt-in because it
                // costs an extra single-partition read plus a fold PER VEHICLE.
                Include = true,

                CountryID = long.TryParse(request.Query["countryId"].FirstOrDefault(), out var countryId)
                    ? countryId
                    : null,

                // All | FreeOnly | PaidOnly. Unlike the transfer rate below, this one is safe to bind from
                // an untrusted caller: it narrows which variants come back and moves no money. Anything
                // unparseable falls back to All rather than 400-ing.
                FreeFilter = Enum.TryParse<ServiceMenuFreeFilter>(request.Query["freeFilter"].FirstOrDefault(), ignoreCase: true, out var freeFilter)
                    ? freeFilter
                    : ServiceMenuFreeFilter.All,

                // Bound from the query string HERE ONLY BECAUSE THIS IS A FUNCTION-KEY-PROTECTED SAMPLE.
                // The transfer rate scales the consumable and therefore the price quoted to a customer, and
                // a caller-supplied value overrides the host's CountrySettingsResolver. A public endpoint
                // must fix it server-side or leave it null. Invariant parsing so "1.15" means the same
                // thing whatever culture the host runs under.
                TransferRate = decimal.TryParse(
                    request.Query["transferRate"].FirstOrDefault(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var transferRate)
                    ? transferRate
                    : null,
            },
        };

        VehicleLookupDTO vehicle;

        try
        {
            vehicle = await vehicleLookup.LookupAsync(vin, requestOptions);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            // The VEHICLE containers, not the menu ones — a menu fault never reaches here, the evaluator
            // contains it and reports VehicleServiceMenuStatus.Unavailable instead.
            logger.LogError(exception, "The vehicle lookup containers are not provisioned.");

            return new ObjectResult(new
            {
                reason =
                    "The CompanyData/Vehicles containers this lookup reads are not provisioned. This sample only "
                    + "provisions the menu containers — point ConnectionStrings:Cosmos at an account that has "
                    + "vehicle data, or use GET api/menu/{basicModelCode} instead, which needs only the menus.",
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }

        var menu = vehicle.ServiceMenu;

        // The line worth reading in the console: Katashiki in, derived code out, and whether it matched.
        // Counting Found against NotFound over a real VIN population is the O3 measurement.
        logger.LogInformation(
            "Vehicle {vin}: katashiki {katashiki} → basic model code {basicModelCode} → {status} ({services} service(s)).",
            vehicle.VIN,
            vehicle.Identifiers?.Katashiki ?? "(none)",
            vehicle.BasicModelCode ?? "(none)",
            menu?.Status,
            menu?.Services?.Count ?? 0);

        return new OkObjectResult(new
        {
            vin = vehicle.VIN,
            katashiki = vehicle.Identifiers?.Katashiki,

            // The derived join key, echoed even when it matches nothing — "which code did it look for" is
            // the first question anyone asks about a miss.
            basicModelCode = vehicle.BasicModelCode,

            serviceMenu = menu,
        });
    }
}
