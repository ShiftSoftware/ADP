using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// What one piece of service history says about the scheduled service it recorded: which milestone
/// was reached, under which programme, and under which variant qualifier.
/// <para>
/// The three parts are reported separately and judged separately. Reading them is a deployment
/// convention that a future data source will supply outright (see
/// <see cref="IServiceMilestoneResolver"/>); deciding which of them count is eligibility grammar and
/// belongs to the condition, not to the reader.
/// </para>
/// </summary>
[Docable]
public class ServiceMilestoneReading
{
    /// <summary>The milestone reached, in kilometres.</summary>
    public long Milestone { get; }

    /// <summary>
    /// The programme the service was booked under, or null when the source names none. Compared with
    /// <c>EligibilityConditionModel.Program</c>.
    /// </summary>
    public string Program { get; }

    /// <summary>
    /// The variant qualifier carried alongside the milestone, or null when the source carries none.
    /// Judged by <c>EligibilityConditionModel.Qualifier</c>. Null and empty mean the same thing here
    /// — a code with nothing after its milestone — and a resolver should report null for both.
    /// </summary>
    public string Qualifier { get; }

    public ServiceMilestoneReading(long milestone, string program, string qualifier)
    {
        Milestone = milestone;
        Program = program;
        Qualifier = qualifier;
    }
}
