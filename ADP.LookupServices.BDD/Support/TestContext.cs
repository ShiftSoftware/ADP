using System.Reflection;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
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
    public LookupOptions Options { get; set; } = NewLookupOptions();

    /// <summary>
    /// The pattern the scenarios' deployment declares for its own service codes. ADP ships none —
    /// a convention is a fact about a source system, and one presented as a framework default reads
    /// a fraction of any estate that does not share it while looking configured — so the harness
    /// declares one the same way a host does.
    /// <para>
    /// It reproduces the shapes real codes come in: an optional programme, optionally glued to a
    /// model token; further tokens, which may be hyphenated; the milestone; and a qualifier that
    /// may be glued to the milestone, trail it as separate tokens, or be absent.
    /// </para>
    /// </summary>
    public const string ScenarioServiceCodePattern =
        @"^(?:(?<program>PGM|ALT|OTH)[A-Z0-9-]*)?(?:\s*[A-Z][A-Z0-9-]*)*\s*(?<milestone>[0-9]{1,3})\s*K(?<qualifier>[A-Z0-9]*(?:\s+[A-Z0-9]+)*)$";

    private static LookupOptions NewLookupOptions()
    {
        var options = new LookupOptions();

        options.ServiceMilestones.Conventions.Add(new ServiceCodeConvention
        {
            Name = "scenario",
            Pattern = ScenarioServiceCodePattern,
        });

        return options;
    }
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public IVehicleLookupStorageService StorageService { get; set; } = null!;

    // Intermediate evaluator results
    public VehicleEntryModel? CurrentVehicle { get; set; }
    public VehicleOwnership? CurrentOwnership { get; set; }
    public VehicleSaleInformation? SaleInformation { get; set; }

    /// <summary>
    /// Selects the vehicle entry and resolves its ownership the same way the production
    /// pipeline does (<see cref="VehicleEntryEvaluator"/> then
    /// <see cref="VehicleOwnershipEvaluator"/>), recording both on the context.
    /// </summary>
    public (VehicleEntryModel? vehicle, VehicleOwnership ownership) ResolveVehicle()
    {
        var vehicle = new VehicleEntryEvaluator(Aggregate, Options).Evaluate();
        var ownership = new VehicleOwnershipEvaluator(Aggregate).Evaluate(vehicle);
        CurrentVehicle = vehicle;
        CurrentOwnership = ownership;
        return (vehicle, ownership);
    }

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
