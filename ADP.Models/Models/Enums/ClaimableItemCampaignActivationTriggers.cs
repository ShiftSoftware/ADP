using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum ClaimableItemCampaignActivationTrigger
{
    [Description("On Warranty Activation")]
    WarrantyActivation = 1,

    [Description("On Vehicle Inspection")]
    VehicleInspection = 2,
}