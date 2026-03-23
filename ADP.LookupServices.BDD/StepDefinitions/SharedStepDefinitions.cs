using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Models.Vehicle;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class SharedStepDefinitions
{
    private readonly TestContext _context;

    public SharedStepDefinitions(TestContext context)
    {
        _context = context;
    }

    private class FeatureData
    {
        public string? VIN { get; set; }
        public DateTime? InvoiceDate { get; set; }
    }

    public static DateTime? ConvertSinceYearsAgoToDate(string yearsAgo)
    {
        if (string.IsNullOrWhiteSpace(yearsAgo))
            return null;

        return DateTime.Now.Date.AddYears(-1 * int.Parse(yearsAgo.Split(' ').First()));
    }

    private IEnumerable<FeatureData> ParseDataTable(DataTable dataTable)
    {
        var data = dataTable.Rows.Select(x => new FeatureData
        {
            VIN = x.ContainsKey("VIN") ? x["VIN"].ToString() : null,
            InvoiceDate = x.ContainsKey("Invoiced Since") ? ConvertSinceYearsAgoToDate(x["Invoiced Since"]) : null
        });

        return data;
    }


    [Given("a dealer with the following vehicles as initial stock:")]
    public void GivenADealerWithTheFollowingVehiclesInInitialStock(DataTable dataTable)
    {
        var data = this.ParseDataTable(dataTable);

        _context.Aggregate.InitialOfficialVINs.AddRange(data
            .Select(x => new InitialOfficialVINModel
            {
                VIN = x.VIN
            }));
    }


    [Given("a dealer with the following vehicles in their dealer stock \\(coming from their DMS):")]
    public void GivenTheFollowingVehiclesInDealerStock(DataTable dataTable)
    {
        var data = this.ParseDataTable(dataTable);

        _context.Aggregate.VehicleEntries.AddRange(data
            .Select(x => new VehicleEntryModel
            {
                VIN = x.VIN,
                InvoiceDate = x.InvoiceDate
            }));
    }


    [Given("a dealer with the following vehicles in official SSC Vehicles \\(Provided by the vehicle manufacturer):")]
    public void GivenTheFollowingVehiclesInSsc(DataTable dataTable)
    {
        var data = this.ParseDataTable(dataTable);

        _context.Aggregate.SSCAffectedVINs.AddRange(
            data.Select(x => new SSCAffectedVINModel
            {
                VIN = x.VIN
            }));
    }


    [When("Checking {string}")]
    public void WhenChecking(string vin)
    {
        _context.Aggregate.VIN = vin;
    }
}
