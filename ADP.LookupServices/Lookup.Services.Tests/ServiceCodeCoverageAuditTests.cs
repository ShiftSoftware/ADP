using ShiftSoftware.ADP.Lookup.Services.Milestones;

namespace Lookup.Services.Tests;

/// <summary>
/// The offline audit — the instrument that would have caught the convention drift that produced the
/// incident, because it asks of the whole estate at once what a per-lookup diagnostic can only ask
/// of one vehicle.
/// </summary>
public class ServiceCodeCoverageAuditTests
{
    private const string DeclaredPattern =
        @"^(?:(?<program>PGM|ALT|OTH)[A-Z0-9-]*)?(?:\s*[A-Z][A-Z0-9-]*)*\s*(?<milestone>[0-9]{1,3})\s*K(?<qualifier>[A-Z0-9]*(?:\s+[A-Z0-9]+)*)$";

    /// <summary>
    /// A corpus weighted the way a real one is: a handful of shapes carrying almost all the volume,
    /// and one heavy code nobody meant to be unreadable.
    /// </summary>
    private static readonly ServiceCodeSample[] Corpus =
    [
        new("PGM MDL100 45K", 1_000),
        new("PGM MDL100 50KQA", 500),
        new("PGMX9 50K", 300),
        new("ALT MDL100 45K", 150),
        new("OTHPGMX6 40KQA QB", 50),
        new("BRAKE PADS", 900),
        new("XYZ44K", 100),
    ];

    private static ServiceMilestoneOptions Declared()
    {
        var options = new ServiceMilestoneOptions();
        options.Conventions.Add(new ServiceCodeConvention { Name = "declared", Pattern = DeclaredPattern });
        return options;
    }

    [Fact]
    public void Reports_coverage_weighted_by_volume()
    {
        var report = ServiceCodeCoverageAudit.Run(Declared(), Corpus);

        Assert.True(report.CanRead);
        Assert.Empty(report.Problems);
        Assert.Equal(7, report.Codes);
        Assert.Equal(3_000, report.Lines);
        Assert.Equal(5, report.ResolvedCodes);
        Assert.Equal(2_000, report.ResolvedLines);
        Assert.Equal(2_000d / 3_000d, report.LineCoverage);
    }

    [Fact]
    public void Reports_volume_by_programme()
    {
        var report = ServiceCodeCoverageAudit.Run(Declared(), Corpus);

        Assert.Equal(
            new[] { ("PGM", 1_800L), ("ALT", 150L), ("OTH", 50L) },
            report.Programs.Select(x => (x.Name, x.Lines)));
    }

    /// <summary>
    /// The distribution a condition's qualifier setting is calibrated against. Deciding which
    /// variants count from the shape of a catalog rather than from these volumes is how a rule comes
    /// to describe a small minority of the work it was meant to cover.
    /// </summary>
    [Fact]
    public void Reports_volume_by_qualifier_including_codes_carrying_none()
    {
        var report = ServiceCodeCoverageAudit.Run(Declared(), Corpus);

        Assert.Equal(
            new[] { ((string?)null, 1_450L), ("QA", 500L), ("QA QB", 50L) },
            report.Qualifiers.Select(x => ((string?)x.Name, x.Lines)));
    }

    [Fact]
    public void Reports_the_heaviest_unresolved_codes_with_the_reason()
    {
        var report = ServiceCodeCoverageAudit.Run(Declared(), Corpus);

        Assert.Collection(
            report.TopUnresolved,
            heaviest =>
            {
                Assert.Equal("BRAKE PADS", heaviest.Code);
                Assert.Equal(900, heaviest.Lines);
                Assert.Equal(ServiceCodeReadOutcome.NoConventionMatched, heaviest.Reason);
            },
            // Claimed by a convention and then discarded — the one to look at, because a code the
            // reader half-understands is a pattern fault rather than unscheduled work.
            next =>
            {
                Assert.Equal("XYZ44K", next.Code);
                Assert.Equal(ServiceCodeReadOutcome.ImplausibleMilestone, next.Reason);
                Assert.Equal("declared", next.Convention);
                Assert.Equal(4_000, next.MilestoneInKilometres);
            });
    }

    [Fact]
    public void Caps_the_unresolved_list_where_asked()
    {
        Assert.Single(ServiceCodeCoverageAudit.Run(Declared(), Corpus, unresolvedLimit: 1).TopUnresolved);
    }

    /// <summary>
    /// A convention matching nothing is reported at zero rather than left out. Superseded shapes and
    /// conventions shadowed by the one above them look identical to a correct configuration until
    /// somebody can see the row.
    /// </summary>
    [Fact]
    public void Reports_a_convention_that_matched_nothing_in_place()
    {
        var options = Declared();
        options.Conventions.Add(new ServiceCodeConvention
        {
            Name = "legacy",
            Pattern = @"^(?<milestone>[0-9]{1,3})KM-(?<program>[A-Z]+)$",
        });

        var report = ServiceCodeCoverageAudit.Run(options, Corpus);

        Assert.Equal(
            new[] { ("declared", 2_000L), ("legacy", 0L) },
            report.Conventions.Select(x => (x.Name, x.Lines)));
    }

    /// <summary>
    /// "No conventions configured" and "this estate has no milestones" produce the same coverage
    /// figure and mean entirely different things. The report says which.
    /// </summary>
    [Fact]
    public void Reports_that_no_conventions_are_configured()
    {
        var report = ServiceCodeCoverageAudit.Run(new ServiceMilestoneOptions(), Corpus);

        Assert.False(report.CanRead);
        Assert.Equal(0, report.ResolvedLines);
        Assert.All(
            report.TopUnresolved,
            code => Assert.Equal(ServiceCodeReadOutcome.NoConventionsConfigured, code.Reason));
    }

    [Fact]
    public void Reports_a_convention_it_could_not_use()
    {
        var options = new ServiceMilestoneOptions();
        options.Conventions.Add(new ServiceCodeConvention { Name = "unusable", Pattern = "^(?<milestone>[0-9]+K$" });

        var report = ServiceCodeCoverageAudit.Run(options, Corpus);

        Assert.False(report.CanRead);
        var problem = Assert.Single(report.Problems);
        Assert.Equal("unusable", problem.Convention);
        Assert.Equal(ServiceMilestoneConfigurationProblemKind.PatternDoesNotCompile, problem.Kind);
        Assert.NotNull(problem.Detail);
    }

    /// <summary>
    /// Adding a convention to the list a host already handed us changes the configuration as much as
    /// assigning a new list does. A reader cached against the old contents would report the coverage
    /// of a configuration that is no longer in force.
    /// </summary>
    [Fact]
    public void A_convention_added_after_the_first_read_takes_effect()
    {
        var options = Declared();

        Assert.Equal(2_000, ServiceCodeCoverageAudit.Run(options, Corpus).ResolvedLines);

        options.Conventions.Add(new ServiceCodeConvention
        {
            Name = "letter-prefixed",
            Pattern = @"^[A-Z]+(?<milestone>[0-9]{1,3})K$",
        });
        options.MinimumInKilometres = 1_000;
        options.StepInKilometres = 1_000;

        Assert.Equal(2_100, ServiceCodeCoverageAudit.Run(options, Corpus).ResolvedLines);
    }

    /// <summary>
    /// A host that supplies its own resolver is auditable too. It explains no refusals, so the
    /// unresolved codes come back without a reason rather than with a guessed one.
    /// </summary>
    [Fact]
    public void Audits_a_host_supplied_resolver_without_inventing_reasons()
    {
        var report = ServiceCodeCoverageAudit.Run(new StubResolver(), Corpus);

        Assert.True(report.CanRead);
        Assert.Equal(1_000, report.ResolvedLines);
        Assert.Empty(report.Conventions);
        Assert.All(report.TopUnresolved, code => Assert.Null(code.Reason));
    }

    private sealed class StubResolver : IServiceMilestoneResolver
    {
        public ServiceMilestoneReading? Resolve(string packageCode) =>
            packageCode == "PGM MDL100 45K" ? new ServiceMilestoneReading(45_000, "PGM", null) : null;
    }
}
