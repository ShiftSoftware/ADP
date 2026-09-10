using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleSSCStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<SscDTO>? _result;
    private bool _evaluated;
    private bool _traceRequested;

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

    [Given("interchangeable SSC labor codes:")]
    public void GivenInterchangeableSscLaborCodes(DataTable dataTable)
    {
        // One row per group; the "Codes" cell lists the group's members comma-separated, the way a host would
        // declare them in LookupOptions.SSCInterchangeableLaborCodeGroups.
        foreach (var row in dataTable.Rows)
        {
            var codes = row["Codes"]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            _context.Options.SSCInterchangeableLaborCodeGroups.Add(codes);
        }
    }

    [Given("SSC evaluation tracing is requested")]
    public void GivenSscEvaluationTracingIsRequested()
    {
        _traceRequested = true;
    }

    private void EnsureEvaluated()
    {
        if (!_evaluated)
        {
            _result = new VehicleSSCEvaluator(_context.Aggregate, _context.Options).Evaluate(_traceRequested);
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

    private SscRepairTraceDTO GetTrace(string sscCode)
    {
        var trace = GetSsc(sscCode).Trace;
        Assert.True(trace is not null, $"Expected SSC '{sscCode}' to carry a trace, but it did not. Add 'Given SSC evaluation tracing is requested'.");
        return trace!;
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

    [Then("SSC {string} repair source is {string}")]
    public void ThenSSCRepairSourceIs(string sscCode, string expectedSource)
    {
        var ssc = GetSsc(sscCode);
        Assert.Equal(Enum.Parse<SscRepairSource>(expectedSource), ssc.RepairSource);
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

    // --- Trace -------------------------------------------------------------------------------------------------

    [Then("SSC {string} has no trace")]
    public void ThenSSCHasNoTrace(string sscCode)
    {
        var ssc = GetSsc(sscCode);
        Assert.True(ssc.Trace is null, $"Expected SSC '{sscCode}' to carry no trace unless tracing is requested, but it did.");
    }

    [Then("SSC {string} trace repair source is {string}")]
    public void ThenSSCTraceRepairSourceIs(string sscCode, string expectedSource)
    {
        var trace = GetTrace(sscCode);
        Assert.Equal(Enum.Parse<SscRepairSource>(expectedSource), trace.RepairSource);
        // The trace never disagrees with the DTO it explains.
        Assert.Equal(GetSsc(sscCode).RepairSource, trace.RepairSource);
    }

    [Then("SSC {string} trace lists interchangeable code {string} standing for {string}")]
    public void ThenSSCTraceListsInterchangeableCode(string sscCode, string laborCode, string campaignLaborCode)
    {
        var trace = GetTrace(sscCode);
        Assert.Contains(trace.InterchangeableLaborCodes, x => x.LaborCode == laborCode && x.CampaignLaborCode == campaignLaborCode);
    }

    private SscRepairTraceWarrantyClaimDTO GetTraceClaim(string sscCode, string repairCompletionDate)
    {
        var expected = DateTime.Parse(repairCompletionDate);
        var claim = GetTrace(sscCode).WarrantyClaims.FirstOrDefault(c => c.RepairCompletionDate == expected);
        Assert.True(claim is not null, $"Expected SSC '{sscCode}' trace to list a warranty claim completed on {repairCompletionDate}.");
        return claim!;
    }

    [Then("SSC {string} trace warranty claim completed on {string} is selected with labor code {string} standing for {string}")]
    public void ThenSSCTraceWarrantyClaimIsSelectedWithLaborCode(string sscCode, string repairCompletionDate, string laborCode, string campaignLaborCode)
    {
        var claim = GetTraceClaim(sscCode, repairCompletionDate);
        Assert.True(claim.StatusQualifies, "Expected the selected claim's status to qualify.");
        Assert.True(claim.Matches, "Expected the selected claim to match the campaign.");
        Assert.True(claim.Selected, "Expected the claim to be the one that decided the verdict.");
        Assert.Contains(claim.MatchedLaborCodes, x => x.LaborCode == laborCode && x.CampaignLaborCode == campaignLaborCode);
    }

    [Then("SSC {string} trace warranty claim completed on {string} matches but does not qualify by status")]
    public void ThenSSCTraceWarrantyClaimMatchesButDoesNotQualify(string sscCode, string repairCompletionDate)
    {
        var claim = GetTraceClaim(sscCode, repairCompletionDate);
        Assert.True(claim.Matches, "Expected the claim to match the campaign.");
        Assert.False(claim.StatusQualifies, "Expected the claim's status not to qualify.");
        Assert.False(claim.Selected, "A claim that does not qualify must not be selected.");
    }

    [Then("SSC {string} trace warranty claim completed on {string} qualifies by status but does not match")]
    public void ThenSSCTraceWarrantyClaimQualifiesButDoesNotMatch(string sscCode, string repairCompletionDate)
    {
        var claim = GetTraceClaim(sscCode, repairCompletionDate);
        Assert.True(claim.StatusQualifies, "Expected the claim's status to qualify.");
        Assert.False(claim.Matches, "Expected the claim not to reference the campaign.");
        Assert.False(claim.Selected, "A claim that does not match must not be selected.");
    }

    [Then("SSC {string} trace has {int} warranty claims and none selected")]
    public void ThenSSCTraceHasWarrantyClaimsAndNoneSelected(string sscCode, int expectedCount)
    {
        var trace = GetTrace(sscCode);
        Assert.Equal(expectedCount, trace.WarrantyClaims.Count);
        Assert.DoesNotContain(trace.WarrantyClaims, c => c.Selected);
    }

    [Then("SSC {string} trace examined {int} service history labor lines and lists {int} matching")]
    public void ThenSSCTraceExaminedServiceHistoryLaborLines(string sscCode, int examined, int listed)
    {
        var trace = GetTrace(sscCode);
        Assert.Equal(examined, trace.ServiceHistoryLaborLinesExamined);
        Assert.Equal(listed, trace.ServiceHistoryLaborLines.Count);
    }

    private SscRepairTraceLaborLineDTO GetTraceLaborLine(string sscCode, string invoiceDate)
    {
        var expected = DateTime.Parse(invoiceDate);
        var line = GetTrace(sscCode).ServiceHistoryLaborLines.FirstOrDefault(l => l.InvoiceDate == expected);
        Assert.True(line is not null, $"Expected SSC '{sscCode}' trace to list a service history labor line invoiced on {invoiceDate}.");
        return line!;
    }

    [Then("SSC {string} trace labor line invoiced on {string} is selected")]
    public void ThenSSCTraceLaborLineIsSelected(string sscCode, string invoiceDate)
    {
        var line = GetTraceLaborLine(sscCode, invoiceDate);
        Assert.True(line.StatusQualifies, "Expected the selected line's invoice status to qualify.");
        Assert.True(line.Selected, "Expected the line to be the one that decided the verdict.");
    }

    [Then("SSC {string} trace labor line invoiced on {string} does not qualify by status")]
    public void ThenSSCTraceLaborLineDoesNotQualify(string sscCode, string invoiceDate)
    {
        var line = GetTraceLaborLine(sscCode, invoiceDate);
        Assert.False(line.StatusQualifies, "Expected the line's invoice status not to qualify.");
        Assert.False(line.Selected, "A line that does not qualify must not be selected.");
    }

    // --- Part availability ---------------------------------------------------------------------------------------

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
