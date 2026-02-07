using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class VehicleLookupService
{
    private readonly IVehicleLoockupStorageService vehicleLoockupStorageService;
    private readonly LookupOptions lookupOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogCosmosService logCosmosService;

    public VehicleLookupService(
        IVehicleLoockupStorageService vehicleLoockupStorageService,
        IServiceProvider services = null,
        ILogCosmosService logCosmosService = null,
        LookupOptions options = null
    )
    {
        this.vehicleLoockupStorageService = vehicleLoockupStorageService;
        this.lookupOptions = options;
        this.serviceProvider = services;
        this.logCosmosService = logCosmosService;
    }

    public async Task<CompanyDataAggregateModel> GetAggregatedCompanyDataAsync(string vin)
    {
        return await vehicleLoockupStorageService.GetAggregatedCompanyData(vin);
    }

    public async Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyDataAsync(IEnumerable<string> vins, IEnumerable<string> itemTypes)
    {
        return await vehicleLoockupStorageService.GetAggregatedCompanyData(vins, itemTypes);
    }

    public async Task<VehicleLookupDTO> LookupAsync(string vin)
    {
        return await this.LookupAsync(vin, new VehicleLookupRequestOptions());
    }

    public async Task<VehicleLookupDTO> LookupAsync(string vin, VehicleLookupRequestOptions requestOptions)
    {
        if (requestOptions is null)
            requestOptions = new VehicleLookupRequestOptions();

        // Get all items related to the VIN from the cosmos container
        var companyDataAggregate = await vehicleLoockupStorageService.GetAggregatedCompanyData(vin);

        VehicleEntryModel vehicle = new VehicleEntryEvaluator(companyDataAggregate).Evaluate();

        var data = new VehicleLookupDTO()
        {
            VIN = vin,
            IsAuthorized = new VehicleAuthorizationEvaluator(companyDataAggregate).Evaluate(),
            PaintThicknessInspections = await new VehiclePaintThicknessEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(requestOptions.LanguageCode),
            Identifiers = new VehicleIdentifierEvaluator(companyDataAggregate).Evaluate(vehicle),
            VehicleSpecification = await new VehicleSpecificationEvaluator(vehicleLoockupStorageService).Evaluate(vehicle),
            ServiceHistory = await new VehicleServiceHistoryEvaluator(companyDataAggregate, lookupOptions, this.serviceProvider).Evaluate(requestOptions.LanguageCode, requestOptions.VehicleServiceHistoryConsistencyLevel),
            SSC = new VehicleSSCEvaluator(companyDataAggregate).Evaluate(),
            NextServiceDate = companyDataAggregate.LaborLines?.Max(x => x.NextServiceDate),
            Accessories = await new VehicleAccessoriesEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(requestOptions.LanguageCode),
            SaleInformation = await new VehicleSaleInformationEvaluator(companyDataAggregate, lookupOptions, serviceProvider, vehicleLoockupStorageService).Evaluate(requestOptions),
        };

        if (requestOptions.LegacyPaintThickness)
        {
            var paintThickness = data.PaintThicknessInspections
                ?.Where(x => x.Source == "PDI")
                ?.FirstOrDefault();

            if (paintThickness is not null)
            {
                data.PaintThickness = new LegacyPaintThicknessDTO
                {
                    Parts = paintThickness.Panels.GroupBy(x => new { x.PanelType, x.PanelPosition }).Select(x => new LegacyPaintThicknessPartDTO
                    {
                        Part = $"{x.Key.PanelType.Describe()} ({x.Key.PanelPosition.Describe()})",
                        Left = x.FirstOrDefault(x => x.PanelSide is null || x.PanelSide == Models.Enums.VehiclePanelSide.Left)?.MeasuredThickness.ToString(),
                        Right = x.FirstOrDefault(x => x.PanelSide == Models.Enums.VehiclePanelSide.Right)?.MeasuredThickness.ToString(),
                    }),
                    ImageGroups = paintThickness.Panels.Where(x => x.Images.Count() > 0).Select(x => new LegacyPaintThicknessImageGroupDTO
                    {
                        Name = $"{x.PanelType.Describe()} ({x.PanelPosition.Describe()} {x.PanelSide.Describe()})",
                        Images = x.Images.ToList()
                    })
                };
            }
        }

        data.Warranty = new WarrantyAndFreeServiceDateEvaluator(companyDataAggregate, lookupOptions)
            .Evaluate(vehicle, data.SaleInformation, requestOptions.IgnoreBrokerStock);

        var serviceItemsResult = await new VehicleServiceItemEvaluator(
            this.vehicleLoockupStorageService, companyDataAggregate, this.lookupOptions, this.serviceProvider
        ).Evaluate(
            vehicle,
            data.Warranty?.FreeServiceStartDate,
            data.SaleInformation,
            requestOptions.LanguageCode
        );

        data.ServiceItems = serviceItemsResult.serviceItems;

        if (data.Warranty is not null && data.Warranty.WarrantyStartDate is not null)
            data.Warranty.ActivationIsRequired = serviceItemsResult.activationRequired;

        if (requestOptions.InsertSSCLog)
        {
            //_ = Task.Run(async () =>
            //{
            //    try
            //    {
            data.SSCLogId = await logCosmosService?.LogSSCLookupAsync(
                requestOptions.SSCLogInfo,
                data.SSC,
                vin,
                data.IsAuthorized,
                data.Warranty?.HasActiveWarranty ?? false,
                data.Identifiers?.BrandID?.ToLong()
            );
            //    }
            //    catch
            //    {

            //    }
            //});
        }

        if (requestOptions.InsertCustomerVehcileLookupLog)
        {
            //_ = Task.Run(async () =>
            //{
            try
            {
                await logCosmosService.LogCustomerVehicleLookupAsync(
                    requestOptions.CustomerVehicleLookupLogInfo,
                    vin,
                    data.IsAuthorized,
                    data.Warranty?.HasActiveWarranty ?? false,
                    data.Identifiers?.BrandID?.ToLong()
                );
            }
            catch
            {

            }
            //});
        }

        return data;
    }

    public async Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync()
    {
        return await vehicleLoockupStorageService.GetAllVehicleModelsAsync();
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki)
    {
        return await vehicleLoockupStorageService.GetVehicleModelsByKatashikiAsync(katashiki);
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant)
    {
        return await vehicleLoockupStorageService.GetVehicleModelsByVariantAsync(variant);
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin)
    {
        return await vehicleLoockupStorageService.GetVehicleModelsByVinAsync(vin);
    }
}