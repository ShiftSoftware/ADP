using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class PartDeadStockStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<DeadStockDTO>? _result;

    public PartDeadStockStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating dead stock for part {string}")]
    public async Task WhenEvaluatingDeadStockForPart(string partNumber)
    {
        var evaluator = new PartDeadStockEvaluator(
            _context.PartAggregate, _context.Options, _context.ServiceProvider);
        _result = await evaluator.Evaluate(language: "en");
    }

    [Then("there are {int} dead stock companies")]
    public void ThenThereAreDeadStockCompanies(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count());
    }

    [Then("dead stock company {string} is named {string}")]
    public void ThenDeadStockCompanyIsNamed(string companyHashId, string expectedName)
    {
        Assert.NotNull(_result);
        var company = _result.FirstOrDefault(d => d.CompanyHashID == companyHashId);
        Assert.NotNull(company);
        Assert.Equal(expectedName, company.CompanyName);
    }

    [Then("dead stock company {string} has {int} branches")]
    public void ThenDeadStockCompanyHasBranches(string companyHashId, int count)
    {
        Assert.NotNull(_result);
        var company = _result.FirstOrDefault(d => d.CompanyHashID == companyHashId);
        Assert.NotNull(company);
        Assert.Equal(count, company.BranchDeadStock.Count());
    }

    [Then("dead stock company {string} branch {string} has quantity {decimal}")]
    public void ThenDeadStockCompanyBranchHasQuantity(string companyHashId, string branchHashId, decimal expected)
    {
        Assert.NotNull(_result);
        var company = _result.FirstOrDefault(d => d.CompanyHashID == companyHashId);
        Assert.NotNull(company);
        var branch = company.BranchDeadStock.FirstOrDefault(b => b.CompanyBranchHashID == branchHashId);
        Assert.NotNull(branch);
        Assert.Equal(expected, branch.Quantity);
    }
}
