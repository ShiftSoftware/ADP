using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleSSCStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<SscDTO>? _result;
    private bool _evaluated;

    public VehicleSSCStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("the SSC {string} labor code carries a trailing space")]
    public void GivenTheSscLaborCodeCarriesATrailingSpace(string campaignCode)
    {
        // Gherkin trims table cells, so pad here to simulate untrimmed source data (e.g. "AURCM ").
        var ssc = _context.Aggregate.SSCAffectedVINs.First(x => x.CampaignCode == campaignCode);
        ssc.Labors[0].LaborCode += " ";
    }

    private void EnsureEvaluated()
    {
        if (!_evaluated)
        {
            _result = new VehicleSSCEvaluator(_context.Aggregate).Evaluate();
            _evaluated = true;
        }
    }

    private SscDTO GetSsc(string sscCode)
    {
        EnsureEvaluated();
        var ssc = _result?.FirstOrDefault(x => x.SSCCode == sscCode);
        Assert.NotNull(ssc);
        return ssc;
    }

    [Then("SSC {string} is marked as repaired")]
    public void ThenSSCIsMarkedAsRepaired(string sscCode)
    {
        var ssc = GetSsc(sscCode);
        Assert.True(ssc.Repaired, $"Expected SSC '{sscCode}' to be repaired, but it was not.");
    }

    [Then("SSC {string} is marked as not repaired")]
    public void ThenSSCIsMarkedAsNotRepaired(string sscCode)
    {
        var ssc = GetSsc(sscCode);
        Assert.False(ssc.Repaired, $"Expected SSC '{sscCode}' to be not repaired, but it was.");
    }

    [Then("SSC {string} has repair date {string}")]
    public void ThenSSCHasRepairDate(string sscCode, string expectedDate)
    {
        var ssc = GetSsc(sscCode);
        Assert.Equal(DateTime.Parse(expectedDate), ssc.RepairDate);
    }

    [Then("SSC {string} has {int} labor codes")]
    public void ThenSSCHasLaborCodes(string sscCode, int expectedCount)
    {
        var ssc = GetSsc(sscCode);
        Assert.Equal(expectedCount, ssc.Labors.Count());
    }

    [Then("SSC {string} has {int} part numbers")]
    public void ThenSSCHasPartNumbers(string sscCode, int expectedCount)
    {
        var ssc = GetSsc(sscCode);
        Assert.Equal(expectedCount, ssc.Parts.Count());
    }

    [Then("there are no SSC records")]
    public void ThenThereAreNoSSCRecords()
    {
        EnsureEvaluated();
        Assert.Null(_result);
    }

    [Given("stock part numbers are stored T-prefixed and dash-stripped")]
    public void GivenStockPartNumbersAreStoredTPrefixedAndDashStripped()
    {
        // Mirrors a deployment that stores parts T-prefixed and dash-stripped (04007-07212 -> T0400707212).
        _context.Options.PartNumberStorageKeyResolver =
            pn => "T" + (pn ?? string.Empty).Replace("-", string.Empty).Trim().ToUpperInvariant();
    }

    [When("SSC part availability is applied for stock scope {string}")]
    public void WhenSSCPartAvailabilityIsAppliedForStockScope(string scope)
    {
        EnsureEvaluated();

        var scopeKeys = (scope ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        SSCPartAvailabilityEnricher.ApplyAvailability(
            _result, _context.PartAggregate.StockParts, scopeKeys, _context.Options.PartNumberStorageKeyResolver);
    }

    private SSCPartDTO GetPart(string sscCode, string partNumber)
    {
        var ssc = GetSsc(sscCode);
        var part = ssc.Parts.FirstOrDefault(p => p.PartNumber == partNumber);
        Assert.NotNull(part);
        return part!;
    }

    [Then("SSC {string} part {string} is available")]
    public void ThenSSCPartIsAvailable(string sscCode, string partNumber)
    {
        var part = GetPart(sscCode, partNumber);
        Assert.True(part.IsAvailable, $"Expected SSC '{sscCode}' part '{partNumber}' to be available, but it was not.");
    }

    [Then("SSC {string} part {string} is not available")]
    public void ThenSSCPartIsNotAvailable(string sscCode, string partNumber)
    {
        var part = GetPart(sscCode, partNumber);
        Assert.False(part.IsAvailable, $"Expected SSC '{sscCode}' part '{partNumber}' to be not available, but it was.");
    }

    [Then("SSC {string} part {string} availability is not checked")]
    public void ThenSSCPartAvailabilityIsNotChecked(string sscCode, string partNumber)
    {
        var part = GetPart(sscCode, partNumber);
        Assert.True(part.IsAvailable is null, $"Expected SSC '{sscCode}' part '{partNumber}' availability to be not checked (null), but it was {part.IsAvailable}.");
    }

    [Given("SSC part availability is globally disabled")]
    public void GivenSSCPartAvailabilityIsGloballyDisabled()
    {
        _context.Options.EnableSSCPartAvailability = false;
    }

    [Given("the SSC stock scope resolver must not be called")]
    public void GivenTheSSCStockScopeResolverMustNotBeCalled()
    {
        // If the master switch fails to short-circuit, EnrichAsync will invoke this and the test throws.
        _context.Options.SSCPartStockScopeResolver = _ =>
            throw new Xunit.Sdk.XunitException("SSCPartStockScopeResolver must not be called when the feature is disabled.");
    }

    [When("SSC part availability enrichment runs")]
    public async Task WhenSSCPartAvailabilityEnrichmentRuns()
    {
        EnsureEvaluated();

        // Exercises the real EnrichAsync gate (not the pure ApplyAvailability). With the feature off it must
        // return before touching the resolver or any stock service, so an empty provider is enough.
        await new SSCPartAvailabilityEnricher(_context.Options)
            .EnrichAsync(_result, "VIN", new VehicleLookupRequestOptions(), NullServiceProvider.Instance);
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public static readonly NullServiceProvider Instance = new();
        public object? GetService(Type serviceType) => null;
    }
}
