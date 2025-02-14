using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace Lookup.Services.Functions;

public class FlatRateLookup
{
    private readonly ILogger<FlatRateLookup> _logger;
    private readonly ServiceLookupService serviceLookupService;

    public FlatRateLookup(ILogger<FlatRateLookup> logger, ServiceLookupService serviceLookupService)
    {
        _logger = logger;
        this.serviceLookupService = serviceLookupService;
    }

    [Function("FlatRateLookup")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "FlatRateLookup/{vds}/{wmi?}")] HttpRequest req,
        string vds, string? wmi)
    {
        var data = await this.serviceLookupService.FlatRateLookupAsync(vds, wmi);

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(data);
    }
}
