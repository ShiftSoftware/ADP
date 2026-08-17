using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// Reads a milestone out of a labour line's package code using the conventions this deployment
/// declares, trying them in order and taking the first that matches.
/// <para>
/// This reader knows nothing about how a code is put together. It does not decide that the
/// programme is the leading token or that the qualifier is whatever trails the milestone — a
/// previous version did, and reading a small fraction of a production estate while reporting the
/// rest as unscheduled work is what that cost. Structure comes from the convention's named groups;
/// ADP reads <c>milestone</c>, <c>program</c> and <c>qualifier</c>, and judging them is eligibility
/// grammar rather than the reader's business.
/// </para>
/// <para>
/// Still an inference from how a code is written rather than a fact the source states — see
/// <see cref="IServiceMilestoneResolver"/> for the source that states the interval outright and
/// retires this.
/// </para>
/// </summary>
public sealed class PackageCodeServiceMilestoneResolver : IServiceMilestoneResolver
{
    // Conventions are deployment configuration rather than authored catalog data, so the exposure
    // is small — but a timeout costs one argument and turns a pathological pattern into a code that
    // reads as unscheduled instead of a lookup that never returns.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    private readonly CompiledConvention[] conventions;
    private readonly long minimum;
    private readonly long maximum;
    private readonly long step;
    private readonly bool boundsUsable;

    public PackageCodeServiceMilestoneResolver(ServiceMilestoneOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        minimum = options.MinimumInKilometres;
        maximum = options.MaximumInKilometres;
        step = options.StepInKilometres;
        boundsUsable = minimum > 0 && maximum >= minimum && step >= 0;

        var problems = new List<ServiceMilestoneConfigurationProblem>();
        var compiled = new List<CompiledConvention>();

        if (!boundsUsable)
            problems.Add(new ServiceMilestoneConfigurationProblem
            {
                Kind = ServiceMilestoneConfigurationProblemKind.ImplausibleBounds,
                Detail = string.Format(
                    CultureInfo.InvariantCulture,
                    "minimum {0}, maximum {1}, step {2}",
                    minimum,
                    maximum,
                    step),
            });

        if (options.Conventions != null)
            foreach (var convention in options.Conventions)
            {
                if (convention is null)
                    continue;

                var compiledConvention = Compile(convention, problems);
                if (compiledConvention != null)
                    compiled.Add(compiledConvention);
            }

        conventions = compiled.ToArray();
        Problems = problems.ToArray();
    }

    /// <summary>
    /// Settings that could not be used, with the reason. Empty when everything configured compiled.
    /// A dropped convention is reported rather than thrown, because a lookup that fails outright is
    /// worse than one reading fewer milestones — but dropping it silently is how a total mismatch
    /// comes to look like a working configuration.
    /// </summary>
    public IReadOnlyList<ServiceMilestoneConfigurationProblem> Problems { get; }

    /// <summary>The conventions in use, by name, in the order they are tried.</summary>
    public IReadOnlyList<string> Conventions
    {
        get
        {
            var names = new string[conventions.Length];
            for (var i = 0; i < conventions.Length; i++)
                names[i] = conventions[i].Name;
            return names;
        }
    }

    /// <summary>
    /// Whether this reader can produce a milestone at all. False is a state worth reporting on its
    /// own: it says nothing about the vehicle in front of you.
    /// </summary>
    public bool CanRead => boundsUsable && conventions.Length > 0;

    public ServiceMilestoneReading Resolve(string packageCode) => Read(packageCode).Reading;

    /// <summary>
    /// The same read as <see cref="Resolve"/>, with the reasoning attached. What the coverage audit
    /// reports, and what to call when a code that ought to resolve does not.
    /// </summary>
    public ServiceCodeReadResult Read(string packageCode)
    {
        if (!CanRead)
            return new ServiceCodeReadResult
            {
                Outcome = ServiceCodeReadOutcome.NoConventionsConfigured,
            };

        if (string.IsNullOrWhiteSpace(packageCode))
            return new ServiceCodeReadResult
            {
                Outcome = ServiceCodeReadOutcome.NoConventionMatched,
            };

        foreach (var convention in conventions)
        {
            Match match;
            bool ambiguous;

            try
            {
                match = convention.Pattern.Match(packageCode);

                if (!match.Success)
                    continue;

                // Exactly one match, or none. A convention matching a code in two places says
                // something this reader cannot resolve, and picking one of them would be a guess
                // that quietly decides whether a reward is earned.
                ambiguous = match.NextMatch().Success;
            }
            catch (RegexMatchTimeoutException)
            {
                return new ServiceCodeReadResult
                {
                    Outcome = ServiceCodeReadOutcome.TimedOut,
                    Convention = convention.Name,
                };
            }

            // The first convention that matches decides the reading, including deciding that the
            // code is unreadable. Falling through to the next one on a rejected reading would let a
            // code be read under a convention its shape does not belong to — a wrong answer where
            // this is merely a missing one. The audit names the convention that claimed each code,
            // so one convention swallowing another's codes is visible rather than inferred.
            return ambiguous
                ? new ServiceCodeReadResult
                {
                    Outcome = ServiceCodeReadOutcome.AmbiguousMatch,
                    Convention = convention.Name,
                }
                : ReadMatch(convention.Name, match);
        }

        return new ServiceCodeReadResult
        {
            Outcome = ServiceCodeReadOutcome.NoConventionMatched,
        };
    }

    private ServiceCodeReadResult ReadMatch(string convention, Match match)
    {
        var milestoneGroup = match.Groups[ServiceCodeConvention.MilestoneGroupName];

        if (!milestoneGroup.Success)
            return new ServiceCodeReadResult
            {
                Outcome = ServiceCodeReadOutcome.MilestoneNotCaptured,
                Convention = convention,
            };

        // NumberStyles.None: no sign, no separators, no leading whitespace. An overlong run of
        // digits simply fails to parse, which is the answer we want anyway.
        if (!long.TryParse(
                milestoneGroup.Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var thousands) ||
            thousands > long.MaxValue / 1000)
            return new ServiceCodeReadResult
            {
                Outcome = ServiceCodeReadOutcome.ImplausibleMilestone,
                Convention = convention,
            };

        var milestone = thousands * 1000;

        if (milestone < minimum || milestone > maximum || (step > 0 && milestone % step != 0))
            return new ServiceCodeReadResult
            {
                Outcome = ServiceCodeReadOutcome.ImplausibleMilestone,
                Convention = convention,
                MilestoneInKilometres = milestone,
            };

        return new ServiceCodeReadResult
        {
            Outcome = ServiceCodeReadOutcome.Read,
            Convention = convention,
            MilestoneInKilometres = milestone,
            Reading = new ServiceMilestoneReading(
                milestone,
                GroupValue(match, ServiceCodeConvention.ProgramGroupName),
                GroupValue(match, ServiceCodeConvention.QualifierGroupName)),
        };
    }

    private static CompiledConvention Compile(
        ServiceCodeConvention convention,
        List<ServiceMilestoneConfigurationProblem> problems)
    {
        if (string.IsNullOrWhiteSpace(convention.Pattern))
        {
            problems.Add(Problem(convention, ServiceMilestoneConfigurationProblemKind.MissingPattern, null));
            return null;
        }

        Regex pattern;
        try
        {
            pattern = new Regex(
                convention.Pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                MatchTimeout);
        }
        catch (ArgumentException exception)
        {
            // A pattern that does not compile is a misconfiguration, and one that must not take the
            // whole lookup down with it.
            problems.Add(Problem(
                convention,
                ServiceMilestoneConfigurationProblemKind.PatternDoesNotCompile,
                exception.Message));
            return null;
        }

        // A convention that captures no milestone could only ever match without reading anything.
        // Left in the list it would shadow every convention after it, which is a silent way to read
        // nothing — the exact failure this design exists to end.
        if (Array.IndexOf(pattern.GetGroupNames(), ServiceCodeConvention.MilestoneGroupName) < 0)
        {
            problems.Add(Problem(
                convention,
                ServiceMilestoneConfigurationProblemKind.MissingMilestoneGroup,
                null));
            return null;
        }

        return new CompiledConvention(convention.Name, pattern);
    }

    /// <summary>
    /// A named group's text, or null when the convention declares no such group, the group did not
    /// participate, or it captured nothing but space. Null and blank mean the same thing to a
    /// condition — a code carrying no programme, or nothing after its milestone — so they are
    /// reported the same way rather than left for each comparison to normalise.
    /// </summary>
    private static string GroupValue(Match match, string name)
    {
        var group = match.Groups[name];
        if (!group.Success)
            return null;

        var value = group.Value.Trim();
        return value.Length == 0 ? null : value;
    }

    private static ServiceMilestoneConfigurationProblem Problem(
        ServiceCodeConvention convention,
        ServiceMilestoneConfigurationProblemKind kind,
        string detail) =>
        new ServiceMilestoneConfigurationProblem
        {
            Convention = convention.Name,
            Kind = kind,
            Detail = detail,
        };

    private sealed class CompiledConvention
    {
        internal string Name { get; }
        internal Regex Pattern { get; }

        internal CompiledConvention(string name, Regex pattern)
        {
            Name = name;
            Pattern = pattern;
        }
    }
}
