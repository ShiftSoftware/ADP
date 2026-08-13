using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using NSubstitute;
using System.Text.Json;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class WarrantyDateStepDefinitions
{
    private readonly Support.TestContext _context;
    private VehicleWarrantyDTO? _result;
    private VehicleLookupDTO? _lookupResult;

    public WarrantyDateStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("the sale has a broker without invoice")]
    public void GivenTheSaleHasABrokerWithoutInvoice()
    {
        _context.SaleInformation = new VehicleSaleInformation
        {
            Broker = new VehicleBrokerSaleInformation
            {
                BrokerName = "Test Broker",
                InvoiceDate = null,
            },
        };
    }

    [Given("the sale has a broker with invoice date {string}")]
    public void GivenTheSaleHasABrokerWithInvoiceDate(string invoiceDate)
    {
        _context.SaleInformation = new VehicleSaleInformation
        {
            Broker = new VehicleBrokerSaleInformation
            {
                BrokerName = "Test Broker",
                InvoiceDate = DateTime.Parse(invoiceDate),
            },
        };
    }

    [When("evaluating warranty dates for {string}")]
    public async Task WhenEvaluatingWarrantyDatesFor(string vin)
    {
        _context.Aggregate.VIN = vin;

        var vehicle = new VehicleEntryEvaluator(_context.Aggregate, _context.Options).Evaluate();
        _context.CurrentVehicle = vehicle;

        // Build VehicleSaleInformation from the selected vehicle entry
        // (In production, VehicleSaleInformationEvaluator does this — Phase 4)
        var saleInfo = _context.SaleInformation ?? new VehicleSaleInformation
        {
            InvoiceDate = vehicle?.InvoiceDate,
            WarrantyActivationDate = vehicle?.WarrantyActivationDate,
        };

        _result = await new WarrantyAndFreeServiceDateEvaluator(_context.Aggregate, _context.Options)
            .EvaluateAsync(
                vehicle!,
                saleInfo,
                ignoreBrokerStock: false,
                languageCode: "en",
                serviceProvider: _context.ServiceProvider);
    }

    [When("looking up warranty details for {string}")]
    public async Task WhenLookingUpWarrantyDetailsFor(string vin)
    {
        _context.Aggregate.VIN = vin;
        _context.StorageService.GetAggregatedCompanyData(vin).Returns(_context.Aggregate);
        _context.StorageService.GetServiceItemsAsync(Arg.Any<bool>()).Returns([]);

        var service = new VehicleLookupService(
            _context.StorageService,
            _context.ServiceProvider,
            options: _context.Options);

        _lookupResult = await service.LookupAsync(vin, new VehicleLookupRequestOptions
        {
            LanguageCode = "en",
        });
        _result = _lookupResult.Warranty;
    }

    [Given("extended warranty definitions:")]
    public void GivenExtendedWarrantyDefinitions(DataTable dataTable)
    {
        _context.Options.ExtendedWarrantyDefinitions = dataTable.Rows.Select(row =>
            new ExtendedWarrantyDefinitionModel
            {
                ID = GetOptionalString(row, "ID"),
                ProviderCompanyID = GetOptionalLong(row, "ProviderCompanyID"),
                ActiveFor = GetOptionalInt(row, "ActiveFor"),
                ActiveForDurationType = row.ContainsKey("DurationType") &&
                    !string.IsNullOrWhiteSpace(row["DurationType"])
                        ? Enum.Parse<DurationType>(row["DurationType"])
                        : null,
            }).ToList();
    }

    [Given("extended warranty definition {string} has eligibility conditions:")]
    public void GivenExtendedWarrantyDefinitionHasEligibilityConditions(
        string definitionId,
        DataTable dataTable)
    {
        var definition = _context.Options.ExtendedWarrantyDefinitions
            .SingleOrDefault(item => item.ID == definitionId);
        if (definition is null)
            throw new ReqnrollException($"Extended warranty definition '{definitionId}' was not configured.");

        definition.EligibilityConditions = dataTable.Rows.Select(row =>
        {
            var hasScope =
                (row.ContainsKey("Selection") && !string.IsNullOrWhiteSpace(row["Selection"])) ||
                (row.ContainsKey("Count") && !string.IsNullOrWhiteSpace(row["Count"]));

            var condition = new ServiceItemEligibilityConditionModel
            {
                Field = row["Field"],
                Operator = Enum.Parse<ServiceItemEligibilityConditionOperator>(row["Operator"]),
                Scope = hasScope ? new ServiceItemEligibilityConditionScope
                {
                    Selection = row.ContainsKey("Selection") && !string.IsNullOrWhiteSpace(row["Selection"])
                        ? Enum.Parse<ServiceItemEligibilityConditionSelection>(row["Selection"])
                        : default,
                    Count = GetOptionalInt(row, "Count"),
                } : null,
                Values = row.ContainsKey("ValuesJson")
                    ? JsonSerializer.Deserialize<string[]>(row["ValuesJson"]) ?? []
                    : row["Values"].Split(
                        ',',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            };

            if (row.ContainsKey("ValueMatch") && !string.IsNullOrWhiteSpace(row["ValueMatch"]))
                condition.ValueMatch = Enum.Parse<ServiceItemEligibilityConditionValueMatch>(row["ValueMatch"]);

            return condition;
        }).ToList();
    }

    [Given("company logos resolve as:")]
    public void GivenCompanyLogosResolveAs(DataTable dataTable)
    {
        var logos = dataTable.Rows.ToDictionary(
            row => long.Parse(row["CompanyID"]),
            row => row["Logo"]);

        _context.Options.CompanyLogoResolver = model =>
        {
            var logo = model.Value is { } companyId && logos.TryGetValue(companyId, out var value)
                ? value
                : null;
            return new ValueTask<string?>(logo);
        };
    }

    [Then("the warranty start date is {string}")]
    public void ThenTheWarrantyStartDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.WarrantyStartDate);
    }

    [Then("the warranty start date is empty")]
    public void ThenTheWarrantyStartDateIsEmpty()
    {
        Assert.NotNull(_result);
        Assert.Null(_result.WarrantyStartDate);
    }

    [Then("the warranty end date is {string}")]
    public void ThenTheWarrantyEndDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.WarrantyEndDate);
    }

    [Then("the extended warranty start date is {string}")]
    public void ThenTheExtendedWarrantyStartDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.ExtendedWarrantyStartDate);
    }

    [Then("the extended warranty end date is {string}")]
    public void ThenTheExtendedWarrantyEndDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.ExtendedWarrantyEndDate);
    }

    [Then("the vehicle has extended warranty")]
    public void ThenTheVehicleHasExtendedWarranty()
    {
        Assert.NotNull(_result);
        Assert.True(_result.HasExtendedWarranty);
    }

    [Then("the vehicle does not have extended warranty")]
    public void ThenTheVehicleDoesNotHaveExtendedWarranty()
    {
        Assert.NotNull(_result);
        Assert.False(_result.HasExtendedWarranty);
    }

    [Then("there are {int} extended warranties")]
    public void ThenThereAreExtendedWarranties(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.ExtendedWarranties.Count);
    }

    [Then("extended warranties are:")]
    public void ThenExtendedWarrantiesAre(DataTable dataTable)
    {
        Assert.NotNull(_result);
        Assert.Equal(dataTable.Rows.Count, _result.ExtendedWarranties.Count);

        foreach (var row in dataTable.Rows)
        {
            var warranty = _result.ExtendedWarranties.SingleOrDefault(item => item.ID == row["ID"]);
            Assert.NotNull(warranty);
            Assert.Equal(GetOptionalString(row, "ProviderCompanyID"), warranty.ProviderCompanyID);
            Assert.Equal(GetOptionalString(row, "ProviderCompanyLogo"), warranty.ProviderCompanyLogo);
            Assert.Equal(GetOptionalDate(row, "StartDate"), warranty.StartDate);
            Assert.Equal(GetOptionalDate(row, "EndDate"), warranty.EndDate);
        }
    }

    [Then("the free service start date is {string}")]
    public void ThenTheFreeServiceStartDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.FreeServiceStartDate);
    }

    [Then("the free service start date is empty")]
    public void ThenTheFreeServiceStartDateIsEmpty()
    {
        Assert.NotNull(_result);
        Assert.Null(_result.FreeServiceStartDate);
    }

    [Then("the de facto service start date is {string}")]
    public void ThenTheDeFactoServiceStartDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.DeFactoServiceStartDate);
    }

    [Then("the de facto service start date is empty")]
    public void ThenTheDeFactoServiceStartDateIsEmpty()
    {
        Assert.NotNull(_result);
        Assert.Null(_result.DeFactoServiceStartDate);
    }

    private static string? GetOptionalString(DataTableRow row, string column)
    {
        if (!row.ContainsKey(column))
            return null;
        var value = row[column];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static long? GetOptionalLong(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : long.Parse(value);
    }

    private static int? GetOptionalInt(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : int.Parse(value);
    }

    private static DateTime? GetOptionalDate(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : DateTime.Parse(value);
    }
}
