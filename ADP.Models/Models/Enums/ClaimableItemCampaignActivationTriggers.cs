using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum ClaimableItemCampaignActivationTrigger
{
    [Description("On Warranty Activation")]
    WarrantyActivation = 1,

    [Description("On Used Car Vehicle Inspection")]
    VehicleInspection = 2,

    [Description("On Taxi Card Registration")]
    OnTaxiCardRegistration = 3,

    [Description("On Dynamic Survey Answer")]
    OnDynamicSurveyAnswer = 4,

    [Description("On New Owner Ceremony Visit")]
    OnNewOwnerCeremonyVisit = 5,

    [Description("On University Graduation")]
    OnUniversityGraduation = 6,
}