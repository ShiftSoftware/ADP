using System.Text.Json;
using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Service;
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

    public static DateTime? ConvertSinceYearsAgoToDate(string yearsAgo)
    {
        if (string.IsNullOrWhiteSpace(yearsAgo))
            return null;

        return DateTime.Now.Date.AddYears(-1 * int.Parse(yearsAgo.Split(' ').First()));
    }

    private static string? GetOptionalString(DataTableRow row, string column)
    {
        if (!row.ContainsKey(column))
            return null;

        var value = row[column];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static DateTime? GetOptionalDate(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : DateTime.Parse(value);
    }

    private static long? GetOptionalLong(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : long.Parse(value);
    }

    private static double? GetOptionalDouble(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : double.Parse(value);
    }

    private static decimal? GetOptionalDecimal(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : decimal.Parse(value);
    }


    [Given("a dealer with the following vehicles as initial stock:")]
    public void GivenADealerWithTheFollowingVehiclesInInitialStock(DataTable dataTable)
    {
        _context.Aggregate.InitialOfficialVINs.AddRange(
            dataTable.Rows.Select(row => new InitialOfficialVINModel
            {
                VIN = GetOptionalString(row, "VIN")
            }));
    }


    [Given("a dealer with the following vehicles in their dealer stock \\(coming from their DMS):")]
    [Given("vehicles in dealer stock:")]
    public void GivenTheFollowingVehiclesInDealerStock(DataTable dataTable)
    {
        _context.Aggregate.VehicleEntries.AddRange(
            dataTable.Rows.Select(row => new VehicleEntryModel
            {
                VIN = GetOptionalString(row, "VIN"),
                InvoiceDate = row.ContainsKey("Invoiced Since")
                    ? ConvertSinceYearsAgoToDate(row["Invoiced Since"])
                    : GetOptionalDate(row, "InvoiceDate"),
                VariantCode = GetOptionalString(row, "VariantCode"),
                Katashiki = GetOptionalString(row, "Katashiki"),
                ExteriorColorCode = GetOptionalString(row, "ExteriorColorCode"),
                InteriorColorCode = GetOptionalString(row, "InteriorColorCode"),
                BrandID = GetOptionalLong(row, "BrandID"),
                CompanyID = GetOptionalLong(row, "CompanyID"),
                BranchID = GetOptionalLong(row, "BranchID"),
            }));
    }


    [Given("a dealer with the following vehicles in official SSC Vehicles \\(Provided by the vehicle manufacturer):")]
    [Given("SSC affected vehicles:")]
    public void GivenTheFollowingVehiclesInSsc(DataTable dataTable)
    {
        _context.Aggregate.SSCAffectedVINs.AddRange(
            dataTable.Rows.Select(row => new SSCAffectedVINModel
            {
                VIN = GetOptionalString(row, "VIN"),
                CampaignCode = GetOptionalString(row, "CampaignCode"),
                Description = GetOptionalString(row, "Description"),
                LaborCode1 = GetOptionalString(row, "LaborCode1"),
                LaborCode2 = GetOptionalString(row, "LaborCode2"),
                LaborCode3 = GetOptionalString(row, "LaborCode3"),
                LaborHour1 = GetOptionalDouble(row, "LaborHour1"),
                LaborHour2 = GetOptionalDouble(row, "LaborHour2"),
                LaborHour3 = GetOptionalDouble(row, "LaborHour3"),
                PartNumber1 = GetOptionalString(row, "PartNumber1"),
                PartNumber2 = GetOptionalString(row, "PartNumber2"),
                PartNumber3 = GetOptionalString(row, "PartNumber3"),
                RepairDate = GetOptionalDate(row, "RepairDate"),
                CompanyID = GetOptionalLong(row, "CompanyID"),
            }));
    }


    [Given("warranty claims:")]
    public void GivenWarrantyClaims(DataTable dataTable)
    {
        _context.Aggregate.WarrantyClaims.AddRange(
            dataTable.Rows.Select(row => new WarrantyClaimModel
            {
                ClaimStatus = row.ContainsKey("ClaimStatus") && !string.IsNullOrWhiteSpace(row["ClaimStatus"])
                    ? Enum.Parse<ClaimStatus>(row["ClaimStatus"])
                    : default,
                RepairCompletionDate = GetOptionalDate(row, "RepairCompletionDate"),
                DistributorComment = GetOptionalString(row, "DistributorComment"),
                LaborOperationNumberMain = GetOptionalString(row, "LaborCode"),
                LaborLines = BuildWarrantyClaimLaborLines(row),
                VIN = GetOptionalString(row, "VIN"),
                CompanyID = GetOptionalLong(row, "CompanyID"),
            }));
    }

    private static IEnumerable<WarrantyClaimLaborLineModel> BuildWarrantyClaimLaborLines(DataTableRow row)
    {
        var laborCode = GetOptionalString(row, "LaborCode");
        if (laborCode is null)
            return Enumerable.Empty<WarrantyClaimLaborLineModel>();

        return new[]
        {
            new WarrantyClaimLaborLineModel { LaborCode = laborCode }
        };
    }


    [Given("labor lines:")]
    public void GivenLaborLines(DataTable dataTable)
    {
        _context.Aggregate.LaborLines.AddRange(
            dataTable.Rows.Select(row => new OrderLaborLineModel
            {
                LaborCode = GetOptionalString(row, "LaborCode"),
                InvoiceDate = GetOptionalDate(row, "InvoiceDate"),
                InvoiceStatus = GetOptionalString(row, "InvoiceStatus"),
                CompanyID = GetOptionalLong(row, "CompanyID"),
                BranchID = GetOptionalLong(row, "BranchID"),
                InvoiceNumber = GetOptionalString(row, "InvoiceNumber"),
                VIN = GetOptionalString(row, "VIN"),
            }));
    }


    [Given("the {string} environment is loaded")]
    public void GivenTheEnvironmentIsLoaded(string environmentName)
    {
        var path = Path.Combine(
            TestContext.GetTestDataRoot(),
            "environments", $"{environmentName}.json");

        var json = File.ReadAllText(path);
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new NullableLongDictionaryConverter());
        var environment = JsonSerializer.Deserialize<TestEnvironment>(json, jsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize environment '{environmentName}'");

        _context.Environment = environment;
        _context.Options = environment.LookupOptions;
    }

    [Given("loading vehicle {string} from the environment")]
    public void GivenLoadingVehicleFromEnvironment(string vin)
    {
        var environment = _context.Environment
            ?? throw new InvalidOperationException("No environment loaded. Use 'Given the \"...\" environment is loaded' first.");

        if (!environment.Vehicles.TryGetValue(vin, out var vehicleData))
            throw new KeyNotFoundException($"VIN '{vin}' not found in loaded environment.");

        vehicleData.VIN = vin;
        vehicleData.BrokerInitialVehicles.AddRange(environment.BrokerInitialVehicles);
        vehicleData.BrokerInvoices.AddRange(environment.BrokerInvoices);
        _context.Aggregate = vehicleData;
    }

    [When("Checking {string}")]
    public void WhenChecking(string vin)
    {
        _context.Aggregate.VIN = vin;
    }
}
