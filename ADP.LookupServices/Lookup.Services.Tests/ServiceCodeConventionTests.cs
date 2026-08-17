using ShiftSoftware.ADP.Lookup.Services.Milestones;

namespace Lookup.Services.Tests;

/// <summary>
/// The package-code reader, read directly rather than through eligibility.
/// <para>
/// Codes here are invented. What they reproduce is the <i>shapes</i> service codes come in — a
/// programme glued to a model token, a spec suffix glued to the milestone, trailing tokens, a
/// hyphenated model — because those shapes are what a reader inferring structure from token
/// positions gets wrong, and getting them wrong reports work a customer paid for as work that never
/// happened.
/// </para>
/// </summary>
public class ServiceCodeConventionTests
{
    /// <summary>
    /// The shape of a declared convention: a programme that may be glued to what follows, any
    /// number of further tokens, the milestone, and a qualifier that may be glued or trail.
    /// </summary>
    private const string SpacedOrGluedPattern =
        @"^(?:(?<program>PGM|ALT|OTH)[A-Z0-9-]*)?(?:\s*[A-Z][A-Z0-9-]*)*\s*(?<milestone>[0-9]{1,3})\s*K(?<qualifier>[A-Z0-9]*(?:\s+[A-Z0-9]+)*)$";

    private static ServiceMilestoneOptions Options(params (string Name, string Pattern)[] conventions)
    {
        var options = new ServiceMilestoneOptions();

        foreach (var (name, pattern) in conventions)
            options.Conventions.Add(new ServiceCodeConvention { Name = name, Pattern = pattern });

        return options;
    }

    private static PackageCodeServiceMilestoneResolver Reader(
        params (string Name, string Pattern)[] conventions) =>
        new(Options(conventions));

    private static PackageCodeServiceMilestoneResolver DeclaredReader() =>
        Reader(("declared", SpacedOrGluedPattern));

    // ---- One convention ----

    [Theory]
    // The programme is where the convention says it is, not where a token count would put it.
    [InlineData("PGM MDL100 50K", 50_000, "PGM", null)]
    [InlineData("PGMX9 50K", 50_000, "PGM", null)]
    [InlineData("PGMALT MDL100 50K", 50_000, "PGM", null)]
    [InlineData("PGM 50K", 50_000, "PGM", null)]
    [InlineData("50K", 50_000, null, null)]
    [InlineData("XYZ MDL100 50K", 50_000, null, null)]
    // The qualifier likewise: glued to the milestone, trailing it, both, or absent.
    [InlineData("PGM MDL100 50KQA", 50_000, "PGM", "QA")]
    [InlineData("PGM MDL100 50K QA", 50_000, "PGM", "QA")]
    [InlineData("PGM MDL100 50KQA QB", 50_000, "PGM", "QA QB")]
    // A hyphenated model token is part of the shape, not a separator.
    [InlineData("PGM MT-MDL121 40K", 40_000, "PGM", null)]
    // Compared case-insensitively, because a source system's casing is not a decision.
    [InlineData("pgm mdl100 50k", 50_000, "pgm", null)]
    public void Reads_the_three_groups_a_convention_declares(
        string code,
        long milestone,
        string? program,
        string? qualifier)
    {
        var reading = DeclaredReader().Resolve(code);

        Assert.NotNull(reading);
        Assert.Equal(milestone, reading!.Milestone);
        Assert.Equal(program, reading.Program);
        Assert.Equal(qualifier, reading.Qualifier);
    }

    [Theory]
    [InlineData("BRAKE PADS")]
    [InlineData("CONSUMABLES")]
    [InlineData("PGM MDL100")]
    [InlineData("PGM MDL100 50")]
    public void Unscheduled_work_reads_as_no_milestone(string code)
    {
        var read = DeclaredReader().Read(code);

        Assert.Equal(ServiceCodeReadOutcome.NoConventionMatched, read.Outcome);
        Assert.Null(read.Reading);
    }

    // ---- The plausibility guard ----

    [Theory]
    // A number glued to a letter prefix: the convention finds a milestone-shaped token, and what it
    // reads is not a believable service interval.
    [InlineData("XYZ44K", 4_000)]
    // A model token sitting where the milestone should be, the code's own K belonging to a trailing
    // qualifier. This is the shape that costs tens of thousands of lines in a real estate.
    [InlineData("PGM EOR MDL200 KQ", 0)]
    [InlineData("PGM MDL100 7K", 7_000)]
    [InlineData("PGM MDL100 1K", 1_000)]
    [InlineData("PGM MDL100 999K", 999_000)]
    public void An_implausible_reading_is_discarded_and_says_what_it_read(string code, long read)
    {
        var result = DeclaredReader().Read(code);

        Assert.Equal(ServiceCodeReadOutcome.ImplausibleMilestone, result.Outcome);
        Assert.Equal(read, result.MilestoneInKilometres);
        Assert.Equal("declared", result.Convention);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void Bounds_are_the_deployments_to_set()
    {
        var options = Options(("declared", SpacedOrGluedPattern));
        options.MinimumInKilometres = 1_000;
        options.StepInKilometres = 1_000;

        Assert.Equal(7_000, new PackageCodeServiceMilestoneResolver(options).Resolve("PGM MDL100 7K")?.Milestone);
    }

    [Fact]
    public void Bounds_that_admit_nothing_are_a_reported_misconfiguration()
    {
        var options = Options(("declared", SpacedOrGluedPattern));
        options.MinimumInKilometres = 0;

        var reader = new PackageCodeServiceMilestoneResolver(options);

        Assert.False(reader.CanRead);
        Assert.Equal(
            ServiceMilestoneConfigurationProblemKind.ImplausibleBounds,
            Assert.Single(reader.Problems).Kind);
        Assert.Equal(ServiceCodeReadOutcome.NoConventionsConfigured, reader.Read("PGM MDL100 50K").Outcome);
    }

    // ---- Several conventions ----

    [Theory]
    [InlineData("PGM MDL100 50K", "current", 50_000)]
    [InlineData("50KM-PGM", "legacy", 50_000)]
    public void Conventions_are_tried_in_order_and_the_first_to_match_decides(
        string code,
        string convention,
        long milestone)
    {
        var read = Reader(
                ("current", @"^(?<program>PGM)\s+[A-Z0-9]+\s+(?<milestone>[0-9]{1,3})K$"),
                ("legacy", @"^(?<milestone>[0-9]{1,3})KM-(?<program>[A-Z]+)$"))
            .Read(code);

        Assert.Equal(ServiceCodeReadOutcome.Read, read.Outcome);
        Assert.Equal(convention, read.Convention);
        Assert.Equal(milestone, read.Reading!.Milestone);
    }

    /// <summary>
    /// The first convention to match owns the code, including owning the decision that it is
    /// unreadable. Falling through would let a code be read under a convention its shape does not
    /// belong to — a wrong answer where this is merely a missing one.
    /// </summary>
    [Fact]
    public void A_matching_convention_owns_the_code_even_when_its_reading_is_discarded()
    {
        // "first" reads the leading number and rejects it as implausible; "second" would have read
        // the trailing one and accepted. The reading stands as "first" made it.
        var read = Reader(
                ("first", @"^(?<program>PGM)\s+(?<milestone>[0-9]{1,3})\s"),
                ("second", @"^(?<program>PGM)\s+[0-9]+\s+(?<milestone>[0-9]{1,3})K$"))
            .Read("PGM 7 50K");

        Assert.Equal(ServiceCodeReadOutcome.ImplausibleMilestone, read.Outcome);
        Assert.Equal("first", read.Convention);
        Assert.Equal(7_000, read.MilestoneInKilometres);
    }

    // ---- Conventions ADP cannot use ----

    [Fact]
    public void No_conventions_configured_is_a_state_of_its_own()
    {
        var reader = Reader();

        Assert.False(reader.CanRead);
        Assert.Empty(reader.Problems);
        Assert.Equal(ServiceCodeReadOutcome.NoConventionsConfigured, reader.Read("PGM MDL100 50K").Outcome);
    }

    [Theory]
    [InlineData(@"^(?<program>PGM)\s+[A-Z0-9]+\s+[0-9]{1,3}K$", ServiceMilestoneConfigurationProblemKind.MissingMilestoneGroup)]
    [InlineData(@"^(?<milestone>[0-9]{1,3}K$", ServiceMilestoneConfigurationProblemKind.PatternDoesNotCompile)]
    [InlineData("", ServiceMilestoneConfigurationProblemKind.MissingPattern)]
    [InlineData(null, ServiceMilestoneConfigurationProblemKind.MissingPattern)]
    public void A_convention_ADP_cannot_use_is_refused_and_named(
        string? pattern,
        ServiceMilestoneConfigurationProblemKind kind)
    {
        var reader = Reader(("unusable", pattern!));

        var problem = Assert.Single(reader.Problems);
        Assert.Equal(kind, problem.Kind);
        Assert.Equal("unusable", problem.Convention);
        Assert.False(reader.CanRead);
        Assert.Empty(reader.Conventions);
    }

    /// <summary>
    /// A convention with no milestone group would match without reading anything, and so would
    /// shadow every convention after it. Refusing it is what keeps the list honest.
    /// </summary>
    [Fact]
    public void A_refused_convention_does_not_shadow_the_ones_after_it()
    {
        var reader = Reader(
            ("unusable", @"^(?<program>PGM)\s+[A-Z0-9]+\s+[0-9]{1,3}K$"),
            ("usable", @"^(?<program>PGM)\s+[A-Z0-9]+\s+(?<milestone>[0-9]{1,3})K$"));

        Assert.Equal(50_000, reader.Resolve("PGM MDL100 50K")?.Milestone);
        Assert.Equal(new[] { "usable" }, reader.Conventions);
    }

    [Fact]
    public void A_milestone_group_that_captures_nothing_reads_nothing()
    {
        var read = Reader(("optional", @"^(?<program>PGM)(?:\s+(?<milestone>[0-9]{1,3})K)?$")).Read("PGM");

        Assert.Equal(ServiceCodeReadOutcome.MilestoneNotCaptured, read.Outcome);
    }

    // ---- Guards on the match itself ----

    [Theory]
    [InlineData("PGM 50K", ServiceCodeReadOutcome.Read)]
    [InlineData("PGM 50K 100K", ServiceCodeReadOutcome.AmbiguousMatch)]
    public void An_unanchored_convention_matching_twice_reads_nothing(
        string code,
        ServiceCodeReadOutcome outcome)
    {
        Assert.Equal(outcome, Reader(("unanchored", @"(?<milestone>[0-9]{1,3})K")).Read(code).Outcome);
    }

    /// <summary>
    /// A pattern that backtracks catastrophically costs one unread code, not a lookup that never
    /// returns. Conventions are deployment configuration rather than authored catalog data, so the
    /// exposure is small — but the failure mode without a timeout is an outage.
    /// </summary>
    [Fact]
    public void A_pattern_that_runs_away_is_abandoned_rather_than_left_to_run()
    {
        var read = Reader(("runaway", @"^(?<milestone>([0-9]+)+)K$"))
            .Read(new string('9', 44) + "X");

        Assert.Equal(ServiceCodeReadOutcome.TimedOut, read.Outcome);
        Assert.Equal("runaway", read.Convention);
    }
}
