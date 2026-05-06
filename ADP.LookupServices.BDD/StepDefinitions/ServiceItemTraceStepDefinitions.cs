using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class ServiceItemTraceStepDefinitions
{
    private readonly Support.TestContext _context;
    private DateTime? _freeServiceStartDate;
    private ServiceItemTrace? _trace;

    public ServiceItemTraceStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("the trace free service start date is {string}")]
    public void GivenFreeServiceStartDate(string date) => _freeServiceStartDate = DateTime.Parse(date);

    [When("evaluating service items with trace for {string} with language {string}")]
    public async Task WhenEvaluatingWithTrace(string vin, string language)
    {
        _context.Aggregate.VIN = vin;
        var vehicle = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = vehicle;

        var saleInfo = _context.SaleInformation ?? new VehicleSaleInformation
        {
            InvoiceDate = vehicle?.InvoiceDate,
            WarrantyActivationDate = vehicle?.WarrantyActivationDate,
        };

        var collector = new ServiceItemTraceCollector(vin);
        var evaluator = new VehicleServiceItemEvaluator(
            _context.StorageService, _context.Aggregate, _context.Options, _context.ServiceProvider)
        {
            Trace = collector,
        };

        await evaluator.Evaluate(vehicle!, _freeServiceStartDate, saleInfo, language);
        _trace = collector.Build();
    }

    [Then("the trace records {int} eligibility decisions")]
    public void ThenTraceRecordsDecisions(int count)
    {
        Assert.NotNull(_trace);
        Assert.Equal(count, _trace!.Eligibility.Decisions.Count);
    }

    [Then("the trace records {string} as accepted")]
    public void ThenTraceAccepted(string serviceItemId)
    {
        Assert.NotNull(_trace);
        var d = _trace!.Eligibility.Decisions.FirstOrDefault(x => x.ServiceItemID == serviceItemId);
        Assert.NotNull(d);
        Assert.Equal(EligibilityVerdict.Accepted, d!.Verdict);
    }

    [Then("the trace records {string} as rejected at {string}")]
    public void ThenTraceRejectedAt(string serviceItemId, string stage)
    {
        Assert.NotNull(_trace);
        var d = _trace!.Eligibility.Decisions.FirstOrDefault(x => x.ServiceItemID == serviceItemId);
        Assert.NotNull(d);
        Assert.Equal(EligibilityVerdict.Rejected, d!.Verdict);
        Assert.Equal(stage, d.RejectionStage.ToString());
        Assert.False(string.IsNullOrWhiteSpace(d.Reason), "Expected a non-empty reason");
    }

    [Then("the trace final result has {int} items")]
    public void ThenTraceFinalCount(int count)
    {
        Assert.NotNull(_trace);
        Assert.Equal(count, _trace!.FinalResult.Count);
    }

    [Then("the trace has at least {int} stage timing")]
    [Then("the trace has at least {int} stage timings")]
    public void ThenTraceHasStageTimings(int min)
    {
        Assert.NotNull(_trace);
        Assert.True(_trace!.StageTimings.Count >= min,
            $"Expected at least {min} stage timings, got {_trace.StageTimings.Count}");
    }
}
