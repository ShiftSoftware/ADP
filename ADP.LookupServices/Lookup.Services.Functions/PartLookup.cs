using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace Lookup.Services.Functions;

public class PartLookup
{
    private readonly ILogger<PartLookup> _logger;
    private readonly PartLookupService partLookupService;

    public PartLookup(
        ILogger<PartLookup> logger,
        PartLookupService partLookupService)
    {
        _logger = logger;
        this.partLookupService = partLookupService;
    }

    [Function("PartLookup")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "PartLookup/{partNumber}/{distributorStockLookupQuantity:int?}")] HttpRequest req, string partNumber, int? distributorStockLookupQuantity)
    {
        var data = await this.partLookupService.LookupAsync(partNumber, distributorStockLookupQuantity);

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(data);
    }
}
