using NSubstitute;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using TestContext = LookupServices.BDD.Support.TestContext;

namespace LookupServices.BDD.StepDefinitions;

/// <summary>
/// Drives <see cref="VehicleLookupService.LookupAsync"/> — the entry point that logs — with a recording
/// log service, and asserts what was written. The rule under test lives next to the log calls in
/// VehicleLookupService: a traced request is a re-read, never a lookup, and is never logged.
/// </summary>
[Binding]
public class VehicleLookupLoggingStepDefinitions
{
    private readonly TestContext _context;
    private readonly ILogCosmosService _log = Substitute.For<ILogCosmosService>();

    public VehicleLookupLoggingStepDefinitions(TestContext context)
    {
        _context = context;
    }

    private async Task LookupAsync(string vin, VehicleLookupRequestOptions options)
    {
        _context.Aggregate.VIN = vin;
        _context.StorageService.GetAggregatedCompanyData(vin).Returns(_context.Aggregate);
        _context.StorageService.GetServiceItemsAsync(Arg.Any<bool>()).Returns([]);

        var service = new VehicleLookupService(_context.StorageService, _context.ServiceProvider, _log, _context.Options);

        await service.LookupAsync(vin, options);
    }

    [When("{string} is looked up with the SSC log flag")]
    public Task WhenLookedUpWithTheSscLogFlag(string vin) =>
        LookupAsync(vin, new VehicleLookupRequestOptions { InsertSSCLog = true, SSCLogInfo = new SSCLogInfo() });

    [When("{string} is looked up with the SSC log flag and an SSC trace")]
    public Task WhenLookedUpWithTheSscLogFlagAndAnSscTrace(string vin) =>
        LookupAsync(vin, new VehicleLookupRequestOptions { InsertSSCLog = true, SSCLogInfo = new SSCLogInfo(), TraceSSCEvaluation = true });

    [When("{string} is looked up with the SSC log flag and a service-item trace")]
    public Task WhenLookedUpWithTheSscLogFlagAndAServiceItemTrace(string vin) =>
        LookupAsync(vin, new VehicleLookupRequestOptions { InsertSSCLog = true, SSCLogInfo = new SSCLogInfo(), TraceServiceItemEvaluation = true });

    [When("{string} is looked up with the customer lookup log flag")]
    public Task WhenLookedUpWithTheCustomerLookupLogFlag(string vin) =>
        LookupAsync(vin, new VehicleLookupRequestOptions { InsertCustomerVehcileLookupLog = true, CustomerVehicleLookupLogInfo = new CustomerVehicleLookupLogInfo() });

    [When("{string} is looked up with the customer lookup log flag and an SSC trace")]
    public Task WhenLookedUpWithTheCustomerLookupLogFlagAndAnSscTrace(string vin) =>
        LookupAsync(vin, new VehicleLookupRequestOptions { InsertCustomerVehcileLookupLog = true, CustomerVehicleLookupLogInfo = new CustomerVehicleLookupLogInfo(), TraceSSCEvaluation = true });

    [Then("one SSC lookup log entry is written")]
    public async Task ThenOneSscLookupLogEntryIsWritten() =>
        await _log.Received(1).LogSSCLookupAsync(Arg.Any<SSCLogInfo>(), Arg.Any<IEnumerable<SscDTO>>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<long?>());

    [Then("no SSC lookup log entry is written")]
    public async Task ThenNoSscLookupLogEntryIsWritten() =>
        await _log.DidNotReceive().LogSSCLookupAsync(Arg.Any<SSCLogInfo>(), Arg.Any<IEnumerable<SscDTO>>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<long?>());

    [Then("one customer vehicle lookup log entry is written")]
    public async Task ThenOneCustomerVehicleLookupLogEntryIsWritten() =>
        await _log.Received(1).LogCustomerVehicleLookupAsync(Arg.Any<CustomerVehicleLookupLogInfo>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<long?>());

    [Then("no customer vehicle lookup log entry is written")]
    public async Task ThenNoCustomerVehicleLookupLogEntryIsWritten() =>
        await _log.DidNotReceive().LogCustomerVehicleLookupAsync(Arg.Any<CustomerVehicleLookupLogInfo>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<long?>());
}
