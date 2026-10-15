using Moq;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class CampaignMembershipTests
{
    private const string Vin = "SYNTHETIC-VEHICLE";
    private static readonly DateTime Start = new(2026, 1, 1);
    private static EligibilityConditionModel Membership() => new()
    {
        Field = "campaignVinEntries.campaignID",
        Operator = EligibilityConditionOperator.ContainsAll,
        Values = ["500"],
    };

    private static CompanyDataAggregateModel Aggregate(bool inspection, long? campaign) => new()
    {
        VIN = Vin,
        VehicleInspections = inspection ? [new()
        {
            id = "inspection", VIN = Vin, VehicleInspectionTypeID = 10, InspectionDate = Start.AddDays(5),
        }] : [],
        CampaignVinEntries = campaign is null ? [] : [new()
        {
            id = "membership", VIN = Vin, CampaignID = campaign, RecordedDate = Start,
        }],
    };

    private static ServiceItemModel Voucher(bool restricted) => new()
    {
        IntegrationID = restricted ? "target-voucher" : "ordinary-voucher", CampaignID = 500,
        CampaignStartDate = Start, CampaignEndDate = Start.AddYears(1), VehicleInspectionTypeID = 10,
        CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.VehicleInspection,
        CampaignActivationType = ClaimableItemCampaignActivationTypes.FirstTriggerOnly,
        ValidityMode = ClaimableItemValidityMode.RelativeToActivation,
        ActiveFor = 1, ActiveForDurationType = DurationType.Months, FixedCost = 25,
        EligibilityConditions = restricted ? [Membership()] : [],
    };

    private static async Task<List<VehicleServiceItemDTO>> Evaluate(CompanyDataAggregateModel aggregate)
    {
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<ServiceItemModel> { Voucher(false), Voucher(true) });
        var options = new LookupOptions { TimeProvider = new FixedClock() };
        var evaluator = new VehicleServiceItemEvaluator(storage.Object, aggregate, options, Mock.Of<IServiceProvider>());
        var result = await evaluator.Evaluate(null!, new VehicleOwnership(), null, "en");
        return result.serviceItems.ToList();
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, 500L, false)]
    [InlineData(true, null, false)]
    [InlineData(true, 501L, false)]
    [InlineData(true, 500L, true)]
    public async Task Target_voucher_requires_inspection_and_matching_membership(bool inspection, long? campaign, bool offered)
    {
        var items = await Evaluate(Aggregate(inspection, campaign));
        Assert.Equal(offered, items.Any(i => i.ServiceItemID == "target-voucher"));
        Assert.Equal(inspection, items.Any(i => i.ServiceItemID == "ordinary-voucher"));
        if (offered)
        {
            var target = Assert.Single(items, i => i.ServiceItemID == "target-voucher");
            Assert.True(target.Claimable);
            Assert.Equal("inspection", target.VehicleInspectionID);
            Assert.Null(target.CampaignVinEntryID);
            Assert.Equal(Start.AddDays(5), target.ActivatedAt);
            Assert.Equal(Start.AddDays(5).AddMonths(1), target.ExpiresAt);
        }
    }

    [Theory]
    [InlineData("deleted")]
    [InlineData("different-vin")]
    [InlineData("missing-vin")]
    [InlineData("missing-list")]
    public async Task Unusable_membership_does_not_offer_target_voucher(string problem)
    {
        var aggregate = Aggregate(true, 500);
        if (problem == "deleted") aggregate.CampaignVinEntries[0].IsDeleted = true;
        if (problem == "different-vin") aggregate.CampaignVinEntries[0].VIN = "ANOTHER-VEHICLE";
        if (problem == "missing-vin") aggregate.CampaignVinEntries[0].VIN = null!;
        if (problem == "missing-list") aggregate.CampaignVinEntries = null!;
        Assert.DoesNotContain(await Evaluate(aggregate), i => i.ServiceItemID == "target-voucher");
    }

    [Fact]
    public async Task Repeated_membership_keeps_one_inspection_entitlement_and_existing_claim_identity()
    {
        var aggregate = Aggregate(true, 500);
        aggregate.CampaignVinEntries.Add(new() { id = "replay", VIN = Vin, CampaignID = 500 });
        Assert.Single(await Evaluate(aggregate), i => i.ServiceItemID == "target-voucher");
        aggregate.ItemClaims.Add(new()
        {
            ServiceItemID = "target-voucher", VehicleInspectionID = "inspection",
            ClaimDate = Start.AddDays(7), InvoiceNumber = "CLAIM-1", Cost = 25,
        });
        var claimed = Assert.Single(await Evaluate(aggregate), i => i.ServiceItemID == "target-voucher");
        Assert.False(claimed.Claimable);
        Assert.Equal("processed", claimed.Status);
        Assert.Equal("CLAIM-1", claimed.InvoiceNumber);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(366, 10)]
    [InlineData(5, 11)]
    public async Task Membership_does_not_bypass_inspection_date_or_type(int days, long type)
    {
        var aggregate = Aggregate(true, 500);
        aggregate.VehicleInspections[0].InspectionDate = Start.AddDays(days);
        aggregate.VehicleInspections[0].VehicleInspectionTypeID = type;
        Assert.Empty(await Evaluate(aggregate));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("invalid-id")]
    [InlineData("zero")]
    [InlineData("scope")]
    [InlineData("operator")]
    [InlineData("suffix")]
    [InlineData("program")]
    [InlineData("qualifier")]
    public void Unsupported_condition_shapes_fail_closed(string problem)
    {
        var condition = Membership();
        if (problem == "empty") condition.Values = [];
        if (problem == "invalid-id") condition.Values = ["not-an-id"];
        if (problem == "zero") condition.Values = ["0"];
        if (problem == "scope") condition.Scope = new();
        if (problem == "operator") condition.Operator = EligibilityConditionOperator.Equals;
        if (problem == "suffix") condition.ValueMatch = EligibilityConditionValueMatch.EndsWith;
        if (problem == "program") condition.Program = ["BASE"];
        if (problem == "qualifier") condition.Qualifier = new();
        Assert.False(new VehicleEligibilityConditionEvaluator(Aggregate(true, 500), new LookupOptions()).MatchesAll([condition]));
    }

    [Fact]
    public void Every_requested_campaign_is_required()
    {
        var aggregate = Aggregate(true, 500);
        var condition = Membership();
        condition.Values = ["500", "501"];
        var evaluator = new VehicleEligibilityConditionEvaluator(aggregate, new LookupOptions());
        Assert.False(evaluator.MatchesAll([condition]));
        aggregate.CampaignVinEntries.Add(new() { VIN = Vin, CampaignID = 501 });
        Assert.True(evaluator.MatchesAll([condition]));
    }

    [Fact]
    public void Missing_aggregate_identity_cannot_establish_membership()
    {
        var aggregate = Aggregate(true, 500);
        aggregate.VIN = null!;
        Assert.False(new VehicleEligibilityConditionEvaluator(aggregate, new LookupOptions()).MatchesAll([Membership()]));
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Start.AddDays(10), TimeSpan.Zero);
    }
}
