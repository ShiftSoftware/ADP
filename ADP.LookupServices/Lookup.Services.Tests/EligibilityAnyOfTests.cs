using System.Text.Json;
using Moq;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class EligibilityAnyOfTests
{
    private const string Vin = "SYNTHETIC-VEHICLE";
    private static readonly DateTime Start = new(2026, 1, 1);
    private static EligibilityConditionModel Code() => new() { Field = "vehicleClassifications.code", Values = ["member"] };
    private static EligibilityConditionModel Target() => new() { Field = "campaignVinEntries.campaignID", Values = ["500"] };
    private static EligibilityConditionModel Group() => new() { AnyOf = [Code(), Target()] };

    [Theory]
    [InlineData(true, true, 0, 10, 1)]
    [InlineData(true, false, 1, 10, 1)]
    [InlineData(true, true, 1, 10, 1)]
    [InlineData(true, false, 0, 10, 0)]
    [InlineData(false, true, 0, 10, 0)]
    [InlineData(false, false, 1, 10, 1)]
    [InlineData(true, false, 2, 10, 0)]
    [InlineData(true, false, 3, 10, 0)]
    [InlineData(true, false, 4, 10, 0)]
    [InlineData(true, true, 1, 0, 0)]
    [InlineData(true, true, 1, 11, 0)]
    public async Task Alternatives_share_one_benefit_and_leave_inspection_in_charge_online_and_in_bulk(
        bool resolver, bool member, int membership, int inspectionType, int expected)
    {
        var aggregate = new CompanyDataAggregateModel { VIN = Vin };
        if (membership > 0) aggregate.CampaignVinEntries.Add(new() { VIN = membership == 4 ? "OTHER" : Vin,
            CampaignID = membership == 3 ? 999 : 500, IsDeleted = membership == 2 });
        if (inspectionType > 0) aggregate.VehicleInspections.Add(new() { id = "inspection", VIN = Vin,
            VehicleInspectionTypeID = inspectionType, InspectionDate = Start.AddDays(5) });
        var item = new ServiceItemModel { IntegrationID = "voucher", CampaignID = 500,
            CampaignStartDate = Start, CampaignEndDate = Start.AddYears(1), VehicleInspectionTypeID = 10,
            CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.VehicleInspection,
            CampaignActivationType = ClaimableItemCampaignActivationTypes.FirstTriggerOnly,
            ValidityMode = ClaimableItemValidityMode.RelativeToActivation, ActiveFor = 1, ActiveForDurationType = DurationType.Months,
            EligibilityConditions = [Group()] };
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(new List<ServiceItemModel> { item });
        var options = new LookupOptions { TimeProvider = new Clock() }; var calls = 0;
        if (resolver) options.VehicleClassificationResolver = _ => { calls++; return member ? [new() { Code = "member" }] : []; };
        var service = new VehicleLookupService(storage.Object, options: options);
        var online = await service.LookupAsync(Vin);
        var bulk = await service.LookupAsync(aggregate);
        Assert.Equal(expected, online.ServiceItems.Count());
        Assert.Equal(expected, bulk.ServiceItems.Count());
        Assert.Equal(resolver ? 2 : 0, calls);
        if (expected == 1)
        {
            var voucher = Assert.Single(online.ServiceItems);
            Assert.True(voucher.Claimable);
            Assert.Equal("inspection", voucher.VehicleInspectionID);
            Assert.Equal(Start.AddDays(5), voucher.ActivatedAt);
            Assert.Equal(Start.AddDays(5).AddMonths(1), voucher.ExpiresAt);
        }
    }

    [Theory]
    [InlineData(EligibilityConditionUnmetBehavior.Hide, (int)EligibilityConditionState.Hidden)]
    [InlineData(EligibilityConditionUnmetBehavior.Lock, (int)EligibilityConditionState.Locked)]
    [InlineData(EligibilityConditionUnmetBehavior.Miss, (int)EligibilityConditionState.Missed)]
    public void Group_owns_unmet_behavior_and_does_not_change_the_activation_clock(
        EligibilityConditionUnmetBehavior behavior, int state)
    {
        var group = Group(); group.WhenUnmet = behavior;
        var missing = new VehicleEligibilityConditionEvaluator(new() { VIN = Vin }, new());
        Assert.Equal((EligibilityConditionState)state, missing.Evaluate([group]).State);
        var met = new VehicleEligibilityConditionEvaluator(new() { VIN = Vin }, new(), classificationCodes: ["member"]);
        Assert.True(met.Evaluate([group]).IsMet);
        Assert.Null(met.Evaluate([group]).UnlockedOn);
    }

    [Fact]
    public void Top_level_AND_and_existing_unmet_precedence_remain_unchanged()
    {
        var evaluator = new VehicleEligibilityConditionEvaluator(new() { VIN = Vin }, new(), classificationCodes: ["member"]);
        Assert.False(evaluator.MatchesAll([Code(), Target()]));
        Assert.True(evaluator.MatchesAll([Group()]));
        Assert.False(evaluator.MatchesAll([Group(), Target()]));
        var locked = Target(); locked.WhenUnmet = EligibilityConditionUnmetBehavior.Lock;
        var missed = Target(); missed.WhenUnmet = EligibilityConditionUnmetBehavior.Miss;
        Assert.Equal(EligibilityConditionState.Locked, evaluator.Evaluate([Group(), locked, missed]).State);
        Assert.Equal(EligibilityConditionState.Hidden, evaluator.Evaluate([Group(), locked, Target()]).State);
    }

    [Theory]
    [InlineData("{\"anyOf\":[]}")]
    [InlineData("{\"anyOf\":null}")]
    [InlineData("""{"anyOf":null,"field":"vehicleClassifications.code","values":["member"]}""")]
    [InlineData("{\"anyOf\":[null]}")]
    [InlineData("{\"anyOf\":[{\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"]},{\"field\":\"unknown\",\"values\":[\"x\"]}]}")]
    [InlineData("{\"anyOf\":[{\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"],\"whenUnmet\":\"Lock\"}]}")]
    [InlineData("{\"anyOf\":[{\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"],\"operator\":99}]}")]
    [InlineData("{\"anyOf\":[{\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"]}],\"extra\":true}")]
    [InlineData("{\"anyOf\":[{\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"],\"typo\":true}]}")]
    [InlineData("{\"anyOf\":[{\"anyOf\":[{\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"]}]}]}")]
    [InlineData("{\"anyOf\":[{\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"]}],\"field\":\"vehicleClassifications.code\",\"values\":[\"member\"]}")]
    public void Malformed_groups_never_qualify_even_with_a_matching_branch(string json)
    {
        var evaluator = new VehicleEligibilityConditionEvaluator(new() { VIN = Vin }, new(), classificationCodes: ["member"]);
        var condition = JsonSerializer.Deserialize<EligibilityConditionModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(EligibilityConditionState.Hidden, evaluator.Evaluate([condition]).State);
        var cosmos = Newtonsoft.Json.JsonConvert.DeserializeObject<EligibilityConditionModel>(json)!;
        Assert.Equal(EligibilityConditionState.Hidden, evaluator.Evaluate([cosmos]).State);
        var copied = JsonSerializer.Deserialize<EligibilityConditionModel>(JsonSerializer.Serialize(condition))!;
        Assert.Equal(EligibilityConditionState.Hidden, evaluator.Evaluate([copied]).State);
    }

    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Start.AddDays(10), TimeSpan.Zero); }
}
