using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class InspectionCompanyEligibilityTests
{
    private static EligibilityConditionModel Rule() => new()
    {
        Field = "vehicleInspections.companyID", Values = ["42"],
        Scope = new() { Selection = EligibilityConditionSelection.All, InspectionTypeID = 7 }
    };

    [Theory]
    [InlineData("VIN", 7, 42, false, true)]
    [InlineData("OTHER", 7, 42, false, false)]
    [InlineData("VIN", 8, 42, false, false)]
    [InlineData("VIN", 7, 43, false, false)]
    [InlineData("VIN", 7, 42, true, false)]
    public void Requires_same_vehicle_type_and_company(string vin, long type, long company, bool deleted, bool expected)
    {
        var aggregate = new CompanyDataAggregateModel { VIN = "VIN", VehicleInspections = [new VehicleInspectionModel
            { VIN = vin, VehicleInspectionTypeID = type, CompanyID = company, IsDeleted = deleted }] };
        var evaluator = new VehicleEligibilityConditionEvaluator(aggregate, new());
        Assert.Equal(expected, evaluator.MatchesAll([Rule()]));
        Assert.Equal(expected, evaluator.MatchesAll([new() { AnyOf = [Rule()] }]));
    }

    [Fact]
    public void Foreign_field_and_non_positive_type_scope_fail_closed()
    {
        var evaluator = new VehicleEligibilityConditionEvaluator(new() { VIN = "VIN" }, new(), campaignInspectionTypeID: 7);
        var rule = Rule(); rule.Field = "serviceHistory.laborLines.packageCode";
        Assert.False(evaluator.MatchesAll([rule]));
        Assert.False(EligibilityConditionGroup.IsValid(new() { AnyOf = [rule] }));
        rule = Rule(); rule.Scope.InspectionTypeID = 0;
        Assert.False(evaluator.MatchesAll([rule]));
        Assert.False(EligibilityConditionGroup.IsValid(new() { AnyOf = [rule] }));
    }

    [Theory]
    [InlineData(7L, 7, 42, true)]
    [InlineData(7L, 8, 42, false)]
    [InlineData(7L, 7, 43, false)]
    [InlineData(null, 7, 42, false)]
    public void Omitted_type_reads_the_campaigns_inspection_type(long? campaignType, long type, long company, bool expected)
    {
        var aggregate = new CompanyDataAggregateModel { VIN = "VIN", VehicleInspections = [new VehicleInspectionModel
            { VIN = "VIN", VehicleInspectionTypeID = type, CompanyID = company }] };
        var evaluator = new VehicleEligibilityConditionEvaluator(aggregate, new(), campaignInspectionTypeID: campaignType);
        var rule = Rule(); rule.Scope.InspectionTypeID = null;
        Assert.True(EligibilityConditionGroup.IsValid(new() { AnyOf = [rule] }));
        Assert.Equal(expected, evaluator.MatchesAll([rule]));
        Assert.Equal(expected, evaluator.MatchesAll([new() { AnyOf = [rule] }]));
    }

    [Fact]
    public void Explicit_type_wins_over_the_campaigns_type()
    {
        var aggregate = new CompanyDataAggregateModel { VIN = "VIN", VehicleInspections = [new VehicleInspectionModel
            { VIN = "VIN", VehicleInspectionTypeID = 7, CompanyID = 42 }] };
        var evaluator = new VehicleEligibilityConditionEvaluator(aggregate, new(), campaignInspectionTypeID: 9);
        Assert.True(evaluator.MatchesAll([Rule()]));
    }
}
