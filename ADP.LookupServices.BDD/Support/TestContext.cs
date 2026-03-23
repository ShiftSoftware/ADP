using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Vehicle;

namespace LookupServices.BDD.Support;

public class TestContext
{
    public CompanyDataAggregateModel Aggregate { get; set; } = new();
    public LookupOptions Options { get; set; } = new();
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public IVehicleLoockupStorageService StorageService { get; set; } = null!;

    // Intermediate evaluator results
    public VehicleEntryModel? CurrentVehicle { get; set; }
    public VehicleSaleInformation? SaleInformation { get; set; }
}
