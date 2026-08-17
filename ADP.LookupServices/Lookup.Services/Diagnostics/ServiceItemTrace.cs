using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
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
    public ServiceItemTraceBaseScheduleCap BaseScheduleCap { get; set; } = new();
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
    /// <summary>The resolved owning CompanyID (activation-aware, see <c>VehicleOwnership</c>) that eligibility filtered against.</summary>
    public long? CompanyID { get; set; }
    public string Katashiki { get; set; }
    public string VariantCode { get; set; }
    public bool VehicleLoaded { get; set; }
    /// <summary>The resolved owning CountryID (activation-aware, see <c>VehicleOwnership</c>) that eligibility filtered against.</summary>
    public long? SaleCountryID { get; set; }
    public DateTime? FreeServiceStartDate { get; set; }
    public bool FreeServiceStartDateOverriddenByDateShift { get; set; }
    public DateTime? FreeServiceStartDateBeforeDateShift { get; set; }
    public bool ShowingInactivatedItems { get; set; }

    /// <summary>
    /// Earliest non-deleted ItemClaim.ClaimDate for this VIN, mirroring
    /// <c>VehicleWarrantyDTO.DeFactoServiceStartDate</c>. Populated whenever any non-deleted
    /// claim exists. When this matches <see cref="FreeServiceStartDate"/> AND no per-VIN
    /// date shift applied, the warranty evaluator's claim-anchored fallback is the most
    /// likely source — the evaluator records a clarifying <see cref="ServiceItemTrace.Notes"/>
    /// entry in that case (e.g. broker without invoice + previously claimed).
    /// </summary>
    public DateTime? DeFactoServiceStartDate { get; set; }
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

    /// <summary>
    /// Items returned locked or missed. Counted apart from both other totals because they are
    /// neither offered nor dropped, and folding them into either would misreport the screen.
    /// </summary>
    public int UnclaimableCount { get; set; }
    public List<ServiceItemEligibilityDecision> Decisions { get; set; } = new();

    /// <summary>
    /// How this deployment reads milestones out of its service codes. Recorded whether or not any
    /// milestone condition was evaluated, because "this deployment cannot read milestones" and
    /// "this vehicle has none" are different findings that look identical everywhere else.
    /// </summary>
    public ServiceItemTraceMilestoneReader MilestoneReader { get; set; } = new();
}

/// <summary>
/// The state of the milestone reader for this lookup.
/// <para>
/// A reader that reads nothing is a configuration to fix, not a customer without service history,
/// and on a card the two are indistinguishable. Reporting it per lookup is what makes the
/// difference visible without waiting for the estate-wide audit.
/// </para>
/// </summary>
public class ServiceItemTraceMilestoneReader
{
    /// <summary>Whether the reader can produce a milestone at all.</summary>
    public bool CanRead { get; set; }

    /// <summary>
    /// The conventions in use, by name, in the order they are tried. Empty for a host-supplied
    /// resolver, which declares none.
    /// </summary>
    public List<string> Conventions { get; set; } = new();

    /// <summary>Settings that could not be used, with the reason.</summary>
    public List<ServiceMilestoneConfigurationProblem> Problems { get; set; } = new();
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
    CustomCondition,

    /// <summary>
    /// A custom condition failed, and the item is shown locked rather than dropped. Rejected in the
    /// sense that it is not being offered — the customer can still earn it.
    /// </summary>
    CustomConditionLocked,

    /// <summary>
    /// A custom condition failed and the window has closed. Also shown rather than dropped, which is
    /// the point: an item that vanishes explains nothing.
    /// </summary>
    CustomConditionMissed,
}

/// <summary>
/// A service-history code that did not count towards a milestone condition, and the reason it did
/// not.
/// <para>
/// Recorded because the alternative is guessing. Nothing distinguishes, on the screen, a service a
/// customer never had from one the reader could not read: both are simply absent. These are the
/// only evidence a lookup can produce about which of the two it is — and the reason matters, since
/// a code excluded on its qualifier is a rule to calibrate with the deployment, while a code the
/// reader could not read at all is a convention to fix.
/// </para>
/// </summary>
public class ServiceItemMilestoneNearMiss
{
    /// <summary>Why the code did not count.</summary>
    public ServiceItemMilestoneNearMissReason Reason { get; set; }

    /// <summary>The code, as the source system wrote it.</summary>
    public string PackageCode { get; set; }

    /// <summary>
    /// The milestone the code named, in kilometres, or null when nothing was read out of it —
    /// which is the whole of what is known about an
    /// <see cref="ServiceItemMilestoneNearMissReason.Unresolved"/> code.
    /// </summary>
    public long? Milestone { get; set; }

    /// <summary>The programme the code was booked under, or null when it named none.</summary>
    public string Program { get; set; }

    /// <summary>The qualifier the code carried, or null when it carried none.</summary>
    public string Qualifier { get; set; }
}

/// <summary>Why a service-history code did not count towards a milestone condition.</summary>
public enum ServiceItemMilestoneNearMissReason
{
    /// <summary>
    /// The reader made nothing of the code. Ordinary for unscheduled work, which is most of it —
    /// and the shape of a convention that has stopped fitting, which is why it is recorded at all.
    /// A reader with no conventions configured reports every code this way.
    /// </summary>
    Unresolved = 0,

    /// <summary>A milestone was read, under a programme this condition does not count.</summary>
    ProgrammeFiltered = 1,

    /// <summary>A milestone was read, under a programme that counts, and dropped on its qualifier.</summary>
    QualifierFiltered = 2,
}

public class ServiceItemTraceBaseScheduleCap
{
    public long? MaximumMileage { get; set; }
    public List<ServiceItemBaseScheduleCapDecision> Decisions { get; set; } = new();
}

public class ServiceItemBaseScheduleCapDecision
{
    public string ServiceItemID { get; set; }
    public string Name { get; set; }
    public bool Included { get; set; }
    public BaseScheduleCapDecisionReason Reason { get; set; }
    public EligibilityRejectionStage StaticRejectionStage { get; set; }
    public ServiceItemSnapshot Item { get; set; }
}

public enum BaseScheduleCapDecisionReason
{
    None,
    StaticFilter,
    ProgramRole,
    ActivationTrigger,
    ValidityMode,
    MissingMaximumMileage,
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

    /// <summary>
    /// The prerequisites this item waits on, when it is locked or missed. Empty otherwise.
    /// </summary>
    public List<VehicleServiceItemPrerequisiteDTO> Prerequisites { get; set; } = new();

    /// <summary>
    /// Codes this item's milestone conditions passed over, with the reason. See
    /// <see cref="ServiceItemMilestoneNearMiss"/>.
    /// </summary>
    public List<ServiceItemMilestoneNearMiss> MilestoneNearMisses { get; set; } = new();
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
    public ServiceItemProgramRole ProgramRole { get; set; }
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
