using LookupServices.BDD.Support;
using NSubstitute;
using Reqnroll;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class MockStorageStepDefinitions
{
    private readonly Support.TestContext _context;

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
        var items = dataTable.Rows.Select(row =>
        {
            var activationTrigger = row.ContainsKey("ActivationTrigger") && !string.IsNullOrWhiteSpace(row["ActivationTrigger"])
                ? Enum.Parse<ClaimableItemCampaignActivationTrigger>(row["ActivationTrigger"])
                : ClaimableItemCampaignActivationTrigger.WarrantyActivation;

            var validityMode = row.ContainsKey("ValidityMode") && !string.IsNullOrWhiteSpace(row["ValidityMode"])
                ? Enum.Parse<ClaimableItemValidityMode>(row["ValidityMode"])
                : ClaimableItemValidityMode.RelativeToActivation;

            var brandId = GetOptionalLong(row, "BrandID");

            return new ServiceItemModel
            {
                IntegrationID = GetOptionalString(row, "ServiceItemID"),
                Name = new Dictionary<string, string> { { "en", GetOptionalString(row, "Name") ?? "" } },
                IsDeleted = false,
                BrandIDs = brandId is not null ? new List<long?> { brandId } : null,
                CampaignStartDate = GetOptionalDate(row, "CampaignStartDate") ?? DateTime.MinValue,
                CampaignEndDate = GetOptionalDate(row, "CampaignEndDate") ?? DateTime.MaxValue,
                CampaignActivationTrigger = activationTrigger,
                ValidityMode = validityMode,
                ActiveFor = row.ContainsKey("ActiveForMonths") && !string.IsNullOrWhiteSpace(row["ActiveForMonths"])
                    ? int.Parse(row["ActiveForMonths"]) : null,
                ActiveForDurationType = row.ContainsKey("ActiveForMonths") && !string.IsNullOrWhiteSpace(row["ActiveForMonths"])
                    ? DurationType.Months : null,
                MaximumMileage = GetOptionalLong(row, "MaximumMileage"),
                PackageCode = GetOptionalString(row, "PackageCode"),
            };
        }).ToList();

        _context.StorageService
            .GetServiceItemsAsync(Arg.Any<bool>())
            .Returns(items);
    }

    [Given("LookupOptions has broker stock lookup enabled")]
    public void GivenLookupOptionsHasBrokerStockLookupEnabled()
    {
        _context.Options.LookupBrokerStock = true;
    }
}
