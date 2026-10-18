using Moq;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class VehicleClassificationTests
{
    private const string Vin = "SYNTHETIC-VEHICLE";
    private static readonly DateTime Start = new(2026, 1, 1);
    private static CompanyDataAggregateModel Aggregate() => new() { VIN = Vin, VehicleInspections = [new()
        { id = "inspection", VIN = Vin, VehicleInspectionTypeID = 10, InspectionDate = Start.AddDays(5) }] };
    private static ServiceItemModel Item(string id, string? code = null) => new()
    {
        IntegrationID = id, CampaignID = 500, CampaignStartDate = Start, CampaignEndDate = Start.AddYears(1),
        VehicleInspectionTypeID = 10, CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.VehicleInspection,
        CampaignActivationType = ClaimableItemCampaignActivationTypes.FirstTriggerOnly,
        ValidityMode = ClaimableItemValidityMode.RelativeToActivation, ActiveFor = 1, ActiveForDurationType = DurationType.Months,
        EligibilityConditions = code is null ? [] : [new() { Field = "vehicleClassifications.code",
            Operator = EligibilityConditionOperator.ContainsAll, Values = [code] }],
    };
    private static Mock<IVehicleLookupStorageService> Storage(CompanyDataAggregateModel aggregate)
    {
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(new List<ServiceItemModel>
            { Item("ordinary"), Item("first", "member"), Item("second", "member") });
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        return storage;
    }
    private static LookupOptions Options() => new() { TimeProvider = new FixedClock() };
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Start.AddDays(10), TimeSpan.Zero); }

    [Fact]
    public async Task No_resolver_keeps_ordinary_items_and_fails_required_codes_without_extra_storage_reads()
    {
        var aggregate = Aggregate(); var storage = Storage(aggregate);
        var evaluator = new VehicleServiceItemEvaluator(storage.Object, aggregate, Options(), Mock.Of<IServiceProvider>());
        var result = await evaluator.Evaluate(null!, new(), null, "en");
        Assert.Equal("ordinary", Assert.Single(result.serviceItems).ServiceItemID);
        storage.Verify(s => s.GetServiceItemsAsync(It.IsAny<bool>()), Times.Once);
        storage.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("member", "Different display label", 3)]
    [InlineData("other", "member", 1)]
    [InlineData("Member", "member", 1)]
    [InlineData("", "member", 1)]
    public async Task Resolve_once_and_match_only_exact_codes(string code, string label, int itemCount)
    {
        var aggregate = Aggregate(); var storage = Storage(aggregate); var options = Options(); var calls = 0;
        options.VehicleClassificationResolver = context => {
            calls++; Assert.Same(aggregate, context.Aggregate); Assert.False(context.IsAuthorized);
            Assert.Equal(3, context.ServiceItemCatalog.Count);
            return [new() { Code = code, Label = label }];
        };
        var result = await new VehicleLookupService(storage.Object, options: options).LookupAsync(aggregate);
        Assert.Equal(itemCount, result.ServiceItems.Count()); Assert.Equal(1, calls);
        storage.Verify(s => s.GetServiceItemsAsync(It.IsAny<bool>()), Times.Once);
        storage.Verify(s => s.GetAggregatedCompanyData(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Online_and_loaded_aggregate_paths_share_authorization_classifications_and_eligibility()
    {
        var aggregate = Aggregate(); aggregate.SSCAffectedVINs.Add(new() { VIN = Vin });
        var storage = Storage(aggregate); var options = Options(); var calls = 0;
        options.VehicleClassificationResolver = context => {
            calls++; Assert.True(context.IsAuthorized);
            return [new() { Code = "member", Label = "Display only" }];
        };
        var lookup = new VehicleLookupService(storage.Object, options: options);
        var online = await lookup.LookupAsync(Vin);
        var bulk = await lookup.LookupAsync(aggregate);
        Assert.Equal(2, calls); Assert.True(online.IsAuthorized); Assert.True(bulk.IsAuthorized);
        Assert.Equal("member", Assert.Single(online.Classifications).Code);
        Assert.Equal("Display only", Assert.Single(bulk.Classifications).Label);
        Assert.Equal(online.ServiceItems.Select(x => x.ServiceItemID), bulk.ServiceItems.Select(x => x.ServiceItemID));
        Assert.Equal(3, online.ServiceItems.Count());
    }

    [Theory]
    [InlineData(ConsistencyLevels.Strong, 1, 0)]
    [InlineData(ConsistencyLevels.Eventual, 3, 1)]
    public async Task Request_consistency_is_shared_by_classification_and_presentation(
        ConsistencyLevels requestLevel, int expectedItems, int displayedInvoices)
    {
        var aggregate = Aggregate();
        aggregate.LaborLines.Add(new() { VIN = Vin, CompanyID = 1, InvoiceNumber = "INVOICE", OrderDocumentNumber = "JOB",
            InvoiceDate = Start, NumberOfPartLines = 1 }); // Expected part is absent.
        var storage = Storage(aggregate); var options = Options(); var calls = 0;
        options.VehicleClassificationResolver = context => {
            calls++; Assert.Equal(requestLevel, context.ServiceHistoryConsistencyLevel);
            return context.ServiceHistory.Count > 0 ? [new() { Code = "member", Label = "Member" }] : [];
        };
        var result = await new VehicleLookupService(storage.Object, options: options).LookupAsync(Vin,
            new() { VehicleServiceHistoryConsistencyLevel = requestLevel });
        Assert.Equal(1, calls);
        Assert.Equal(expectedItems, result.ServiceItems.Count());
        Assert.Equal(displayedInvoices, result.ServiceHistory.Count());
    }

    [Fact]
    public async Task Conditions_remain_AND_and_classification_does_not_replace_inspection_activation()
    {
        var aggregate = Aggregate(); var storage = Storage(aggregate); var options = Options();
        options.VehicleClassificationResolver = _ => [new() { Code = "member", Label = "Member" }];
        var item = Item("restricted", "member");
        item.EligibilityConditions = item.EligibilityConditions.Append(new() { Field = "campaignVinEntries.campaignID",
            Operator = EligibilityConditionOperator.ContainsAll, Values = ["999"] });
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(new List<ServiceItemModel> { item });
        var lookup = new VehicleLookupService(storage.Object, options: options);
        Assert.Empty((await lookup.LookupAsync(aggregate)).ServiceItems);
        aggregate.CampaignVinEntries.Add(new() { VIN = Vin, CampaignID = 999 });
        aggregate.VehicleInspections.Clear();
        Assert.Empty((await lookup.LookupAsync(aggregate)).ServiceItems);
    }
}
