using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class ServiceMilestoneEvidenceTests
{
    private static EligibilityConditionOutcome Evaluate(params OrderLaborLineModel[] lines)
    {
        var options = new LookupOptions();
        options.ServiceMilestones.Conventions.Add(new ServiceCodeConvention
        {
            Name = "scheduled",
            Pattern = @"^(?<program>PGM) (?<milestone>[0-9]+)K$",
        });
        var aggregate = new CompanyDataAggregateModel { LaborLines = lines.ToList() };
        return new VehicleEligibilityConditionEvaluator(aggregate, options).Evaluate(new[]
        {
            new EligibilityConditionModel
            {
                Field = VehicleEligibilityConditionEvaluator.ServiceHistoryPackageCodeField,
                Operator = EligibilityConditionOperator.ContainsAll,
                ValueMatch = EligibilityConditionValueMatch.Milestone,
                Scope = new EligibilityConditionScope { Selection = EligibilityConditionSelection.All },
                Program = new[] { "PGM" },
                Qualifier = new EligibilityConditionQualifier { Selection = EligibilityConditionQualifierSelection.None },
                Values = new[] { "45000", "50000" },
                WhenUnmet = EligibilityConditionUnmetBehavior.Lock,
            },
        });
    }

    private static OrderLaborLineModel Line(string invoice, string package, DateTime? date, int? odometer = null) => new()
    {
        InvoiceNumber = invoice, OrderDocumentNumber = "JOB-" + invoice, LineID = "LINE-" + invoice,
        PackageCode = package, InvoiceDate = date, Odometer = odometer,
        CompanyID = 1, BranchID = 10, ServiceCode = "MAINT", LaborCode = "OIL",
        ServiceDescription = "Oil and filter replacement", JobDescription = "Periodic service",
    };

    [Fact]
    public void Keeps_the_earliest_exact_line_and_does_not_move_evidence_on_equal_dates()
    {
        var early = new DateTime(2026, 2, 1);
        var outcome = Evaluate(
            Line("LATE", "PGM 45K", early.AddDays(10), 45900),
            Line("FIRST", "PGM 45K", early, 45120),
            Line("TIED", "PGM 45K", early, 45200),
            Line("SECOND", "PGM 50K", early.AddMonths(1), 50300));

        Assert.True(outcome.IsMet);
        Assert.Null(outcome.ToLockDTO());
        Assert.Equal(2, outcome.Prerequisites.Count);
        var requirement = outcome.Prerequisites[0];
        Assert.Equal("45K", requirement.Label);
        Assert.Equal(early, requirement.SatisfiedOn);
        Assert.Equal("FIRST", requirement.Evidence!.InvoiceNumber);
        Assert.Equal("JOB-FIRST", requirement.Evidence.JobNumber);
        Assert.Equal("LINE-FIRST", requirement.Evidence.LineID);
        Assert.Equal(45120, requirement.Evidence.Odometer);
        Assert.Equal("PGM 45K", requirement.Evidence.PackageCode);
        Assert.Equal("Oil and filter replacement", requirement.Evidence.ServiceDescription);
        Assert.Equal("Periodic service", requirement.Evidence.JobDescription);
        Assert.Equal(early.AddMonths(1), outcome.UnlockedOn);
    }

    [Fact]
    public void Missing_requirement_has_no_evidence_and_a_nearby_odometer_does_not_satisfy_it()
    {
        var outcome = Evaluate(Line("OTHER", "PGM 5K", new DateTime(2026, 2, 1), 50000));
        Assert.False(outcome.IsMet);
        Assert.All(outcome.Prerequisites, requirement =>
        {
            Assert.False(requirement.Satisfied);
            Assert.Null(requirement.SatisfiedOn);
            Assert.Null(requirement.Evidence);
        });
    }

    [Fact]
    public void Undated_matching_line_is_evidence_without_an_invented_date_or_odometer()
    {
        var outcome = Evaluate(Line("UNDATED", "PGM 45K", null));
        var requirement = outcome.Prerequisites[0];
        Assert.True(requirement.Satisfied);
        Assert.Null(requirement.SatisfiedOn);
        Assert.Null(requirement.Evidence!.InvoiceDate);
        Assert.Null(requirement.Evidence.Odometer);
        Assert.Equal("UNDATED", requirement.Evidence.InvoiceNumber);
        Assert.Null(outcome.UnlockedOn);
    }
}
