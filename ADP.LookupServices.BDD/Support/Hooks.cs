using NSubstitute;
using Reqnroll;
using Reqnroll.BoDi;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace LookupServices.BDD.Support;

[Binding]
public class Hooks
{
    private readonly IObjectContainer _container;

    public Hooks(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        var context = new TestContext
        {
            ServiceProvider = Substitute.For<IServiceProvider>(),
            StorageService = Substitute.For<IVehicleLoockupStorageService>()
        };
        _container.RegisterInstanceAs(context);
    }
}
