using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>A code that yielded no milestone, and why.</summary>
[Docable]
public class UnresolvedServiceCode
{
    /// <summary>The code as the source system wrote it.</summary>
    public string Code { get; set; }

    /// <summary>Labour lines carrying it.</summary>
    public long Lines { get; set; }

    /// <summary>
    /// Why it did not resolve, or null when the configured resolver does not explain itself. Most
    /// entries are ordinary unscheduled work; the ones to look at are those a convention claimed
    /// and then discarded.
    /// </summary>
    public ServiceCodeReadOutcome? Reason { get; set; }

    /// <summary>The convention that matched it, when one did and the reading was then discarded.</summary>
    public string Convention { get; set; }

    /// <summary>
    /// The mileage that was read and rejected, when the reason is
    /// <see cref="ServiceCodeReadOutcome.ImplausibleMilestone"/>. Tells a pattern reading the wrong
    /// part of the code from bounds set tighter than the deployment schedules.
    /// </summary>
    public long? MilestoneInKilometres { get; set; }
}
