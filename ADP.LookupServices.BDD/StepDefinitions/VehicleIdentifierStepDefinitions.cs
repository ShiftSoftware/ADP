using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleIdentifierStepDefinitions
{
    private readonly Support.TestContext _context;

    public VehicleIdentifierStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Then("the vehicle identifiers are:")]
    public void ThenTheVehicleIdentifiersAre(DataTable dataTable)
    {
        var vehicle = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = vehicle;

        var result = new VehicleIdentifierEvaluator(_context.Aggregate).Evaluate(vehicle);

        var expected = dataTable.Rows.ToDictionary(
            row => row["Field"].Trim(),
            row => string.IsNullOrWhiteSpace(row["Value"]) ? null : row["Value"].Trim());

        foreach (var (field, expectedValue) in expected)
        {
            var actualValue = field switch
            {
                "VIN" => result.VIN,
                "Variant" => result.Variant,
                "Katashiki" => result.Katashiki,
                "Color" => result.Color,
                "Trim" => result.Trim,
                "BrandID" => result.BrandID,
                _ => throw new ArgumentException($"Unknown identifier field: {field}")
            };

            Assert.Equal(expectedValue, actualValue);
        }
    }
}
