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
    public void Missing_type_and_foreign_field_scope_fail_closed()
    {
        var evaluator = new VehicleEligibilityConditionEvaluator(new() { VIN = "VIN" }, new());
        var rule = Rule(); rule.Scope.InspectionTypeID = null;
        Assert.False(evaluator.MatchesAll([rule]));
        Assert.False(EligibilityConditionGroup.IsValid(new() { AnyOf = [rule] }));
        rule = Rule(); rule.Field = "serviceHistory.laborLines.packageCode";
        Assert.False(evaluator.MatchesAll([rule]));
        Assert.False(EligibilityConditionGroup.IsValid(new() { AnyOf = [rule] }));
    }
}
