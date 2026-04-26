using System.Reflection;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Vehicle;

namespace LookupServices.BDD.Support;

public class TestContext
{
    public CompanyDataAggregateModel Aggregate { get; set; } = new();
    public PartAggregateCosmosModel PartAggregate { get; set; } = new()
    {
        CatalogParts = [],
        StockParts = [],
        CompanyDeadStockParts = [],
    };
    public LookupOptions Options { get; set; } = new();
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public IVehicleLookupStorageService StorageService { get; set; } = null!;

    // Intermediate evaluator results
    public VehicleEntryModel? CurrentVehicle { get; set; }
    public VehicleSaleInformation? SaleInformation { get; set; }

    // Loaded environment (populated by environment loading step)
    public TestEnvironment? Environment { get; set; }

    /// <summary>
    /// Walks up from the test assembly directory to find the repo root
    /// (identified by the ADP.TestData directory), then returns the ADP.TestData path.
    /// </summary>
    public static string GetTestDataRoot()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        while (dir != null)
        {
            var testDataPath = Path.Combine(dir, "ADP.TestData");
            if (Directory.Exists(testDataPath))
                return testDataPath;

            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException(
            "Could not find ADP.TestData directory. " +
            "Searched upward from: " + Assembly.GetExecutingAssembly().Location);
    }
}
