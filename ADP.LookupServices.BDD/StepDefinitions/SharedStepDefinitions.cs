using System.Text.Json;
using System.Text.Json.Serialization;
using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Part;
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

    private static int? GetOptionalInt(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : int.Parse(value);
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
                WarrantyActivationDate = GetOptionalDate(row, "WarrantyActivationDate"),
                CustomerID = GetOptionalString(row, "CustomerID"),
                InvoiceNumber = GetOptionalString(row, "InvoiceNumber"),
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
                OrderDocumentNumber = GetOptionalString(row, "OrderDocumentNumber"),
                ParentInvoiceNumber = GetOptionalString(row, "ParentInvoiceNumber"),
                ServiceDescription = GetOptionalString(row, "ServiceDescription"),
                JobDescription = GetOptionalString(row, "JobDescription"),
                ServiceCode = GetOptionalString(row, "ServiceCode"),
                PackageCode = GetOptionalString(row, "PackageCode"),
                AccountNumber = GetOptionalString(row, "AccountNumber"),
                NumberOfPartLines = GetOptionalInt(row, "NumberOfPartLines") ?? 0,
                Odometer = GetOptionalInt(row, "Odometer"),
                VIN = GetOptionalString(row, "VIN"),
            }));
    }


    [Given("vehicle service activations:")]
    public void GivenVehicleServiceActivations(DataTable dataTable)
    {
        _context.Aggregate.VehicleServiceActivations.AddRange(
            dataTable.Rows.Select(row => new VehicleServiceActivation
            {
                id = GetOptionalString(row, "id") ?? Guid.NewGuid().ToString(),
                WarrantyActivationDate = GetOptionalDate(row, "WarrantyActivationDate"),
                CompanyID = GetOptionalLong(row, "CompanyID"),
            }));
    }

    [Given("warranty date shifts:")]
    public void GivenWarrantyDateShifts(DataTable dataTable)
    {
        _context.Aggregate.WarrantyDateShifts.AddRange(
            dataTable.Rows.Select(row => new WarrantyDateShiftModel
            {
                NewDate = DateTime.Parse(row["NewDate"]),
            }));
    }

    [Given("free service item date shifts:")]
    public void GivenFreeServiceItemDateShifts(DataTable dataTable)
    {
        _context.Aggregate.FreeServiceItemDateShifts.AddRange(
            dataTable.Rows.Select(row => new FreeServiceItemDateShiftModel
            {
                VIN = GetOptionalString(row, "VIN"),
                NewDate = DateTime.Parse(row["NewDate"]),
            }));
    }

    [Given("extended warranty entries:")]
    public void GivenExtendedWarrantyEntries(DataTable dataTable)
    {
        _context.Aggregate.ExtendedWarrantyEntries.AddRange(
            dataTable.Rows.Select(row => new ExtendedWarrantyModel
            {
                StartDate = GetOptionalDate(row, "StartDate"),
                EndDate = GetOptionalDate(row, "EndDate"),
            }));
    }


    [Given("part lines:")]
    public void GivenPartLines(DataTable dataTable)
    {
        _context.Aggregate.PartLines.AddRange(
            dataTable.Rows.Select(row => new OrderPartLineModel
            {
                PartNumber = GetOptionalString(row, "PartNumber"),
                InvoiceDate = GetOptionalDate(row, "InvoiceDate"),
                InvoiceStatus = GetOptionalString(row, "InvoiceStatus"),
                CompanyID = GetOptionalLong(row, "CompanyID"),
                BranchID = GetOptionalLong(row, "BranchID"),
                InvoiceNumber = GetOptionalString(row, "InvoiceNumber"),
                OrderDocumentNumber = GetOptionalString(row, "OrderDocumentNumber"),
                ParentInvoiceNumber = GetOptionalString(row, "ParentInvoiceNumber"),
                PackageCode = GetOptionalString(row, "PackageCode"),
                AccountNumber = GetOptionalString(row, "AccountNumber"),
                SoldQuantity = GetOptionalDecimal(row, "SoldQuantity"),
                NumberOfLaborLines = GetOptionalInt(row, "NumberOfLaborLines") ?? 0,
                VIN = GetOptionalString(row, "VIN"),
            }));
    }

    [Given("accessories:")]
    public void GivenAccessories(DataTable dataTable)
    {
        _context.Aggregate.Accessories.AddRange(
            dataTable.Rows.Select(row => new VehicleAccessoryModel
            {
                PartNumber = GetOptionalString(row, "PartNumber"),
                PartDescription = GetOptionalString(row, "PartDescription"),
                Image = GetOptionalString(row, "Image"),
            }));
    }

    [Given("paid service invoices:")]
    public void GivenPaidServiceInvoices(DataTable dataTable)
    {
        _context.Aggregate.PaidServiceInvoices.AddRange(
            dataTable.Rows.Select(row => new PaidServiceInvoiceModel
            {
                InvoiceDate = DateTime.Parse(row["InvoiceDate"]),
                InvoiceNumber = long.Parse(row["InvoiceNumber"]),
                Lines = BuildPaidServiceInvoiceLines(row),
            }));
    }

    private static IEnumerable<PaidServiceInvoiceLineModel> BuildPaidServiceInvoiceLines(DataTableRow row)
    {
        var serviceItemId = GetOptionalString(row, "ServiceItemID");
        if (serviceItemId is null)
            return Enumerable.Empty<PaidServiceInvoiceLineModel>();

        return new[]
        {
            new PaidServiceInvoiceLineModel
            {
                ServiceItemID = serviceItemId,
                ExpireDate = GetOptionalDate(row, "ExpireDate"),
                PackageCode = GetOptionalString(row, "PackageCode"),
                ServiceItem = new ServiceItemModel
                {
                    Name = new Dictionary<string, string> { { "en", GetOptionalString(row, "ServiceItemName") ?? "" } },
                    MaximumMileage = GetOptionalLong(row, "MaximumMileage"),
                },
            }
        };
    }

    [Given("item claims:")]
    public void GivenItemClaims(DataTable dataTable)
    {
        _context.Aggregate.ItemClaims.AddRange(
            dataTable.Rows.Select(row => new ItemClaimModel
            {
                ServiceItemID = GetOptionalString(row, "ServiceItemID"),
                ClaimDate = row.ContainsKey("ClaimDate") && !string.IsNullOrWhiteSpace(row["ClaimDate"])
                    ? DateTimeOffset.Parse(row["ClaimDate"]) : default,
                JobNumber = GetOptionalString(row, "JobNumber"),
                InvoiceNumber = GetOptionalString(row, "InvoiceNumber"),
                CompanyID = GetOptionalLong(row, "CompanyID"),
                PackageCode = GetOptionalString(row, "PackageCode"),
                VehicleInspectionID = GetOptionalString(row, "VehicleInspectionID"),
                CampaignVinEntryID = GetOptionalString(row, "CampaignVinEntryID"),
            }));
    }

    [Given("free service item excluded VINs:")]
    public void GivenFreeServiceItemExcludedVINs(DataTable dataTable)
    {
        _context.Aggregate.FreeServiceItemExcludedVINs.AddRange(
            dataTable.Rows.Select(row => new FreeServiceItemExcludedVINModel
            {
                VIN = GetOptionalString(row, "VIN"),
            }));
    }


    [Given("vehicle inspections:")]
    public void GivenVehicleInspections(DataTable dataTable)
    {
        _context.Aggregate.VehicleInspections.AddRange(
            dataTable.Rows.Select(row => new VehicleInspectionModel
            {
                id = GetOptionalString(row, "id") ?? Guid.NewGuid().ToString(),
                InspectionDate = DateTimeOffset.Parse(row["InspectionDate"]),
                VehicleInspectionTypeID = row.ContainsKey("VehicleInspectionTypeID") && !string.IsNullOrWhiteSpace(row["VehicleInspectionTypeID"])
                    ? long.Parse(row["VehicleInspectionTypeID"]) : 0,
            }));
    }

    [Given("campaign VIN entries:")]
    public void GivenCampaignVinEntries(DataTable dataTable)
    {
        _context.Aggregate.CampaignVinEntries.AddRange(
            dataTable.Rows.Select(row => new CampaignVinEntryModel
            {
                id = GetOptionalString(row, "id") ?? Guid.NewGuid().ToString(),
                VIN = GetOptionalString(row, "VIN"),
                CampaignID = GetOptionalLong(row, "CampaignID"),
                CampaignUniqueReference = GetOptionalString(row, "CampaignUniqueReference"),
                RecordedDate = DateTimeOffset.Parse(row["RecordedDate"]),
                CompanyID = GetOptionalLong(row, "CompanyID"),
            }));
    }

    [Given("paint thickness inspections:")]
    public void GivenPaintThicknessInspections(DataTable dataTable)
    {
        var inspections = (_context.Aggregate.PaintThicknessInspections?.ToList())
            ?? new List<PaintThicknessInspectionModel>();

        inspections.AddRange(
            dataTable.Rows.Select(row => new PaintThicknessInspectionModel
            {
                InspectionDate = GetOptionalDate(row, "InspectionDate"),
                Source = GetOptionalString(row, "Source"),
                Panels = new List<PaintThicknessInspectionPanelModel>(),
            }));

        _context.Aggregate.PaintThicknessInspections = inspections;
    }

    [Given("paint thickness panels for inspection on {string}:")]
    public void GivenPaintThicknessPanelsForInspection(string inspectionDate, DataTable dataTable)
    {
        var date = DateTime.Parse(inspectionDate);
        var inspection = _context.Aggregate.PaintThicknessInspections?
            .FirstOrDefault(i => i.InspectionDate == date)
            ?? throw new InvalidOperationException($"No paint thickness inspection found for date '{inspectionDate}'");

        var panels = dataTable.Rows.Select(row => new PaintThicknessInspectionPanelModel
        {
            PanelType = Enum.Parse<VehiclePanelType>(row["PanelType"]),
            PanelSide = row.ContainsKey("PanelSide") && !string.IsNullOrWhiteSpace(row["PanelSide"])
                ? Enum.Parse<VehiclePanelSide>(row["PanelSide"]) : null,
            PanelPosition = row.ContainsKey("PanelPosition") && !string.IsNullOrWhiteSpace(row["PanelPosition"])
                ? Enum.Parse<VehiclePanelPosition>(row["PanelPosition"]) : null,
            MeasuredThickness = decimal.Parse(row["MeasuredThickness"]),
            Images = row.ContainsKey("Images") && !string.IsNullOrWhiteSpace(row["Images"])
                ? row["Images"].Split(',', StringSplitOptions.TrimEntries) : [],
        }).ToList();

        inspection.Panels = panels;
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
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
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
