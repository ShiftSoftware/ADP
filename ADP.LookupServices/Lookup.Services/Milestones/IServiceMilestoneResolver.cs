namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// Reads the scheduled service a service-history entry recorded.
/// <para>
/// <b>This interface exists to be replaced.</b> Today the only implementation infers the milestone
/// from the shape of a labour line's package code, which is a convention of the source system rather
/// than a fact it states. The service-menu subsystem records the interval outright, per scheduled
/// service and in kilometres, which is the same answer without the inference — when a vehicle's menu
/// is available to the lookup, a resolver reading it replaces the heuristic and nothing else about
/// eligibility moves. Keep that swap to one implementation: no caller may re-derive a milestone for
/// itself.
/// </para>
/// </summary>
public interface IServiceMilestoneResolver
{
    /// <summary>
    /// The milestone a package code records, or null when it records none. Returning null is the
    /// ordinary outcome, not an error: most service work is unscheduled and carries no milestone at
    /// all, and a code that cannot be read confidently must be reported the same way rather than
    /// guessed at.
    /// </summary>
    ServiceMilestoneReading Resolve(string packageCode);
}
