using ShiftSoftware.ADP.Lookup.Services.Aggregate;
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

public class VehicleServiceItemEvaluator
{
    private readonly CompanyDataAggregateModel companyDataAggregate;
    private readonly IVehicleLoockupStorageService lookupCosmosService;
    private readonly LookupOptions options;
    private readonly IServiceProvider services;

    public VehicleServiceItemEvaluator(IVehicleLoockupStorageService lookupCosmosService, CompanyDataAggregateModel companyDataAggregate, LookupOptions options, IServiceProvider services)
    {
        this.lookupCosmosService = lookupCosmosService;
        this.companyDataAggregate = companyDataAggregate;
        this.options = options;
        this.services = services;
    }

    public async Task<(IEnumerable<VehicleServiceItemDTO> serviceItems, bool activationRequired)> Evaluate(
        VehicleEntryModel vehicle,
        DateTime? freeServiceStartDate,
        VehicleSaleInformation vehicleSaleInformation,
        string languageCode
    )
    {
        var paidServices = companyDataAggregate.PaidServiceInvoices;
        var tlpTransactionLines = companyDataAggregate.ItemClaims;
        var vehicleInspections = companyDataAggregate.VehicleInspections;
        var serviceActivation = companyDataAggregate.VehicleServiceActivations.FirstOrDefault();

        var result = new List<VehicleServiceItemDTO>();
        IEnumerable<ServiceItemModel> serviceItems = new List<ServiceItemModel>();

        //if (vehicle is not null)
        serviceItems = await lookupCosmosService.GetServiceItemsAsync(useCache: true);

        var shiftDay = companyDataAggregate.FreeServiceItemDateShifts?.FirstOrDefault(x => x.VIN == vehicle.VIN);
        if (shiftDay is not null)
            freeServiceStartDate = shiftDay.NewDate;

        var showingInactivatedItems = false;

        //Allow showing free service items as 'Activation Required'
        if (options.IncludeInactivatedFreeServiceItems && freeServiceStartDate is null)
        {
            freeServiceStartDate = DateTime.Now.Date;
            showingInactivatedItems = true;
        }

        var eligibleServiceItems = FilterEligibleServiceItems(
            serviceItems,
            vehicle,
            vehicleSaleInformation,
            freeServiceStartDate,
            vehicleInspections);

        if (eligibleServiceItems?.Count() > 0)
        {
            // Order them by mileage
            eligibleServiceItems = eligibleServiceItems
                .OrderByDescending(x => x.MaximumMileage.HasValue)
                .ThenBy(x => x.MaximumMileage);

            foreach (var item in eligibleServiceItems)
            {
                result.Add(BuildFreeServiceItemDto(item, vehicle, languageCode));
            }
        }

        if (paidServices?.Count() > 0)
        {
            foreach (var paidService in paidServices)
            {
                if (paidService?.Lines?.Count() > 0)
                {
                    foreach (var line in paidService.Lines)
                    {
                        result.Add(BuildPaidServiceItemDto(paidService, line, languageCode));
                    }
                }
            }
        }

        CalculateRollingExpireDateForWarrantyActivatedFreeServiceItems(
            result.Where(x => x.ValidityModeEnum == ClaimableItemValidityMode.RelativeToActivation)
                  .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free)
                  .Where(x => x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation),
            freeServiceStartDate
        );

        var newVehicleInspectionActivatedItems = CalculateRollingExpireDateForVehicleInspectionActivatedFreeServiceItems(
            result.Where(x => x.TypeEnum == VehcileServiceItemTypes.Free)
                  .Where(x => x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.VehicleInspection),
            vehicleInspections
        );

        result.RemoveAll(x => x.TypeEnum == VehcileServiceItemTypes.Free && x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.VehicleInspection);

        result.AddRange(newVehicleInspectionActivatedItems);

        var activationRequired = await CalculateServiceItemStatusAndClaimability(result, showingInactivatedItems, languageCode);

        // companyDataAggregate is loaded per-VIN by the storage layer, so FreeServiceItemExcludedVINs
        // only ever contains entries for the current vehicle — a non-empty list means "this vehicle
        // is excluded from warranty-activated items". No per-item VIN check is needed here.
        // Runs after CalculateServiceItemStatusAndClaimability so activationRequired still reflects
        // the unfiltered list — that's a likely bug; see issue #22 in STATUS.md (pinned pending a
        // LookupOptions flag to choose between suppressing the prompt vs. still requesting activation
        // for customer-data collection).
        var currentVehicleIsExcludedFromWarrantyActivatedItems = companyDataAggregate.FreeServiceItemExcludedVINs.Any();

        if (currentVehicleIsExcludedFromWarrantyActivatedItems)
            result.RemoveAll(x => x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation);

        var ineligibleServiceItems = await GetIneligibleServiceItems(
            result,
            serviceItems,
            vehicle?.Katashiki,
            vehicle?.VariantCode,
            languageCode
        );

        result.AddRange(ineligibleServiceItems);

        ProccessDynamicCanceledFreeServiceItems(result);

        // Order items by mileage
        result = result
            .OrderBy(x => x.TypeEnum)
            .ThenByDescending(x => x.MaximumMileage.HasValue)
            .ThenBy(x => x.MaximumMileage)
            .ThenBy(x => x.ExpiresAt)
            .ThenBy(x => x.StatusEnum)
            .ToList();

        var itemSignatureExpiry = DateTime.UtcNow;

        if (this.options?.SignatureValidityDuration != null)
            itemSignatureExpiry = itemSignatureExpiry.Add(this.options.SignatureValidityDuration);

        foreach (var item in result)
        {
            item.SignatureExpiry = itemSignatureExpiry;

            item.Signature = item.GenerateSignature(companyDataAggregate.VIN, this.options.SigningSecreteKey);

            //if (options.VehicleInspectionPreClaimVoucherPrintingURL is not null && item.VehicleInspectionID is not null)
            //    item.PrintUrl = $"{options.VehicleInspectionPreClaimVoucherPrintingURL}{item.VehicleInspectionID}/{item.ServiceItemID}";

            if (options.VehicleInspectionPreClaimVoucherPrintingURLResolver is not null && item.VehicleInspectionID is not null)
                item.PrintUrl = await options.VehicleInspectionPreClaimVoucherPrintingURLResolver(new(new(item.VehicleInspectionID, item.ServiceItemID), languageCode, this.services));

            //Service Activation takes priority and overrides PrintURL if applicable
            //if (options.ServiceActivationPreClaimVoucherPrintingURL is not null && serviceActivation is not null)
            //    item.PrintUrl = $"{options.ServiceActivationPreClaimVoucherPrintingURL}{serviceActivation.id}/{item.ServiceItemID}";

            if (options.ServiceActivationPreClaimVoucherPrintingURLResolver is not null && serviceActivation is not null)
                item.PrintUrl = await options.ServiceActivationPreClaimVoucherPrintingURLResolver(new (new (serviceActivation.id, item.ServiceItemID), languageCode, this.services));

            item.Warnings = options.StandardItemClaimWarnings;
        }

        return (result, activationRequired);
    }

    private static IEnumerable<ServiceItemModel> FilterEligibleServiceItems(
        IEnumerable<ServiceItemModel> serviceItems,
        VehicleEntryModel vehicle,
        VehicleSaleInformation vehicleSaleInformation,
        DateTime? freeServiceStartDate,
        IEnumerable<VehicleInspectionModel> vehicleInspections)
    {
        return serviceItems
            .Where(x => !x.IsDeleted)
            .Where(x => MatchesBrand(x, vehicle))
            .Where(x => MatchesCompany(x, vehicle))
            .Where(x => MatchesCountry(x, vehicle, vehicleSaleInformation))
            .Where(x => IsWithinCampaignWindow(x, freeServiceStartDate, vehicleInspections))
            .Where(x => IsApplicableToVehicle(x, vehicle));
    }

    private static bool MatchesBrand(ServiceItemModel item, VehicleEntryModel vehicle) =>
        vehicle is null || item.BrandIDs is null || item.BrandIDs.Any(a => a == vehicle.BrandID);

    private static bool MatchesCompany(ServiceItemModel item, VehicleEntryModel vehicle) =>
        vehicle is null || item.CompanyIDs is null || !item.CompanyIDs.Any() || item.CompanyIDs.Any(a => a == vehicle.CompanyID);

    // Note: this short-circuits on `vehicle is null` rather than `saleInformation is null`,
    // matching the original code. The country filter doesn't actually use `vehicle`, so the
    // guard looks like a copy-paste from the brand/company filters. Behavior is pinned for
    // now; revisit alongside other latent issues.
    private static bool MatchesCountry(ServiceItemModel item, VehicleEntryModel vehicle, VehicleSaleInformation saleInformation) =>
        vehicle is null || item.CountryIDs is null || !item.CountryIDs.Any() || item.CountryIDs.Any(a => a == saleInformation?.CountryID?.ToLong());

    private static bool IsWithinCampaignWindow(
        ServiceItemModel item,
        DateTime? freeServiceStartDate,
        IEnumerable<VehicleInspectionModel> vehicleInspections) =>
        item.CampaignActivationTrigger switch
        {
            ClaimableItemCampaignActivationTrigger.WarrantyActivation =>
                freeServiceStartDate >= item.CampaignStartDate && freeServiceStartDate <= item.CampaignEndDate,

            ClaimableItemCampaignActivationTrigger.VehicleInspection =>
                vehicleInspections?.Any(i =>
                    i.VehicleInspectionTypeID == item.VehicleInspectionTypeID &&
                    i.InspectionDate >= item.CampaignStartDate &&
                    i.InspectionDate <= item.CampaignEndDate) ?? false,

            _ => false,
        };

    // An item applies to the vehicle if either:
    //   - it has no per-model costs (item targets all vehicles), or
    //   - the vehicle's Katashiki / VariantCode prefix-matches one of the item's model costs.
    // The all-vehicles rule is gated to skip warranty-activated items when no vehicle is loaded
    // (lookups without a vehicle should not see warranty-activated free items).
    private static bool IsApplicableToVehicle(ServiceItemModel item, VehicleEntryModel vehicle)
    {
        if (HasMatchingModelCost(item, vehicle))
            return true;

        var hasVehicle = vehicle is not null;
        var isWarrantyActivationCampaign = item.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation;
        var modelCostCount = item.ModelCosts?.Count() ?? 0;

        return (hasVehicle || !isWarrantyActivationCampaign) && modelCostCount == 0;
    }

    private static bool HasMatchingModelCost(ServiceItemModel item, VehicleEntryModel vehicle) =>
        item.ModelCosts?.Any(cost =>
            HasPrefixMatch(vehicle?.Katashiki, cost?.Katashiki) ||
            HasPrefixMatch(vehicle?.VariantCode, cost?.Variant)) ?? false;

    private static bool HasPrefixMatch(string vehicleValue, string costValue) =>
        !string.IsNullOrWhiteSpace(vehicleValue) &&
        !string.IsNullOrWhiteSpace(costValue) &&
        vehicleValue.StartsWith(costValue, StringComparison.InvariantCultureIgnoreCase);

    private ServiceItemCostModel GetModelCost(
        IEnumerable<ServiceItemCostModel> modelCosts,
        string katashiki,
        string variant)
    {
        if (modelCosts is null || modelCosts?.Count() == 0)
            return null;

        return modelCosts?
            .Where(x => katashiki.StartsWith(x?.Katashiki ?? "") && !string.IsNullOrWhiteSpace(x?.Katashiki ?? "")
                || variant.StartsWith(x?.Variant ?? "") && !string.IsNullOrWhiteSpace(x?.Variant ?? ""))
            .FirstOrDefault();
    }

    private VehicleServiceItemDTO BuildFreeServiceItemDto(
        ServiceItemModel item,
        VehicleEntryModel vehicle,
        string languageCode)
    {
        ServiceItemCostModel modelCost = null;

        if (item.ModelCosts != null)
            modelCost = GetModelCost(item.ModelCosts, vehicle?.Katashiki, vehicle?.VariantCode);

        var serviceItem = new VehicleServiceItemDTO
        {
            ServiceItemID = item.IntegrationID,
            Name = Utility.GetLocalizedText(item.Name, languageCode),
            Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
            Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
            //Image = await GetFirstImageFullUrl(item.Photo),
            Type = "free",
            TypeEnum = VehcileServiceItemTypes.Free,
            ModelCostID = modelCost?.ID,
            PackageCode = modelCost?.PackageCode ?? item.PackageCode,
            Cost = modelCost == null ? item?.FixedCost : modelCost?.Cost,
            CampaignID = item.CampaignID,
            CampaignUniqueReference = item.CampaignUniqueReference,

            MaximumMileage = item.MaximumMileage,
            CampaignActivationTrigger = item.CampaignActivationTrigger,
            CampaignActivationType = item.CampaignActivationType,

            ValidityModeEnum = item.ValidityMode,
            ClaimingMethodEnum = item.ClaimingMethod,

            ShowDocumentUploader = item.AttachmentFieldBehavior != ClaimableItemAttachmentFieldBehavior.Hidden,
            DocumentUploaderIsRequired = item.AttachmentFieldBehavior == ClaimableItemAttachmentFieldBehavior.Required,

            VehicleInspectionTypeID = item.VehicleInspectionTypeID.ToString()
        };

        if (item.ValidityMode == ClaimableItemValidityMode.FixedDateRange)
        {
            serviceItem.ActivatedAt = item.ValidFrom.Value;
            serviceItem.ExpiresAt = item.ValidTo;
        }
        else if (item.ValidityMode == ClaimableItemValidityMode.RelativeToActivation)
        {
            serviceItem.ActiveFor = item.ActiveFor;
            serviceItem.ActiveForDurationType = item.ActiveForDurationType;
        }

        return serviceItem;
    }

    private static VehicleServiceItemDTO BuildPaidServiceItemDto(
        PaidServiceInvoiceModel paidService,
        PaidServiceInvoiceLineModel line,
        string languageCode)
    {
        return new VehicleServiceItemDTO
        {
            ServiceItemID = line.ServiceItemID,
            PaidServiceInvoiceLineID = line.IntegrationID,
            ActivatedAt = paidService.InvoiceDate,
            CampaignUniqueReference = line.ServiceItem?.CampaignUniqueReference,
            Description = Utility.GetLocalizedText(line.ServiceItem?.PrintoutDescription, languageCode),
            //Image = await GetFirstImageFullUrl(line.ServiceItem?.Photo),
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
    }

    private void CalculateRollingExpireDateForWarrantyActivatedFreeServiceItems(IEnumerable<VehicleServiceItemDTO> serviceItems, DateTime? invoiceDate)
    {
        if (invoiceDate is null)
            return;

        var sequentialServiceItems = serviceItems.Where(x => x.MaximumMileage is not null);
        var nonSequentialServiceItems = serviceItems.Where(x => x.MaximumMileage is null);

        DateTime? rollingDate = invoiceDate.Value;

        foreach (var item in sequentialServiceItems)
        {
            item.ActivatedAt = rollingDate.Value;
            item.ExpiresAt = AddInterval(rollingDate.Value, item.ActiveFor, item.ActiveForDurationType);

            rollingDate = item.ExpiresAt;
        }

        // Issue #14 (pinned): non-sequential items inherit the bundle's collective end date.
        // When no sequential items exist, rollingDate stays at invoiceDate, so the non-sequential
        // item ends up activated and expired on the same day. Production data always bundles
        // these alongside sequential items, so this hasn't surfaced in practice. See
        // ServiceItems_Expiration.feature ("Sole non-sequential item expires at the free service start date — pinned").
        foreach (var item in nonSequentialServiceItems)
        {
            item.ActivatedAt = invoiceDate.Value;
            item.ExpiresAt = rollingDate;
        }
    }

    private List<VehicleServiceItemDTO> CalculateRollingExpireDateForVehicleInspectionActivatedFreeServiceItems(IEnumerable<VehicleServiceItemDTO> serviceItems, IEnumerable<VehicleInspectionModel> vehicleInspections)
    {
        var newList = new List<VehicleServiceItemDTO>();

        foreach (var item in serviceItems)
        {
            foreach (var inspection in SelectInspectionsForActivation(item, vehicleInspections))
                newList.Add(CloneWithInspectionActivation(item, inspection));
        }

        return newList;
    }

    private static IEnumerable<VehicleInspectionModel> SelectInspectionsForActivation(
        VehicleServiceItemDTO item,
        IEnumerable<VehicleInspectionModel> vehicleInspections)
    {
        var matching = vehicleInspections
            .Where(x => x.VehicleInspectionTypeID.ToString() == item.VehicleInspectionTypeID);

        if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.EveryTrigger)
            return matching;

        if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.FirstTriggerOnly)
            return new[] { matching.OrderBy(x => x.InspectionDate).First() };

        if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.ExtendOnEachTrigger)
            return new[] { matching.OrderByDescending(x => x.InspectionDate).First() };

        // Pinning original behavior: an unset/unexpected CampaignActivationType used to fall
        // through with a null inspection and NRE downstream on `vehicleInspection.id`. The
        // single-null array reproduces that failure mode.
        return new VehicleInspectionModel[] { null };
    }

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

    private DateTime AddInterval(DateTime date, int? intervalValue, DurationType? durationType)
    {
        if (durationType == DurationType.Seconds)
            return date.AddSeconds(intervalValue.GetValueOrDefault());
        else if (durationType == DurationType.Minutes)
            return date.AddMinutes(intervalValue.GetValueOrDefault());
        else if (durationType == DurationType.Hours)
            return date.AddHours(intervalValue.GetValueOrDefault());
        else if (durationType == DurationType.Days)
            return date.AddDays(intervalValue.GetValueOrDefault());
        else if (durationType == DurationType.Weeks)
            return date.AddDays(7 * intervalValue.GetValueOrDefault());
        else if (durationType == DurationType.Months)
            return date.AddMonths(intervalValue.GetValueOrDefault());
        else if (durationType == DurationType.Years)
            return date.AddYears(intervalValue.GetValueOrDefault());
        else
            return date;
    }

    private async Task<bool> CalculateServiceItemStatusAndClaimability(
        List<VehicleServiceItemDTO> serviceItems,
        bool showingInactivatedItems,
        string languageCode)
    {
        await CalculateServiceItemStatus(serviceItems, showingInactivatedItems, languageCode);

        var activationRequired = serviceItems.Any(x => x.StatusEnum == VehcileServiceItemStatuses.ActivationRequired);

        foreach (var item in serviceItems)
        {
            if (item.StatusEnum == VehcileServiceItemStatuses.Pending)
                item.Claimable = true;

            if (item.ValidityModeEnum == ClaimableItemValidityMode.FixedDateRange && item.ActivatedAt > DateTime.Now)
                item.Claimable = false;
        }

        return activationRequired;
    }

    private async Task CalculateServiceItemStatus(IEnumerable<VehicleServiceItemDTO> serviceItems, bool showingInactivatedItems, string languageCode)
    {
        foreach (var item in serviceItems)
        {
            var statusResult = ProcessServiceItemStatus(
                item,
                companyDataAggregate.ItemClaims,
                showingInactivatedItems
            );

            item.Status = statusResult.statusText;
            item.StatusEnum = statusResult.status;
            item.ClaimDate = statusResult.claimDate;
            item.JobNumber = statusResult.wip;
            item.InvoiceNumber = statusResult.invoice;
            //item.CompanyID = statusResult.companyID;
            item.PackageCode = statusResult.packageCode ?? item.PackageCode;

            if (statusResult.companyID != null && statusResult.companyID != 0 && options.CompanyNameResolver is not null)
                item.CompanyName = await options.CompanyNameResolver(new(statusResult.companyID, languageCode, services));
        }
    }

    private (string statusText, VehcileServiceItemStatuses status, DateTimeOffset? claimDate, string wip, string invoice, long? companyID, string packageCode)
    ProcessServiceItemStatus(
        VehicleServiceItemDTO item,
        IEnumerable<ItemClaimModel> serviceClaimLines,
        bool showingInactivatedItems
    )
    {
        var claimLine = serviceClaimLines?
            .Where(x => x?.ServiceItemID == item.ServiceItemID.ToString())
            .Where(x => x?.VehicleInspectionID == item.VehicleInspectionID)
            .FirstOrDefault();

        if (claimLine is not null)
        {
            return ("processed", VehcileServiceItemStatuses.Processed,
                claimLine.ClaimDate,
                claimLine.JobNumber,
                claimLine.InvoiceNumber, claimLine.CompanyID, claimLine.PackageCode);
        }

        if (item.ExpiresAt is not null && item.ExpiresAt < DateTime.Now)
            return ("expired", VehcileServiceItemStatuses.Expired, null, null, null, null, null);

        if (showingInactivatedItems && item.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation)
            return ("activationRequired", VehcileServiceItemStatuses.ActivationRequired, null, null, null, null, null);

        return ("pending", VehcileServiceItemStatuses.Pending, null, null, null, null, null);
    }

    private async Task<IEnumerable<VehicleServiceItemDTO>> GetIneligibleServiceItems(
        IEnumerable<VehicleServiceItemDTO> eligibleServiceItems,
        IEnumerable<ServiceItemModel> availableServiceItems,
        string katashiki,
        string variant,
        string languageCode
        )
    {
        var result = new List<VehicleServiceItemDTO>();

        var existingServiceItemIds = eligibleServiceItems?
            .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free)
            .Select(x => x.ServiceItemID.ToString())
            .ToList();

        var claimedItems = companyDataAggregate.ItemClaims?
            .Select(x => x?.ServiceItemID)
            .Where(x => !(existingServiceItemIds?.Any(s => s == x) ?? false));

        foreach (var item in availableServiceItems.Where(x => claimedItems?.Any(a => a == x.IntegrationID) ?? false))
        {
            //var modelCost = GetModelCost(item.ModelCosts, katashiki, variant);

            var claimLine = companyDataAggregate
                .ItemClaims?
                .FirstOrDefault(t => t.ServiceItemID == item.IntegrationID);

            var serviceItem = new VehicleServiceItemDTO
            {
                ServiceItemID = item.IntegrationID,
                Name = Utility.GetLocalizedText(item.Name, languageCode),
                Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
                Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
                //Image = await GetFirstImageFullUrl(item.Photo),
                Type = "free",
                TypeEnum = VehcileServiceItemTypes.Free,
                StatusEnum = VehcileServiceItemStatuses.Processed,
                Status = "processed",
                //ModelCostID = modelCost?.ID,
                PackageCode = claimLine.PackageCode, //modelCost?.PackageCode ?? item.PackageCode,
                ClaimDate = claimLine?.ClaimDate,
                InvoiceNumber = claimLine?.InvoiceNumber,
                JobNumber = claimLine?.JobNumber,
                //CompanyID = claimLine?.CompanyID,
                MaximumMileage = item.MaximumMileage
            };

            if (claimLine.CompanyID != null && claimLine.CompanyID != 0 && options.CompanyNameResolver is not null)
                serviceItem.CompanyName = await options.CompanyNameResolver(new(claimLine?.CompanyID, languageCode, services));

            result.Add(serviceItem);
        }

        return result;
    }

    private void ProccessDynamicCanceledFreeServiceItems(IEnumerable<VehicleServiceItemDTO> serviceItems)
    {
        var freeItems = serviceItems
            .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free)
            .Where(x => x.MaximumMileage.HasValue)
            .OrderBy(x => x.MaximumMileage);

        foreach (var item in freeItems)
        {
            if (item.StatusEnum == VehcileServiceItemStatuses.Pending)
            {
                if (freeItems.Any(x => x.StatusEnum == VehcileServiceItemStatuses.Processed && x.MaximumMileage > item.MaximumMileage))
                {
                    item.Status = "cancelled";
                    item.StatusEnum = VehcileServiceItemStatuses.Cancelled;
                    item.Claimable = false;
                }
            }
        }
    }
}
