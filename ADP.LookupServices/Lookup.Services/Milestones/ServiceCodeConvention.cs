using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// One way this deployment's source system writes a service code, declared as a regular expression
/// with named groups.
/// <para>
/// The structure of a service code belongs to the source system, not to ADP. A reader that infers
/// it — that the programme is the first token, say, or that everything after the milestone is a
/// variant — fits exactly the convention it was written against and reads a fraction of anything
/// else, silently: an unreadable code is indistinguishable from a service that never happened, so
/// the customer is simply denied a reward they earned. A deployment declares its structure here
/// instead, and what ADP does with the parts stays eligibility grammar.
/// </para>
/// <para>
/// Several conventions may be declared, in order, and the first whose pattern matches decides the
/// reading. That covers a network that changed its codes at some point and holds both shapes in
/// accumulated history — which is the ordinary case, because eligibility reads history rather than
/// the current catalog — without collapsing them into one unreadable pattern.
/// </para>
/// </summary>
[Docable]
public class ServiceCodeConvention
{
    /// <summary>
    /// The group naming the milestone, in thousands of kilometres. Required: a convention that
    /// captures no milestone reads nothing, and is reported as a misconfiguration rather than
    /// quietly matching.
    /// </summary>
    public const string MilestoneGroupName = "milestone";

    /// <summary>
    /// The group naming the programme the work was booked under. Optional; a code shape that
    /// carries no programme simply omits it, and a condition filtering on programme then admits
    /// nothing read by this convention.
    /// </summary>
    public const string ProgramGroupName = "program";

    /// <summary>
    /// The group naming the spec or variant text carried alongside the milestone. Optional. A group
    /// that matches nothing, or only whitespace, reads as no qualifier at all.
    /// </summary>
    public const string QualifierGroupName = "qualifier";

    /// <summary>
    /// What this convention is called. Diagnostics only — it names the convention in the coverage
    /// audit and in configuration problems, which is how a convention that has stopped matching
    /// anything becomes visible.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The pattern, matched case-insensitively. ADP reads exactly three named groups —
    /// <see cref="MilestoneGroupName"/>, <see cref="ProgramGroupName"/> and
    /// <see cref="QualifierGroupName"/> — and ignores every other group the pattern declares, so a
    /// convention may capture whatever else it needs to describe the shape.
    /// <para>
    /// Prefer <c>[0-9]</c> over <c>\d</c>: .NET matches Unicode digits with <c>\d</c> by default,
    /// and a deployment rendering other scripts will meet digits this convention was not written
    /// in. Anchor the pattern unless the shape genuinely does not allow it — an unanchored
    /// convention that matches twice in one code reads as no milestone at all, because choosing one
    /// of the two would be a guess about whether a reward was earned.
    /// </para>
    /// </summary>
    public string Pattern { get; set; }
}
