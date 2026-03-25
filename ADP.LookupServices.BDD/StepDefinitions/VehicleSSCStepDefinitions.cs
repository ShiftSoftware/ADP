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
}
