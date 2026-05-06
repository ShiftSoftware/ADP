using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Diagnostics;

/// <summary>
/// Structured trace of a single VehicleServiceItemEvaluator run. Populated only when
/// VehicleLookupRequestOptions.TraceServiceItemEvaluation is true. Use
/// ServiceItemTraceRenderer to render as Mermaid / HTML.
/// </summary>
public class ServiceItemTrace
{
    public string Vin { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime FinishedUtc { get; set; }
    public TimeSpan Elapsed => FinishedUtc - StartedUtc;

    public ServiceItemTraceInputs Inputs { get; set; }
    public List<ServiceItemTraceStageTiming> StageTimings { get; set; } = new();

    public ServiceItemTraceEligibility Eligibility { get; set; } = new();
    public List<ServiceItemTraceBuild> FreeBuilds { get; set; } = new();
    public List<ServiceItemTracePaidBuild> PaidBuilds { get; set; } = new();

    public ServiceItemTraceWarrantyRollingExpansion WarrantyRollingExpansion { get; set; } = new();
    public List<ServiceItemTraceTriggerExpansion> VehicleInspectionExpansions { get; set; } = new();
    public List<ServiceItemTraceTriggerExpansion> ManualVinEntryExpansions { get; set; } = new();

    public List<ServiceItemTraceStatus> Statuses { get; set; } = new();
    public ServiceItemTracePostProcessing PostProcessing { get; set; } = new();
    public ServiceItemTraceFinalResult FinalResult { get; set; } = new();

    /// <summary>
    /// Free-form notes raised during the run. Used for "known issue triggered" callouts
    /// (e.g. issue #14 non-sequential rolling expiry, issue #21 country filter null short-circuit,
    /// issue #22 VIN exclusion + activationRequired mismatch).
    /// </summary>
    public List<string> Notes { get; set; } = new();

    /// <summary>
    /// Resolved human-readable names for IDs that appear in this trace. Populated only
    /// when trace is enabled and the matching <c>LookupOptions.*Resolver</c> is configured.
    /// One round-trip per unique ID, batched at the end of evaluation.
    /// </summary>
    public ServiceItemTraceNameResolutions ResolvedNames { get; set; } = new();
}

public class ServiceItemTraceNameResolutions
{
    public Dictionary<long, string> Brands { get; set; } = new();
    public Dictionary<long, string> Companies { get; set; } = new();
    public Dictionary<long, string> Countries { get; set; } = new();
    public Dictionary<long, string> Regions { get; set; } = new();
}

public class ServiceItemTraceInputs
{
    public string Vin { get; set; }
    public long? BrandID { get; set; }
    public long? CompanyID { get; set; }
    public string Katashiki { get; set; }
    public string VariantCode { get; set; }
    public bool VehicleLoaded { get; set; }
    public long? SaleCountryID { get; set; }
    public DateTime? FreeServiceStartDate { get; set; }
    public bool FreeServiceStartDateOverriddenByDateShift { get; set; }
    public DateTime? FreeServiceStartDateBeforeDateShift { get; set; }
    public bool ShowingInactivatedItems { get; set; }
    public ServiceItemTraceAggregateCounts AggregateCounts { get; set; } = new();
}

public class ServiceItemTraceAggregateCounts
{
    public int CosmosServiceItems { get; set; }
    public int PaidServiceInvoices { get; set; }
    public int PaidServiceInvoiceLines { get; set; }
    public int ItemClaims { get; set; }
    public int VehicleInspections { get; set; }
    public int CampaignVinEntries { get; set; }
    public int FreeServiceItemExcludedVINs { get; set; }
    public int FreeServiceItemDateShifts { get; set; }
    public int VehicleServiceActivations { get; set; }
}

public class ServiceItemTraceEligibility
{
    public int InputCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<ServiceItemEligibilityDecision> Decisions { get; set; } = new();
}

public enum EligibilityVerdict { Accepted, Rejected }

public enum EligibilityRejectionStage
{
    None,
    IsDeleted,
    Brand,
    Company,
    Country,
    CampaignWindow,
    VehicleApplicability,
}

public class ServiceItemEligibilityDecision
{
    public string ServiceItemID { get; set; }
    public string CosmosId { get; set; }
    public string Name { get; set; }
    public EligibilityVerdict Verdict { get; set; }
    public EligibilityRejectionStage RejectionStage { get; set; }
    public string Reason { get; set; }
    public ServiceItemSnapshot Item { get; set; }
}

public class ServiceItemSnapshot
{
    public List<long?> BrandIDs { get; set; }
    public List<long?> CompanyIDs { get; set; }
    public List<long?> CountryIDs { get; set; }
    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }
    public ClaimableItemCampaignActivationTypes CampaignActivationType { get; set; }
    public ClaimableItemValidityMode ValidityMode { get; set; }
    public DateTime CampaignStartDate { get; set; }
    public DateTime CampaignEndDate { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public long? CampaignID { get; set; }
    public long? VehicleInspectionTypeID { get; set; }
    public long? MaximumMileage { get; set; }
    public int ModelCostCount { get; set; }
}

public class ServiceItemTraceBuild
{
    public string ServiceItemID { get; set; }
    public string Name { get; set; }
    public long? MatchedModelCostID { get; set; }
    public string MatchedKatashiki { get; set; }
    public string MatchedVariant { get; set; }
    public decimal? Cost { get; set; }
    public string PackageCode { get; set; }
    public ClaimableItemValidityMode ValidityMode { get; set; }
    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ServiceItemTracePaidBuild
{
    public string ServiceItemID { get; set; }
    public string PaidServiceInvoiceLineID { get; set; }
    public string Name { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string PackageCode { get; set; }
}

public class ServiceItemTraceWarrantyRollingExpansion
{
    public DateTime? AnchorDate { get; set; }
    public bool Skipped { get; set; }
    public string SkippedReason { get; set; }
    public List<ServiceItemTraceRollingItem> SequentialItems { get; set; } = new();
    public List<ServiceItemTraceRollingItem> NonSequentialItems { get; set; } = new();
}

public class ServiceItemTraceRollingItem
{
    public string ServiceItemID { get; set; }
    public string Name { get; set; }
    public long? MaximumMileage { get; set; }
    public int? ActiveFor { get; set; }
    public DurationType? ActiveForDurationType { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Note { get; set; }
}

public class ServiceItemTraceTriggerExpansion
{
    public string ServiceItemID { get; set; }
    public string Name { get; set; }
    public ClaimableItemCampaignActivationTypes ActivationType { get; set; }
    public int CandidateTriggerCount { get; set; }
    public int SelectedTriggerCount { get; set; }
    public string Note { get; set; }
    public List<ServiceItemTraceTriggerOutput> Outputs { get; set; } = new();
}

public class ServiceItemTraceTriggerOutput
{
    public string TriggerID { get; set; }
    public DateTime TriggerDate { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Note { get; set; }
}

public class ServiceItemTraceStatus
{
    public string ServiceItemID { get; set; }
    public string Name { get; set; }
    public string VehicleInspectionID { get; set; }
    public string CampaignVinEntryID { get; set; }
    public VehcileServiceItemStatuses Status { get; set; }
    public bool ClaimMatched { get; set; }
    public string ClaimMatchedJobNumber { get; set; }
    public string ClaimMatchedInvoiceNumber { get; set; }
    public bool Claimable { get; set; }
    public string ClaimabilityReason { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ActivatedAt { get; set; }
}

public class ServiceItemTracePostProcessing
{
    public bool VinExclusionApplied { get; set; }
    public int RemovedByVinExclusion { get; set; }
    public int IneligibleItemsPickedUp { get; set; }
    public List<ServiceItemTraceCancellation> DynamicallyCancelled { get; set; } = new();
}

public class ServiceItemTraceCancellation
{
    public string CancelledServiceItemID { get; set; }
    public long? CancelledMaximumMileage { get; set; }
    public string SupersededByServiceItemID { get; set; }
    public long? SupersededByMaximumMileage { get; set; }
}

public class ServiceItemTraceFinalResult
{
    public int Count { get; set; }
    public bool ActivationRequired { get; set; }
    public List<ServiceItemTraceFinalItem> Items { get; set; } = new();
}

public class ServiceItemTraceFinalItem
{
    public string ServiceItemID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
    public bool Claimable { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long? MaximumMileage { get; set; }
    public string VehicleInspectionID { get; set; }
    public string CampaignVinEntryID { get; set; }
}

public class ServiceItemTraceStageTiming
{
    public string Stage { get; set; }
    public TimeSpan Elapsed { get; set; }
}
