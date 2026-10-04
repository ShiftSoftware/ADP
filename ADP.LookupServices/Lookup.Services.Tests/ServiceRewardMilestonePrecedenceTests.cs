using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class ServiceRewardMilestonePrecedenceTests
{
    public static IEnumerable<object[]> PassedRewards()
    {
        foreach (var cap in new[] { 40000, 60000, 80000 })
        foreach (var reverse in new[] { false, true })
        foreach (var hasFirst in new[] { false, true })
        foreach (var beyondReward in new[] { 0, 15000 })
            yield return new object[] { cap, reverse, hasFirst, beyondReward };
    }

    [Theory]
    [MemberData(nameof(PassedRewards))]
    public void Reward_or_higher_service_misses_reward_without_all_prerequisites(
        int cap, bool reverse, bool hasFirst, int beyondReward)
    {
        var lines = new List<OrderLaborLineModel>();
        if (hasFirst)
            lines.Add(Line("FIRST", $"PGM {(cap + 5000) / 1000}K"));
        lines.Add(Line("PASSED", $"PGM {(cap + 15000 + beyondReward) / 1000}K"));
        var outcome = Evaluator(lines).Evaluate(Conditions(cap, reverse), cap);

        Assert.Equal(EligibilityConditionState.Missed, outcome.State);
        Assert.Equal(hasFirst, outcome.Prerequisites[0].Satisfied);
        Assert.False(outcome.Prerequisites[1].Satisfied);
        Assert.Null(outcome.UnlockedOn);
        Assert.NotNull(outcome.ToLockDTO());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("OIL SERVICE")]
    [InlineData("PGM 5K")]
    [InlineData("PGM 45K")]
    [InlineData("PGM 50K")]
    [InlineData("OTHER 55K")]
    [InlineData("PGM 55K EXCLUDED")]
    public void Missing_or_nonqualifying_maximum_does_not_override_lock(string? package)
    {
        foreach (var reverse in new[] { false, true })
        {
            // A high odometer is deliberately irrelevant to the package-based decision.
            var lines = package is null ? new List<OrderLaborLineModel>() : new() { Line("ONLY", package) };
            Assert.Equal(EligibilityConditionState.Locked,
                Evaluator(lines).Evaluate(Conditions(40000, reverse), 40000).State);
        }
    }

    [Fact]
    public void Incomplete_reward_invoice_does_not_prove_the_ceiling_was_passed()
    {
        var line = Line("INCOMPLETE", "PGM 55K");
        line.NumberOfPartLines = 1;
        Assert.Equal(EligibilityConditionState.Locked,
            Evaluator(new() { line }).Evaluate(Conditions(), 40000).State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Wrong_schedule_cap_still_hides_a_reward_with_a_higher_service(bool reverse)
    {
        var conditions = Conditions(40000, reverse);
        conditions.Add(new()
        {
            Field = VehicleEligibilityConditionEvaluator.BaseScheduleMaximumMileageField,
            Operator = EligibilityConditionOperator.Equals,
            Values = new[] { "60000" },
            WhenUnmet = EligibilityConditionUnmetBehavior.Hide,
        });
        if (reverse)
            conditions.Reverse();
        Assert.Equal(EligibilityConditionState.Hidden,
            Evaluator(new() { Line("PASSED", "PGM 55K") }).Evaluate(conditions, 40000).State);
    }

    [Fact]
    public void Malformed_maximum_clause_cannot_prove_a_passed_ceiling()
    {
        var conditions = Conditions();
        conditions[1].Operator = EligibilityConditionOperator.ContainsAll;
        Assert.Equal(EligibilityConditionState.Locked,
            Evaluator(new() { Line("PASSED", "PGM 55K") }).Evaluate(conditions, 40000).State);
    }

    [Fact]
    public void An_unrelated_miss_clause_does_not_override_missing_prerequisites()
    {
        var conditions = Conditions();
        conditions[1] = new()
        {
            Field = VehicleEligibilityConditionEvaluator.ServiceHistoryPackageCodeField,
            Operator = EligibilityConditionOperator.ContainsAll,
            Scope = new() { Selection = EligibilityConditionSelection.All },
            Values = new[] { "ABSENT" },
            WhenUnmet = EligibilityConditionUnmetBehavior.Miss,
        };
        Assert.Equal(EligibilityConditionState.Locked,
            Evaluator(new() { Line("PASSED", "PGM 55K") }).Evaluate(conditions, 40000).State);
    }

    [Fact]
    public void Reusing_evaluator_does_not_carry_a_passed_ceiling_to_another_reward()
    {
        var evaluator = Evaluator(new() { Line("PASSED", "PGM 55K") });
        Assert.Equal(EligibilityConditionState.Missed, evaluator.Evaluate(Conditions(), 40000).State);
        Assert.Equal(EligibilityConditionState.Locked, evaluator.Evaluate(Conditions(60000), 60000).State);
    }

    [Fact]
    public void Qualifier_any_accepts_a_qualified_reward_service()
    {
        var conditions = Conditions();
        foreach (var condition in conditions)
            condition.Qualifier.Selection = EligibilityConditionQualifierSelection.Any;
        Assert.Equal(EligibilityConditionState.Missed,
            Evaluator(new() { Line("PASSED", "PGM 55K KS") }).Evaluate(conditions, 40000).State);
    }

    private static VehicleEligibilityConditionEvaluator Evaluator(List<OrderLaborLineModel> lines)
    {
        var options = new LookupOptions();
        options.ServiceMilestones.Conventions.Add(new()
        {
            Name = "scheduled",
            Pattern = @"^(?<program>PGM|OTHER) (?<milestone>[0-9]+)K(?: (?<qualifier>[A-Z]+))?$",
        });
        return new(new CompanyDataAggregateModel { LaborLines = lines }, options);
    }

    private static OrderLaborLineModel Line(string invoice, string package) => new()
    {
        CompanyID = 1, BranchID = 10, InvoiceNumber = invoice, OrderDocumentNumber = "JOB-" + invoice,
        PackageCode = package, InvoiceDate = new DateTime(2026, 2, 1), Odometer = 999999,
    };

    private static List<EligibilityConditionModel> Conditions(int cap = 40000, bool reverse = false)
    {
        var conditions = new List<EligibilityConditionModel>
        {
            new()
            {
                Field = VehicleEligibilityConditionEvaluator.ServiceHistoryPackageCodeField,
                Operator = EligibilityConditionOperator.ContainsAll,
                ValueMatch = EligibilityConditionValueMatch.Milestone,
                Program = new[] { "PGM" },
                Qualifier = new() { Selection = EligibilityConditionQualifierSelection.None },
                Scope = new() { Selection = EligibilityConditionSelection.All },
                Values = new[] { (cap + 5000).ToString(), (cap + 10000).ToString() },
                WhenUnmet = EligibilityConditionUnmetBehavior.Lock,
            },
            new()
            {
                Field = VehicleEligibilityConditionEvaluator.ServiceHistoryMaximumMilestoneField,
                Operator = EligibilityConditionOperator.Equals,
                Program = new[] { "PGM" },
                Qualifier = new() { Selection = EligibilityConditionQualifierSelection.None },
                Values = new[] { (cap + 10000).ToString() },
                WhenUnmet = EligibilityConditionUnmetBehavior.Miss,
            },
        };
        if (reverse)
            conditions.Reverse();
        return conditions;
    }
}
