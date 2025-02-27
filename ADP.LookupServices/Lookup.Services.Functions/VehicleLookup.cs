using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace Lookup.Services.Functions;

public class VehicleLookup
{
    private readonly ILogger<VehicleLookup> _logger;
    private readonly CosmosClient client;
    private readonly IConfiguration config;
    private readonly VehicleLookupService lookupService;

    public VehicleLookup(
        ILogger<VehicleLookup> logger,
        CosmosClient client,
        IConfiguration config,
        VehicleLookupService lookupService
        )
    {
        _logger = logger;
        this.client = client;
        this.config = config;
        this.lookupService = lookupService;
    }

    [Function("Secure" + nameof(VehicleLookup))]
    public async Task<IActionResult> RunSecure([HttpTrigger(AuthorizationLevel.Function, "get", Route = "secure-vehicle-lookup/{vin}")] HttpRequest req, string vin)
    {
        return await ProcessRequest(req, vin);
    }

    [Function("Secure" + nameof(AggregatedCompanyData))]
    public async Task<IActionResult> AggregatedCompanyData(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "company-data/{vin}")] HttpRequest req, string vin)
    {
        var data = await lookupService.lookupCosmosService.GetAggregatedCompanyData(vin);

        return new OkObjectResult(data);
    }

    [Function("Secure_Multiple" + nameof(AggregatedCompanyData))]
    public async Task<IActionResult> MultipleAggregatedCompanyData(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "company-data-multiple/{vins}")] HttpRequest req, string vins)
    {
        var data = await lookupService.lookupCosmosService.GetAggregatedCompanyData(vins.Split(',').ToList(), new List<string> { "VS", "Labor", "Part" });

        return new OkObjectResult(new
        {
            data.LaborLines,
            data.PartLines,
            data.VehicleEntries
        });
    }

    private async Task<IActionResult> ProcessRequest(HttpRequest req, string vin)
    {
        string regionIntegrationId = req.Query?.FirstOrDefault(x => x.Key.ToLower() == "RegionIntegrationID".ToLower())
            .Value.FirstOrDefault() ?? "1";

        bool ignoreBrokerStock = false;
        bool.TryParse(req.Query?.FirstOrDefault(x => x.Key.ToLower() == "IgnoreBrokerStock".ToLower())
            .Value.FirstOrDefault(), out ignoreBrokerStock);

        var acceptLanguage = req.Headers["Accept-Language"].FirstOrDefault();

        var result = await lookupService.LookupAsync(vin, regionIntegrationId, acceptLanguage, ignoreBrokerStock,true);

        return new OkObjectResult(result);
    }

    [Function(nameof(GetVTModelsByKatashiki))]
    public async Task<IActionResult> GetVTModelsByKatashiki([HttpTrigger(AuthorizationLevel.Function, "get", Route = "vtmodels-by-katashiki/{katashiki}")] HttpRequest req, string katashiki)
    {
        return new OkObjectResult(await lookupService.GetVTModelsByKatashikiAsync(katashiki));
    }

    [Function(nameof(GetVTModelsByVariant))]
    public async Task<IActionResult> GetVTModelsByVariant([HttpTrigger(AuthorizationLevel.Function, "get", Route = "vtmodels-by-variant/{variant}")] HttpRequest req, string variant)
    {
        return new OkObjectResult(await lookupService.GetVTModelsByVariantAsync(variant));
    }

    [Function(nameof(GetVTModelsByVin))]
    public async Task<IActionResult> GetVTModelsByVin([HttpTrigger(AuthorizationLevel.Function, "get", Route = "vtmodels-by-vin/{vin}")] HttpRequest req, string vin)
    {
        return new OkObjectResult(await lookupService.GetVTModelsByVinAsync(vin));
    }
}