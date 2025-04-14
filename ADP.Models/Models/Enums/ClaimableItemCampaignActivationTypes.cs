using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum ClaimableItemCampaignActivationTypes
{
    [Description("Activate on First Trigger Only (One-time activation)")]
    FirstTriggerOnly = 1,

    [Description("Activate on Every Trigger (Repeatable Rewards)")]
    EveryTrigger = 2,

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