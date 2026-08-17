using ShiftSoftware.ADP.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// How this deployment's service-history codes name a scheduled service, and what counts as a
/// believable milestone once one is read.
/// <para>
/// Declared rather than inferred, and deliberately without a default. A convention shipped as a
/// framework default is one network's writing habits presented as everyone's: it looks configured,
/// it matches a plausible-looking fraction, and every code it does not fit reads as work that never
/// happened. Nothing here reads anything until a deployment says how its codes are written, and
/// <see cref="ServiceCodeCoverageAudit"/> exists so that "how many of our codes does this actually
/// read" is a number somebody can look at rather than a discovery made from a customer complaint.
/// </para>
/// </summary>
[Docable]
public class ServiceMilestoneOptions
{
    /// <summary>
    /// The ways this deployment's source system writes a service code, in the order they are tried;
    /// the first whose pattern matches decides the reading. Empty by design — a deployment that
    /// declares none reads no milestones at all, and says so as a distinct state rather than
    /// reporting every vehicle as having no service history.
    /// </summary>
    public IList<ServiceCodeConvention> Conventions { get; set; } = new List<ServiceCodeConvention>();

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
    /// milestones from a source that states them rather than implies them, and the seam that lets a
    /// network whose codes are not regex-tractable be fixed without an ADP release. When unset, the
    /// settings above build the built-in reader.
    /// </summary>
    public IServiceMilestoneResolver Resolver { get; set; }

    // Built once and reused: the evaluator constructs a condition evaluator per catalog item, so
    // compiling conventions on the way past would pay for the parse on every item of every lookup.
    // Held as one reference so a reader either sees a resolver together with the settings it was
    // built from or sees neither — the settings are not expected to change after startup, but a
    // torn pair would be an unreproducible wrong answer rather than a slow one.
    private CachedResolver cached;

    internal IServiceMilestoneResolver GetResolver()
    {
        if (Resolver != null)
            return Resolver;

        var signature = Signature();

        var current = cached;
        if (current != null && string.Equals(current.Signature, signature, StringComparison.Ordinal))
            return current.Resolver;

        var built = new CachedResolver(signature, new PackageCodeServiceMilestoneResolver(this));
        cached = built;
        return built.Resolver;
    }

    /// <summary>
    /// The settings the cached reader was built from, taken over the conventions' contents rather
    /// than the list's identity: a host that adds a convention to the list it already handed us has
    /// changed the configuration just as much as one that assigned a new list.
    /// </summary>
    private string Signature()
    {
        // A separator that cannot occur in a name or a pattern, so no two different
        // configurations can write the same signature by running two fields together.
        const char Separator = '\u0000';

        var signature = new StringBuilder()
            .Append(MinimumInKilometres).Append(Separator)
            .Append(MaximumInKilometres).Append(Separator)
            .Append(StepInKilometres);

        if (Conventions != null)
            foreach (var convention in Conventions)
                signature
                    .Append(Separator).Append(convention?.Name)
                    .Append(Separator).Append(convention?.Pattern);

        return signature.ToString();
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
