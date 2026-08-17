using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// What this deployment's conventions make of a corpus of service codes: how much of it reads, what
/// it reads as, and what is left over.
/// <para>
/// The instrument that turns convention drift into a ticket instead of a customer complaint. A
/// source system that changes how it writes its codes does not announce it, and an unreadable code
/// is indistinguishable, per lookup, from a service that never happened — so the only way to notice
/// is to ask, of the whole estate at once, how much still reads. Run it as a health check or a
/// nightly job over the distinct package codes in the labour-line store.
/// </para>
/// <para>
/// Read it against what the deployment schedules rather than against 100%: most service work is
/// unscheduled and carries no milestone, so the figure that matters is whether it moved, and
/// whether anything large sits in <see cref="TopUnresolved"/> that ought to read.
/// </para>
/// </summary>
[Docable]
public class ServiceCodeCoverageReport
{
    /// <summary>
    /// Whether the reader can produce a milestone at all. False means <b>no usable convention is
    /// configured</b> — every code below is unresolved for that one reason, and the corpus says
    /// nothing about the deployment's codes.
    /// </summary>
    public bool CanRead { get; set; }

    /// <summary>Settings that could not be used, with the reason. Empty when everything configured compiled.</summary>
    public IReadOnlyList<ServiceMilestoneConfigurationProblem> Problems { get; set; }

    /// <summary>Distinct codes in the corpus.</summary>
    public long Codes { get; set; }

    /// <summary>Labour lines the corpus accounts for.</summary>
    public long Lines { get; set; }

    /// <summary>Distinct codes that yielded a milestone.</summary>
    public long ResolvedCodes { get; set; }

    /// <summary>Labour lines carrying a code that yielded a milestone.</summary>
    public long ResolvedLines { get; set; }

    /// <summary>
    /// Resolved lines as a fraction of all lines, 0 to 1. Weighted by volume rather than by distinct
    /// code, because one code on a hundred thousand invoices matters more than a hundred codes on
    /// one each — and the reverse ranking is how a reader can look healthy while missing the work
    /// customers actually have done.
    /// </summary>
    public double LineCoverage { get; set; }

    /// <summary>
    /// Volume by programme. Reading this is how a mis-ordered alternation is caught: a programme the
    /// deployment knows it books work under, showing zero lines, is a pattern fault rather than a
    /// fact about the business.
    /// </summary>
    public IReadOnlyList<ServiceCodeCoverageGroup> Programs { get; set; }

    /// <summary>
    /// Volume by qualifier, the unnamed group being codes that carry none. The distribution a
    /// condition's <c>Qualifier</c> setting is calibrated against — deciding which variants count
    /// from the shape of the catalog rather than from these volumes is how a rule comes to describe
    /// a small minority of the work it was meant to cover.
    /// </summary>
    public IReadOnlyList<ServiceCodeCoverageGroup> Qualifiers { get; set; }

    /// <summary>
    /// Volume by convention, in the order they are tried, including conventions that matched
    /// nothing. A convention at zero has either been superseded or been shadowed by one above it.
    /// </summary>
    public IReadOnlyList<ServiceCodeCoverageGroup> Conventions { get; set; }

    /// <summary>
    /// The codes that did not resolve, heaviest first, capped at the requested limit. Where drift
    /// shows up first: a shape that used to read, or has never read, sitting near the top.
    /// </summary>
    public IReadOnlyList<UnresolvedServiceCode> TopUnresolved { get; set; }
}
