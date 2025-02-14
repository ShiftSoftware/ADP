using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace Lookup.Services.Functions;

public class BrokerStock
{
    private readonly ILogger<BrokerStock> _logger;
    private readonly TBPCosmosService tbpCosmosService;

    public BrokerStock(
        ILogger<BrokerStock> logger,
        TBPCosmosService tbpCosmosService)
    {
        _logger = logger;
        this.tbpCosmosService = tbpCosmosService;
    }

    [Function("GetBrokerStock")]
    public async Task<IActionResult> GetBrokerStock([HttpTrigger(AuthorizationLevel.Function, "get", Route = "secure-broker-stock/{brokerId:long}")] HttpRequest req, long brokerId)
    {
        var data = await this.tbpCosmosService.GetBrokerStockAsync(brokerId);

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(data);
    }

    [Function("GetBrokersStock")]
    public async Task<IActionResult> GetBrokersStock([HttpTrigger(AuthorizationLevel.Function, "get", Route = "secure-brokers-stock")] HttpRequest req)
    {
        var brokerIds = req.Query["brokerId"].Select(x => long.Parse(x)).AsEnumerable();
        var data = await this.tbpCosmosService.GetBrokerStockAsync(brokerIds);

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(data);
    }

    [Function("GetBrokerStockByVIN")]
    public async Task<IActionResult> GetBrokerStockByVIN([HttpTrigger(AuthorizationLevel.Function, "get",
        Route = "secure-broker-stock-by-vin/{brokerId:long}/{vin}")] HttpRequest req, long brokerId, string vin)
    {
        var data = await this.tbpCosmosService.GetBrokerStockAsync(brokerId, vin);

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(data);
    }
}
