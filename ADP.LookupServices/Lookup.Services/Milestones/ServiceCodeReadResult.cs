using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// What the package-code reader made of one code, including why it made nothing of it.
/// <para>
/// <see cref="IServiceMilestoneResolver.Resolve"/> answers the only question eligibility asks —
/// which milestone, if any — and null is its ordinary answer, because most service work is
/// unscheduled. That is exactly why a null carries no signal on its own: a convention that fits
/// nothing produces the same null as a brake job. This is the same read with its reasoning
/// attached, which is what the coverage audit reports and what a host debugging a convention
/// wants to see.
/// </para>
/// </summary>
[Docable]
public class ServiceCodeReadResult
{
    /// <summary>What happened.</summary>
    public ServiceCodeReadOutcome Outcome { get; set; }

    /// <summary>
    /// The convention that matched, by name, or null when none did. Named even when the reading was
    /// then discarded: which convention claimed a code is the first thing to know when one of them
    /// is drifting.
    /// </summary>
    public string Convention { get; set; }

    /// <summary>The reading, when there is one. Null for every outcome other than <see cref="ServiceCodeReadOutcome.Read"/>.</summary>
    public ServiceMilestoneReading Reading { get; set; }

    /// <summary>
    /// The mileage the convention captured, even when the plausibility guard then rejected it.
    /// Null when nothing numeric was captured at all. This is what tells a bad pattern ("it read
    /// the model code as a milestone") from bounds set too tight ("it read the milestone, and the
    /// deployment schedules services further out than the bounds admit").
    /// </summary>
    public long? MilestoneInKilometres { get; set; }
}

/// <summary>Why a code did or did not yield a milestone.</summary>
public enum ServiceCodeReadOutcome
{
    /// <summary>A convention matched and the reading is believable.</summary>
    Read = 0,

    /// <summary>
    /// This deployment declares no usable convention, so nothing can be read from any code. Distinct
    /// from a vehicle that simply has no milestones in its history, and the distinction is the point
    /// — one is a configuration to fix, the other is a fact about a customer.
    /// </summary>
    NoConventionsConfigured = 1,

    /// <summary>Every convention was tried and none matched. Ordinary for unscheduled work.</summary>
    NoConventionMatched = 2,

    /// <summary>
    /// A convention matched, but its <c>milestone</c> group captured nothing — an optional group
    /// that did not participate. Fails closed rather than reading a milestone of zero.
    /// </summary>
    MilestoneNotCaptured = 3,

    /// <summary>
    /// A convention matched and what it captured is not a believable milestone: outside the
    /// configured bounds, off the configured interval, or too large to be a mileage at all.
    /// </summary>
    ImplausibleMilestone = 4,

    /// <summary>
    /// A convention matched the code in more than one place, so which match names the milestone
    /// would be a guess. Anchoring the convention settles it.
    /// </summary>
    AmbiguousMatch = 5,

    /// <summary>
    /// Matching ran past the time the reader allows. Reported rather than thrown: a pathological
    /// pattern must cost one unread code, not a lookup that never returns.
    /// </summary>
    TimedOut = 6,
}
