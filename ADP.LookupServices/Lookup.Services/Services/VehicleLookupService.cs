using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Extensions;
using ShiftSoftware.ADP.Models.Aggregate;
using ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;
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

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
    public class VehicleLookupService
    {
        public readonly IVehicleLoockupCosmosService lookupCosmosService;
        readonly LookupOptions options;
        readonly IIdentityCosmosService identityCosmosService;
        private readonly IServiceProvider services;
        private readonly ILogCosmosService? logCosmosService;
        DealerDataAggregateCosmosModel dealerDataAggregate = new DealerDataAggregateCosmosModel();


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
            dealerDataAggregate = await lookupCosmosService.GetAggregatedDealerData(vin);

            // Set paint thickness
            data.PaintThickness = await GetPaintThickness();

            this.languageCode = languageCode;

            // Get the latest VSData
            VehicleEntryModel vsData = null;

            if (dealerDataAggregate.VSData?.Count() > 0)
                if (dealerDataAggregate.VSData.Any(x => x.InvoiceDate is null))
                    vsData = dealerDataAggregate.VSData.FirstOrDefault(x => x.InvoiceDate is null);
                else
                    vsData = dealerDataAggregate.VSData.OrderByDescending(x => x.InvoiceDate).FirstOrDefault();

            // Set identifiers
            data.VIN = vin;
            data.Identifiers = new VehicleIdentifiersDTO { VIN = vin };

            if (vsData is not null)
            {
                // Set identifiers
                data.Identifiers = GetIdentifiers(vsData, vin);

                // Set Specification
                data.VehicleSpecification = await GetSpecificationAsync(vsData);
            }

            // Set IsAuthorized
            data.IsAuthorized = dealerDataAggregate.TIQOfficialVIN?.Count() > 0 || dealerDataAggregate.VSData?.Count() > 0 || dealerDataAggregate.TiqSSCAffectedVin.Count() > 0;

            // Set NextServiceDate
            data.NextServiceDate = dealerDataAggregate.CPU?.OrderByDescending(x => x.InvoiceDate).FirstOrDefault()?.NextServiceDate;

            // Set ServiceHistories
            data.ServiceHistory = await GetServiceHistory(dealerDataAggregate.CPU, dealerDataAggregate.SOLabor, dealerDataAggregate.SOPart);

            // Set SSC
            data.SSC = await GetSSCAsync(dealerDataAggregate.TiqSSCAffectedVin, dealerDataAggregate.ToyotaWarrantyClaim, dealerDataAggregate.SOLabor, regionIntegrationId);

            DateTime? warrantyStartDate = null;
            DateTime? freeServiceStartDate = null;

            if (vsData is not null)
            {
                // Set SaleInformation
                data.SaleInformation = await GetSaleInformationAsync();

                //Normal Dealer Sale
                if (data.SaleInformation?.Broker is null)
                {
                    warrantyStartDate = data.SaleInformation?.InvoiceDate;
                    freeServiceStartDate = data.SaleInformation?.InvoiceDate;
                }
                else
                {
                    //Broker Stock
                    if (data.SaleInformation.Broker.InvoiceDate is null)
                    {
                        if (ignoreBrokerStock)
                        {
                            warrantyStartDate = null;
                            freeServiceStartDate = data.SaleInformation?.InvoiceDate;
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
                data.Warranty = await GetWarrantyAsync(warrantyStartDate, vsData.Brand);
            }

            data.ServiceItems = await GetServiceItems(freeServiceStartDate, vsData, dealerDataAggregate.TLPPackageInvoice, dealerDataAggregate.ToyotaLoyaltyProgramTransactionLine);

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

            foreach (var accessory in dealerDataAggregate.Accessories)
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
            if (dealerDataAggregate.PaintThicknessVehicle is null)
                return null;

            var groups = dealerDataAggregate.PaintThicknessVehicle.Images?.GroupBy(x => x.ToLower().Replace("left", string.Empty).Replace("right", string.Empty));
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
                Parts = dealerDataAggregate.PaintThicknessVehicle.Parts?.Select(x => new PaintThicknessPartDTO
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
            IEnumerable<InvoiceLaborLineModel> labors,
            IEnumerable<InvoicePartLineModel> parts)
        {
            var partData = (await lookupCosmosService.GetStockItemsAsync(parts?.Select(x => x.PartNumber)))?
                .DistinctBy(x => x.PartNumber)?.ToDictionary(x => x.PartNumber);

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
                        CompanyIntegrationID = x.CompanyIntegrationID,
                        BranchIntegrationID = x.BranchIntegrationID,
                        Account = x.AccountNumber,
                        InvoiceNumber = x.InvoiceNumber,
                        WipNumber = x.JobNumber,
                        LaborLines = labors?.Where(l => l.JobNumber == x.JobNumber && l.InvoiceNumber == x.InvoiceNumber &&
                            l.CompanyIntegrationID == x.CompanyIntegrationID)
                                .Select(l => new VehicleLaborDTO
                                {
                                    Description = l.ServiceDescription,
                                    MenuCode = l.MenuCode,
                                    RTSCode = l.LaborCode,
                                    ServiceCode = l.ServiceCode
                                }),
                        PartLines = parts?.Where(p => p.JobNumber == x.JobNumber && p.InvoiceNumber == x.InvoiceNumber &&
                            p.CompanyIntegrationID == x.CompanyIntegrationID)
                                .Select(p => new VehiclePartDTO
                                {
                                    MenuCode = p.MenuCode,
                                    PartNumber = p.PartNumber,
                                    QTY = p.Quantity,
                                    PartDescription = partData?.FirstOrDefault(pd => pd.Key == p.PartNumber).Value?.PartDescription
                                })
                    };

                    if (options.CompanyNameResolver is not null)
                        result.CompanyName = await options.CompanyNameResolver(new(x.CompanyIntegrationID, languageCode, services));

                    if (options.CompanyBranchNameResolver is not null)
                        result.BranchName = await options.CompanyBranchNameResolver(
                            new(new(x.CompanyIntegrationID, x.BranchIntegrationID, DepartmentType.Service), languageCode, services));

                    serviceHistory.Add(result);
                }
            }

            return serviceHistory;
        }

        private async Task<VehicleSpecificationDTO> GetSpecificationAsync(VehicleEntryModel vsData)
        {
            VehicleSpecificationDTO result = new();

            var vtModel = vsData?.VehicleModel;

            if (vtModel is null)
            {
                vtModel = await lookupCosmosService.GetVTModelAsync(vsData?.VariantCode, vsData?.Brand);

                if (vtModel is not null)
                    lookupCosmosService.UpdateVSDataModel(vsData, vtModel);
            }

            if (vtModel is not null)
            {
                result = new VehicleSpecificationDTO
                {
                    BodyType = vtModel.BodyType,
                    Class = vtModel.Class,
                    Cylinders = vtModel.Cylinders,
                    Doors = vtModel.Doors,
                    Engine = vtModel.Engine,
                    EngineType = vtModel.EngineType,
                    Fuel = vtModel.Fuel,
                    LightHeavy = vtModel.LightHeavyType,
                    ModelDesc = vtModel.ModelDescription,
                    Side = vtModel.Side,
                    Style = vtModel.Style,
                    TankCap = vtModel.TankCap,
                    Transmission = vtModel.Transmission,
                    VariantDesc = vtModel.VariantDescription,
                    Color = vsData?.ExteriorColor?.Description,
                    Trim = vsData?.InteriorColor?.Description
                };
            }

            if (vsData?.ExteriorColorCode is null)
            {
                var color = await lookupCosmosService.GetVTColorAsync(vsData?.ExteriorColorCode, vsData?.Brand);
                if (color is not null)
                {
                    result.Color = color?.Description;
                    lookupCosmosService.UpdateVSDataColor(vsData, color);
                }
            }

            if (vsData?.InteriorColorCode is null)
            {
                var trim = await lookupCosmosService.GetVTTrimAsync(vsData?.InteriorColorCode, vsData?.Brand);
                if (trim is not null)
                {
                    result.Trim = trim?.Description;
                    lookupCosmosService.UpdateVSDataTrim(vsData, trim);
                }
            }

            await lookupCosmosService.SaveChangesAsync();

            return result;
        }

        private VehicleIdentifiersDTO GetIdentifiers(VehicleEntryModel vsData, string vin)
        {
            return new VehicleIdentifiersDTO
            {
                VIN = vin,
                Variant = vsData.VariantCode,
                Katashiki = vsData.Katashiki,
                Color = vsData.ExteriorColorCode,
                Trim = vsData.InteriorColorCode,
                Brand = vsData.Brand,
                BrandIntegrationID = vsData.BrandIntegrationID
            };
        }

        private async Task<VehicleSaleInformation> GetSaleInformationAsync()
        {
            VehicleSaleInformation result = new();
            var i = dealerDataAggregate.VSData.ToList();
            if (!(dealerDataAggregate.VSData?.Any() ?? false))
                return null;

            var vsData = dealerDataAggregate.VSData
                .OrderByDescending(x => x.InvoiceDate == null)
                .ThenByDescending(x => x.InvoiceDate)
                .FirstOrDefault();

            result.InvoiceDate = vsData?.InvoiceDate;
            result.ProgressCode = vsData.LineStatus;
            result.LocationCode = vsData.LocationCode;
            result.ACSStatus = vsData.Status;
            result.SaleType = vsData.SaleType;
            result.InvoiceAccount = vsData.AccountNumber;
            result.RegionIntegrationId = vsData.RegionIntegrationId;

            result.InvoiceNumber = vsData?.InvoiceNumber ?? 0;
            result.InvoiceTotal = vsData?.InvoiceTotal ?? 0;
            result.DealerIntegrationID = vsData?.CompanyIntegrationID;
            result.BranchIntegrationID= vsData?.BranchIntegrationID;

            result.CustomerID = vsData?.CustomerID;
            result.CustomerAccount = vsData?.CustomerAccountNumber;

            if (options.CompanyNameResolver is not null)
                result.DealerName = await options.CompanyNameResolver(new(vsData.CompanyIntegrationID, languageCode, services));

            if (options.CompanyBranchNameResolver is not null)
                result.BranchName = await options.CompanyBranchNameResolver(
                    new(new(vsData.CompanyIntegrationID, vsData.BranchIntegrationID, DepartmentType.Sales), languageCode, services));

            string? dealerLogo = null;

            if(options.CompanyLogoResolver is not null)
                dealerLogo = await options.CompanyLogoResolver(new(vsData.CompanyIntegrationID, languageCode, services));   

            if(!string.IsNullOrWhiteSpace(dealerLogo))
                try
                {
                    result.DealerLogo = await GetDealerLogo(JsonSerializer.Deserialize<List<ShiftFileDTO>>(dealerLogo));
                }
                catch (Exception){}

            if (dealerDataAggregate.BrokerInvoice?.Any() ?? false)
            {
                var brokerInvoice = dealerDataAggregate.BrokerInvoice.FirstOrDefault();
                var broker = await lookupCosmosService.GetBrokerAsync(brokerInvoice.Id);

                result.Broker = new VehicleBrokerSaleInformation
                {
                    BrokerId = brokerInvoice.Id,
                    BrokerName = broker?.Name,
                    CustomerID = (brokerInvoice.BrokerCustomerID ?? brokerInvoice.NonOfficialBrokerCustomerID) ?? 0,
                    InvoiceDate = brokerInvoice.InvoiceDate,
                    InvoiceNumber = brokerInvoice.InvoiceNumber,
                };
            }
            else
            {
                var broker = await lookupCosmosService.GetBrokerAsync(vsData?.CustomerAccountNumber, vsData?.CompanyIntegrationID);

                // If vehicle sold to broker and the broker is terminated, then make vsdata as start date.
                // If vehicle sold to broker before start date and it is not exists in broker intial vehicles,
                // then make vsdata as start date.
                if (broker is not null)
                    if (!broker.TerminationDate.HasValue && (broker.AccountStartDate <= vsData?.InvoiceDate || dealerDataAggregate.BrokerInitialVehicle?.Count(x => x?.BrokerID == broker.Id) > 0))
                        result.Broker = new VehicleBrokerSaleInformation
                        {
                            BrokerId = broker.Id,
                            BrokerName = broker.Name
                        };
            }

            return result;
        }

        private async Task<List<ShiftFileDTO>> GetDealerLogo(List<ShiftFileDTO> dealerLogo)
        {
            if (options?.DealerLogImageResolver is not null)
                return await options.DealerLogImageResolver(new(dealerLogo, this.languageCode, this.services));

            return dealerLogo;
        }

        private async Task<VehicleWarrantyDTO> GetWarrantyAsync(DateTime? invoiceDate, Brands brand)
        {
            VehicleWarrantyDTO result = new();

            var shiftDate = dealerDataAggregate.WarrantyShiftDate?.FirstOrDefault();
            if (shiftDate is not null)
                invoiceDate = shiftDate.NewInvoiceDate;

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

        private async Task<IEnumerable<SSCDTO>> GetSSCAsync(
            IEnumerable<SSCAffectedVINModel> ssc,
            IEnumerable<WarrantyClaimModel> warrantyClaims,
            IEnumerable<InvoiceLaborLineModel> labors,
            string regionIntegrationId)
        {
            if (ssc?.Count() == 0)
                return null;

            var data = new List<SSCDTO>();

            data = ssc?.Select(x =>
            {
                var parts = new List<SSCPartDTO>();
                var sscLabors = new List<SSCLaborDTO>();

                var isRepared = false;
                DateTime? repairDate = null;

                var warrantyClaim = warrantyClaims?
                    .Where(w => new List<int> { 1, 2, 5, 6 }.Contains(w?.ClaimStatus ?? 0))
                    .OrderByDescending(w => w.RepairDate)
                    .FirstOrDefault(w => (w.DistComment1?.Contains(x.CampaignCode) ?? false) || w.LaborOperationNoMain == x.OpCode);

                if (warrantyClaim is not null)
                {
                    isRepared = true;
                    repairDate = warrantyClaim.RepairDate;
                }
                else
                {
                    var labor = labors?.OrderByDescending(s => s.InvoiceDate)
                        .FirstOrDefault(s => s.LaborCode.Equals(x.OpCode) && (s.Status.Equals("X") || s.LineStatus.Equals("C")));

                    if (labor is not null)
                    {
                        isRepared = true;
                        repairDate = labor.InvoiceDate;
                    }
                }

                var sscData = new SSCDTO
                {
                    Description = x.Description,
                    SSCCode = x.CampaignCode,
                    Repaired = isRepared,
                    RepairDate = repairDate
                };

                if (!string.IsNullOrWhiteSpace(x.OpCode))
                    sscLabors.Add(new SSCLaborDTO
                    {
                        LaborCode = x.OpCode,
                    });

                if (!string.IsNullOrWhiteSpace(x.OpCode2))
                    sscLabors.Add(new SSCLaborDTO
                    {
                        LaborCode = x.OpCode2,
                    });

                if (!string.IsNullOrWhiteSpace(x.OpCode3))
                    sscLabors.Add(new SSCLaborDTO
                    {
                        LaborCode = x.OpCode3,
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

                return sscData;
            }).ToList();

            // Get partnumbers and format it to match the stock item
            var partNumbers = data?.SelectMany(x => x.Parts.Select(p => p.PartNumber)).Distinct();

            var partData = await lookupCosmosService.GetStockItemsAsync(partNumbers);

            foreach (var sscData in data)
            {
                foreach (var part in sscData.Parts)
                {
                    var partItems = partData?.Where(x => x.PartNumber == part.PartNumber).ToList();
                    if (partItems?.Count > 0)
                    {
                        part.PartDescription = partItems.FirstOrDefault()?.PartDescription;
                        part.IsAvailable = partItems.FirstOrDefault(x => x.Location == regionIntegrationId)?.Quantity > 0;
                    }
                }
            }

            return data;
        }

        private async Task<IEnumerable<VehicleServiceItemDTO>> GetServiceItems(
            DateTime? invoiceDate,
            VehicleEntryModel vsData,
            IEnumerable<PaidServiceInvoiceModel> paidServices,
            IEnumerable<ServiceItemClaimLineModel> tlpTransactionLines
        )
        {
            var result = new List<VehicleServiceItemDTO>();
            IEnumerable<ServiceItemModel> redeeambleItems = new List<ServiceItemModel>();

            if (vsData is not null)
                redeeambleItems = await lookupCosmosService.GetRedeemableItemsAsync(vsData.Brand);

            var shiftDay = dealerDataAggregate.VehicleFreeServiceInvoiceDateShiftVIN?.FirstOrDefault(x => x.VIN == vsData.VIN);
            if (shiftDay is not null)
                invoiceDate = shiftDay.NewInvoiceDate;

            // Free services
            if (!dealerDataAggregate.VehicleFreeServiceExcludedVIN.Any())
            {
                var eligableRedeemableItems = redeeambleItems?
                    .Where(x => !(x.IsDeleted))
                    .Where(x => invoiceDate >= x.PublishDate && invoiceDate <= x.ExpireDate)
                    .Where(x => (x.ModelCosts?.Count() ?? 0) == 0
                        ||
                        (x.ModelCosts?.Any(a => vsData.Katashiki.StartsWith(a?.Katashiki ?? "") && !string.IsNullOrWhiteSpace(a?.Katashiki)
                            || vsData.VariantCode.StartsWith(a?.Variant ?? "") && !string.IsNullOrWhiteSpace(a?.Variant)) ?? false));

                if (eligableRedeemableItems?.Count() > 0)
                {
                    // Order them by mileage
                    eligableRedeemableItems = eligableRedeemableItems
                        .OrderByDescending(x => x.MaximumMileage.HasValue)
                        .ThenBy(x => x.MaximumMileage);

                    var startDate = invoiceDate;

                    foreach (var item in eligableRedeemableItems)
                    {
                        var modelCost = GetModelCost(item.ModelCosts, vsData.Katashiki, vsData.VariantCode);

                        var serviceItem = new VehicleServiceItemDTO
                        {
                            ToyotaLoyaltyProgramRedeemableItemID = item.Id,
                            Name = Utility.GetLocalizedText(item.Name, languageCode),
                            Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
                            Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
                            Image = await GetFirstImageFullUrl(item.Photo),
                            Type = "free",
                            TypeEnum = VehcileServiceItemTypes.Free,
                            ModelCostID = modelCost?.Id,
                            MenuCode = modelCost?.MenuCode ?? item.MenuCode,
                            SkipZeroTrust = item.SkipZeroTrust,
                            MaximumMileage = item.MaximumMileage,
                            ActiveFor = item.ActiveFor,
                            ActiveForInterval = item.ActiveForInterval
                        };

                        result.Add(serviceItem);
                    }
                }
            }

            if (paidServices?.Count() > 0)
            {
                foreach (var paidService in paidServices)
                {
                    if (paidService?.Items?.Count() > 0)
                    {
                        foreach (var item in paidService.Items)
                        {
                            var itemResult = new VehicleServiceItemDTO
                            {
                                ToyotaLoyaltyProgramRedeemableItemID = item.ServiceItemID,
                                TLPPackageInvoiceTLPItemID = item.Id,
                                ActivatedAt = paidService.ActivationDate,
                                CampaignCode = null,
                                Description = Utility.GetLocalizedText(item.ServiceItem?.PrintoutDescription, languageCode),
                                Image = await GetFirstImageFullUrl(item.ServiceItem?.Photo),
                                Name = Utility.GetLocalizedText(item.ServiceItem?.Name, languageCode),
                                Title = Utility.GetLocalizedText(item.ServiceItem?.PrintoutTitle, languageCode),
                                ExpiresAt = item.ExpireDate,
                                Type = "paid",
                                MaximumMileage = item.ServiceItem?.MaximumMileage,
                                TypeEnum = VehcileServiceItemTypes.Paid,
                                MenuCode = item.MenuCode,
                            };

                            result.Add(itemResult);
                        }
                    }
                }
            }

            CalculateRollingExpireDateForFreeServiceItems(result, invoiceDate);
            await CalulateServiceItemStatus(result);

            result.AddRange(await GetRedeemedItemsThatNotEligable(
                result,
                redeeambleItems,
                vsData?.Katashiki,
                vsData?.VariantCode));

            ProccessDynamicCanceledFreeServiceItems(result);

            // Order items by mileage
            result = result
                .OrderBy(x => x.TypeEnum)
                .ThenByDescending(x => x.MaximumMileage.HasValue)
                .ThenBy(x => x.MaximumMileage)
                .ThenBy(x => x.StatusEnum)
                .ToList();

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
                    }
                }
            }
        }

        private DateTime AddInterval(DateTime date, int? intervalValue, string interval)
        {
            if (string.Equals(interval, "days", StringComparison.OrdinalIgnoreCase))
                return date.AddDays(intervalValue.GetValueOrDefault());
            else if (string.Equals(interval, "months", StringComparison.OrdinalIgnoreCase))
                return date.AddMonths(intervalValue.GetValueOrDefault());
            else if (string.Equals(interval, "years", StringComparison.OrdinalIgnoreCase))
                return date.AddYears(intervalValue.GetValueOrDefault());
            else
                return date;
        }

        private (string statusText, VehcileServiceItemStatuses status, DateTime? redeemDate, string wip, string invoice, string dealerIntegrationId)
            ProcessServiceItemStatus(
            long id,
            DateTime activatedAt,
            DateTime? expiresAt,
            IEnumerable<ServiceItemClaimLineModel> tlpTransactionLines,
            int redeemType)
        {
            var transactionLine = tlpTransactionLines?.FirstOrDefault(x => x?.ServiceItemID == id.ToString() && x.RedeemType == redeemType);

            if (transactionLine is not null)
                return ("processed", VehcileServiceItemStatuses.Processed,
                    transactionLine.ClaimDate.HasValue ? transactionLine.ClaimDate.Value : null,
                    transactionLine.ToyotaLoyaltyProgramTransaction?.JobNumber,
                    transactionLine.ToyotaLoyaltyProgramTransaction?.InvoiceNumber, transactionLine.CompanyIntegrationID);
            else if (expiresAt is not null && expiresAt < DateTime.Now)
                return ("expired", VehcileServiceItemStatuses.Expired, null, null, null, null);
            else
                return ("pending", VehcileServiceItemStatuses.Pending, null, null, null, null);
        }

        private async Task<IEnumerable<VehicleServiceItemDTO>> GetRedeemedItemsThatNotEligable(
            IEnumerable<VehicleServiceItemDTO> serviceItems,
            IEnumerable<ServiceItemModel> redeemableItems,
            string katashiki,
            string variant)
        {
            var result = new List<VehicleServiceItemDTO>();
            serviceItems = serviceItems?.Where(x => x.TypeEnum == VehcileServiceItemTypes.Free);

            var redeemedItems = dealerDataAggregate.ToyotaLoyaltyProgramTransactionLine?.Where(x => x?.RedeemType == 4)
                .Select(x => x?.ServiceItemID)
                .Where(x => !(serviceItems?.Any(s => s.ToyotaLoyaltyProgramRedeemableItemID.ToString() == x) ?? false));

            if (redeemableItems != null)
            {
                foreach (var item in redeemableItems.Where(x => redeemedItems?.Any(a => a == x.Id.ToString()) ?? false))
                {
                    var modelCost = GetModelCost(item.ModelCosts, katashiki, variant);

                    var transactionLine = dealerDataAggregate.ToyotaLoyaltyProgramTransactionLine?
                            .FirstOrDefault(t => t.ServiceItemID == item.Id.ToString() && t.RedeemType == 4);

                    var serviceItem = new VehicleServiceItemDTO
                    {
                        ToyotaLoyaltyProgramRedeemableItemID = item.Id,
                        Name = Utility.GetLocalizedText(item.Name, languageCode),
                        Description = Utility.GetLocalizedText(item.PrintoutDescription, languageCode),
                        Title = Utility.GetLocalizedText(item.PrintoutTitle, languageCode),
                        Image = await GetFirstImageFullUrl(item.Photo),
                        Type = "free",
                        TypeEnum = VehcileServiceItemTypes.Free,
                        StatusEnum = VehcileServiceItemStatuses.Processed,
                        Status = "processed",
                        ModelCostID = modelCost?.Id,
                        MenuCode = modelCost?.MenuCode ?? item.MenuCode,
                        RedeemDate = transactionLine?.ClaimDate,
                        InvoiceNumber = transactionLine?.ToyotaLoyaltyProgramTransaction?.InvoiceNumber,
                        WIP = transactionLine?.ToyotaLoyaltyProgramTransaction?.JobNumber,
                        SkipZeroTrust = item.SkipZeroTrust,
                        DealerIntegrationID = transactionLine?.CompanyIntegrationID,
                        MaximumMileage = item.MaximumMileage
                    };

                    if (!string.IsNullOrWhiteSpace(transactionLine?.CompanyIntegrationID) && options.CompanyNameResolver is not null)
                        serviceItem.DealerName = await options.CompanyNameResolver(new(transactionLine?.CompanyIntegrationID, languageCode, services));

                    result.Add(serviceItem);
                }
            }

            return result;
        }

        private ServiceItemModelCostModel GetModelCost(
            IEnumerable<ServiceItemModelCostModel> modelCosts,
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
                return await options.RedeemableItemImageUrlResolver(new(images, languageCode, services));
        }

        private void CalculateRollingExpireDateForFreeServiceItems(IEnumerable<VehicleServiceItemDTO> serviceItems, DateTime? invoiceDate)
        {
            if (invoiceDate is null)
                return;

            var startDate = invoiceDate;

            var sequencialServiceItems = serviceItems.Where(x => x.TypeEnum == VehcileServiceItemTypes.Free && x.MaximumMileage is not null);

            foreach (var item in sequencialServiceItems)
            {
                item.ActivatedAt = startDate.GetValueOrDefault();
                item.ExpiresAt = AddInterval(startDate.GetValueOrDefault(), item.ActiveFor, item.ActiveForInterval);

                startDate = item.ExpiresAt;
            }

            var nonSequencialServiceItems = serviceItems.Where(x => x.TypeEnum == VehcileServiceItemTypes.Free && x.MaximumMileage is null);

            foreach (var item in nonSequencialServiceItems)
            {
                item.ActivatedAt = invoiceDate.Value;
                item.ExpiresAt = startDate;
            }
        }

        private async Task CalulateServiceItemStatus(IEnumerable<VehicleServiceItemDTO> serviceItems)
        {
            foreach (var item in serviceItems)
            {
                var statusResult = ProcessServiceItemStatus(item.ToyotaLoyaltyProgramRedeemableItemID,
                    item.ActivatedAt,
                    item.ExpiresAt,
                    dealerDataAggregate.ToyotaLoyaltyProgramTransactionLine, item.TypeEnum == VehcileServiceItemTypes.Free ? 4 : 7);

                item.Status = statusResult.statusText;
                item.StatusEnum = statusResult.status;
                item.RedeemDate = statusResult.redeemDate;
                item.WIP = statusResult.wip;
                item.InvoiceNumber = statusResult.invoice;
                item.DealerIntegrationID = statusResult.dealerIntegrationId;

                if(!string.IsNullOrWhiteSpace(statusResult.dealerIntegrationId) && options.CompanyNameResolver is not null)
                    item.DealerName = await options.CompanyNameResolver(new(statusResult.dealerIntegrationId, languageCode, services));
            }
        }

        public async Task<IEnumerable<VehicleModelModel>> GetAllVTModelsAsync()
        {
            return await lookupCosmosService.GetAllVTModelsAsync();
        }

        public async Task<IEnumerable<VehicleModelModel>> GetVTModelsByKatashikiAsync(string katashiki)
        {
            return await lookupCosmosService.GetVTModelsByKatashikiAsync(katashiki);
        }

        public async Task<IEnumerable<VehicleModelModel>> GetVTModelsByVariantAsync(string variant)
        {
            return await lookupCosmosService.GetVTModelsByVariantAsync(variant);
        }

        public async Task<IEnumerable<VehicleModelModel>> GetVTModelsByVinAsync(string vin)
        {
            return await lookupCosmosService.GetVTModelsByVinAsync(vin);
        }
    }
}