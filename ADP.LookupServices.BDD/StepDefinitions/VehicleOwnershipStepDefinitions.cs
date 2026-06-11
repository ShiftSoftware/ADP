using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleOwnershipStepDefinitions
{
    private readonly Support.TestContext _context;
    private Exception? _exception;

    public VehicleOwnershipStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("resolving ownership for {string}")]
    public void WhenResolvingOwnershipFor(string vin)
    {
        _context.Aggregate.VIN = vin;

        try
        {
            _context.ResolveVehicle();
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Then("ownership resolution fails because the service activation is incomplete")]
    public void ThenOwnershipResolutionFailsBecauseTheServiceActivationIsIncomplete()
    {
        Assert.NotNull(_exception);
        Assert.IsType<IncompleteVehicleServiceActivationException>(_exception);
    }
}
