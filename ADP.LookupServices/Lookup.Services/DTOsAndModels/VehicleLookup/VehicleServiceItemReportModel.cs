using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleServiceItemReportModel
{
    public string VIN { get; set; }
    public DateTime? FreeServiceItemStartDate { get; set; }
    public string ServiceItemId { get; set; }
    public string ServiceItemName { get; set; }
    public string GroupName { get; set; }
    public int? GroupTabOrder { get; set; }
    public bool? GroupIsDefault { get; set; }
    public bool? GroupIsSequential { get; set; }
    /// <summary>The displayed status: locked or missed when a lock is present, otherwise the lifecycle status.</summary>
    public string Status { get; set; }
    /// <summary>The displayed status code; existing lifecycle codes 0–4 are preserved, with Locked = 5 and Missed = 6.</summary>
    public VehicleServiceItemReportStatuses? StatusEnum { get; set; }
    public string Type { get; set; }
    public VehcileServiceItemTypes? TypeEnum { get; set; }
    public decimal? Price { get; set; }
    public string MenuCode { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTimeOffset? ClaimDate { get; set; }
    public long? CampaignId { get; set; }
    public string CampaignUniqueReference { get; set; }
    public long? ModelCostId { get; set; }
    public string PaidServiceInvoiceLineId { get; set; }
    public string CompanyName { get; set; }
    public string InvoiceNumber { get; set; }
    public string JobNumber { get; set; }
    public long? MaximumMileage { get; set; }
    public bool? Claimable { get; set; }
    public ClaimableItemClaimingMethod? ClaimingMethod { get; set; }
    public string VehicleInspectionId { get; set; }
    public string VehicleInspectionTypeId { get; set; }
}
