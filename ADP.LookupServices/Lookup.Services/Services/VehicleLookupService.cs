using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Invoice;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class VehicleLookupService
{
    public readonly IVehicleLoockupCosmosService lookupCosmosService;
    readonly LookupOptions options;
    readonly IIdentityCosmosService identityCosmosService;
    private readonly IServiceProvider services;
    private readonly ILogCosmosService? logCosmosService;
    CompanyDataAggregateCosmosModel companyDataAggregate = new CompanyDataAggregateCosmosModel();


    string languageCode;


    public VehicleLookupService(
        IVehicleLoockupCosmosService lookupService,
        IIdentityCosmosService identityCosmosService,
        IServiceProvider? services = null,
        ILogCosmosService? logCosmosService = null,
        LookupOptions options = null)
    {
        lookupCosmosService = lookupService;
        this.options = options;
        this.identityCosmosService = identityCosmosService;
        this.services = services;
        this.logCosmosService = logCosmosService;
    }

    public async Task<VehicleLookupDTO> LookupAsync(
        string vin,
        string regionIntegrationId,
        string languageCode = null,
        bool ignoreBrokerStock = false,
        bool insertSSCLog = false,
        SSCLogInfo? sscLogInfo = null,
        bool insertCustomerVehcileLookupLog = false,
        CustomerVehicleLookupLogInfo? customerVehicleLookupLogInfo = null)
    {
        var data = new VehicleLookupDTO();

        // Get all items related to the VIN from the cosmos container
        companyDataAggregate = await lookupCosmosService.GetAggregatedCompanyData(vin);

        // Set paint thickness
        data.PaintThickness = await GetPaintThickness();

        this.languageCode = languageCode;

        // Get the latest Vehicle
        VehicleEntryModel vehicle = null;

        var vehicles = companyDataAggregate
            .VehicleEntries
            //.Select(x => (VehicleEntryModel)x)
            //.Concat(companyDataAggregate.VehicleServiceActivations.Select(x => (VehicleEntryModel)x))
            .ToList();

        if (vehicles?.Count() > 0)
            if (vehicles.Any(x => x.InvoiceDate is null))
                vehicle = vehicles.FirstOrDefault(x => x.InvoiceDate is null);
            else
                vehicle = vehicles.OrderByDescending(x => x.InvoiceDate).FirstOrDefault();

        // Set identifiers
        data.VIN = vin;
        data.Identifiers = new VehicleIdentifiersDTO { VIN = vin };

        if (vehicle is not null)
        {
            // Set identifiers
            data.Identifiers = GetIdentifiers(vehicle, vin);

            // Set Specification
            data.VehicleSpecification = await GetSpecificationAsync(vehicle);
        }

        // Set IsAuthorized
        data.IsAuthorized = companyDataAggregate.InitialOfficialVINs?.Count() > 0 ||
            companyDataAggregate.VehicleEntries?.Count() > 0 ||
            companyDataAggregate.SSCAffectedVINs.Count() > 0;

        // Set NextServiceDate
        data.NextServiceDate = companyDataAggregate.Invoices?.OrderByDescending(x => x.InvoiceDate).FirstOrDefault()?.NextServiceDate;

        // Set ServiceHistories
        data.ServiceHistory = await GetServiceHistory(companyDataAggregate.Invoices, companyDataAggregate.LaborLines, companyDataAggregate.PartLines);

        // Set SSC
        data.SSC = await GetSSCAsync(companyDataAggregate.SSCAffectedVINs, companyDataAggregate.WarrantyClaims, companyDataAggregate.LaborLines, regionIntegrationId);

        DateTime? warrantyStartDate = null;
        DateTime? freeServiceStartDate = null;

        if (vehicle is not null)
        {
            // Set SaleInformation
            data.SaleInformation = await GetSaleInformationAsync(vehicles);

            //Normal company Sale
            if (data.SaleInformation?.Broker is null)
            {
                warrantyStartDate = data.SaleInformation?.WarrantyActivationDate;

                var serviceActivation = companyDataAggregate.VehicleServiceActivations.FirstOrDefault();

                if (serviceActivation is not null)
                    warrantyStartDate = serviceActivation.WarrantyActivationDate;

                if (warrantyStartDate is null && options.WarrantyStartDateDefaultsToInvoiceDate)
                    warrantyStartDate = data.SaleInformation?.InvoiceDate;

                freeServiceStartDate = warrantyStartDate;
            }
            else
            {
                //Broker Stock
                if (data.SaleInformation.Broker.InvoiceDate is null)
                {
                    if (ignoreBrokerStock)
                    {
                        warrantyStartDate = null;
                        freeServiceStartDate = data.SaleInformation?.WarrantyActivationDate ?? data.SaleInformation?.InvoiceDate;
                    }
                }
                //Normal Broker Sale
                else
                {
                    warrantyStartDate = data.SaleInformation?.Broker?.InvoiceDate;
                    freeServiceStartDate = data.SaleInformation?.Broker?.InvoiceDate;
                }
            }

            // Set Warranty
            data.Warranty = await GetWarrantyAsync(warrantyStartDate, vehicle.Brand);
        }

        data.ServiceItems = await GetServiceItems(
            vin,
            freeServiceStartDate, 
            vehicle, 
            data.SaleInformation,
            companyDataAggregate.PaidServiceInvoices, 
            companyDataAggregate.ItemClaims,
            companyDataAggregate.VehicleInspections
        );

        // Set accessories
        data.Accessories = await GetAccessories();

        if (insertSSCLog)
        {
            var logId = await logCosmosService?.LogSSCLookupAsync(sscLogInfo, data.SSC, vin, data.IsAuthorized, data.Warranty?.HasActiveWarranty ?? false, data.Identifiers?.Brand);
            data.SSCLogId = logId;
        }

        if (insertCustomerVehcileLookupLog)
        {
            await logCosmosService.LogCustomerVehicleLookupAsync(customerVehicleLookupLogInfo, vin, data.IsAuthorized , data.Warranty?.HasActiveWarranty ?? false, data.Identifiers?.Brand);
        }

        return data;
    }

    private async Task<IEnumerable<AccessoryDTO>> GetAccessories()
    {
        var accessories = new List<AccessoryDTO>();

        foreach (var accessory in companyDataAggregate.Accessories)
        {
            var accessoryDTO = new AccessoryDTO
            {
                PartNumber = accessory.PartNumber,
                Description = accessory.PartDescription,
                Image = await GetPaintAccessoryImageFullUrl(accessory.Image)
            };

            accessories.Add(accessoryDTO);
        }

        return accessories;
    }

    private async Task<string> GetPaintAccessoryImageFullUrl(string image)
    {
        if (options?.AccessoryImageUrlResolver is not null)
            return await options.AccessoryImageUrlResolver(new(image,languageCode,services));

        return image;
    }

    private async Task<PaintThicknessDTO> GetPaintThickness()
    {
        if (companyDataAggregate.PaintThicknessInspections is null)
            return null;

        var groups = companyDataAggregate.PaintThicknessInspections.Images?.GroupBy(x => x.ToLower().Replace("left", string.Empty).Replace("right", string.Empty));
        var nonPairGroups = groups?.Where(x => x.Count() == 1).SelectMany(x => x)
            .GroupBy(x => x.Substring(0, x.LastIndexOf('_')));

        var imageGroups = new List<PaintThicknessImageDTO>();

        if (groups != null)
        {
            foreach (var group in groups.Where(x => x.Count() == 2))
            {
                string filename = Path.GetFileNameWithoutExtension(group.FirstOrDefault());

                var name = Regex.Replace(filename, "left", string.Empty, RegexOptions.IgnoreCase);
                name = Regex.Replace(name, "right", string.Empty, RegexOptions.IgnoreCase);
                name = name.Replace("_", " ").Trim();

                var images = new List<string>();
                foreach (var image in group.OrderBy(o => o))
                {
                    images.Add(await GetPaintThicknessImageFullUrl(image));
                }

                var result = new PaintThicknessImageDTO
                {
                    Images = images,
                    Name = name
                };

                imageGroups.Add(result);
            }
        }

        if (nonPairGroups != null)
        {
            foreach (var group in nonPairGroups)
            {
                string filename = Path.GetFileNameWithoutExtension(group.FirstOrDefault());

                var name = filename.Substring(0, filename.LastIndexOf('_'));
                name = name.Replace("_", " ").Trim();

                var images = new List<string>();
                foreach (var image in group.OrderBy(o => o))
                {
                    images.Add(await GetPaintThicknessImageFullUrl(image));
                }

                var result = new PaintThicknessImageDTO
                {
                    Images = images,
                    Name = name
                };

                imageGroups.Add(result);
            }
        }

        return new PaintThicknessDTO
        {
            Parts = companyDataAggregate.PaintThicknessInspections.Parts?.Select(x => new PaintThicknessPartDTO
            {
                Part = x.Part,
                Left = x.Left,
                Right = x.Right,
            }),
            ImageGroups = imageGroups
        };
    }

    private async Task<string> GetPaintThicknessImageFullUrl(string image)
    {
        if (options?.PaintThickneesImageUrlResolver is not null)
            return await options.PaintThickneesImageUrlResolver(new (image,languageCode,services));

        return image;
    }

    private async Task<IEnumerable<VehicleServiceHistoryDTO>> GetServiceHistory(
        IEnumerable<InvoiceModel> cpus,
        IEnumerable<OrderLaborLineModel> labors,
        IEnumerable<OrderPartLineModel> parts)
    {
        var serviceHistory = new List<VehicleServiceHistoryDTO>();

        if (cpus != null)
        {
            foreach (var x in cpus.OrderByDescending(x => x.InvoiceDate))
            {
                // Remove the branch id from the service type
                var serviceType = x.ServiceDetails;
                var slashIndex = serviceType.IndexOf("/");
                if (slashIndex > 0)
                {
                    var branchId = serviceType.Substring(0, slashIndex);
                    if (int.TryParse(branchId, out _))
                        serviceType = serviceType.Substring(slashIndex + 1);
                }

                var result = new VehicleServiceHistoryDTO
                {
                    ServiceType = serviceType,
                    ServiceDate = x.InvoiceDate,
                    Mileage = x.Mileage,
                    CompanyID = x.CompanyID,
                    BranchID = x.BranchID,
                    AccountNumber = x.AccountNumber,
                    InvoiceNumber = x.InvoiceNumber,
                    JobNumber = x.OrderDocumentNumber,
                    LaborLines = labors?.Where(l => l.OrderDocumentNumber == x.OrderDocumentNumber && l.InvoiceNumber == x.InvoiceNumber &&
                        l.CompanyID == x.CompanyID)
                            .Select(l => new VehicleLaborDTO
                            {
                                Description = l.ServiceDescription,
                                MenuCode = l.MenuCode,
                                RTSCode = l.LaborCode,
                                ServiceCode = l.ServiceCode
                            }),
                    PartLines = parts?.Where(p => p.OrderDocumentNumber == x.OrderDocumentNumber && p.InvoiceNumber == x.InvoiceNumber &&
                        p.CompanyID == x.CompanyID)
                            .Select(p => new VehiclePartDTO
                            {
                                MenuCode = p.MenuCode,
                                PartNumber = p.PartNumber,
                                QTY = p.Quantity,
                            })
                };

                if (options.CompanyNameResolver is not null)
                    result.CompanyName = await options.CompanyNameResolver(new(x.CompanyID, languageCode, services));

                if (options.CompanyBranchNameResolver is not null)
                    result.BranchName = await options.CompanyBranchNameResolver(
                        new(x.BranchID, languageCode, services));

                serviceHistory.Add(result);
            }
        }

        return serviceHistory;
    }

    private async Task<VehicleSpecificationDTO> GetSpecificationAsync(VehicleEntryModel vehicle)
    {
        VehicleSpecificationDTO result = new();

        var vehicleModel = vehicle?.VehicleModel;

        if (vehicleModel is null)
        {
            vehicleModel = await lookupCosmosService.GetVehicleModelsAsync(vehicle?.VariantCode, vehicle?.Brand);

            if (vehicleModel is not null)
                lookupCosmosService.UpdateVSDataModel(vehicle, vehicleModel);
        }

        //if (vtModel is not null)
        {
            result = new VehicleSpecificationDTO
            {
                BodyType = vehicleModel?.BodyType,
                Class = vehicleModel?.Class,
                Cylinders = vehicleModel?.Cylinders,
                Doors = vehicleModel?.Doors,
                Engine = vehicleModel?.Engine,
                EngineType = vehicleModel?.EngineType,
                Fuel = vehicleModel?.Fuel,
                LightHeavyType = vehicleModel?.LightHeavyType,
                ModelCode = vehicleModel?.ModelCode,
                ProductionDate = vehicle?.ProductionDate,
                ModelYear = vehicle?.ModelYear,
                FuelLiter = null,
                ModelDescription = vehicleModel?.ModelDescription,
                Side = vehicleModel?.Side,
                Style = vehicleModel?.Style,
                TankCap = vehicleModel?.TankCap,
                Transmission = vehicleModel?.Transmission,
                VariantDescription = vehicleModel?.VariantDescription,
                ExteriorColor = vehicle?.ExteriorColor?.Description,
                InteriorColor = vehicle?.InteriorColor?.Description
            };
        }

        if (vehicle?.ExteriorColor is null)
        {
            var color = await lookupCosmosService.GetExteriorColorsAsync(vehicle?.ExteriorColorCode, vehicle?.Brand);
            if (color is not null)
            {
                result.ExteriorColor = color?.Description;
                lookupCosmosService.UpdateVSDataColor(vehicle, color);
            }
        }

        if (vehicle?.InteriorColor is null)
        {
            var trim = await lookupCosmosService.GetInteriorColorsAsync(vehicle?.InteriorColorCode, vehicle?.Brand);
            if (trim is not null)
            {
                result.InteriorColor = trim?.Description;
                lookupCosmosService.UpdateVSDataTrim(vehicle, trim);
            }
        }

        await lookupCosmosService.SaveChangesAsync();

        return result;
    }

    private VehicleIdentifiersDTO GetIdentifiers(VehicleEntryModel vehicle, string vin)
    {
        return new VehicleIdentifiersDTO
        {
            VIN = vin,
            Variant = vehicle.VariantCode,
            Katashiki = vehicle.Katashiki,
            Color = vehicle.ExteriorColorCode,
            Trim = vehicle.InteriorColorCode,
            Brand = vehicle.Brand,
            BrandID = vehicle.BrandID
        };
    }

    private async Task<VehicleSaleInformation> GetSaleInformationAsync(List<VehicleEntryModel> vehicles)
    {
        VehicleSaleInformation result = new();

        if (!(vehicles?.Any() ?? false))
            return null;

        var vsData = vehicles
            .OrderByDescending(x => x.InvoiceDate == null)
            .ThenByDescending(x => x.InvoiceDate)
            .FirstOrDefault();

        result.InvoiceDate = vsData?.InvoiceDate;
        result.WarrantyActivationDate = vsData?.WarrantyActivationDate;
        result.Status = vsData.OrderStatus;
        result.Location = vsData.Location;
        result.SaleType = vsData.SaleType;
        result.AccountNumber = vsData.AccountNumber;
        result.RegionID = vsData.RegionID;

        result.InvoiceNumber = vsData?.InvoiceNumber;
        result.InvoiceTotal = vsData?.InvoiceTotal ?? 0;
        result.CompanyID = vsData?.CompanyID;
        result.BranchID = vsData?.BranchID;

        result.CustomerID = vsData?.CustomerID;
        result.CustomerAccountNumber = vsData?.CustomerAccountNumber;

        if (options.CountryFromBranchIDResolver is not null)
        {
            var countryResult = await options.CountryFromBranchIDResolver(new(vsData.BranchID, languageCode, services));

            if (countryResult is not null)
            {
                result.CountryID = countryResult.Value.countryID;
                result.CountryName = countryResult.Value.countryName;
            }
        }

        if (options.CompanyNameResolver is not null)
            result.CompanyName = await options.CompanyNameResolver(new(vsData.CompanyID, languageCode, services));

        if (options.CompanyBranchNameResolver is not null)
            result.BranchName = await options.CompanyBranchNameResolver(
                new(vsData.BranchID, languageCode, services));

        string? companyLogo = null;

        if(options.CompanyLogoResolver is not null)
            companyLogo = await options.CompanyLogoResolver(new(vsData.CompanyID, languageCode, services));   

        //if(!string.IsNullOrWhiteSpace(companyLogo))
        //    try
        //    {
        //        result.CompanyLogo = await GetCompanyLogo(JsonSerializer.Deserialize<List<ShiftFileDTO>>(companyLogo));
        //    }
        //    catch (Exception){}

        if (companyDataAggregate.BrokerInvoices?.Any() ?? false)
        {
            var brokerInvoice = companyDataAggregate.BrokerInvoices.FirstOrDefault();
            var broker = await lookupCosmosService.GetBrokerAsync(brokerInvoice.ID);

            result.Broker = new VehicleBrokerSaleInformation
            {
                BrokerID = brokerInvoice.ID,
                BrokerName = broker?.Name,
                CustomerID = (brokerInvoice.BrokerCustomerID ?? brokerInvoice.NonOfficialBrokerCustomerID) ?? 0,
                InvoiceDate = brokerInvoice.InvoiceDate,
                InvoiceNumber = brokerInvoice.InvoiceNumber,
            };
        }
        else
        {
            var broker = await lookupCosmosService.GetBrokerAsync(vsData?.CustomerAccountNumber, vsData?.CompanyID);

            // If vehicle sold to broker and the broker is terminated, then make vsdata as start date.
            // If vehicle sold to broker before start date and it is not exists in broker intial vehicles,
            // then make vsdata as start date.
            if (broker is not null)
                if (!broker.TerminationDate.HasValue && (broker.AccountStartDate <= vsData?.InvoiceDate || companyDataAggregate.BrokerInitialVehicles?.Count(x => x?.BrokerID == broker.ID) > 0))
                    result.Broker = new VehicleBrokerSaleInformation
                    {
                        BrokerID = broker.ID,
                        BrokerName = broker.Name
                    };
        }

        return result;
    }

    private async Task<List<ShiftFileDTO>> GetCompanyLogo(List<ShiftFileDTO> companyLogo)
    {
        if (options?.CompanyLogoImageResolver is not null)
            return await options.CompanyLogoImageResolver(new(companyLogo, this.languageCode, this.services));

        return companyLogo;
    }

    private async Task<VehicleWarrantyDTO> GetWarrantyAsync(DateTime? invoiceDate, Brands brand)
    {
        VehicleWarrantyDTO result = new();

        var shiftDate = companyDataAggregate.WarrantyDateShifts?.FirstOrDefault();
        if (shiftDate is not null)
            invoiceDate = shiftDate.NewDate;

        result.WarrantyStartDate = invoiceDate;

        if (brand == Brands.Lexus)
            result.WarrantyEndDate = invoiceDate?.AddYears(4);
        else
            result.WarrantyEndDate = invoiceDate?.AddYears(3);


        // Extended warranty
        //var extendedWarranty = extendedWarranties?.FirstOrDefault(x => x.ResponseType == "3");

        //if (extendedWarranty is not null)
        //{
        //    result.ExtendedWarrantyStartDate = extendedWarranty?.UpdatedDate is null ? null
        //        : DateOnly.FromDateTime(extendedWarranty.UpdatedDate.Value);
        //    result.ExtendedWarrantyEndDate = result.ExtendedWarrantyStartDate?.AddYears(2);
        //}

        return result;
    }

    private async Task<IEnumerable<SscDTO>> GetSSCAsync(
        IEnumerable<SSCAffectedVINModel> ssc,
        IEnumerable<WarrantyClaimModel> warrantyClaims,
        IEnumerable<OrderLaborLineModel> labors,
        string regionIntegrationId)
    {
        if (ssc?.Count() == 0)
            return null;

        var data = new List<SscDTO>();

        data = ssc?.Select(x =>
        {
            var parts = new List<SSCPartDTO>();
            var sscLabors = new List<SSCLaborDTO>();

            var isRepared = x.RepairDate is not null;
            DateTime? repairDate = x.RepairDate;

            var warrantyClaim = warrantyClaims?
                .Where(w => new List<WarrantyClaimStatus> { WarrantyClaimStatus.Accepted, WarrantyClaimStatus.Certified, WarrantyClaimStatus.Invoiced }.Contains(w?.ClaimStatus ?? 0))
                .OrderByDescending(w => w.RepairCompletionDate)
                .FirstOrDefault(w => (
                    w.DistributorComment?.Contains(x.CampaignCode) ?? false) ||
                    (w.LaborLines.Any(y => new[] { x.LaborCode1, x.LaborCode2, x.LaborCode3 }.Contains(y.LaborCode)))
                );

            if (warrantyClaim is not null)
            {
                isRepared = true;
                repairDate = warrantyClaim.RepairCompletionDate;
            }
            else
            {
                var labor = labors?.OrderByDescending(s => s.InvoiceDate)
                    .FirstOrDefault(s =>
                    (s.LaborCode.Equals(x.LaborCode1) || s.LaborCode.Equals(x.LaborCode2) || s.LaborCode.Equals(x.LaborCode3)) &&
                    (s.OrderStatus.Equals("X") || s.LoadStatus.Equals("C"))
                );

                if (labor is not null)
                {
                    isRepared = true;
                    repairDate = labor.InvoiceDate;
                }
            }

            var sscData = new SscDTO
            {
                Description = x.Description,
                SSCCode = x.CampaignCode,
                Repaired = isRepared,
                RepairDate = repairDate
            };

            if (!string.IsNullOrWhiteSpace(x.LaborCode1))
                sscLabors.Add(new SSCLaborDTO
                {
                    LaborCode = x.LaborCode1,
                });

            if (!string.IsNullOrWhiteSpace(x.LaborCode2))
                sscLabors.Add(new SSCLaborDTO
                {
                    LaborCode = x.LaborCode2,
                });

            if (!string.IsNullOrWhiteSpace(x.LaborCode3))
                sscLabors.Add(new SSCLaborDTO
                {
                    LaborCode = x.LaborCode3,
                });

            if (!string.IsNullOrWhiteSpace(x.PartNumber1))
                parts.Add(new SSCPartDTO
                {
                    PartNumber = x.PartNumber1,
                });

            if (!string.IsNullOrWhiteSpace(x.PartNumber2))
                parts.Add(new SSCPartDTO
                {
                    PartNumber = x.PartNumber2,
                });

            if (!string.IsNullOrWhiteSpace(x.PartNumber3))
                parts.Add(new SSCPartDTO
                {
                    PartNumber = x.PartNumber3,
                });

            sscData.Parts = parts;
            sscData.Labors = sscLabors;

            return sscData;
        }).ToList();

        // Get partnumbers and format it to match the stock item
        var partNumbers = data?.SelectMany(x => x.Parts.Select(p => p.PartNumber)).Distinct();

        return data;
    }

    private async Task<IEnumerable<VehicleServiceItemDTO>> GetServiceItems(
        string vin,
        DateTime? freeServiceStartDate,
        VehicleEntryModel vehicle,
        VehicleSaleInformation vehicleSaleInformation,
        IEnumerable<PaidServiceInvoiceModel> paidServices,
        IEnumerable<ItemClaimModel> tlpTransactionLines,
        IEnumerable<VehicleInspectionModel> vehicleInspections
    )
    {
        var result = new List<VehicleServiceItemDTO>();
        IEnumerable<ServiceItemModel> serviceItems = new List<ServiceItemModel>();

        //if (vehicle is not null)
        serviceItems = await lookupCosmosService.GetServiceItemsAsync();

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

        // Free services
        if (!companyDataAggregate.FreeServiceItemExcludedVINs.Any())
        {
            var eligibleServiceItems = serviceItems.Where(x => !(x.IsDeleted));

            // Brand
            eligibleServiceItems = eligibleServiceItems.Where(x => vehicle is null || x.Brands.Any(a => a == vehicle.Brand));

            // Company
            eligibleServiceItems = eligibleServiceItems.Where(x => x.CompanyIDs is null || x.CompanyIDs.Count() == 0 || vehicle is null || x.CompanyIDs.Any(a => a == vehicle?.CompanyID));

            // Country
            eligibleServiceItems = eligibleServiceItems.Where(x => x.CountryIDs is null || x.CountryIDs.Count() == 0 || vehicle is null || x.CountryIDs.Any(a => a == vehicleSaleInformation?.CountryID));

            // Expiry
            eligibleServiceItems = eligibleServiceItems.Where(x =>
                x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.WarrantyActivation ? (freeServiceStartDate >= x.CampaignStartDate && freeServiceStartDate <= x.CampaignEndDate) :
                x.CampaignActivationTrigger == ClaimableItemCampaignActivationTrigger.VehicleInspection ? (vehicleInspections?.Where(i => i.InspectionDate >= x.CampaignStartDate && i.InspectionDate <= x.CampaignEndDate).Count() > 0) :
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
                        ServiceItemID = item.ID,
                        Name = Utility.GetLocalizedText(item.Name, languageCode),
                        Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
                        Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
                        Image = await GetFirstImageFullUrl(item.Photo),
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
                            PaidServiceInvoiceLineID = item.ID,
                            ActivatedAt = paidService.InvoiceDate,
                            CampaignUniqueReference = item.ServiceItem?.CampaignUniqueReference,
                            Description = Utility.GetLocalizedText(item.ServiceItem?.PrintoutDescription, languageCode),
                            Image = await GetFirstImageFullUrl(item.ServiceItem?.Photo),
                            Name = Utility.GetLocalizedText(item.ServiceItem?.Name, languageCode),
                            Title = Utility.GetLocalizedText(item.ServiceItem?.PrintoutTitle, languageCode),
                            ExpiresAt = item.ExpireDate,
                            Type = "paid",
                            MaximumMileage = item.ServiceItem?.MaximumMileage,
                            TypeEnum = VehcileServiceItemTypes.Paid,
                            PackageCode = item.MenuCode,

                            ClaimingMethodEnum = item.ServiceItem.ClaimingMethod,
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

        await CalulateServiceItemStatus(result, showingInactivatedItems);

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
            vehicle?.VariantCode
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

            item.Signature = item.GenerateSignature(vin, this.options.SigningSecreteKey);
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

    private (string statusText, VehcileServiceItemStatuses status, DateTimeOffset? claimDate, string wip, string invoice, string companyID, string packageCode)
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
        string variant)
    {
        var result = new List<VehicleServiceItemDTO>();

        var existingServiceItemIds = eligibleServiceItems?
            .Where(x => x.TypeEnum == VehcileServiceItemTypes.Free)
            .Select(x => x.ServiceItemID.ToString())
            .ToList();

        var claimedItems = companyDataAggregate.ItemClaims?
            .Select(x => x?.ServiceItemID)
            .Where(x => !(existingServiceItemIds?.Any(s => s == x) ?? false));

        foreach (var item in availableServiceItems.Where(x => claimedItems?.Any(a => a == x.ID.ToString()) ?? false))
        {
            //var modelCost = GetModelCost(item.ModelCosts, katashiki, variant);

            var claimLine = companyDataAggregate
                .ItemClaims?
                .FirstOrDefault(t => t.ServiceItemID == item.ID.ToString());

            var serviceItem = new VehicleServiceItemDTO
            {
                ServiceItemID = item.ID,
                Name = Utility.GetLocalizedText(item.Name, languageCode),
                Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
                Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
                Image = await GetFirstImageFullUrl(item.Photo),
                Type = "free",
                TypeEnum = VehcileServiceItemTypes.Free,
                StatusEnum = VehcileServiceItemStatuses.Processed,
                Status = "processed",
                //ModelCostID = modelCost?.ID,
                PackageCode = claimLine.PackageCode, //modelCost?.PackageCode ?? item.PackageCode,
                ClaimDate = claimLine?.ClaimDate,
                InvoiceNumber = claimLine?.InvoiceNumber,
                JobNumber = claimLine?.JobNumber,
                CompanyID = claimLine?.CompanyID,
                MaximumMileage = item.MaximumMileage
            };

            if (!string.IsNullOrWhiteSpace(claimLine?.CompanyID) && options.CompanyNameResolver is not null)
                serviceItem.CompanyName = await options.CompanyNameResolver(new(claimLine?.CompanyID, languageCode, services));

            result.Add(serviceItem);
        }

        return result;
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

    private async Task<string> GetFirstImageFullUrl(Dictionary<string,string> images)
    {
        if (images is null)
            return null;
        else
            return await options.ServiceItemImageUrlResolver(new(images, languageCode, services));
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
            if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.EveryTrigger)
            {
                foreach (var vehicleInspection in vehicleInspections)
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
                    vehicleInspection = vehicleInspections.OrderBy(x => x.InspectionDate).First();

                else if (item.CampaignActivationType == ClaimableItemCampaignActivationTypes.ExtendOnEachTrigger)
                    vehicleInspection = vehicleInspections.OrderByDescending(x => x.InspectionDate).First();

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

    private async Task CalulateServiceItemStatus(IEnumerable<VehicleServiceItemDTO> serviceItems, bool showingInactivatedItems)
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
            item.CompanyID = statusResult.companyID;
            item.PackageCode = statusResult.packageCode ?? item.PackageCode;

            if(!string.IsNullOrWhiteSpace(statusResult.companyID) && options.CompanyNameResolver is not null)
                item.CompanyName = await options.CompanyNameResolver(new(statusResult.companyID, languageCode, services));
        }
    }

    public async Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync()
    {
        return await lookupCosmosService.GetAllVehicleModelsAsync();
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki)
    {
        return await lookupCosmosService.GetVehicleModelsByKatashikiAsync(katashiki);
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant)
    {
        return await lookupCosmosService.GetVehicleModelsByVariantAsync(variant);
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin)
    {
        return await lookupCosmosService.GetVehicleModelsByVinAsync(vin);
    }
}