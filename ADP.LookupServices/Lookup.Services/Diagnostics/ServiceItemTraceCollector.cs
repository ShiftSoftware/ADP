using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Diagnostics;

/// <summary>
/// Mutable trace collector wired into <c>VehicleServiceItemEvaluator</c>. The evaluator always
/// holds a non-null collector — when tracing is off it holds <see cref="Disabled"/>, whose
/// every method is a no-op virtual override, so call sites stay branch-free and zero-allocation.
///
/// <para>The collector exposes two flavours of API:</para>
/// <list type="bullet">
///   <item>
///     <b>Recording overloads</b> (<c>RecordX(businessObj, ...)</c>) — take raw business
///     objects (DTOs, models, aggregate) and build the trace POCO internally. Call sites
///     stay one line, no <c>if (Trace.IsEnabled)</c> guards needed.
///   </item>
///   <item>
///     <b>Stage scope</b> (<see cref="Stage"/>) — returns an <see cref="IDisposable"/> that
///     starts a stopwatch on construction and stops it on dispose. Use with <c>using</c>
///     so you can't forget the end call.
///   </item>
/// </list>
/// Call <see cref="Build"/> at the end of evaluation to finalize and return the immutable
/// <see cref="ServiceItemTrace"/>.
/// </summary>
public class ServiceItemTraceCollector
{
    public static readonly ServiceItemTraceCollector Disabled = new DisabledCollector();
    private static readonly IDisposable noopScope = new NoopScope();

    public virtual bool IsEnabled => true;

    private readonly Stopwatch totalStopwatch = Stopwatch.StartNew();
    private readonly Dictionary<string, Stopwatch> stageStopwatches = new();
    private readonly ServiceItemTrace trace = new();

    public ServiceItemTraceCollector(string vin)
    {
        trace.Vin = vin;
        trace.StartedUtc = DateTime.UtcNow;
    }

    protected ServiceItemTraceCollector() { }

    // ---- Stage timing ----

    /// <summary>
    /// Starts a stopwatch for the named stage and returns a disposable that stops it.
    /// Use with <c>using (Trace.Stage("X")) { ... }</c> so the end fires on every exit path.
    /// </summary>
    public virtual IDisposable Stage(string name)
    {
        if (!stageStopwatches.TryGetValue(name, out var sw))
            stageStopwatches[name] = sw = new Stopwatch();
        sw.Start();
        return new StageScope(sw);
    }

    private sealed class StageScope : IDisposable
    {
        private readonly Stopwatch sw;
        public StageScope(Stopwatch sw) { this.sw = sw; }
        public void Dispose() => sw.Stop();
    }

    private sealed class NoopScope : IDisposable { public void Dispose() { } }

    // ---- Inputs / final result / notes ----

    public virtual void RecordInputs(
        VehicleEntryModel vehicle,
        DateTime? freeServiceStartDate,
        DateTime? freeServiceStartDateBeforeShift,
        bool showingInactivatedItems,
        IEnumerable<ServiceItemModel> serviceItems,
        CompanyDataAggregateModel aggregate)
    {
        trace.Inputs = new ServiceItemTraceInputs
        {
            Vin = vehicle?.VIN,
            BrandID = vehicle?.BrandID,
            CompanyID = vehicle?.CompanyID,
            Katashiki = vehicle?.Katashiki,
            VariantCode = vehicle?.VariantCode,
            VehicleLoaded = vehicle is not null,
            SaleCountryID = vehicle?.CountryID,
            FreeServiceStartDate = freeServiceStartDate,
            FreeServiceStartDateBeforeDateShift = freeServiceStartDateBeforeShift,
            FreeServiceStartDateOverriddenByDateShift = freeServiceStartDateBeforeShift != freeServiceStartDate && !showingInactivatedItems,
            ShowingInactivatedItems = showingInactivatedItems,
            AggregateCounts = new ServiceItemTraceAggregateCounts
            {
                CosmosServiceItems = serviceItems?.Count() ?? 0,
                PaidServiceInvoices = aggregate.PaidServiceInvoices?.Count() ?? 0,
                PaidServiceInvoiceLines = aggregate.PaidServiceInvoices?.Sum(p => p?.Lines?.Count() ?? 0) ?? 0,
                ItemClaims = aggregate.ItemClaims?.Count() ?? 0,
                VehicleInspections = aggregate.VehicleInspections?.Count() ?? 0,
                CampaignVinEntries = aggregate.CampaignVinEntries?.Count() ?? 0,
                FreeServiceItemExcludedVINs = aggregate.FreeServiceItemExcludedVINs?.Count() ?? 0,
                FreeServiceItemDateShifts = aggregate.FreeServiceItemDateShifts?.Count() ?? 0,
                VehicleServiceActivations = aggregate.VehicleServiceActivations?.Count() ?? 0,
            },
        };
    }

    public virtual void RecordFinalResult(List<VehicleServiceItemDTO> result, bool activationRequired)
    {
        trace.FinalResult = new ServiceItemTraceFinalResult
        {
            Count = result.Count,
            ActivationRequired = activationRequired,
            Items = result.Select(i => new ServiceItemTraceFinalItem
            {
                ServiceItemID = i.ServiceItemID,
                Name = i.Name,
                Type = i.Type,
                Status = i.Status,
                Claimable = i.Claimable,
                ActivatedAt = i.ActivatedAt,
                ExpiresAt = i.ExpiresAt,
                MaximumMileage = i.MaximumMileage,
                VehicleInspectionID = i.VehicleInspectionID,
                CampaignVinEntryID = i.CampaignVinEntryID,
            }).ToList(),
        };
    }

    public virtual void Note(string message) => trace.Notes.Add(message);

    // ---- Eligibility ----

    public virtual void RecordEligibilityInputCount(int count) => trace.Eligibility.InputCount = count;

    public virtual void RecordEligibilityDecision(
        ServiceItemModel item,
        EligibilityRejectionStage stage,
        VehicleEntryModel vehicle)
    {
        var accepted = stage == EligibilityRejectionStage.None;
        var decision = new ServiceItemEligibilityDecision
        {
            ServiceItemID = item.IntegrationID,
            CosmosId = item.id,
            Name = item.Name?.Values?.FirstOrDefault(),
            Item = Snapshot(item),
            Verdict = accepted ? EligibilityVerdict.Accepted : EligibilityVerdict.Rejected,
            RejectionStage = stage,
            Reason = accepted ? null : ServiceItemEligibilityReasonFormatter.Format(item, stage, vehicle),
        };
        trace.Eligibility.Decisions.Add(decision);
        if (accepted) trace.Eligibility.AcceptedCount++;
        else trace.Eligibility.RejectedCount++;
    }

    // ---- DTO builds ----

    public virtual void RecordFreeBuild(
        ServiceItemModel item,
        VehicleServiceItemDTO dto,
        ServiceItemCostModel matchedCost,
        string languageCode)
    {
        trace.FreeBuilds.Add(new ServiceItemTraceBuild
        {
            ServiceItemID = dto.ServiceItemID,
            Name = Utility.GetLocalizedText(item.Name, languageCode),
            MatchedModelCostID = matchedCost?.ID,
            MatchedKatashiki = matchedCost?.Katashiki,
            MatchedVariant = matchedCost?.Variant,
            Cost = dto.Cost,
            PackageCode = dto.PackageCode,
            ValidityMode = item.ValidityMode,
            CampaignActivationTrigger = item.CampaignActivationTrigger,
            ActivatedAt = dto.ActivatedAt,
            ExpiresAt = dto.ExpiresAt,
        });
    }

    public virtual void RecordPaidBuild(
        PaidServiceInvoiceModel invoice,
        PaidServiceInvoiceLineModel line,
        VehicleServiceItemDTO dto,
        string languageCode)
    {
        trace.PaidBuilds.Add(new ServiceItemTracePaidBuild
        {
            ServiceItemID = dto.ServiceItemID,
            PaidServiceInvoiceLineID = line.IntegrationID,
            Name = Utility.GetLocalizedText(line.ServiceItem?.Name, languageCode),
            ActivatedAt = dto.ActivatedAt,
            ExpiresAt = dto.ExpiresAt,
            PackageCode = dto.PackageCode,
        });
    }

    // ---- Warranty rolling expiry ----

    public virtual void RecordWarrantyRollingSkipped(string reason)
    {
        trace.WarrantyRollingExpansion = new ServiceItemTraceWarrantyRollingExpansion
        {
            AnchorDate = null,
            Skipped = true,
            SkippedReason = reason,
        };
    }

    public virtual void RecordWarrantyRollingAnchor(DateTime anchorDate)
    {
        trace.WarrantyRollingExpansion = new ServiceItemTraceWarrantyRollingExpansion { AnchorDate = anchorDate };
    }

    public virtual void RecordWarrantyRollingItem(bool sequential, VehicleServiceItemDTO item, string note = null)
    {
        var record = new ServiceItemTraceRollingItem
        {
            ServiceItemID = item.ServiceItemID,
            Name = item.Name,
            MaximumMileage = sequential ? item.MaximumMileage : null,
            ActiveFor = item.ActiveFor,
            ActiveForDurationType = item.ActiveForDurationType,
            ActivatedAt = item.ActivatedAt,
            ExpiresAt = item.ExpiresAt,
            Note = note,
        };
        if (sequential) trace.WarrantyRollingExpansion.SequentialItems.Add(record);
        else trace.WarrantyRollingExpansion.NonSequentialItems.Add(record);
    }

    // ---- Per-trigger expansions (vehicle inspection / manual VIN entry) ----

    /// <summary>
    /// Records a vehicle-inspection-driven expansion. <paramref name="selectedTriggers"/>
    /// and <paramref name="clones"/> must be parallel — index N in <paramref name="clones"/>
    /// is the DTO produced from the trigger at index N.
    /// </summary>
    public virtual void RecordVehicleInspectionExpansion(
        VehicleServiceItemDTO sourceItem,
        int candidateCount,
        IList<VehicleInspectionModel> selectedTriggers,
        IList<VehicleServiceItemDTO> clones,
        string fallbackNote)
    {
        var expansion = new ServiceItemTraceTriggerExpansion
        {
            ServiceItemID = sourceItem.ServiceItemID,
            Name = sourceItem.Name,
            ActivationType = sourceItem.CampaignActivationType,
            CandidateTriggerCount = candidateCount,
            SelectedTriggerCount = selectedTriggers.Count,
            Note = fallbackNote,
        };
        for (var i = 0; i < selectedTriggers.Count; i++)
        {
            var trigger = selectedTriggers[i];
            var clone = i < clones.Count ? clones[i] : null;
            expansion.Outputs.Add(trigger is null
                ? new ServiceItemTraceTriggerOutput
                {
                    TriggerID = "(null)",
                    Note = "NRE-on-misconfig fallback: unset/unexpected CampaignActivationType produced a null inspection. Downstream NRE on inspection.id is intentional (pinned behavior).",
                }
                : new ServiceItemTraceTriggerOutput
                {
                    TriggerID = trigger.id,
                    TriggerDate = trigger.InspectionDate.DateTime,
                    ActivatedAt = clone?.ActivatedAt,
                    ExpiresAt = clone?.ExpiresAt,
                });
        }
        trace.VehicleInspectionExpansions.Add(expansion);
    }

    public virtual void RecordManualVinEntryExpansion(
        VehicleServiceItemDTO sourceItem,
        int candidateCount,
        IList<CampaignVinEntryModel> selectedTriggers,
        IList<VehicleServiceItemDTO> clones,
        string fallbackNote)
    {
        var expansion = new ServiceItemTraceTriggerExpansion
        {
            ServiceItemID = sourceItem.ServiceItemID,
            Name = sourceItem.Name,
            ActivationType = sourceItem.CampaignActivationType,
            CandidateTriggerCount = candidateCount,
            SelectedTriggerCount = selectedTriggers.Count,
            Note = fallbackNote,
        };
        for (var i = 0; i < selectedTriggers.Count; i++)
        {
            var trigger = selectedTriggers[i];
            var clone = i < clones.Count ? clones[i] : null;
            expansion.Outputs.Add(trigger is null
                ? new ServiceItemTraceTriggerOutput
                {
                    TriggerID = "(null)",
                    Note = "NRE-on-misconfig fallback: unset/unexpected CampaignActivationType produced a null CampaignVinEntry. Downstream NRE on entry.id is intentional (pinned behavior).",
                }
                : new ServiceItemTraceTriggerOutput
                {
                    TriggerID = trigger.id,
                    TriggerDate = trigger.RecordedDate.DateTime,
                    ActivatedAt = clone?.ActivatedAt,
                    ExpiresAt = clone?.ExpiresAt,
                });
        }
        trace.ManualVinEntryExpansions.Add(expansion);
    }

    // ---- Status & claimability ----

    public virtual void RecordStatus(VehicleServiceItemDTO item, string claimabilityReason)
    {
        trace.Statuses.Add(new ServiceItemTraceStatus
        {
            ServiceItemID = item.ServiceItemID,
            Name = item.Name,
            VehicleInspectionID = item.VehicleInspectionID,
            CampaignVinEntryID = item.CampaignVinEntryID,
            Status = item.StatusEnum,
            ClaimMatched = item.StatusEnum == VehcileServiceItemStatuses.Processed,
            ClaimMatchedJobNumber = item.JobNumber,
            ClaimMatchedInvoiceNumber = item.InvoiceNumber,
            Claimable = item.Claimable,
            ClaimabilityReason = claimabilityReason,
            ActivatedAt = item.ActivatedAt,
            ExpiresAt = item.ExpiresAt,
        });
    }

    // ---- Post-processing ----

    public virtual void RecordVinExclusion(int removedCount) => EnsurePostProcessing(p =>
    {
        p.VinExclusionApplied = true;
        p.RemovedByVinExclusion = removedCount;
    });

    public virtual void RecordIneligiblePickup(int count) => EnsurePostProcessing(p => p.IneligibleItemsPickedUp = count);

    public virtual void RecordCancellation(VehicleServiceItemDTO cancelled, VehicleServiceItemDTO supersededBy) => EnsurePostProcessing(p =>
        p.DynamicallyCancelled.Add(new ServiceItemTraceCancellation
        {
            CancelledServiceItemID = cancelled.ServiceItemID,
            CancelledMaximumMileage = cancelled.MaximumMileage,
            SupersededByServiceItemID = supersededBy.ServiceItemID,
            SupersededByMaximumMileage = supersededBy.MaximumMileage,
        }));

    private void EnsurePostProcessing(Action<ServiceItemTracePostProcessing> mutate) =>
        mutate(trace.PostProcessing ??= new ServiceItemTracePostProcessing());

    // ---- Lifecycle ----

    /// <summary>
    /// Returns the in-flight trace so an enrichment pass (e.g. async name resolution)
    /// can mutate it before <see cref="Build"/>. Null when the collector is disabled.
    /// </summary>
    public virtual ServiceItemTrace Peek() => trace;

    public virtual ServiceItemTrace Build()
    {
        totalStopwatch.Stop();
        trace.FinishedUtc = DateTime.UtcNow;
        trace.StageTimings = stageStopwatches
            .Select(kv => new ServiceItemTraceStageTiming { Stage = kv.Key, Elapsed = kv.Value.Elapsed })
            .ToList();
        return trace;
    }

    private static ServiceItemSnapshot Snapshot(ServiceItemModel item) => new()
    {
        BrandIDs = item.BrandIDs?.ToList(),
        CompanyIDs = item.CompanyIDs?.ToList(),
        CountryIDs = item.CountryIDs?.ToList(),
        CampaignActivationTrigger = item.CampaignActivationTrigger,
        CampaignActivationType = item.CampaignActivationType,
        ValidityMode = item.ValidityMode,
        CampaignStartDate = item.CampaignStartDate,
        CampaignEndDate = item.CampaignEndDate,
        ValidFrom = item.ValidFrom,
        ValidTo = item.ValidTo,
        CampaignID = item.CampaignID,
        VehicleInspectionTypeID = item.VehicleInspectionTypeID,
        MaximumMileage = item.MaximumMileage,
        ModelCostCount = item.ModelCosts?.Count() ?? 0,
    };

    /// <summary>
    /// No-op collector returned by <see cref="Disabled"/>. Every recording method is overridden
    /// to do nothing — the JIT inlines them, so trace-off has no measurable cost. <see cref="Stage"/>
    /// returns a shared singleton disposable.
    /// </summary>
    private sealed class DisabledCollector : ServiceItemTraceCollector
    {
        public override bool IsEnabled => false;
        public override IDisposable Stage(string name) => noopScope;
        public override void RecordInputs(VehicleEntryModel vehicle, DateTime? freeServiceStartDate, DateTime? freeServiceStartDateBeforeShift, bool showingInactivatedItems, IEnumerable<ServiceItemModel> serviceItems, CompanyDataAggregateModel aggregate) { }
        public override void RecordFinalResult(List<VehicleServiceItemDTO> result, bool activationRequired) { }
        public override void Note(string message) { }
        public override void RecordEligibilityInputCount(int count) { }
        public override void RecordEligibilityDecision(ServiceItemModel item, EligibilityRejectionStage stage, VehicleEntryModel vehicle) { }
        public override void RecordFreeBuild(ServiceItemModel item, VehicleServiceItemDTO dto, ServiceItemCostModel matchedCost, string languageCode) { }
        public override void RecordPaidBuild(PaidServiceInvoiceModel invoice, PaidServiceInvoiceLineModel line, VehicleServiceItemDTO dto, string languageCode) { }
        public override void RecordWarrantyRollingSkipped(string reason) { }
        public override void RecordWarrantyRollingAnchor(DateTime anchorDate) { }
        public override void RecordWarrantyRollingItem(bool sequential, VehicleServiceItemDTO item, string note = null) { }
        public override void RecordVehicleInspectionExpansion(VehicleServiceItemDTO sourceItem, int candidateCount, IList<VehicleInspectionModel> selectedTriggers, IList<VehicleServiceItemDTO> clones, string fallbackNote) { }
        public override void RecordManualVinEntryExpansion(VehicleServiceItemDTO sourceItem, int candidateCount, IList<CampaignVinEntryModel> selectedTriggers, IList<VehicleServiceItemDTO> clones, string fallbackNote) { }
        public override void RecordStatus(VehicleServiceItemDTO item, string claimabilityReason) { }
        public override void RecordVinExclusion(int removedCount) { }
        public override void RecordIneligiblePickup(int count) { }
        public override void RecordCancellation(VehicleServiceItemDTO cancelled, VehicleServiceItemDTO supersededBy) { }
        public override ServiceItemTrace Peek() => null;
        public override ServiceItemTrace Build() => null;
    }
}
