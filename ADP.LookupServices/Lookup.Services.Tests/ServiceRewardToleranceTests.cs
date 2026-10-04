using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class ServiceRewardToleranceTests
{
    private static readonly DateTime Start = new(2026, 1, 1);
    internal static OrderLaborLineModel Line(string job, string? package, int day, string labor = "MAINT") => new()
    {
        VIN = "SYNTHETIC-VEHICLE", CompanyID = 1, BranchID = 10, OrderDocumentNumber = job,
        InvoiceNumber = "INV-" + job, LineID = "LINE-" + job, InvoiceDate = Start.AddDays(day),
        PackageCode = package!, LaborCode = labor, Odometer = 999999,
    };

    internal static LookupOptions Options(RewardHighPackageBehavior behavior = RewardHighPackageBehavior.ImmediateMiss)
    {
        var options = new LookupOptions
        {
            ServiceRewardTolerance = new()
            {
                StartsBefore = new(2026, 8, 10), ServiceItemIDs = ["reward"], TerminalPrograms = ["BASE"],
                HighPackageBehavior = behavior, IsAdditionalQualifyingWork = l => l.LaborCode == "OIL",
            },
        };
        options.ServiceMilestones.Conventions.Add(new()
        {
            Pattern = @"^(?<program>BASE|PGM|OTHER) (?<milestone>[0-9]+)K(?: (?<qualifier>[A-Z]+))?$",
        });
        return options;
    }

    internal static ServiceItemModel Reward(int cap = 40000) => new()
    {
        IntegrationID = "reward", ProgramRole = ServiceItemProgramRole.Reward, MaximumMileage = cap + 15000,
        EligibilityConditions = new List<EligibilityConditionModel>
        {
            new()
            {
                Field = VehicleEligibilityConditionEvaluator.BaseScheduleMaximumMileageField,
                Operator = EligibilityConditionOperator.Equals, Values = [cap.ToString()],
                WhenUnmet = EligibilityConditionUnmetBehavior.Hide,
            },
            new()
            {
                Field = VehicleEligibilityConditionEvaluator.ServiceHistoryPackageCodeField,
                Operator = EligibilityConditionOperator.ContainsAll, ValueMatch = EligibilityConditionValueMatch.Milestone,
                Scope = new() { Selection = EligibilityConditionSelection.All }, Program = ["PGM"],
                Qualifier = new() { Selection = EligibilityConditionQualifierSelection.Any },
                Values = [(cap + 5000).ToString(), (cap + 10000).ToString()],
                WhenUnmet = EligibilityConditionUnmetBehavior.Lock,
            },
            new()
            {
                Field = VehicleEligibilityConditionEvaluator.ServiceHistoryMaximumMilestoneField,
                Operator = EligibilityConditionOperator.Equals, Values = [(cap + 10000).ToString()], Program = ["PGM"],
                Qualifier = new() { Selection = EligibilityConditionQualifierSelection.Any },
                WhenUnmet = EligibilityConditionUnmetBehavior.Miss,
            },
        },
    };

    private static EligibilityConditionOutcome Evaluate(List<OrderLaborLineModel> lines, int cap = 40000,
        RewardHighPackageBehavior behavior = RewardHighPackageBehavior.ImmediateMiss)
    {
        var options = Options(behavior);
        var result = new VehicleEligibilityConditionEvaluator(new() { LaborLines = lines }, options)
            .EvaluateTolerance(Reward(cap), cap, Start, options.ServiceRewardTolerance, out var diagnostic);
        Assert.NotNull(result);
        Assert.Contains("Historical visit policy:", diagnostic);
        return result;
    }

    public static IEnumerable<object[]> Sequences()
    {
        foreach (var cap in new[] { 40000, 60000, 80000 })
        foreach (var mode in Enum.GetValues<RewardHighPackageBehavior>())
        foreach (var count in new[] { 0, 1, 2, 3 })
            yield return [cap, mode, count];
    }

    [Theory, MemberData(nameof(Sequences))]
    public void First_two_distinct_jobs_satisfy_expected_labels_and_third_misses(int cap, RewardHighPackageBehavior mode, int count)
    {
        var lines = new List<OrderLaborLineModel> { Line("terminal", $"BASE {cap / 1000}K", 0) };
        for (var i = 1; i <= count; i++) lines.Add(Line("return" + i, "PGM 5K", i));
        var result = Evaluate(lines, cap, mode);
        Assert.Equal(count < 2 ? EligibilityConditionState.Locked : count == 2 ? EligibilityConditionState.Met : EligibilityConditionState.Missed, result.State);
        Assert.Equal(new long[] { cap + 5000, cap + 10000 }, result.Prerequisites.Select(p => p.Mileage));
        Assert.Equal(Math.Min(count, 2), result.Prerequisites.Count(p => p.Satisfied));
        Assert.All(result.Prerequisites.Where(p => p.Satisfied), p => Assert.Equal("PGM 5K", p.Evidence.PackageCode));
        Assert.Equal(count == 2 ? Start.AddDays(2) : (DateTime?)null, result.UnlockedOn);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void High_package_setting_only_changes_tolerant_sequence(int highPosition, int count)
    {
        foreach (var cap in new[] { 40000, 60000, 80000 })
        {
            var lines = new List<OrderLaborLineModel> { Line("terminal", $"BASE {cap / 1000}K", 0) };
            for (var i = 1; i <= count; i++) lines.Add(Line("return" + i, i == highPosition ? $"PGM {(cap + 15000) / 1000}K" : "PGM 5K", i));
            Assert.Equal(EligibilityConditionState.Missed, Evaluate(lines, cap).State);
            Assert.Equal(count < 2 ? EligibilityConditionState.Locked : count == 2 ? EligibilityConditionState.Met : EligibilityConditionState.Missed,
                Evaluate(lines, cap, RewardHighPackageBehavior.CountInSequence).State);
            var strict = new VehicleEligibilityConditionEvaluator(new() { LaborLines = lines }, Options(RewardHighPackageBehavior.CountInSequence));
            Assert.Equal(EligibilityConditionState.Missed, strict.Evaluate(Reward(cap).EligibilityConditions, cap).State);
        }
    }

    [Fact]
    public void Split_invoices_and_later_records_for_one_job_count_once_using_first_invoice_date()
    {
        var first = Line("one", "PGM 55K", 10);
        var later = Line("one", "PGM 55K", 100);
        later.InvoiceNumber = "LATER";
        var ancillary = Line("two", null, 30, "WASH");
        var oil = Line("two", null, 32, "OIL");
        oil.InvoiceNumber = "OIL-INVOICE";
        var result = Evaluate([Line("terminal", "BASE 40K", 0), later, oil, first, ancillary],
            behavior: RewardHighPackageBehavior.CountInSequence);
        Assert.Equal(EligibilityConditionState.Met, result.State);
        Assert.Equal(Start.AddDays(30), result.UnlockedOn);
        Assert.Equal(Start.AddDays(32), result.Prerequisites[1].Evidence.InvoiceDate);
        Assert.Equal("OIL", result.Prerequisites[1].Evidence.LaborCode);
    }

    [Fact]
    public void Oil_qualifies_with_blank_package_and_ancillary_work_does_not_veto_or_qualify()
    {
        var lines = new List<OrderLaborLineModel>
        {
            Line("terminal", "BASE 40K", 0), Line("oil1", "OIL-MENU", 10, "OIL"),
            Line("oil1", "ADDITIVE", 10, "ADDITIVE"), Line("oil2", null, 20, "OIL"),
            Line("wash", null, 30, "WASH"), Line("treatment", "ADDITIVE44K", 40, "ADDITIVE"),
        };
        lines.Last().ServiceCode = "EOF";
        lines.Last().ServiceDescription = "Oil and filter replacement";
        Assert.Equal(EligibilityConditionState.Met, Evaluate(lines).State);
        lines.Add(Line("oil3", "OIL-MENU", 50, "OIL"));
        Assert.Equal(EligibilityConditionState.Missed, Evaluate(lines).State);
    }

    [Fact]
    public void Boundary_uses_terminal_package_invoice_and_does_not_need_a_claim()
    {
        var lines = new List<OrderLaborLineModel>
        {
            Line("before", "PGM 45K", -1), Line("terminal", "BASE 40K", 0),
            Line("same-date", "PGM 45K", 0), Line("one", "PGM 45K", 80), Line("two", "PGM 45K", 200),
        };
        var aggregate = new CompanyDataAggregateModel { LaborLines = lines };
        var options = Options();
        var evaluator = new VehicleEligibilityConditionEvaluator(aggregate, options);
        var result = evaluator.EvaluateTolerance(Reward(), 40000, Start, options.ServiceRewardTolerance, out _);
        Assert.Equal(Start.AddDays(200), result.UnlockedOn);
        aggregate.ItemClaims.Add(new() { ServiceItemID = "terminal", ClaimDate = Start.AddDays(80), PackageCode = null });
        var claimed = evaluator.EvaluateTolerance(Reward(), 40000, Start, options.ServiceRewardTolerance, out _);
        Assert.Equal(result.UnlockedOn, claimed.UnlockedOn);
        Assert.Equal(Start.AddDays(80), claimed.Prerequisites[0].SatisfiedOn);
    }

    [Theory]
    [InlineData("2026-08-09", true)]
    [InlineData("2026-08-10", false)]
    [InlineData("2026-08-11", false)]
    [InlineData(null, false)]
    public void Cohort_cutoff_is_exclusive_and_does_not_limit_return_dates(string? start, bool applies)
    {
        var options = Options();
        var evaluator = new VehicleEligibilityConditionEvaluator(new() { LaborLines =
            [Line("terminal", "BASE 40K", 0), Line("one", "PGM 5K", 500), Line("two", "PGM 5K", 600)] }, options);
        var result = evaluator.EvaluateTolerance(Reward(), 40000, start is null ? null : DateTime.Parse(start), options.ServiceRewardTolerance, out _);
        Assert.Equal(applies, result is not null);
        if (applies) Assert.Equal(Start.AddDays(600), result!.UnlockedOn);
    }

    [Fact]
    public void Same_job_number_in_different_branches_is_distinct_without_host_confirmation()
    {
        var second = Line("one", "PGM 5K", 20);
        second.BranchID = 11;
        Assert.Equal(EligibilityConditionState.Met, Evaluate([Line("terminal", "BASE 40K", 0), Line("one", "PGM 5K", 10), second]).State);
    }

    [Fact]
    public void Incomplete_invoice_and_unrecognized_program_do_not_qualify()
    {
        var incomplete = Line("incomplete", "PGM 55K", 20);
        incomplete.NumberOfPartLines = 1;
        var result = Evaluate([Line("terminal", "BASE 40K", 0), Line("one", "PGM 5K", 10), incomplete, Line("other", "OTHER 55K", 30)]);
        Assert.Equal(EligibilityConditionState.Locked, result.State);
        Assert.Single(result.Prerequisites.Where(p => p.Satisfied));
    }

    [Fact]
    public void Host_confirmed_duplicate_jobs_merge_standard_and_return_evidence_once()
    {
        var original = new List<OrderLaborLineModel>
        {
            Line("terminal", "BASE 40K", 0), Line("one", "PGM 5K", 10), Line("two", null, 20, "OIL"),
        };
        var copies = original.Select(l =>
        {
            var copy = Line(l.OrderDocumentNumber, l.PackageCode, (l.InvoiceDate!.Value - Start).Days, l.LaborCode);
            copy.CompanyID = 2;
            copy.BranchID = 20;
            return copy;
        });
        var options = Options();
        options.ServiceRewardTolerance!.AreDuplicateJobs = (a, b) =>
            a[0].VIN == b[0].VIN && a[0].OrderDocumentNumber == b[0].OrderDocumentNumber &&
            a[0].InvoiceNumber == b[0].InvoiceNumber && a[0].InvoiceDate == b[0].InvoiceDate;
        var evaluator = new VehicleEligibilityConditionEvaluator(new() { LaborLines = original.Concat(copies).ToList() }, options);
        var result = evaluator.EvaluateTolerance(Reward(), 40000, Start, options.ServiceRewardTolerance, out _);
        Assert.Equal(EligibilityConditionState.Met, result.State);
        Assert.Equal(Start.AddDays(20), result.UnlockedOn);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("conflicting")]
    [InlineData("undated")]
    [InlineData("identity")]
    public void Unusable_anchor_or_job_returns_a_diagnostic_instead_of_guessing(string problem)
    {
        var terminal = Line("terminal", "BASE 40K", 0);
        var lines = new List<OrderLaborLineModel> { terminal, Line("one", "PGM 5K", 10) };
        if (problem == "missing") lines.Remove(terminal);
        if (problem == "conflicting") lines.Add(Line("other-terminal", "BASE 40K", 5));
        if (problem == "undated") terminal.InvoiceDate = null;
        if (problem == "identity") lines[1].OrderDocumentNumber = null;
        var options = Options();
        var result = new VehicleEligibilityConditionEvaluator(new() { LaborLines = lines }, options)
            .EvaluateTolerance(Reward(), 40000, Start, options.ServiceRewardTolerance, out var diagnostic);
        Assert.Null(result);
        Assert.NotNull(diagnostic);
    }
}
