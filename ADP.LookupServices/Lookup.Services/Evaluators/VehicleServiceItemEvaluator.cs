using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public partial class VehicleServiceItemEvaluator
{
    private readonly CompanyDataAggregateModel companyDataAggregate;
    private readonly IVehicleLookupStorageService lookupCosmosService;
    private readonly LookupOptions options;
    private readonly IServiceProvider services;

    /// <summary>
    /// Optional diagnostic trace collector. Defaults to <see cref="ServiceItemTraceCollector.Disabled"/>
    /// (no-op virtual overrides), so the production hot path is branch-free and zero-allocation.
    /// Populate via object initializer when constructing the evaluator. Trace plumbing lives
    /// in <c>VehicleServiceItemEvaluator.Tracing.cs</c>; see also <see cref="ServiceItemTraceCollector"/>.
    /// </summary>
    public ServiceItemTraceCollector Trace { get; set; } = ServiceItemTraceCollector.Disabled;

    public VehicleServiceItemEvaluator(IVehicleLookupStorageService lookupCosmosService, CompanyDataAggregateModel companyDataAggregate, LookupOptions options, IServiceProvider services)
    {
        this.lookupCosmosService = lookupCosmosService;
        this.companyDataAggregate = companyDataAggregate;
        this.options = options;
        this.services = services;
    }

    /// <summary>
    /// Evaluates the service items available for a vehicle. Reads as an 8-step recipe:
    /// load catalog → resolve activation mode → build eligible free + paid items →
    /// expand by trigger (warranty rolling expiry, vehicle inspection, manual VIN entry) →
    /// determine status & claimability → post-process (VIN exclusion, ineligible pickup,
    /// dynamic cancellation) → stamp signatures & claim warnings.
    /// <paramref name="vehicle"/> supplies spec (brand, Katashiki/Variant); <paramref name="ownership"/>
    /// supplies the resolved owning company/country for the eligibility filters — never read
    /// those off the entry, its ownership can be stale (see <see cref="VehicleOwnershipEvaluator"/>).
    /// <paramref name="brokerSaleInformation"/> (from <c>VehicleSaleInformation.Broker</c>) only
    /// feeds the un-invoiced broker claim warning — pass null when sale info is unavailable.
    /// </summary>
    public async Task<(IEnumerable<VehicleServiceItemDTO> serviceItems, bool activationRequired)> Evaluate(
        VehicleEntryModel vehicle,
        VehicleOwnership ownership,
        DateTime? freeServiceStartDate,
        string languageCode,
        VehicleBrokerSaleInformation brokerSaleInformation = null)
    {
        var requestedStartDate = freeServiceStartDate;
        var serviceItems = (await LoadServiceItemCatalog()).ToList();
        (freeServiceStartDate, var showingInactivatedItems) = ResolveActivationMode(vehicle, freeServiceStartDate);

        // De facto date (earliest non-deleted ItemClaim.ClaimDate) — same computation
        // WarrantyAndFreeServiceDateEvaluator uses to populate VehicleWarrantyDTO.DeFactoServiceStartDate.
        // Recorded on the trace whenever any non-deleted claim exists. When it matches the
        // caller-supplied requestedStartDate AND no per-VIN date shift was applied, the warranty
        // evaluator's claim-anchored fallback is the most likely source — flagged via Trace.Note
        // below for diagnostic clarity (broker-without-invoice + previously claimed scenario).
        var deFactoServiceStartDate = companyDataAggregate.ItemClaims?
            .Where(x => x is not null && !x.IsDeleted)
            .Select(x => (DateTimeOffset?)x.ClaimDate)
            .Min()?
            .ToUniversalTime().Date;

        Trace.RecordInputs(vehicle, ownership, freeServiceStartDate, requestedStartDate, showingInactivatedItems, serviceItems, companyDataAggregate, deFactoServiceStartDate);

        var dateShiftApplied = companyDataAggregate.FreeServiceItemDateShifts?.Any() ?? false;
        if (deFactoServiceStartDate is not null && requestedStartDate == deFactoServiceStartDate && !dateShiftApplied && !showingInactivatedItems)
            Trace.Note($"FreeServiceStartDate matches DeFactoServiceStartDate ({deFactoServiceStartDate:yyyy-MM-dd}) — claim-anchored fallback is the most likely source (broker-without-invoice + previously claimed). A coincidental match with broker/activation/sale date is also possible.");

        var result = new List<VehicleServiceItemDTO>();
        long? baseScheduleMaximumMileage;

        using (Trace.Stage("BaseScheduleCap"))        baseScheduleMaximumMileage = DeriveBaseScheduleMaximumMileage(serviceItems, vehicle, ownership, freeServiceStartDate);
        using (Trace.Stage("Eligibility"))            result.AddRange(BuildEligibleFreeItems(serviceItems, vehicle, ownership, freeServiceStartDate, languageCode, baseScheduleMaximumMileage));
        using (Trace.Stage("PaidItems"))              result.AddRange(BuildPaidItems(languageCode));
        using (Trace.Stage("WarrantyRollingExpiry"))  ApplyWarrantyRollingExpiry(result, freeServiceStartDate);
        using (Trace.Stage("InspectionExpansion"))    ApplyVehicleInspectionExpansion(result);
        using (Trace.Stage("ManualVinExpansion"))     ApplyManualVinEntryExpansion(result);

        bool activationRequired;
        using (Trace.Stage("StatusAndClaimability"))  activationRequired = await CalculateServiceItemStatusAndClaimability(result, showingInactivatedItems, languageCode);
        using (Trace.Stage("PostProcessing"))         result = await ApplyPostProcessing(result, serviceItems, vehicle, languageCode, activationRequired);
        using (Trace.Stage("Signatures"))             await StampSignaturesPrintUrlsAndWarnings(result, ServiceActivation, languageCode, brokerSaleInformation);

        Trace.RecordFinalResult(result, activationRequired);
        if (Trace.IsEnabled)
            using (Trace.Stage("NameResolution"))
                await EnrichTraceWithResolvedNames(languageCode);

        return (result, activationRequired);
    }

    // ===== Step methods =====

    private VehicleServiceActivation ServiceActivation =>
        companyDataAggregate.VehicleServiceActivations.FirstOrDefault();

    private async Task<IEnumerable<ServiceItemModel>> LoadServiceItemCatalog()
    {
        using (Trace.Stage("LoadCatalog"))
            return await lookupCosmosService.GetServiceItemsAsync(useCache: true);
    }

    /// <summary>
    /// Applies the per-VIN date shift (if any) and the "show inactivated" fallback
    /// (when a date is required for warranty-activated items but the caller didn't provide one).
    /// </summary>
    private (DateTime? freeServiceStartDate, bool showingInactivatedItems) ResolveActivationMode(
        VehicleEntryModel vehicle,
        DateTime? freeServiceStartDate)
    {
        var shiftDay = companyDataAggregate.FreeServiceItemDateShifts?.FirstOrDefault(x => x.VIN == vehicle?.VIN);
        if (shiftDay is not null)
            freeServiceStartDate = shiftDay.NewDate;

        if (options.IncludeInactivatedFreeServiceItems && freeServiceStartDate is null)
            return (options.GetUtcNow().Date, showingInactivatedItems: true);

        return (freeServiceStartDate, showingInactivatedItems: false);
    }

    /// <summary>
    /// Filters the catalog to items eligible for this vehicle, then orders them by mileage
    /// (sequential first, smaller mileage first) and builds a free-item DTO for each.
    /// </summary>
    private IEnumerable<VehicleServiceItemDTO> BuildEligibleFreeItems(
        IEnumerable<ServiceItemModel> serviceItems,
        VehicleEntryModel vehicle,
        VehicleOwnership ownership,
        DateTime? freeServiceStartDate,
        string languageCode,
        long? baseScheduleMaximumMileage)
    {
        var eligible = FilterEligibleServiceItems(serviceItems, vehicle, ownership, freeServiceStartDate, baseScheduleMaximumMileage)
            .OrderByDescending(x => x.Item.MaximumMileage.HasValue)
            .ThenBy(x => x.Item.MaximumMileage);

        foreach (var (item, outcome) in eligible)
        {
            var modelCost = GetModelCost(item.ModelCosts, vehicle?.Katashiki, vehicle?.VariantCode);
            var dto = BuildFreeServiceItemDto(item, vehicle, languageCode, modelCost);
            dto.Lock = outcome?.ToLockDTO();
            Trace.RecordFreeBuild(item, dto, modelCost, languageCode);
            yield return dto;
        }
    }

    private IEnumerable<VehicleServiceItemDTO> BuildPaidItems(string languageCode)
    {
        var paidServices = companyDataAggregate.PaidServiceInvoices;
        if (paidServices is null) yield break;

        foreach (var invoice in paidServices)
        {
            if (invoice?.Lines is null) continue;
            foreach (var line in invoice.Lines)
            {
                var dto = BuildPaidServiceItemDto(invoice, line, languageCode);
                Trace.RecordPaidBuild(invoice, line, dto, languageCode);
                yield return dto;
            }
        }
    }

    /// <summary>
    /// Free items in <c>RelativeToActivation</c> mode that are warranty-activated have their
    /// activation/expiry computed by rolling the anchor date forward through each sequential
    /// (mileage-keyed) item. Non-sequential items inherit the bundle's collective end date —
    /// see issue #14 in <c>STATUS.md</c> for the same-day-expiry edge case when no sequential
    /// anchor exists.
    /// </summary>
    private void ApplyWarrantyRollingExpiry(List<VehicleServiceItemDTO> result, DateTime? anchorDate)
    {
        if (anchorDate is null)
        {
            Trace.RecordWarrantyRollingSkipped("freeServiceStartDate is null — rolling expiry is not applied; items keep default ActivatedAt.");
            return;
        }

        Trace.RecordWarrantyRollingAnchor(anchorDate.Value);

        var warrantyActivated = result
            .Where(x => x.ValidityModeEnum == ClaimableItemValidityMode.RelativeToActivation
                     && x.TypeEnum == VehcileServiceItemTypes.Free
                     && x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation)
            .ToList();
        var sequential = warrantyActivated.Where(x => x.MaximumMileage is not null).ToList();
        var nonSequential = warrantyActivated.Where(x => x.MaximumMileage is null).ToList();

        var rollingDate = anchorDate.Value;
        foreach (var item in sequential)
        {
            item.ActivatedAt = rollingDate;
            item.ExpiresAt = DurationCalculator.AddInterval(rollingDate, item.ActiveFor, item.ActiveForDurationType);
            rollingDate = item.ExpiresAt!.Value;
            Trace.RecordWarrantyRollingItem(sequential: true, item);
        }

        var noSequentialAnchor = sequential.Count == 0 && nonSequential.Count > 0;
        if (noSequentialAnchor)
            Trace.Note("Issue #14 triggered: non-sequential items present without any sequential anchor — they will be activated and expired on the same day (freeServiceStartDate). In production these usually come bundled with sequential items, masking this.");

        foreach (var item in nonSequential)
        {
            item.ActivatedAt = anchorDate.Value;
            item.ExpiresAt = rollingDate;
            Trace.RecordWarrantyRollingItem(
                sequential: false,
                item,
                note: noSequentialAnchor ? "Same-day expiry (issue #14 pinned)" : "Inherits bundle end date");
        }
    }

    private void ApplyVehicleInspectionExpansion(List<VehicleServiceItemDTO> result)
    {
        var inspectionItems = result
            .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free
                     && x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.VehicleInspection)
            .ToList();

        if (inspectionItems.Count == 0) return;

        var newItems = new List<VehicleServiceItemDTO>();
        foreach (var item in inspectionItems)
        {
            var matching = (companyDataAggregate.VehicleInspections ?? Enumerable.Empty<VehicleInspectionModel>())
                .Where(x => x.VehicleInspectionTypeID.ToString() == item.VehicleInspectionTypeID)
                .ToList();
            var (selected, fallbackNote) = SelectInspectionsForActivation(item.CampaignActivationType, matching);

            var clones = selected.Select(insp => CloneWithInspectionActivation(item, insp)).ToList();
            newItems.AddRange(clones);
            Trace.RecordVehicleInspectionExpansion(item, matching.Count, selected, clones, fallbackNote);
        }

        result.RemoveAll(x => x.TypeEnum == VehcileServiceItemTypes.Free && x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.VehicleInspection);
        result.AddRange(newItems);
    }

    /// <summary>
    /// Mirrors <see cref="ApplyVehicleInspectionExpansion"/> but keys on <c>CampaignID</c>
    /// against <c>CampaignVinEntries</c> — manual-VIN-entry items don't carry an inspection
    /// type, the entry itself targets a campaign.
    /// </summary>
    private void ApplyManualVinEntryExpansion(List<VehicleServiceItemDTO> result)
    {
        var manualVinItems = result
            .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free
                     && x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.ManualVinEntry)
            .ToList();

        if (manualVinItems.Count == 0) return;

        var newItems = new List<VehicleServiceItemDTO>();
        foreach (var item in manualVinItems)
        {
            var matching = (companyDataAggregate.CampaignVinEntries ?? Enumerable.Empty<CampaignVinEntryModel>())
                .Where(x => x.CampaignID == item.CampaignID)
                .ToList();
            var (selected, fallbackNote) = SelectCampaignVinEntriesForActivation(item.CampaignActivationType, matching);

            var clones = selected.Select(entry => CloneWithCampaignVinEntryActivation(item, entry)).ToList();
            newItems.AddRange(clones);
            Trace.RecordManualVinEntryExpansion(item, matching.Count, selected, clones, fallbackNote);
        }

        result.RemoveAll(x => x.TypeEnum == VehcileServiceItemTypes.Free && x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.ManualVinEntry);
        result.AddRange(newItems);
    }

    /// <summary>
    /// Settles what an unclaimable item shows, once statuses are known.
    /// <para>
    /// An item that has already been claimed is claimed, whatever its conditions say about the
    /// vehicle today — a customer whose later services closed the window still had the reward, and
    /// telling them they missed it would be false. The claim wins and the block goes.
    /// </para>
    /// <para>
    /// What remains locked or missed shows no expiry. A reward active for three months is active for
    /// three months from the moment it unlocks, not from warranty activation, so the rolling date
    /// would count down against a customer who cannot claim yet. Cleared here rather than before the
    /// rolling pass, so the sequence every other item is dated from is the one it would have had.
    /// </para>
    /// </summary>
    private void ResolveUnclaimableItems(List<VehicleServiceItemDTO> serviceItems)
    {
        foreach (var item in serviceItems)
        {
            if (item.Lock is null)
                continue;

            if (item.StatusEnum == VehcileServiceItemStatuses.Processed)
            {
                Trace.Note($"Item {item.ServiceItemID} reads as {item.Lock.State} but has already been claimed; the claim wins and the lock block is dropped.");
                item.Lock = null;
                continue;
            }

            if (item.ExpiresAt is null)
                continue;

            Trace.Note($"Item {item.ServiceItemID} is {item.Lock.State}; cleared ExpiresAt={item.ExpiresAt:yyyy-MM-dd} — its validity starts when it unlocks.");
            item.ExpiresAt = null;
        }
    }

    private async Task<bool> CalculateServiceItemStatusAndClaimability(
        List<VehicleServiceItemDTO> serviceItems,
        bool showingInactivatedItems,
        string languageCode)
    {
        await AssignStatusToItems(serviceItems, showingInactivatedItems, languageCode);

        ResolveUnclaimableItems(serviceItems);

        var activationRequired = serviceItems.Any(x => x.StatusEnum == VehcileServiceItemStatuses.ActivationRequired);

        foreach (var item in serviceItems)
        {
            var reason = ApplyClaimability(item, options.GetUtcNow());
            Trace.RecordStatus(item, reason);
        }

        return activationRequired;
    }

    /// <summary>
    /// Mutates <paramref name="item"/>'s <c>Claimable</c> flag based on its status and validity
    /// mode, returning the reason text for the trace.
    /// </summary>
    private static string ApplyClaimability(VehicleServiceItemDTO item, DateTime nowUtc)
    {
        // Checked before status, and unconditionally: an item on screen to explain itself must never
        // be claimable, whatever status it would otherwise carry. Claimable is part of the signed
        // payload, so this is also what the claim endpoint enforces against.
        if (item.Lock is not null)
        {
            item.Claimable = false;
            return $"{item.Lock.State} item → not claimable.";
        }

        if (item.ValidityModeEnum == ClaimableItemValidityMode.FixedDateRange && item.ActivatedAt > nowUtc)
        {
            item.Claimable = false;
            return $"FixedDateRange item with future ActivatedAt={item.ActivatedAt:yyyy-MM-dd} → not yet claimable.";
        }

        if (item.StatusEnum == VehcileServiceItemStatuses.Pending)
        {
            item.Claimable = true;
            return "Pending status → claimable.";
        }

        return ServiceItemEligibilityReasonFormatter.FormatStatusFallback(item);
    }

    private async Task<List<VehicleServiceItemDTO>> ApplyPostProcessing(
        List<VehicleServiceItemDTO> result,
        IEnumerable<ServiceItemModel> serviceItems,
        VehicleEntryModel vehicle,
        string languageCode,
        bool activationRequiredFromUnfilteredList)
    {
        ApplyVinExclusionFilter(result, activationRequiredFromUnfilteredList);

        var ineligibleClaimedItems = (await BuildIneligibleClaimedItems(result, serviceItems, languageCode)).ToList();
        Trace.RecordIneligiblePickup(ineligibleClaimedItems.Count);
        result.AddRange(ineligibleClaimedItems);

        ApplyDynamicCancellation(result);

        return result
            .OrderBy(x => x.TypeEnum)
            .ThenByDescending(x => x.MaximumMileage.HasValue)
            .ThenBy(x => x.MaximumMileage)
            .ThenBy(x => x.ExpiresAt)
            .ThenBy(x => x.StatusEnum)
            .ToList();
    }

    /// <summary>
    /// <c>FreeServiceItemExcludedVINs</c> is loaded per-VIN by the storage layer, so a non-empty
    /// list means "this vehicle is excluded from warranty-activated items". No per-item VIN
    /// check needed. See issue #22 in <c>STATUS.md</c>: <c>activationRequired</c> was already
    /// computed from the unfiltered list, so the caller may show "activation needed" even
    /// though no items will appear post-activation.
    /// </summary>
    private void ApplyVinExclusionFilter(List<VehicleServiceItemDTO> result, bool activationRequiredFromUnfilteredList)
    {
        if (!companyDataAggregate.FreeServiceItemExcludedVINs.Any()) return;

        var beforeCount = result.Count;
        result.RemoveAll(x => x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation);
        var removed = beforeCount - result.Count;
        Trace.RecordVinExclusion(removed);

        if (activationRequiredFromUnfilteredList && removed > 0)
            Trace.Note("Issue #22 triggered: VIN is on FreeServiceItemExcludedVINs and warranty-activated items were stripped, but activationRequired was already computed from the unfiltered list and is true. Caller may show 'activation needed' for items that will never appear after activation.");
    }

    /// <summary>
    /// A free item that's been claimed (has an ItemClaim row) but is no longer in the eligible
    /// set — e.g. the item was deleted from the catalog after claiming, or no longer matches
    /// the vehicle. We still surface it in the result as "processed" so the dealer can see
    /// the historical record.
    /// </summary>
    private async Task<IEnumerable<VehicleServiceItemDTO>> BuildIneligibleClaimedItems(
        IEnumerable<VehicleServiceItemDTO> eligibleServiceItems,
        IEnumerable<ServiceItemModel> availableServiceItems,
        string languageCode)
    {
        var result = new List<VehicleServiceItemDTO>();

        var existingFreeIds = eligibleServiceItems
            ?.Where(x => x.TypeEnum == VehcileServiceItemTypes.Free)
            .Select(x => x.ServiceItemID.ToString())
            .ToList();

        var orphanClaimedIds = companyDataAggregate.ItemClaims
            ?.Select(x => x?.ServiceItemID)
            .Where(id => !(existingFreeIds?.Any(s => s == id) ?? false));

        var matched = availableServiceItems.Where(x => orphanClaimedIds?.Any(id => id == x.IntegrationID) ?? false);

        foreach (var item in matched)
        {
            var claimLine = companyDataAggregate.ItemClaims?.FirstOrDefault(t => t.ServiceItemID == item.IntegrationID);
            var dto = new VehicleServiceItemDTO
            {
                ServiceItemID = item.IntegrationID,
                Name = Utility.GetLocalizedText(item.Name, languageCode),
                Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
                Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
                Type = "free",
                TypeEnum = VehcileServiceItemTypes.Free,
                StatusEnum = VehcileServiceItemStatuses.Processed,
                Status = "processed",
                PackageCode = claimLine.PackageCode,
                ClaimDate = claimLine?.ClaimDate,
                Cost = claimLine?.Cost,
                InvoiceNumber = claimLine?.InvoiceNumber,
                JobNumber = claimLine?.JobNumber,
                MaximumMileage = item.MaximumMileage,
            };

            if (claimLine.CompanyID is { } companyId && companyId != 0 && options.CompanyNameResolver is not null)
                dto.CompanyName = await options.CompanyNameResolver(new(companyId, languageCode, services));

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// If a higher-mileage free item is already <c>Processed</c>, mark all lower-mileage
    /// <c>Pending</c> items as <c>Cancelled</c> — they were superseded by the customer
    /// jumping straight to a later interval.
    /// <see cref="FindItemsCancelledByClaiming"/> is the pre-claim mirror of this rule
    /// (it feeds the skipped-items claim warning) — keep the two predicates in sync.
    /// </summary>
    private void ApplyDynamicCancellation(IEnumerable<VehicleServiceItemDTO> serviceItems)
    {
        var freeItems = serviceItems
            .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free && x.MaximumMileage.HasValue)
            .OrderBy(x => x.MaximumMileage)
            .ToList();

        foreach (var item in freeItems)
        {
            if (item.StatusEnum != VehcileServiceItemStatuses.Pending) continue;

            var supersededBy = freeItems.FirstOrDefault(x =>
                x.StatusEnum == VehcileServiceItemStatuses.Processed && x.MaximumMileage > item.MaximumMileage);
            if (supersededBy is null) continue;

            item.Status = "cancelled";
            item.StatusEnum = VehcileServiceItemStatuses.Cancelled;
            item.Claimable = false;
            Trace.RecordCancellation(item, supersededBy);
        }
    }

    private async Task StampSignaturesPrintUrlsAndWarnings(
        List<VehicleServiceItemDTO> result,
        VehicleServiceActivation serviceActivation,
        string languageCode,
        VehicleBrokerSaleInformation brokerSaleInformation)
    {
        var now = options.GetUtcNow();
        var itemSignatureExpiry = options?.SignatureValidityDuration is { } d
            ? now.Add(d)
            : now;

        foreach (var item in result)
        {
            item.SignatureExpiry = itemSignatureExpiry;
            item.Signature = item.GenerateSignature(companyDataAggregate.VIN, options.SigningSecretKey);

            if (options.VehicleInspectionPreClaimVoucherPrintingURLResolver is not null && item.VehicleInspectionID is not null)
                item.PrintUrl = await options.VehicleInspectionPreClaimVoucherPrintingURLResolver(new(new(item.VehicleInspectionID, item.ServiceItemID), languageCode, services));

            // Service activation overrides the inspection URL when both apply.
            if (options.ServiceActivationPreClaimVoucherPrintingURLResolver is not null && serviceActivation is not null)
                item.PrintUrl = await options.ServiceActivationPreClaimVoucherPrintingURLResolver(new(new(serviceActivation.id, item.ServiceItemID), languageCode, services));

            item.Warnings = await BuildItemClaimWarnings(item, result, brokerSaleInformation, languageCode);
        }
    }

    /// <summary>
    /// Assembles the claim-form warnings for one item: the dynamic per-item warnings
    /// (skipped lower-mileage items, un-invoiced broker — both resolver opt-in and only
    /// on claimable items) followed by the configured standard warnings.
    /// <see cref="LookupOptions.StandardItemClaimWarnings"/> is a single shared instance:
    /// it is returned as-is when nothing dynamic applies and must never be mutated per item.
    /// </summary>
    private async Task<List<VehicleItemWarning>> BuildItemClaimWarnings(
        VehicleServiceItemDTO item,
        List<VehicleServiceItemDTO> allItems,
        VehicleBrokerSaleInformation brokerSaleInformation,
        string languageCode)
    {
        List<VehicleItemWarning> warnings = null;

        if (item.Claimable)
        {
            if (options.SkippedItemsClaimWarningResolver is not null)
            {
                var skippedItems = FindItemsCancelledByClaiming(item, allItems);

                if (skippedItems.Count > 0)
                {
                    var warning = await options.SkippedItemsClaimWarningResolver(new((item, skippedItems), languageCode, services));
                    if (warning is not null)
                        (warnings ??= new List<VehicleItemWarning>()).Add(warning);
                }
            }

            if (options.UnInvoicedBrokerClaimWarningResolver is not null
                && brokerSaleInformation is not null
                && brokerSaleInformation.InvoiceDate is null)
            {
                var warning = await options.UnInvoicedBrokerClaimWarningResolver(new(brokerSaleInformation, languageCode, services));
                if (warning is not null)
                    (warnings ??= new List<VehicleItemWarning>()).Add(warning);
            }
        }

        if (warnings is null)
            return options.StandardItemClaimWarnings;

        if (options.StandardItemClaimWarnings is not null)
            warnings.AddRange(options.StandardItemClaimWarnings);

        return warnings;
    }

    /// <summary>
    /// The pre-claim mirror of <see cref="ApplyDynamicCancellation"/>: claiming
    /// <paramref name="item"/> makes it <c>Processed</c>, which cancels every lower-mileage
    /// <c>Pending</c> free item on the next evaluation. Only free, mileage-keyed items
    /// participate on either side — paid items and non-sequential free items are never
    /// cancelled and never cancel anything.
    /// </summary>
    private static List<VehicleServiceItemDTO> FindItemsCancelledByClaiming(
        VehicleServiceItemDTO item,
        List<VehicleServiceItemDTO> allItems)
    {
        if (item.TypeEnum != VehcileServiceItemTypes.Free || !item.MaximumMileage.HasValue)
            return new List<VehicleServiceItemDTO>();

        return allItems
            .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free
                     && x.MaximumMileage.HasValue
                     && x.StatusEnum == VehcileServiceItemStatuses.Pending
                     && x.MaximumMileage < item.MaximumMileage)
            .OrderBy(x => x.MaximumMileage)
            .ToList();
    }

    // ===== Eligibility predicates =====

    /// <summary>
    /// Derives the vehicle's base scheduled-service mileage cap from catalog items that pass
    /// the ordinary static filters. Custom eligibility is deliberately not evaluated here:
    /// conditions may consume the cap, so making them contributors would introduce an ordering
    /// dependency. Program role affects only this derivation and no downstream item lifecycle.
    /// </summary>
    private long? DeriveBaseScheduleMaximumMileage(
        IEnumerable<ServiceItemModel> serviceItems,
        VehicleEntryModel vehicle,
        VehicleOwnership ownership,
        DateTime? freeServiceStartDate)
    {
        long? maximumMileage = null;

        foreach (var item in serviceItems ?? Enumerable.Empty<ServiceItemModel>())
        {
            var staticStage = EvaluateStaticItemEligibility(item, vehicle, ownership, freeServiceStartDate);
            if (staticStage != EligibilityRejectionStage.None)
            {
                Trace.RecordBaseScheduleCapDecision(item, included: false, BaseScheduleCapDecisionReason.StaticFilter, staticStage);
                continue;
            }

            if (item.ProgramRole != ServiceItemProgramRole.ScheduledService)
            {
                Trace.RecordBaseScheduleCapDecision(item, included: false, BaseScheduleCapDecisionReason.ProgramRole);
                continue;
            }

            if (item.CampaignActivationTrigger != ClaimableItemCampaignActivationTrigger.WarrantyActivation)
            {
                Trace.RecordBaseScheduleCapDecision(item, included: false, BaseScheduleCapDecisionReason.ActivationTrigger);
                continue;
            }

            if (item.ValidityMode != ClaimableItemValidityMode.RelativeToActivation)
            {
                Trace.RecordBaseScheduleCapDecision(item, included: false, BaseScheduleCapDecisionReason.ValidityMode);
                continue;
            }

            if (item.MaximumMileage is null)
            {
                Trace.RecordBaseScheduleCapDecision(item, included: false, BaseScheduleCapDecisionReason.MissingMaximumMileage);
                continue;
            }

            maximumMileage = !maximumMileage.HasValue || item.MaximumMileage.Value > maximumMileage.Value
                ? item.MaximumMileage
                : maximumMileage;
            Trace.RecordBaseScheduleCapDecision(item, included: true, BaseScheduleCapDecisionReason.None);
        }

        Trace.RecordBaseScheduleCap(maximumMileage);
        return maximumMileage;
    }

    /// <summary>
    /// Per-item eligibility evaluation. Returns <see cref="EligibilityRejectionStage.None"/>
    /// for accepted items, or the first failing stage (predicates are checked in declaration
    /// order). Reason strings are formatted separately by the trace collector — this method
    /// stays predicate-only and allocation-free.
    /// </summary>
    private (EligibilityRejectionStage Stage, EligibilityConditionOutcome Outcome) EvaluateItemEligibility(
        ServiceItemModel item,
        VehicleEntryModel vehicle,
        VehicleOwnership ownership,
        DateTime? freeServiceStartDate,
        long? baseScheduleMaximumMileage)
    {
        var staticStage = EvaluateStaticItemEligibility(item, vehicle, ownership, freeServiceStartDate);
        if (staticStage != EligibilityRejectionStage.None)
            return (staticStage, null);

        // The static filters answer with facts about the vehicle — its brand, its market, its owner —
        // and an item failing one of those was never this customer's to see. Only the custom
        // conditions can produce something worth showing unclaimable.
        var outcome = new VehicleEligibilityConditionEvaluator(companyDataAggregate, options, Trace.IsEnabled)
            .Evaluate(item.EligibilityConditions, baseScheduleMaximumMileage);

        var stage = outcome.State switch
        {
            EligibilityConditionState.Met => EligibilityRejectionStage.None,
            EligibilityConditionState.Locked => EligibilityRejectionStage.CustomConditionLocked,
            EligibilityConditionState.Missed => EligibilityRejectionStage.CustomConditionMissed,
            _ => EligibilityRejectionStage.CustomCondition,
        };

        return (stage, outcome);
    }

    private EligibilityRejectionStage EvaluateStaticItemEligibility(
        ServiceItemModel item,
        VehicleEntryModel vehicle,
        VehicleOwnership ownership,
        DateTime? freeServiceStartDate)
    {
        if (item.IsDeleted) return EligibilityRejectionStage.IsDeleted;
        if (!MatchesBrand(item, vehicle)) return EligibilityRejectionStage.Brand;
        if (!MatchesCompany(item, ownership)) return EligibilityRejectionStage.Company;
        if (!MatchesCountry(item, ownership)) return EligibilityRejectionStage.Country;
        if (!IsWithinCampaignWindow(item, freeServiceStartDate)) return EligibilityRejectionStage.CampaignWindow;
        if (!IsApplicableToVehicle(item, vehicle)) return EligibilityRejectionStage.VehicleApplicability;
        return EligibilityRejectionStage.None;
    }

    private static bool MatchesBrand(ServiceItemModel item, VehicleEntryModel vehicle) =>
        vehicle is null || item.BrandIDs is null || item.BrandIDs.Any(a => a == vehicle.BrandID);

    // Company/country match against the resolved ownership, never the entry — the entry's
    // ownership fields can be stale while the vehicle awaits allocation to the activating
    // company. No vehicle-null leniency here: an unknown (null) owner matches nothing
    // scoped, so another company's items can never leak in (fail closed).

    private static bool MatchesCompany(ServiceItemModel item, VehicleOwnership ownership) =>
        item.CompanyIDs is null || !item.CompanyIDs.Any() || item.CompanyIDs.Any(a => a == ownership?.CompanyID);

    private static bool MatchesCountry(ServiceItemModel item, VehicleOwnership ownership) =>
        item.CountryIDs is null || !item.CountryIDs.Any() || item.CountryIDs.Any(a => a == ownership?.CountryID);

    private bool IsWithinCampaignWindow(ServiceItemModel item, DateTime? freeServiceStartDate) =>
        item.CampaignActivationTrigger switch
        {
            ClaimableItemCampaignActivationTrigger.WarrantyActivation =>
                freeServiceStartDate >= item.CampaignStartDate && freeServiceStartDate <= item.CampaignEndDate,
            ClaimableItemCampaignActivationTrigger.VehicleInspection =>
                companyDataAggregate.VehicleInspections?.Any(i =>
                    i.VehicleInspectionTypeID == item.VehicleInspectionTypeID &&
                    i.InspectionDate >= item.CampaignStartDate &&
                    i.InspectionDate <= item.CampaignEndDate) ?? false,
            ClaimableItemCampaignActivationTrigger.ManualVinEntry =>
                companyDataAggregate.CampaignVinEntries?.Any(e =>
                    e.CampaignID == item.CampaignID &&
                    e.RecordedDate >= item.CampaignStartDate &&
                    e.RecordedDate <= item.CampaignEndDate) ?? false,
            _ => false,
        };

    /// <summary>
    /// An item applies to the vehicle if either: (a) it has no per-model costs (targets all
    /// vehicles), or (b) the vehicle's Katashiki / VariantCode prefix-matches one of the
    /// item's model costs. The all-vehicles rule is gated to skip warranty-activated items
    /// when no vehicle is loaded.
    /// </summary>
    private static bool IsApplicableToVehicle(ServiceItemModel item, VehicleEntryModel vehicle)
    {
        if (HasMatchingModelCost(item, vehicle)) return true;

        var modelCostCount = item.ModelCosts?.Count() ?? 0;
        var isWarrantyActivation = item.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation;
        return (vehicle is not null || !isWarrantyActivation) && modelCostCount == 0;
    }

    private static bool HasMatchingModelCost(ServiceItemModel item, VehicleEntryModel vehicle) =>
        item.ModelCosts?.Any(cost =>
            HasPrefixMatch(vehicle?.Katashiki, cost?.Katashiki) ||
            HasPrefixMatch(vehicle?.VariantCode, cost?.Variant)) ?? false;

    private static bool HasPrefixMatch(string vehicleValue, string costValue) =>
        !string.IsNullOrWhiteSpace(vehicleValue) &&
        !string.IsNullOrWhiteSpace(costValue) &&
        vehicleValue.StartsWith(costValue, StringComparison.InvariantCultureIgnoreCase);

    private static ServiceItemCostModel GetModelCost(IEnumerable<ServiceItemCostModel> modelCosts, string katashiki, string variant)
    {
        if (modelCosts is null) return null;
        return modelCosts.FirstOrDefault(x =>
            (!string.IsNullOrWhiteSpace(x?.Katashiki) && (katashiki ?? "").StartsWith(x.Katashiki)) ||
            (!string.IsNullOrWhiteSpace(x?.Variant) && (variant ?? "").StartsWith(x.Variant)));
    }

    /// <summary>
    /// Walks the catalog item-by-item and yields accepted items. Recording happens via the
    /// trace collector — the <c>Disabled</c> sink ignores the calls, so this is the only
    /// path (no separate fast path needed).
    /// </summary>
    private IEnumerable<(ServiceItemModel Item, EligibilityConditionOutcome Outcome)> FilterEligibleServiceItems(
        IEnumerable<ServiceItemModel> serviceItems,
        VehicleEntryModel vehicle,
        VehicleOwnership ownership,
        DateTime? freeServiceStartDate,
        long? baseScheduleMaximumMileage)
    {
        Trace.RecordEligibilityInputCount(serviceItems?.Count() ?? 0);
        Trace.RecordMilestoneReader(options?.ServiceMilestones?.GetResolver());

        foreach (var item in serviceItems ?? Enumerable.Empty<ServiceItemModel>())
        {
            var (stage, outcome) = EvaluateItemEligibility(item, vehicle, ownership, freeServiceStartDate, baseScheduleMaximumMileage);
            Trace.RecordEligibilityDecision(item, stage, vehicle, ownership, outcome?.Prerequisites, outcome?.MilestoneNearMisses);

            // Locked and missed items pass this point carrying their reason. Everything else that
            // failed is dropped, as it always was — an item excluded by brand, market or programme
            // would put another catalog on this dealer's screen.
            if (stage == EligibilityRejectionStage.None ||
                stage == EligibilityRejectionStage.CustomConditionLocked ||
                stage == EligibilityRejectionStage.CustomConditionMissed)
                yield return (item, outcome);
        }
    }

    // ===== DTO builders =====

    private VehicleServiceItemDTO BuildFreeServiceItemDto(
        ServiceItemModel item,
        VehicleEntryModel vehicle,
        string languageCode,
        ServiceItemCostModel modelCost)
    {
        var dto = new VehicleServiceItemDTO
        {
            ServiceItemID = item.IntegrationID,
            Name = Utility.GetLocalizedText(item.Name, languageCode),
            Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
            Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
            Type = "free",
            TypeEnum = VehcileServiceItemTypes.Free,
            ModelCostID = modelCost?.ID,
            PackageCode = modelCost?.PackageCode ?? item.PackageCode,
            Cost = modelCost?.Cost ?? item?.FixedCost,
            CampaignID = item.CampaignID,
            CampaignUniqueReference = item.CampaignUniqueReference,
            MaximumMileage = item.MaximumMileage,
            CampaignActivationTrigger = item.CampaignActivationTrigger,
            CampaignActivationType = item.CampaignActivationType,
            ValidityModeEnum = item.ValidityMode,
            ClaimingMethodEnum = item.ClaimingMethod,
            ShowDocumentUploader = item.AttachmentFieldBehavior != ClaimableItemAttachmentFieldBehavior.Hidden,
            DocumentUploaderIsRequired = item.AttachmentFieldBehavior == ClaimableItemAttachmentFieldBehavior.Required,
            VehicleInspectionTypeID = item.VehicleInspectionTypeID.ToString(),
        };

        if (item.ValidityMode == ClaimableItemValidityMode.FixedDateRange)
        {
            dto.ActivatedAt = item.ValidFrom.Value;
            dto.ExpiresAt = item.ValidTo;
        }
        else if (item.ValidityMode == ClaimableItemValidityMode.RelativeToActivation)
        {
            dto.ActiveFor = item.ActiveFor;
            dto.ActiveForDurationType = item.ActiveForDurationType;
        }

        return dto;
    }

    private static VehicleServiceItemDTO BuildPaidServiceItemDto(
        PaidServiceInvoiceModel paidService,
        PaidServiceInvoiceLineModel line,
        string languageCode) => new()
    {
        ServiceItemID = line.ServiceItemID,
        PaidServiceInvoiceLineID = line.IntegrationID,
        ActivatedAt = paidService.InvoiceDate,
        CampaignUniqueReference = line.ServiceItem?.CampaignUniqueReference,
        Description = Utility.GetLocalizedText(line.ServiceItem?.PrintoutDescription, languageCode),
        Name = Utility.GetLocalizedText(line.ServiceItem?.Name, languageCode),
        Title = Utility.GetLocalizedText(line.ServiceItem?.PrintoutTitle, languageCode),
        ExpiresAt = line.ExpireDate,
        Type = "paid",
        MaximumMileage = line.ServiceItem?.MaximumMileage,
        TypeEnum = VehcileServiceItemTypes.Paid,
        PackageCode = line.PackageCode,
        ClaimingMethodEnum = line.ServiceItem?.ClaimingMethod ?? ClaimableItemClaimingMethod.ClaimByEnteringInvoiceAndJobNumber,
        VehicleInspectionTypeID = line.ServiceItem?.VehicleInspectionTypeID?.ToString(),
    };

    // ===== Per-trigger expansion helpers =====

    /// <summary>
    /// Selects which inspections drive activation of an item, per the item's
    /// <c>CampaignActivationType</c>. An unrecognized value produces a single-null sentinel
    /// — pinned NRE-on-misconfig behavior; the caller will throw downstream on
    /// <c>inspection.id</c>.
    /// </summary>
    private static (List<VehicleInspectionModel> selected, string fallbackNote) SelectInspectionsForActivation(
        ClaimableItemCampaignActivationTypes activationType,
        List<VehicleInspectionModel> matching) => activationType switch
    {
        ClaimableItemCampaignActivationTypes.EveryTrigger => (matching, null),
        ClaimableItemCampaignActivationTypes.FirstTriggerOnly => (new() { matching.OrderBy(x => x.InspectionDate).First() }, null),
        ClaimableItemCampaignActivationTypes.ExtendOnEachTrigger => (new() { matching.OrderByDescending(x => x.InspectionDate).First() }, null),
        _ => (new() { null }, $"Unrecognized CampaignActivationType={activationType} → emitting null sentinel (pinned NRE-on-misconfig)."),
    };

    private static (List<CampaignVinEntryModel> selected, string fallbackNote) SelectCampaignVinEntriesForActivation(
        ClaimableItemCampaignActivationTypes activationType,
        List<CampaignVinEntryModel> matching) => activationType switch
    {
        ClaimableItemCampaignActivationTypes.EveryTrigger => (matching, null),
        ClaimableItemCampaignActivationTypes.FirstTriggerOnly => (new() { matching.OrderBy(x => x.RecordedDate).First() }, null),
        ClaimableItemCampaignActivationTypes.ExtendOnEachTrigger => (new() { matching.OrderByDescending(x => x.RecordedDate).First() }, null),
        _ => (new() { null }, $"Unrecognized CampaignActivationType={activationType} → emitting null sentinel (pinned NRE-on-misconfig)."),
    };

    private VehicleServiceItemDTO CloneWithInspectionActivation(VehicleServiceItemDTO item, VehicleInspectionModel inspection)
    {
        var cloned = item.Clone();
        cloned.VehicleInspectionID = inspection.id;
        if (cloned.ValidityModeEnum == ClaimableItemValidityMode.RelativeToActivation)
        {
            cloned.ActivatedAt = inspection.InspectionDate.DateTime;
            cloned.ExpiresAt = DurationCalculator.AddInterval(cloned.ActivatedAt, cloned.ActiveFor, cloned.ActiveForDurationType);
        }
        return cloned;
    }

    private VehicleServiceItemDTO CloneWithCampaignVinEntryActivation(VehicleServiceItemDTO item, CampaignVinEntryModel entry)
    {
        var cloned = item.Clone();
        cloned.CampaignVinEntryID = entry.id;
        if (cloned.ValidityModeEnum == ClaimableItemValidityMode.RelativeToActivation)
        {
            cloned.ActivatedAt = entry.RecordedDate.DateTime;
            cloned.ExpiresAt = DurationCalculator.AddInterval(cloned.ActivatedAt, cloned.ActiveFor, cloned.ActiveForDurationType);
        }
        return cloned;
    }

    // ===== Status assignment =====

    private async Task AssignStatusToItems(IEnumerable<VehicleServiceItemDTO> serviceItems, bool showingInactivatedItems, string languageCode)
    {
        foreach (var item in serviceItems)
        {
            var verdict = ResolveItemStatus(item, companyDataAggregate.ItemClaims, showingInactivatedItems, options.GetUtcNow(), options.TreatServiceItemExpiryAsEndOfDay);

            item.Status = verdict.statusText;
            item.StatusEnum = verdict.status;
            item.ClaimDate = verdict.claimDate;
            item.JobNumber = verdict.wip;
            item.InvoiceNumber = verdict.invoice;
            item.PackageCode = verdict.packageCode ?? item.PackageCode;

            // Once claimed, the recorded claim cost is authoritative — service item / model
            // cost can change later, but the price billed at claim time must not.
            if (verdict.claimedCost is not null)
                item.Cost = verdict.claimedCost;

            if (verdict.companyID is { } id && id != 0 && options.CompanyNameResolver is not null)
                item.CompanyName = await options.CompanyNameResolver(new(id, languageCode, services));
        }
    }

    private static (string statusText, VehcileServiceItemStatuses status, DateTimeOffset? claimDate, string wip, string invoice, long? companyID, string packageCode, decimal? claimedCost)
    ResolveItemStatus(
        VehicleServiceItemDTO item,
        IEnumerable<ItemClaimModel> serviceClaimLines,
        bool showingInactivatedItems,
        DateTime nowUtc,
        bool expiryIsEndOfDay)
    {
        var claimLine = serviceClaimLines?
            .Where(x => x?.ServiceItemID == item.ServiceItemID.ToString())
            .Where(x => x?.VehicleInspectionID == item.VehicleInspectionID)
            .Where(x => x?.CampaignVinEntryID == item.CampaignVinEntryID)
            .FirstOrDefault();

        if (claimLine is not null)
            return ("processed", VehcileServiceItemStatuses.Processed,
                claimLine.ClaimDate, claimLine.JobNumber, claimLine.InvoiceNumber,
                claimLine.CompanyID, claimLine.PackageCode, claimLine.Cost);

        if (item.ExpiresAt is not null && EffectiveExpiry(item.ExpiresAt.Value, expiryIsEndOfDay) < nowUtc)
            return ("expired", VehcileServiceItemStatuses.Expired, null, null, null, null, null, null);

        if (showingInactivatedItems && item.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation)
            return ("activationRequired", VehcileServiceItemStatuses.ActivationRequired, null, null, null, null, null, null);

        return ("pending", VehcileServiceItemStatuses.Pending, null, null, null, null, null, null);
    }

    /// <summary>
    /// The instant an item actually stops being valid. Expiry is a calendar date everywhere it is
    /// built, so <see cref="LookupOptions.TreatServiceItemExpiryAsEndOfDay"/> stretches it to the last
    /// tick of its own day — making the date the item carries the last valid day rather than the first
    /// expired one. Off (the default), the stored value is used as-is and the item expires as its date
    /// begins. Deliberately applied at the comparison only: <c>ExpiresAt</c> keeps its midnight value so
    /// the signature, the serialized date and the sequential rolling chain are unaffected.
    /// </summary>
    private static DateTime EffectiveExpiry(DateTime expiresAt, bool endOfDay) =>
        endOfDay ? expiresAt.Date.AddDays(1).AddTicks(-1) : expiresAt;
}
