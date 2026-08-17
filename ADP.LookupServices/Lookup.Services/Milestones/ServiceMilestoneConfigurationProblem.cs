using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// A setting that cannot be used, and what is wrong with it.
/// <para>
/// A misconfigured convention is dropped rather than thrown, because a lookup that returns nothing
/// is worse than one that returns fewer milestones — but dropping it silently is how a total
/// mismatch comes to look like a working configuration. Every dropped setting is reported here, and
/// the coverage audit carries the list.
/// </para>
/// </summary>
[Docable]
public class ServiceMilestoneConfigurationProblem
{
    /// <summary>The convention this concerns, or null when the problem is with the bounds.</summary>
    public string Convention { get; set; }

    /// <summary>What is wrong.</summary>
    public ServiceMilestoneConfigurationProblemKind Kind { get; set; }

    /// <summary>The detail the framework reported, when there is one — a regex parse error, say.</summary>
    public string Detail { get; set; }
}

/// <summary>The ways a milestone reader can be configured such that it reads nothing.</summary>
public enum ServiceMilestoneConfigurationProblemKind
{
    /// <summary>The convention declares no pattern.</summary>
    MissingPattern = 0,

    /// <summary>The pattern is not a regular expression this runtime can compile.</summary>
    PatternDoesNotCompile = 1,

    /// <summary>
    /// The pattern compiles but declares no <c>milestone</c> group. Such a convention could only
    /// ever match without reading anything, so it is refused rather than allowed to shadow the
    /// conventions after it.
    /// </summary>
    MissingMilestoneGroup = 2,

    /// <summary>
    /// The plausibility bounds admit nothing — a minimum at or below zero, or a maximum below the
    /// minimum. No reading could pass them, so no convention is even tried.
    /// </summary>
    ImplausibleBounds = 3,
}
