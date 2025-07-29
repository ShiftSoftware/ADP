using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Features;

[Binding]
public class SharedStepDefinitions
{
    private readonly ScenarioContext scenarioContext;
    private readonly CompanyDataAggregateCosmosModel _companyDataAggregate;

    public SharedStepDefinitions(ScenarioContext scenarioContext, CompanyDataAggregateCosmosModel companyDataAggregate)
    {
        this.scenarioContext = scenarioContext;
        this._companyDataAggregate = companyDataAggregate;
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

        _companyDataAggregate.InitialOfficialVINs.AddRange(data.Select(x => new InitialOfficialVINModel { VIN = x.VIN }));

        this.scenarioContext["companyData"] = _companyDataAggregate;
    }


    [Given("a dealer with the following vehicles in their dealer stock \\(coming from their DMS):")]
    public void GivenTheFollowingVehiclesInDealerStock(DataTable dataTable)
    {
        var data = this.ParseDataTable(dataTable);

        _companyDataAggregate.VehicleEntries.AddRange(data.Select(x => new VehicleEntryModel {
            VIN = x.VIN,
            InvoiceDate = x.InvoiceDate
        }));

        this.scenarioContext["companyData"] = _companyDataAggregate;
    }


    [Given("a dealer with the following vehicles in official SSC Vehciles \\(Provided by the vehicle manufacturer):")]
    public void GivenTheFollowingVehiclesInSsc(DataTable dataTable)
    {
        var data = this.ParseDataTable(dataTable);

        _companyDataAggregate.SSCAffectedVINs.AddRange(data.Select(x => new SSCAffectedVINModel { VIN = x.VIN }));

        this.scenarioContext["companyData"] = _companyDataAggregate;
    }


    [When("Checking {string}")]
    public void WhenCheckingWhichIsInInitialStock(string vin)
    {
        this.scenarioContext["vin"] = vin;
    }
}