using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class ActivationStatusStepDefinitions
{
    private readonly Support.TestContext _context;
    private bool _activationDue;
    private long? _requestingCompanyID;
    private WarrantyActivationStatus _status;

    public ActivationStatusStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("activation is due")]
    public void GivenActivationIsDue() => _activationDue = true;

    [Given("activation is not due")]
    public void GivenActivationIsNotDue() => _activationDue = false;

    [Given("LookupOptions has require-allocation-for-activation enabled")]
    public void GivenRequireAllocationForActivationEnabled() => _context.Options.RequireAllocationForActivation = true;

    [Given("the requesting company is {string}")]
    public void GivenTheRequestingCompanyIs(string companyID) => _requestingCompanyID = long.Parse(companyID);

    [When("resolving the warranty activation status")]
    public void WhenResolvingTheWarrantyActivationStatus()
    {
        _status = new ActivationStatusEvaluator(_context.Aggregate, _context.Options)
            .Evaluate(_activationDue, _requestingCompanyID);
    }

    [Then("the warranty activation status is {string}")]
    public void ThenTheWarrantyActivationStatusIs(string expected)
    {
        Assert.Equal(expected, _status.ToString());
    }
}
