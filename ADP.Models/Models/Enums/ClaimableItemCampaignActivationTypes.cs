using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Defines how a claimable item campaign responds to repeated activation triggers.
/// </summary>
[Docable]
public enum ClaimableItemCampaignActivationTypes
{
    /// <summary>
    /// The campaign activates only on the first trigger. Subsequent triggers are ignored (one-time activation).
    /// </summary>
    [Description("Activate on First Trigger Only (One-time activation)")]
    FirstTriggerOnly = 1,

    /// <summary>
    /// The campaign activates fresh on every trigger, creating new claimable items each time (repeatable rewards).
    /// </summary>
    [Description("Activate on Every Trigger (Repeatable Rewards)")]
    EveryTrigger = 2,

    /// <summary>
    /// The campaign activates once but extends its validity on each subsequent trigger (rolling/renewable rewards).
    /// </summary>
    [Description("Activate Once, but Extend on Every Trigger (Rolling/Renewable Rewards)")]
    ExtendOnEachTrigger = 3,
}

public static class ClaimableItemCampaignActivationRules
{
    public static readonly Dictionary<ClaimableItemCampaignActivationTrigger, ClaimableItemCampaignActivationTypes[]> ValidActivationTypes =
        new Dictionary<ClaimableItemCampaignActivationTrigger, ClaimableItemCampaignActivationTypes[]>
        {
            {
                ClaimableItemCampaignActivationTrigger.WarrantyActivation,
                new[] { ClaimableItemCampaignActivationTypes.FirstTriggerOnly }
            },
            {
                ClaimableItemCampaignActivationTrigger.VehicleInspection,
                new[]
                {
                    ClaimableItemCampaignActivationTypes.FirstTriggerOnly,
                    ClaimableItemCampaignActivationTypes.EveryTrigger,
                    ClaimableItemCampaignActivationTypes.ExtendOnEachTrigger
                }
            }
        };

    public static ClaimableItemCampaignActivationTypes[] GetValidTypes(ClaimableItemCampaignActivationTrigger trigger) =>
        ValidActivationTypes.TryGetValue(trigger, out var types) ? types : Array.Empty<ClaimableItemCampaignActivationTypes>();
}