using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
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
    /// Evaluates the service items available for a vehicle. Reads as a 7-step recipe:
    /// load catalog → resolve activation mode → build eligible free + paid items →
    /// expand by trigger (warranty rolling expiry, vehicle inspection, manual VIN entry) →
    /// determine status & claimability → post-process (VIN exclusion, ineligible pickup,
    /// dynamic cancellation) → stamp signatures.
    /// </summary>
    public async Task<(IEnumerable<VehicleServiceItemDTO> serviceItems, bool activationRequired)> Evaluate(
        VehicleEntryModel vehicle,
        DateTime? freeServiceStartDate,
        VehicleSaleInformation vehicleSaleInformation,
        string languageCode)
    {
        var requestedStartDate = freeServiceStartDate;
        var serviceItems = await LoadServiceItemCatalog();
        (freeServiceStartDate, var showingInactivatedItems) = ResolveActivationMode(vehicle, freeServiceStartDate);
        Trace.RecordInputs(vehicle, vehicleSaleInformation, freeServiceStartDate, requestedStartDate, showingInactivatedItems, serviceItems, companyDataAggregate);

        var result = new List<VehicleServiceItemDTO>();

        using (Trace.Stage("Eligibility"))            result.AddRange(BuildEligibleFreeItems(serviceItems, vehicle, vehicleSaleInformation, freeServiceStartDate, languageCode));
        using (Trace.Stage("PaidItems"))              result.AddRange(BuildPaidItems(languageCode));
        using (Trace.Stage("WarrantyRollingExpiry"))  ApplyWarrantyRollingExpiry(result, freeServiceStartDate);
        using (Trace.Stage("InspectionExpansion"))    ApplyVehicleInspectionExpansion(result);
        using (Trace.Stage("ManualVinExpansion"))     ApplyManualVinEntryExpansion(result);

        bool activationRequired;
        using (Trace.Stage("StatusAndClaimability"))  activationRequired = await CalculateServiceItemStatusAndClaimability(result, showingInactivatedItems, languageCode);
        using (Trace.Stage("PostProcessing"))         result = await ApplyPostProcessing(result, serviceItems, vehicle, languageCode, activationRequired);
        using (Trace.Stage("Signatures"))             await StampSignaturesAndPrintUrls(result, ServiceActivation, languageCode);

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
        var shiftDay = companyDataAggregate.FreeServiceItemDateShifts?.FirstOrDefault(x => x.VIN == vehicle.VIN);
        if (shiftDay is not null)
            freeServiceStartDate = shiftDay.NewDate;

        if (options.IncludeInactivatedFreeServiceItems && freeServiceStartDate is null)
            return (DateTime.Now.Date, showingInactivatedItems: true);

        return (freeServiceStartDate, showingInactivatedItems: false);
    }

    /// <summary>
    /// Filters the catalog to items eligible for this vehicle, then orders them by mileage
    /// (sequential first, smaller mileage first) and builds a free-item DTO for each.
    /// </summary>
    private IEnumerable<VehicleServiceItemDTO> BuildEligibleFreeItems(
        IEnumerable<ServiceItemModel> serviceItems,
        VehicleEntryModel vehicle,
        VehicleSaleInformation saleInformation,
        DateTime? freeServiceStartDate,
        string languageCode)
    {
        var eligible = FilterEligibleServiceItems(serviceItems, vehicle, saleInformation, freeServiceStartDate)
            .OrderByDescending(x => x.MaximumMileage.HasValue)
            .ThenBy(x => x.MaximumMileage);

        foreach (var item in eligible)
        {
            var modelCost = GetModelCost(item.ModelCosts, vehicle?.Katashiki, vehicle?.VariantCode);
            var dto = BuildFreeServiceItemDto(item, vehicle, languageCode, modelCost);
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
            item.ExpiresAt = AddInterval(rollingDate, item.ActiveFor, item.ActiveForDurationType);
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

    private async Task<bool> CalculateServiceItemStatusAndClaimability(
        List<VehicleServiceItemDTO> serviceItems,
        bool showingInactivatedItems,
        string languageCode)
    {
        await AssignStatusToItems(serviceItems, showingInactivatedItems, languageCode);

        var activationRequired = serviceItems.Any(x => x.StatusEnum == VehcileServiceItemStatuses.ActivationRequired);

        foreach (var item in serviceItems)
        {
            var reason = ApplyClaimability(item);
            Trace.RecordStatus(item, reason);
        }

        return activationRequired;
    }

    /// <summary>
    /// Mutates <paramref name="item"/>'s <c>Claimable</c> flag based on its status and validity
    /// mode, returning the reason text for the trace.
    /// </summary>
    private static string ApplyClaimability(VehicleServiceItemDTO item)
    {
        if (item.ValidityModeEnum == ClaimableItemValidityMode.FixedDateRange && item.ActivatedAt > DateTime.Now)
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

    private async Task StampSignaturesAndPrintUrls(
        List<VehicleServiceItemDTO> result,
        VehicleServiceActivation serviceActivation,
        string languageCode)
    {
        var itemSignatureExpiry = options?.SignatureValidityDuration is { } d
            ? DateTime.UtcNow.Add(d)
            : DateTime.UtcNow;

        foreach (var item in result)
        {
            item.SignatureExpiry = itemSignatureExpiry;
            item.Signature = item.GenerateSignature(companyDataAggregate.VIN, options.SigningSecretKey);

            if (options.VehicleInspectionPreClaimVoucherPrintingURLResolver is not null && item.VehicleInspectionID is not null)
                item.PrintUrl = await options.VehicleInspectionPreClaimVoucherPrintingURLResolver(new(new(item.VehicleInspectionID, item.ServiceItemID), languageCode, services));

            // Service activation overrides the inspection URL when both apply.
            if (options.ServiceActivationPreClaimVoucherPrintingURLResolver is not null && serviceActivation is not null)
                item.PrintUrl = await options.ServiceActivationPreClaimVoucherPrintingURLResolver(new(new(serviceActivation.id, item.ServiceItemID), languageCode, services));

            item.Warnings = options.StandardItemClaimWarnings;
        }
    }

    // ===== Eligibility predicates =====

    /// <summary>
    /// Per-item eligibility evaluation. Returns <see cref="EligibilityRejectionStage.None"/>
    /// for accepted items, or the first failing stage (predicates are checked in declaration
    /// order). Reason strings are formatted separately by the trace collector — this method
    /// stays predicate-only and allocation-free.
    /// </summary>
    private EligibilityRejectionStage EvaluateItemEligibility(
        ServiceItemModel item,
        VehicleEntryModel vehicle,
        VehicleSaleInformation saleInformation,
        DateTime? freeServiceStartDate)
    {
        if (item.IsDeleted) return EligibilityRejectionStage.IsDeleted;
        if (!MatchesBrand(item, vehicle)) return EligibilityRejectionStage.Brand;
        if (!MatchesCompany(item, vehicle)) return EligibilityRejectionStage.Company;
        if (!MatchesCountry(item, vehicle, saleInformation)) return EligibilityRejectionStage.Country;
        if (!IsWithinCampaignWindow(item, freeServiceStartDate)) return EligibilityRejectionStage.CampaignWindow;
        if (!IsApplicableToVehicle(item, vehicle)) return EligibilityRejectionStage.VehicleApplicability;
        return EligibilityRejectionStage.None;
    }

    private static bool MatchesBrand(ServiceItemModel item, VehicleEntryModel vehicle) =>
        vehicle is null || item.BrandIDs is null || item.BrandIDs.Any(a => a == vehicle.BrandID);

    private static bool MatchesCompany(ServiceItemModel item, VehicleEntryModel vehicle) =>
        vehicle is null || item.CompanyIDs is null || !item.CompanyIDs.Any() || item.CompanyIDs.Any(a => a == vehicle.CompanyID);

    // Note: short-circuits on `vehicle is null` rather than `saleInformation is null` — copy-paste
    // from the brand/company filters. Behavior pinned (issue #21 in STATUS.md).
    private static bool MatchesCountry(ServiceItemModel item, VehicleEntryModel vehicle, VehicleSaleInformation saleInformation) =>
        vehicle is null || item.CountryIDs is null || !item.CountryIDs.Any() || item.CountryIDs.Any(a => a == saleInformation?.CountryID?.ToLong());

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
    private IEnumerable<ServiceItemModel> FilterEligibleServiceItems(
        IEnumerable<ServiceItemModel> serviceItems,
        VehicleEntryModel vehicle,
        VehicleSaleInformation vehicleSaleInformation,
        DateTime? freeServiceStartDate)
    {
        Trace.RecordEligibilityInputCount(serviceItems?.Count() ?? 0);

        // Issue #21 callout (latent): the country filter short-circuits on `vehicle is null`,
        // not `saleInformation is null`. With a null vehicle, items pass the country check
        // regardless of CountryIDs. Surface it via the trace so it's visible if it bites.
        if (vehicle is null)
            Trace.Note("Issue #21 (latent): vehicle is null — country eligibility filter is being short-circuited via the brand/company copy-paste guard. Items will pass the country check regardless of their CountryIDs.");

        foreach (var item in serviceItems ?? Enumerable.Empty<ServiceItemModel>())
        {
            var stage = EvaluateItemEligibility(item, vehicle, vehicleSaleInformation, freeServiceStartDate);
            Trace.RecordEligibilityDecision(item, stage, vehicle, vehicleSaleInformation);
            if (stage == EligibilityRejectionStage.None)
                yield return item;
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
            cloned.ExpiresAt = AddInterval(cloned.ActivatedAt, cloned.ActiveFor, cloned.ActiveForDurationType);
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
            cloned.ExpiresAt = AddInterval(cloned.ActivatedAt, cloned.ActiveFor, cloned.ActiveForDurationType);
        }
        return cloned;
    }

    private static DateTime AddInterval(DateTime date, int? intervalValue, DurationType? durationType) => durationType switch
    {
        DurationType.Seconds => date.AddSeconds(intervalValue.GetValueOrDefault()),
        DurationType.Minutes => date.AddMinutes(intervalValue.GetValueOrDefault()),
        DurationType.Hours => date.AddHours(intervalValue.GetValueOrDefault()),
        DurationType.Days => date.AddDays(intervalValue.GetValueOrDefault()),
        DurationType.Weeks => date.AddDays(7 * intervalValue.GetValueOrDefault()),
        DurationType.Months => date.AddMonths(intervalValue.GetValueOrDefault()),
        DurationType.Years => date.AddYears(intervalValue.GetValueOrDefault()),
        _ => date,
    };

    // ===== Status assignment =====

    private async Task AssignStatusToItems(IEnumerable<VehicleServiceItemDTO> serviceItems, bool showingInactivatedItems, string languageCode)
    {
        foreach (var item in serviceItems)
        {
            var verdict = ResolveItemStatus(item, companyDataAggregate.ItemClaims, showingInactivatedItems);

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
        bool showingInactivatedItems)
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

        if (item.ExpiresAt is not null && item.ExpiresAt < DateTime.Now)
            return ("expired", VehcileServiceItemStatuses.Expired, null, null, null, null, null, null);

        if (showingInactivatedItems && item.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation)
            return ("activationRequired", VehcileServiceItemStatuses.ActivationRequired, null, null, null, null, null, null);

        return ("pending", VehcileServiceItemStatuses.Pending, null, null, null, null, null, null);
    }
}
