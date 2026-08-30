using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
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
    /// clause. Carried whatever the item's state: an offered item has nothing to explain to a
    /// customer — <see cref="ToLockDTO"/> still shows a block only for a locked or missed one — but
    /// "which prerequisites did this rule read, and when were they met" is the question the trace
    /// exists to answer, and it is at its least obvious precisely when everything passed.
    /// </summary>
    internal IReadOnlyList<VehicleServiceItemPrerequisiteDTO> Prerequisites { get; }

    /// <summary>
    /// Service codes a milestone condition passed over, with the reason. Collected only while
    /// tracing; they are evidence about how a deployment's codes are actually written and booked,
    /// which is a question the catalog cannot answer.
    /// </summary>
    internal IReadOnlyList<ServiceItemMilestoneNearMiss> MilestoneNearMisses { get; }

    /// <summary>
    /// When the last outstanding prerequisite fell into place, or null when the item has none, still
    /// has one outstanding, or reached one on a line that carries no date.
    /// <para>
    /// A reward is active for its configured duration from the moment it unlocks, not from warranty
    /// activation. That is the rule a locked item's cleared expiry already follows, read in the other
    /// direction: before unlock there is no honest date to show, and after it there is exactly one.
    /// </para>
    /// <para>
    /// Populated only for an item whose conditions are met. A locked or missed one keeps the rolling
    /// sequence's dates, so nothing about the sequence depends on a history that is still moving.
    /// </para>
    /// </summary>
    internal DateTime? UnlockedOn { get; }

    internal EligibilityConditionOutcome(
        EligibilityConditionState state,
        IReadOnlyList<VehicleServiceItemPrerequisiteDTO> prerequisites,
        IReadOnlyList<ServiceItemMilestoneNearMiss> milestoneNearMisses,
        DateTime? unlockedOn = null)
    {
        State = state;
        Prerequisites = prerequisites ?? new List<VehicleServiceItemPrerequisiteDTO>();
        MilestoneNearMisses = milestoneNearMisses ?? new List<ServiceItemMilestoneNearMiss>();
        UnlockedOn = unlockedOn;
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
