using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.ClaimableItems.Web;

/// <summary>
/// Consumer plug-in for campaign-form UI that is specific to a particular activation trigger.
///
/// Activation triggers (VehicleInspection, ManualVinEntry, …) are shared, but the extra form UI a trigger
/// needs is consumer-specific (e.g. the original host application's VehicleInspection trigger needs a
/// VehicleInspectionType picker that references a host-owned DTO; other consumers will have their own triggers
/// with their own UI). A consumer registers one or more implementations in DI; the module's
/// <c>CampaignForm</c> renders each applicable contributor's <see cref="ComponentType"/> inline for the
/// currently-selected trigger, passing the campaign form model as a <c>Campaign</c> parameter (a
/// <c>CampaignDTO</c>, two-way bound through the shared reference).
///
/// This is the extension point that lets consumers inject their own triggers' UI without the module
/// knowing anything about them.
/// </summary>
public interface ICampaignTriggerFormFields
{
    /// <summary>True if this contributor renders fields for the given activation trigger.</summary>
    bool AppliesTo(ClaimableItemCampaignActivationTrigger trigger);

    /// <summary>
    /// A Blazor component that renders the trigger-specific fields. It must expose a
    /// <c>[Parameter] public CampaignDTO Campaign { get; set; }</c> (the form model) and bind its inputs
    /// to that instance so edits flow back into the form.
    /// </summary>
    Type ComponentType { get; }
}
