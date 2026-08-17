using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// What a set of eligibility conditions decided about one item.
/// <para>
/// A single boolean cannot express this. Two conditions failing for different reasons is not one
/// rejection twice over: "you have not finished your prerequisites" and "you went past the window"
/// are different things to say to a customer, and the difference between them is the whole reason
/// the rule is written as two clauses rather than one predicate.
/// </para>
/// </summary>
internal sealed class EligibilityConditionOutcome
{
    internal static readonly EligibilityConditionOutcome Met =
        new EligibilityConditionOutcome(EligibilityConditionState.Met, null, null);

    internal static readonly EligibilityConditionOutcome Hidden =
        new EligibilityConditionOutcome(EligibilityConditionState.Hidden, null, null);

    internal EligibilityConditionState State { get; }

    /// <summary>
    /// The services this item waits on, satisfied or not, gathered from every locking milestone
    /// clause. Empty unless the item is locked or missed — an offered item has nothing to explain.
    /// </summary>
    internal IReadOnlyList<VehicleServiceItemPrerequisiteDTO> Prerequisites { get; }

    /// <summary>
    /// Milestone codes that were dropped on their qualifier alone. Collected only while tracing;
    /// they are evidence about how a deployment's codes are actually booked, which is a question the
    /// catalog cannot answer.
    /// </summary>
    internal IReadOnlyList<ServiceItemMilestoneQualifierNearMiss> QualifierNearMisses { get; }

    internal EligibilityConditionOutcome(
        EligibilityConditionState state,
        IReadOnlyList<VehicleServiceItemPrerequisiteDTO> prerequisites,
        IReadOnlyList<ServiceItemMilestoneQualifierNearMiss> qualifierNearMisses)
    {
        State = state;
        Prerequisites = prerequisites ?? new List<VehicleServiceItemPrerequisiteDTO>();
        QualifierNearMisses = qualifierNearMisses ?? new List<ServiceItemMilestoneQualifierNearMiss>();
    }

    internal bool IsMet => State == EligibilityConditionState.Met;

    /// <summary>The lock block an unclaimable item carries, or null when the item is offered or hidden.</summary>
    internal VehicleServiceItemLockDTO ToLockDTO()
    {
        if (State != EligibilityConditionState.Locked && State != EligibilityConditionState.Missed)
            return null;

        return new VehicleServiceItemLockDTO
        {
            State = State == EligibilityConditionState.Locked
                ? VehicleServiceItemLockState.Locked
                : VehicleServiceItemLockState.Missed,
            Prerequisites = Prerequisites.ToList(),
        };
    }
}

internal enum EligibilityConditionState
{
    /// <summary>Every condition held. The item is offered.</summary>
    Met = 0,

    /// <summary>The item is not this vehicle's to see, and is dropped.</summary>
    Hidden = 1,

    /// <summary>The customer can still earn it.</summary>
    Locked = 2,

    /// <summary>They could have, and no longer can.</summary>
    Missed = 3,
}
