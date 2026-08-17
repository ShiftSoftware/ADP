using LookupServices.BDD.Support;
using NSubstitute;
using Reqnroll;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Text.Json;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class MockStorageStepDefinitions
{
    private readonly Support.TestContext _context;
    private List<ServiceItemModel> _serviceItems = [];

    public MockStorageStepDefinitions(Support.TestContext context)
    {
        _context = context;
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

    private static IEnumerable<string> GetEligibilityConditionValues(DataTableRow row)
    {
        if (row.ContainsKey("ValuesJson"))
            return JsonSerializer.Deserialize<string[]>(row["ValuesJson"]) ?? [];

        return row["Values"].Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool HasValue(DataTableRow row, string column) =>
        row.ContainsKey(column) && !string.IsNullOrWhiteSpace(row[column]);

    /// <summary>
    /// An optional list-valued condition property, written either comma-separated for readability or
    /// as a JSON array when the scenario is about a shape the shorthand cannot express — an empty
    /// list, or an entry that is blank or null. Absent from both columns means the property was
    /// omitted, which is a distinct case from an empty list in this grammar.
    /// </summary>
    private static IEnumerable<string>? GetOptionalConditionList(
        DataTableRow row,
        string column,
        string jsonColumn)
    {
        if (HasValue(row, jsonColumn))
            return JsonSerializer.Deserialize<string[]>(row[jsonColumn]) ?? [];

        if (!HasValue(row, column))
            return null;

        return row[column].Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    [Given("broker stock for brand {long}:")]
    public void GivenBrokerStockForBrand(long brandId, DataTable dataTable)
    {
        var stocks = dataTable.Rows.Select(row =>
        {
            var isAtStock = row.ContainsKey("IsAtStock") && !string.IsNullOrWhiteSpace(row["IsAtStock"])
                && bool.Parse(row["IsAtStock"]);

            var invoiceDate = GetOptionalDate(row, "InvoiceDate");
            var invoiceNumber = GetOptionalLong(row, "InvoiceNumber");

            var invoices = new List<TBP_Invoice>();
            if (invoiceDate is not null)
            {
                invoices.Add(new TBP_Invoice
                {
                    InvoiceDate = new DateTimeOffset(invoiceDate.Value, TimeSpan.Zero),
                    InvoiceNumber = invoiceNumber,
                    IsDeleted = false,
                    IsCompleted = row.ContainsKey("IsCompleted") && !string.IsNullOrWhiteSpace(row["IsCompleted"])
                        ? bool.Parse(row["IsCompleted"]) : true,
                    CustomerName = GetOptionalString(row, "CustomerName"),
                    CustomerPhone = GetOptionalString(row, "CustomerPhone"),
                    CustomerIDNumber = GetOptionalString(row, "CustomerIDNumber"),
                });
            }

            return new TBP_StockModel
            {
                BrandID = brandId,
                BrokerID = long.Parse(row["BrokerID"]),
                Quantity = isAtStock ? 1 : 0,
                Broker = new TBP_BrokerModel
                {
                    ID = long.Parse(row["BrokerID"]),
                    Name = GetOptionalString(row, "BrokerName"),
                },
                Invoices = invoices,
            };
        }).ToList();

        _context.StorageService
            .GetBrokerStockAsync(brandId, Arg.Any<string>())
            .Returns(stocks);
    }

    [Given("customer {string} at company {long} has name {string} and phone {string}")]
    public void GivenCustomer(string customerId, long companyId, string name, string phone)
    {
        _context.StorageService
            .GetCustomerAsync(customerId, companyId)
            .Returns(new CustomerModel
            {
                CustomerID = customerId,
                CompanyID = companyId,
                FullName = name,
                PhoneNumbers = new[] { phone },
            });
    }

    [Given("vehicle model for variant {string} brand {long}:")]
    public void GivenVehicleModelForVariantBrand(string variant, long brandId, DataTable dataTable)
    {
        var row = dataTable.Rows.First();
        _context.StorageService
            .GetVehicleModelsAsync(variant, brandId)
            .Returns(new VehicleModelModel
            {
                VariantCode = variant,
                BrandID = brandId,
                ModelDescription = GetOptionalString(row, "ModelDescription"),
                BodyType = GetOptionalString(row, "BodyType"),
                Engine = GetOptionalString(row, "Engine"),
                Transmission = GetOptionalString(row, "Transmission"),
                Fuel = GetOptionalString(row, "Fuel"),
                VariantDescription = GetOptionalString(row, "VariantDescription"),
            });
    }

    [Given("service items:")]
    public void GivenServiceItems(DataTable dataTable)
    {
        _serviceItems = dataTable.Rows.Select(row =>
        {
            var activationTrigger = row.ContainsKey("ActivationTrigger") && !string.IsNullOrWhiteSpace(row["ActivationTrigger"])
                ? Enum.Parse<ClaimableItemCampaignActivationTrigger>(row["ActivationTrigger"])
                : ClaimableItemCampaignActivationTrigger.WarrantyActivation;

            var activationType = row.ContainsKey("ActivationType") && !string.IsNullOrWhiteSpace(row["ActivationType"])
                ? Enum.Parse<ClaimableItemCampaignActivationTypes>(row["ActivationType"])
                : default;

            var validityMode = row.ContainsKey("ValidityMode") && !string.IsNullOrWhiteSpace(row["ValidityMode"])
                ? Enum.Parse<ClaimableItemValidityMode>(row["ValidityMode"])
                : ClaimableItemValidityMode.RelativeToActivation;

            var programRole = row.ContainsKey("ProgramRole") && !string.IsNullOrWhiteSpace(row["ProgramRole"])
                ? Enum.Parse<ServiceItemProgramRole>(row["ProgramRole"])
                : ServiceItemProgramRole.ScheduledService;

            var brandId = GetOptionalLong(row, "BrandID");
            var companyId = GetOptionalLong(row, "CompanyID");
            var countryId = GetOptionalLong(row, "CountryID");

            var katashiki = GetOptionalString(row, "ModelCostKatashiki");
            var variant = GetOptionalString(row, "ModelCostVariant");
            List<ServiceItemCostModel>? modelCosts = null;
            if (katashiki is not null || variant is not null)
            {
                modelCosts = new List<ServiceItemCostModel>
                {
                    new() { Katashiki = katashiki, Variant = variant },
                };
            }

            return new ServiceItemModel
            {
                IntegrationID = GetOptionalString(row, "ServiceItemID"),
                Name = new Dictionary<string, string> { { "en", GetOptionalString(row, "Name") ?? "" } },
                IsDeleted = row.ContainsKey("IsDeleted") && !string.IsNullOrWhiteSpace(row["IsDeleted"]) && bool.Parse(row["IsDeleted"]),
                BrandIDs = brandId is not null ? new List<long?> { brandId } : null,
                CompanyIDs = companyId is not null ? new List<long?> { companyId } : null,
                CountryIDs = countryId is not null ? new List<long?> { countryId } : null,
                CampaignStartDate = GetOptionalDate(row, "CampaignStartDate") ?? new DateTime(1900, 1, 1),
                CampaignEndDate = GetOptionalDate(row, "CampaignEndDate") ?? new DateTime(2100, 1, 1),
                CampaignActivationTrigger = activationTrigger,
                CampaignActivationType = activationType,
                ValidityMode = validityMode,
                ValidFrom = GetOptionalDate(row, "ValidFrom"),
                ValidTo = GetOptionalDate(row, "ValidTo"),
                ActiveFor = row.ContainsKey("ActiveForMonths") && !string.IsNullOrWhiteSpace(row["ActiveForMonths"])
                    ? int.Parse(row["ActiveForMonths"]) : null,
                ActiveForDurationType = row.ContainsKey("ActiveForMonths") && !string.IsNullOrWhiteSpace(row["ActiveForMonths"])
                    ? DurationType.Months : null,
                MaximumMileage = GetOptionalLong(row, "MaximumMileage"),
                ProgramRole = programRole,
                PackageCode = GetOptionalString(row, "PackageCode"),
                VehicleInspectionTypeID = GetOptionalLong(row, "VehicleInspectionTypeID"),
                CampaignID = GetOptionalLong(row, "CampaignID"),
                ModelCosts = modelCosts,
            };
        }).ToList();

        _context.StorageService
            .GetServiceItemsAsync(Arg.Any<bool>())
            .Returns(_serviceItems);
    }

    [Given("service item {string} has eligibility conditions:")]
    public void GivenServiceItemHasEligibilityConditions(string serviceItemId, DataTable dataTable)
    {
        var item = _serviceItems.SingleOrDefault(x => x.IntegrationID == serviceItemId);
        if (item is null)
            throw new ReqnrollException($"Service item '{serviceItemId}' was not configured.");

        item.EligibilityConditions = dataTable.Rows.Select(row =>
        {
            var hasScope =
                (row.ContainsKey("Selection") && !string.IsNullOrWhiteSpace(row["Selection"])) ||
                (row.ContainsKey("Count") && !string.IsNullOrWhiteSpace(row["Count"]));
            var condition = new EligibilityConditionModel
            {
                Field = row["Field"],
                Operator = Enum.Parse<EligibilityConditionOperator>(row["Operator"]),
                Scope = hasScope ? new EligibilityConditionScope
                {
                    Selection = row.ContainsKey("Selection") && !string.IsNullOrWhiteSpace(row["Selection"])
                        ? Enum.Parse<EligibilityConditionSelection>(row["Selection"])
                        : default,
                    Count = row.ContainsKey("Count") && !string.IsNullOrWhiteSpace(row["Count"])
                        ? int.Parse(row["Count"])
                        : null,
                } : null,
                Values = GetEligibilityConditionValues(row),
            };

            if (row.ContainsKey("ValueMatch") && !string.IsNullOrWhiteSpace(row["ValueMatch"]))
                condition.ValueMatch = Enum.Parse<EligibilityConditionValueMatch>(row["ValueMatch"]);

            if (HasValue(row, "WhenUnmet"))
                condition.WhenUnmet = Enum.Parse<EligibilityConditionUnmetBehavior>(row["WhenUnmet"]);

            condition.Program = GetOptionalConditionList(row, "Program", "ProgramJson");

            // Any of the three columns brings the qualifier into being, so a scenario can pin the
            // selection, its values, or both — and leaving all three out is how a scenario says the
            // author omitted the qualifier altogether.
            if (HasValue(row, "Qualifier") ||
                HasValue(row, "QualifierValues") ||
                HasValue(row, "QualifierValuesJson"))
            {
                condition.Qualifier = new EligibilityConditionQualifier
                {
                    Selection = HasValue(row, "Qualifier")
                        ? Enum.Parse<EligibilityConditionQualifierSelection>(row["Qualifier"])
                        : default,
                    Values = GetOptionalConditionList(row, "QualifierValues", "QualifierValuesJson"),
                };
            }

            return condition;
        }).ToList();
    }

    [Given("LookupOptions has broker stock lookup enabled")]
    public void GivenLookupOptionsHasBrokerStockLookupEnabled()
    {
        _context.Options.LookupBrokerStock = true;
    }
}
