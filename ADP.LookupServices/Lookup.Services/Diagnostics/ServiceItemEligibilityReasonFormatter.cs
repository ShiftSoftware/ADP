using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Diagnostics;

/// <summary>
/// Formats human-readable rejection reasons for the eligibility trace. Kept out of the
/// evaluator so the rule logic stays terse and the text strings live next to the trace
/// types they feed.
/// </summary>
internal static class ServiceItemEligibilityReasonFormatter
{
    public static string Format(
        ServiceItemModel item,
        EligibilityRejectionStage stage,
        VehicleEntryModel vehicle) => stage switch
    {
        EligibilityRejectionStage.IsDeleted =>
            "Item is marked deleted.",
        EligibilityRejectionStage.Brand =>
            $"Vehicle BrandID={vehicle?.BrandID} not in item.BrandIDs=[{Join(item.BrandIDs)}].",
        EligibilityRejectionStage.Company =>
            $"Vehicle CompanyID={vehicle?.CompanyID} not in item.CompanyIDs=[{Join(item.CompanyIDs)}].",
        EligibilityRejectionStage.Country =>
            $"Vehicle CountryID={vehicle?.CountryID} not in item.CountryIDs=[{Join(item.CountryIDs)}].",
        EligibilityRejectionStage.CampaignWindow =>
            FormatCampaignWindow(item),
        EligibilityRejectionStage.VehicleApplicability =>
            FormatVehicleApplicability(item, vehicle),
        _ => "",
    };

    private static string FormatCampaignWindow(ServiceItemModel item) => item.CampaignActivationTrigger switch
    {
        ClaimableItemCampaignActivationTrigger.WarrantyActivation =>
            $"freeServiceStartDate not within [{Fmt(item.CampaignStartDate)}, {Fmt(item.CampaignEndDate)}].",
        ClaimableItemCampaignActivationTrigger.VehicleInspection =>
            $"No matching VehicleInspection for typeID={item.VehicleInspectionTypeID} within [{Fmt(item.CampaignStartDate)}, {Fmt(item.CampaignEndDate)}].",
        ClaimableItemCampaignActivationTrigger.ManualVinEntry =>
            $"No matching CampaignVinEntry for campaignID={item.CampaignID} within [{Fmt(item.CampaignStartDate)}, {Fmt(item.CampaignEndDate)}].",
        _ => $"CampaignActivationTrigger={item.CampaignActivationTrigger} did not satisfy the campaign-window rule.",
    };

    private static string FormatVehicleApplicability(ServiceItemModel item, VehicleEntryModel vehicle)
    {
        var modelCount = item.ModelCosts?.Count() ?? 0;
        return modelCount > 0
            ? $"None of the {modelCount} model costs prefix-match vehicle Katashiki={vehicle?.Katashiki} / Variant={vehicle?.VariantCode}."
            : "Item targets all vehicles, but vehicle is null and item is warranty-activated (gate excludes it).";
    }

    /// <summary>
    /// Reason text shown next to the status verdict (used by post-status claimability tracing).
    /// Kept here so all reason strings live in one place.
    /// </summary>
    public static string FormatStatusFallback(VehicleServiceItemDTO item) => item.StatusEnum switch
    {
        VehcileServiceItemStatuses.Processed => "Already processed (claim line matched).",
        VehcileServiceItemStatuses.Expired => $"ExpiresAt={item.ExpiresAt:yyyy-MM-dd} is in the past.",
        VehcileServiceItemStatuses.ActivationRequired => "Showing as activation required (warranty-activated, inactivated mode).",
        VehcileServiceItemStatuses.Cancelled => "Cancelled (set later in post-processing).",
        _ => "",
    };

    private static string Join(IEnumerable<long?> ids) => ids is null ? "null" : string.Join(",", ids);
    private static string Fmt(System.DateTime d) => d.ToString("yyyy-MM-dd");
}
