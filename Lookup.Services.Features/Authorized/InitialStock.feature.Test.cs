using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using Xunit;

namespace Lookup.Services.Features.Authorized
{
    [Binding]
    public class InitialStock
    {
        private readonly ScenarioContext _scenarioContext;
        private VehicleLookupService _vehicleLookupService;

        public InitialStock(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given("A VIN {string} that exists in initial stock")]
        public void GivenAVINThatExistsInInitialStock(string vin)
        {
            this._scenarioContext["VIN"] = vin;

            var vehicleLoockupCosmosService = new Moq.Mock<IVehicleLoockupCosmosService>();

            vehicleLoockupCosmosService.Setup(x => x.GetAggregatedCompanyData(vin))
                .Returns(() =>
                {
                    return Task.FromResult(new CompanyDataAggregateCosmosModel
                    {
                        VehicleEntries = new List<ShiftSoftware.ADP.Models.Vehicle.VehicleEntryModel> { },
                        PaintThicknessInspections = null,
                        InitialOfficialVINs = new List<ShiftSoftware.ADP.Models.Vehicle.InitialOfficialVINModel> {
                            new ShiftSoftware.ADP.Models.Vehicle.InitialOfficialVINModel { VIN = vin }
                        },
                        SSCAffectedVINs = new List<ShiftSoftware.ADP.Models.Vehicle.SSCAffectedVINModel>(),
                        Accessories = new List<ShiftSoftware.ADP.Models.Vehicle.VehicleAccessoryModel>(),
                        FreeServiceItemDateShifts = new List<ShiftSoftware.ADP.Models.Vehicle.FreeServiceItemDateShiftModel> { },
                        FreeServiceItemExcludedVINs = new List<ShiftSoftware.ADP.Models.Vehicle.FreeServiceItemExcludedVINModel> { },
                    });
                });

            this._vehicleLookupService = new VehicleLookupService(
                vehicleLoockupCosmosService.Object,
                null,
                null,
                null,
                new ShiftSoftware.ADP.Lookup.Services.LookupOptions { }
            );
        }

        [When("looking it up")]
        public async Task WhenLookingItUp()
        {
           this._scenarioContext["Vehicle"] = await _vehicleLookupService.LookupAsync(this._scenarioContext["VIN"].ToString(), null);
        }

        [Then("it should be marked as Authorized")]
        public void ThenItShouldBeMarkedAsAuthorized()
        {
            var vehicle = (this._scenarioContext["Vehicle"] as VehicleLookupDTO)!;

            Assert.True(vehicle.IsAuthorized);
        }
    }
}