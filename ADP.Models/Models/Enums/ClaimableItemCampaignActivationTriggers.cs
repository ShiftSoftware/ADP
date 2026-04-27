using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Defines what event triggers a claimable item campaign to become active for a vehicle.
/// </summary>
[Docable]
public enum ClaimableItemCampaignActivationTrigger
{
    /// <summary>
    /// The campaign activates when the vehicle's warranty is activated.
    /// </summary>
    [Description("On Warranty Activation")]
    WarrantyActivation = 1,

    /// <summary>
    /// The campaign activates when a vehicle inspection is performed.
    /// </summary>
    [Description("On Vehicle Inspection")]
    VehicleInspection = 2,

    /// <summary>
    /// The campaign activates when a per-VIN entry is manually recorded against the campaign
    /// (e.g., admin tags specific VINs as eligible for a one-off reward).
    /// </summary>
    [Description("On Manual VIN Entry")]
    ManualVinEntry = 3,
}