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
        serviceItems = await lookupCosmosService.GetServiceItemsAsync();

        var shiftDay = companyDataAggregate.FreeServiceItemDateShifts?.FirstOrDefault(x => x.VIN == vehicle.VIN);
        if (shiftDay is not null)
            freeServiceStartDate = shiftDay.NewDate;

        var showingInactivatedItems = false;

        //Allow showing free service items as 'Activation Required'
        if ((options?.IncludeInactivatedFreeServiceItems ?? false) && freeServiceStartDate is null)
        {
            freeServiceStartDate = DateTime.Now.Date;
            showingInactivatedItems = true;
        }

        // Free services  
        var eligibleServiceItems = serviceItems.Where(x => !(x.IsDeleted));

        // Brand
        eligibleServiceItems = eligibleServiceItems.Where(x => vehicle is null || x.BrandIDs is null || x.BrandIDs.Where(a => a == vehicle.BrandID).Count() > 0);

        // Company
        eligibleServiceItems = eligibleServiceItems.Where(x => x.CompanyIDs is null || x.CompanyIDs.Count() == 0 || vehicle is null || x.CompanyIDs.Where(a => a == vehicle?.CompanyID).Count() > 0);

        // Country
        eligibleServiceItems = eligibleServiceItems.Where(x => x.CountryIDs is null || x.CountryIDs.Count() == 0 || vehicle is null || x.CountryIDs.Where(a => a == vehicleSaleInformation?.CountryID?.ToLong()).Count() > 0);

        // Expiry
        eligibleServiceItems = eligibleServiceItems.Where(x =>

            x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation ?
            (freeServiceStartDate >= x.CampaignStartDate && freeServiceStartDate <= x.CampaignEndDate) :

            x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.VehicleInspection ?
            (vehicleInspections?.Where(i => i.VehicleInspectionTypeID == x.VehicleInspectionTypeID && i.InspectionDate >= x.CampaignStartDate && i.InspectionDate <= x.CampaignEndDate).Count() > 0) :

            false
        );

        bool modelCodeMatchingEvaluator(ServiceItemModel x) =>
            x.ModelCosts?.Any(a =>
                (!string.IsNullOrWhiteSpace(vehicle?.Katashiki) && !string.IsNullOrWhiteSpace(a?.Katashiki) && vehicle.Katashiki.StartsWith(a?.Katashiki ?? "", StringComparison.InvariantCultureIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(vehicle?.VariantCode) && !string.IsNullOrWhiteSpace(a?.Variant) && vehicle.VariantCode.StartsWith(a?.Variant ?? "", StringComparison.InvariantCultureIgnoreCase))
            ) ?? false;

        eligibleServiceItems = eligibleServiceItems.Where(item =>
        {
            var x = false;

            var modelCodeMatch = modelCodeMatchingEvaluator(item);

            //Per vehicle is only applicable for warranty activated (official) items
            if (
                item.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation &&
                options.PerVehicleEligibilitySupport
            )
            {
                x = vehicle?.EligibleServiceItemUniqueReferences is not null &&
                    vehicle.EligibleServiceItemUniqueReferences
                    .Select(y => y?.Trim())
                    .Contains(item.UniqueReference?.Trim(), StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                //Items targgeting all vehicles are applicable for official cars and for non-official cars (In case the item is not warranty activated).
                if (vehicle is not null || item.CampaignActivationTrigger != ClaimableItemCampaignActivationTrigger.WarrantyActivation)
                    x = (item.ModelCosts?.Count() ?? 0) == 0;
            }

            return x || modelCodeMatch;
        });

        if (eligibleServiceItems?.Count() > 0)
        {
            // Order them by mileage
            eligibleServiceItems = eligibleServiceItems
                .OrderByDescending(x => x.MaximumMileage.HasValue)
                .ThenBy(x => x.MaximumMileage);

            foreach (var item in eligibleServiceItems)
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

                result.Add(serviceItem);
            }
        }

        if (paidServices?.Count() > 0)
        {
            foreach (var paidService in paidServices)
            {
                if (paidService?.Lines?.Count() > 0)
                {
                    foreach (var item in paidService.Lines)
                    {
                        var itemResult = new VehicleServiceItemDTO
                        {
                            ServiceItemID = item.ServiceItemID,
                            PaidServiceInvoiceLineID = item.IntegrationID,
                            ActivatedAt = paidService.InvoiceDate,
                            CampaignUniqueReference = item.ServiceItem?.CampaignUniqueReference,
                            Description = Utility.GetLocalizedText(item.ServiceItem?.PrintoutDescription, languageCode),
                            //Image = await GetFirstImageFullUrl(item.ServiceItem?.Photo),
                            Name = Utility.GetLocalizedText(item.ServiceItem?.Name, languageCode),
                            Title = Utility.GetLocalizedText(item.ServiceItem?.PrintoutTitle, languageCode),
                            ExpiresAt = item.ExpireDate,
                            Type = "paid",
                            MaximumMileage = item.ServiceItem?.MaximumMileage,
                            TypeEnum = VehcileServiceItemTypes.Paid,
                            PackageCode = item.PackageCode,

                            ClaimingMethodEnum = item.ServiceItem.ClaimingMethod,
                            VehicleInspectionTypeID = item.ServiceItem.VehicleInspectionTypeID?.ToString(),
                        };

                        result.Add(itemResult);
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

        await CalulateServiceItemStatus(result, showingInactivatedItems, languageCode);

        var activationRequired = result.Any(x => x.StatusEnum == VehcileServiceItemStatuses.ActivationRequired);

        if (companyDataAggregate.FreeServiceItemExcludedVINs.Any())
            result = result.Where(x => x.CampaignActivationTrigger != ClaimableItemCampaignActivationTrigger.WarrantyActivation).ToList();

        foreach (var item in result)
        {
            if (item.StatusEnum == VehcileServiceItemStatuses.Pending)
                item.Claimable = true;

            if (item.ValidityModeEnum == ClaimableItemValidityMode.FixedDateRange && item.ActivatedAt > DateTime.Now)
                item.Claimable = false;
        }

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

            item.Signature = item.GenerateSignature(companyDataAggregate.VIN, this.options?.SigningSecreteKey);

            //if (options.VehicleInspectionPreClaimVoucherPrintingURL is not null && item.VehicleInspectionID is not null)
            //    item.PrintUrl = $"{options.VehicleInspectionPreClaimVoucherPrintingURL}{item.VehicleInspectionID}/{item.ServiceItemID}";

            if (options?.VehicleInspectionPreClaimVoucherPrintingURLResolver is not null && item.VehicleInspectionID is not null)
                item.PrintUrl = await options.VehicleInspectionPreClaimVoucherPrintingURLResolver(new(new(item.VehicleInspectionID, item.ServiceItemID), languageCode, this.services));

            //Service Activation takes priority and overrides PrintURL if applicable
            //if (options.ServiceActivationPreClaimVoucherPrintingURL is not null && serviceActivation is not null)
            //    item.PrintUrl = $"{options.ServiceActivationPreClaimVoucherPrintingURL}{serviceActivation.id}/{item.ServiceItemID}";

            if (options?.ServiceActivationPreClaimVoucherPrintingURLResolver is not null && serviceActivation is not null)
                item.PrintUrl = await options.ServiceActivationPreClaimVoucherPrintingURLResolver(new (new (serviceActivation.id, item.ServiceItemID), languageCode, this.services));

            item.Warnings = options?.StandardItemClaimWarnings ?? [];
        }

        return (result, activationRequired);
    }

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

    private void CalculateRollingExpireDateForWarrantyActivatedFreeServiceItems(IEnumerable<VehicleServiceItemDTO> serviceItems, DateTime? invoiceDate)
    {
        if (invoiceDate is null)
            return;

        var startDate = invoiceDate;

        var sequencialServiceItems = serviceItems.Where(x => x.MaximumMileage is not null);

        foreach (var item in sequencialServiceItems)
        {
            item.ActivatedAt = startDate.GetValueOrDefault();
            item.ExpiresAt = AddInterval(startDate.GetValueOrDefault(), item.ActiveFor, item.ActiveForDurationType);

            startDate = item.ExpiresAt;
        }

        var nonSequencialServiceItems = serviceItems.Where(x => x.MaximumMileage is null);

        foreach (var item in nonSequencialServiceItems)
        {
            item.ActivatedAt = invoiceDate.Value;
            item.ExpiresAt = startDate;
        }
    }

    private List<VehicleServiceItemDTO> CalculateRollingExpireDateForVehicleInspectionActivatedFreeServiceItems(IEnumerable<VehicleServiceItemDTO> serviceItems, IEnumerable<VehicleInspectionModel> vehicleInspections)
    {
        var newList = new List<VehicleServiceItemDTO>();

        foreach (var item in serviceItems)
        {
            var filteredVehicleInspections = vehicleInspections
                .Where(x => x.VehicleInspectionTypeID.ToString() == item.VehicleInspectionTypeID);

            if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.EveryTrigger)
            {
                foreach (var vehicleInspection in filteredVehicleInspections)
                {
                    var cloned = item.Clone();

                    if (item.ValidityModeEnum == ClaimableItemValidityMode.RelativeToActivation)
                    {
                        cloned.ActivatedAt = vehicleInspection.InspectionDate.DateTime;
                        cloned.ExpiresAt = AddInterval(cloned.ActivatedAt, item.ActiveFor, cloned.ActiveForDurationType);
                    }

                    cloned.VehicleInspectionID = vehicleInspection.id;

                    newList.Add(cloned);
                }
            }
            else
            {
                VehicleInspectionModel vehicleInspection = null;

                if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.FirstTriggerOnly)
                    vehicleInspection = filteredVehicleInspections.OrderBy(x => x.InspectionDate).First();

                else if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.ExtendOnEachTrigger)
                    vehicleInspection = filteredVehicleInspections.OrderByDescending(x => x.InspectionDate).First();

                var cloned = item.Clone();

                cloned.VehicleInspectionID = vehicleInspection.id;

                if (cloned.ValidityModeEnum == ClaimableItemValidityMode.RelativeToActivation)
                {
                    cloned.ActivatedAt = vehicleInspection.InspectionDate.DateTime;
                    cloned.ExpiresAt = AddInterval(cloned.ActivatedAt, cloned.ActiveFor, cloned.ActiveForDurationType);
                }

                newList.Add(cloned);
            }
        }

        return newList;
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

    private async Task CalulateServiceItemStatus(IEnumerable<VehicleServiceItemDTO> serviceItems, bool showingInactivatedItems, string languageCode)
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

            if (claimLine.CompanyID != null && claimLine.CompanyID != 0 && options?.CompanyNameResolver is not null)
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