using Moq;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class NativeAuthorizationEligibilityTests
{
    private const string Vin = "SYNTHETIC-VEHICLE";
    private static readonly DateTime Start = new(2026, 1, 1);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Start.AddDays(10), TimeSpan.Zero); }
    private static EligibilityConditionModel Condition(bool expected) => new()
    {
        Field = "isAuthorized", Operator = EligibilityConditionOperator.Equals,
        Values = [expected ? "true" : "false"],
    };

    [Theory]
    [InlineData("none", false)]
    [InlineData("initial", true)]
    [InlineData("entry", true)]
    [InlineData("ssc", true)]
    [InlineData("other-vin", false)]
    public async Task Native_verdict_controls_items_online_and_in_bulk_without_relying_on_tags(string evidence, bool authorized)
    {
        var aggregate = new CompanyDataAggregateModel { VIN = Vin };
        if (evidence == "initial") aggregate.InitialOfficialVINs.Add(new() { VIN = Vin });
        if (evidence == "entry") aggregate.VehicleEntries.Add(new() { VIN = Vin });
        if (evidence is "ssc" or "other-vin") aggregate.SSCAffectedVINs.Add(new() { VIN = evidence == "ssc" ? Vin : "OTHER" });
        aggregate.VehicleInspections.Add(new() { id = "inspection", VIN = Vin, VehicleInspectionTypeID = 10, InspectionDate = Start.AddDays(5) });
        ServiceItemModel Item(bool expected) => new()
        {
            IntegrationID = expected ? "authorized" : "unauthorized", CampaignID = 500,
            CampaignStartDate = Start, CampaignEndDate = Start.AddYears(1), VehicleInspectionTypeID = 10,
            CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.VehicleInspection,
            CampaignActivationType = ClaimableItemCampaignActivationTypes.FirstTriggerOnly,
            ValidityMode = ClaimableItemValidityMode.RelativeToActivation, ActiveFor = 1, ActiveForDurationType = DurationType.Months,
            EligibilityConditions = [new() { AnyOf = [Condition(expected)] }],
        };
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(new List<ServiceItemModel> { Item(true), Item(false) });
        var options = new LookupOptions { TimeProvider = new Clock() };
        foreach (var misleadingTags in new[] { false, true })
        {
            // Even a contradictory display classification cannot override native evidence.
            options.VehicleClassificationResolver = misleadingTags
                ? _ => [new() { Code = authorized ? "unofficial" : "official", Label = "Display only" }]
                : null;
            var lookup = new VehicleLookupService(storage.Object, options: options);
            var online = await lookup.LookupAsync(Vin);
            var bulk = await lookup.LookupAsync(aggregate);
            Assert.Equal(authorized, online.IsAuthorized);
            Assert.Equal(authorized, bulk.IsAuthorized);
            Assert.Equal(authorized ? "authorized" : "unauthorized", Assert.Single(online.ServiceItems).ServiceItemID);
            Assert.Equal(online.ServiceItems.Select(x => x.ServiceItemID), bulk.ServiceItems.Select(x => x.ServiceItemID));
        }
        // Direct evaluator consumers use the same native evaluator when no precomputed verdict is passed.
        Assert.True(new VehicleEligibilityConditionEvaluator(aggregate, options).MatchesAll([Condition(authorized)]));
        Assert.False(new VehicleEligibilityConditionEvaluator(aggregate, options).MatchesAll([Condition(!authorized)]));
    }

    [Theory]
    [InlineData("", false, false)]
    [InlineData("unknown", false, false)]
    [InlineData("false", true, false)]
    [InlineData("false", false, true)]
    public void Invalid_authorization_conditions_fail_closed_including_inside_groups(string value, bool wrongOperator, bool scoped)
    {
        var condition = Condition(false);
        condition.Values = [value];
        if (wrongOperator) condition.Operator = EligibilityConditionOperator.ContainsAll;
        if (scoped) condition.Scope = new() { Selection = EligibilityConditionSelection.All };
        var aggregate = new CompanyDataAggregateModel { VIN = Vin };
        var evaluator = new VehicleEligibilityConditionEvaluator(aggregate, new());
        Assert.False(evaluator.MatchesAll([condition]));
        Assert.False(evaluator.MatchesAll([new() { AnyOf = [condition] }]));
    }
}
