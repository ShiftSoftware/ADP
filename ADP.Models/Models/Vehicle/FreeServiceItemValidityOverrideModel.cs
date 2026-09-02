using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Overrides the validity window of a single free service item on a single vehicle.
/// <para>
/// The evaluator dates a free item from the schedule it belongs to: a warranty-activated item rolls
/// forward through the sequence, and a reward whose eligibility prerequisites are met is re-dated
/// from the service that completed them. Both are rules about a population, and a rule about a
/// population is occasionally wrong about one vehicle — most often because a customer was already
/// told they had the item under dates the rule has since recomputed. This record is how that
/// promise is honoured for that vehicle, without bending the rule for everyone else.
/// </para>
/// <para>
/// It moves dates and nothing else. An item the vehicle is not offered, or one that is locked or
/// missed because its conditions are unmet, is not granted by naming it here — an override is not a
/// substitute for eligibility, and an operator who wants to hand out an unearned item has
/// <see cref="Enums.ClaimableItemCampaignActivationTrigger.ManualVinEntry"/> for exactly that.
/// </para>
/// </summary>
[Docable]
public class FreeServiceItemValidityOverrideModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) this override applies to.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The service item whose dates are overridden, matched against
    /// <see cref="ServiceItemModel.IntegrationID"/> — the same identifier
    /// <see cref="ItemClaimModel.ServiceItemID"/> is matched by. An override naming an item this
    /// vehicle is not offered is inert; the lookup trace reports it rather than failing.
    /// </summary>
    public string ServiceItemID { get; set; }

    /// <summary>
    /// The moment to treat the item as earned. The item activates here and expires its own
    /// <see cref="ServiceItemModel.ActiveFor"/> later, which is the same arithmetic the evaluator
    /// applies to a reward's real unlock date — so an operator states the fact ("this customer
    /// earned it on the 1st") rather than back-computing a window from it.
    /// <para>
    /// Only an item carrying a duration is re-dated this way. One with a
    /// <see cref="Enums.ClaimableItemValidityMode.FixedDateRange"/> window has no duration to add,
    /// so it takes the activation date and keeps the expiry it had; use <see cref="ExpiresAt"/> to
    /// move that.
    /// </para>
    /// </summary>
    public DateTime? UnlockedOn { get; set; }

    /// <summary>
    /// The date the item expires, overriding both the schedule's answer and anything
    /// <see cref="UnlockedOn"/> computes. Set it alone to extend an item in place, or alongside
    /// <see cref="UnlockedOn"/> to state both ends of the window outright.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Why the override exists, in the operator's own words. Carried into the lookup trace, so the
    /// answer to "why is this one vehicle's item still open" is the reason it was granted rather
    /// than a reconstruction of it.
    /// </summary>
    public string Reason { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.FreeServiceItemValidityOverride;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// Indicates whether this override has been deleted (returning the item to the dates the
    /// schedule computes for it).
    /// </summary>
    public bool IsDeleted { get; set; }
}
