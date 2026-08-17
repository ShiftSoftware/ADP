using ShiftSoftware.ADP.Models;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// How this deployment's service-history codes name a scheduled service, and what counts as a
/// believable milestone once one is read.
/// <para>
/// Configured rather than hard-coded because the convention belongs to the source system, not to
/// ADP: another deployment will write its milestones differently, and the failure mode is silent —
/// a pattern that fits nothing reads every service as unscheduled, and every milestone condition
/// simply stops matching.
/// </para>
/// </summary>
[Docable]
public class ServiceMilestoneOptions
{
    /// <summary>
    /// Matches a milestone written as a number of thousands followed by K, as a standalone token:
    /// the digits are captured, and the surrounding characters must not be letters or digits so a
    /// model code carrying digits cannot be mistaken for one.
    /// <para>
    /// <c>[0-9]</c> and not <c>\d</c> deliberately. .NET's <c>\d</c> matches Unicode digits, and a
    /// deployment rendering Arabic or Kurdish will meet digits that are not the ones this convention
    /// is written in.
    /// </para>
    /// </summary>
    public const string DefaultPackageCodePattern = @"(?<![A-Z0-9])([0-9]+)\s*K(?![A-Z0-9])";

    /// <summary>
    /// The pattern that reads a milestone out of a package code, capturing the number of thousands
    /// in its first group. Matched case-insensitively. A code holding more than one match is read as
    /// holding none, because which of them is the milestone would be a guess.
    /// </summary>
    public string PackageCodePattern { get; set; } = DefaultPackageCodePattern;

    /// <summary>The smallest believable milestone, in kilometres.</summary>
    public long MinimumInKilometres { get; set; } = 5_000;

    /// <summary>
    /// The largest believable milestone, in kilometres. Set clear of the largest milestone the
    /// deployment actually schedules rather than exactly on it, so a genuine service added later is
    /// not silently discarded by a bound nobody remembers setting.
    /// </summary>
    public long MaximumInKilometres { get; set; } = 500_000;

    /// <summary>
    /// The interval milestones are scheduled at, in kilometres. A reading that is not a whole number
    /// of these is not a milestone. Set to 0 to accept any spacing.
    /// </summary>
    public long StepInKilometres { get; set; } = 5_000;

    /// <summary>
    /// Replaces the built-in package-code reader entirely — the seam a host uses to supply
    /// milestones from a source that states them rather than implies them. When unset, the settings
    /// above build the built-in reader.
    /// </summary>
    public IServiceMilestoneResolver Resolver { get; set; }

    // Built once and reused: the evaluator constructs a condition evaluator per catalog item, so
    // building a Regex on the way past would pay for the parse on every item of every lookup. Held
    // as one reference so a reader either sees a resolver together with the settings it was built
    // from or sees neither — the settings are not expected to change after startup, but a torn pair
    // would be an unreproducible wrong answer rather than a slow one.
    private CachedResolver cached;

    internal IServiceMilestoneResolver GetResolver()
    {
        if (Resolver != null)
            return Resolver;

        var signature = string.Join(
            "\u0000",
            PackageCodePattern ?? string.Empty,
            MinimumInKilometres.ToString(),
            MaximumInKilometres.ToString(),
            StepInKilometres.ToString());

        var current = cached;
        if (current != null && string.Equals(current.Signature, signature, StringComparison.Ordinal))
            return current.Resolver;

        var built = new CachedResolver(signature, new PackageCodeServiceMilestoneResolver(this));
        cached = built;
        return built.Resolver;
    }

    private sealed class CachedResolver
    {
        internal string Signature { get; }
        internal IServiceMilestoneResolver Resolver { get; }

        internal CachedResolver(string signature, IServiceMilestoneResolver resolver)
        {
            Signature = signature;
            Resolver = resolver;
        }
    }
}
