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
    private readonly IVehicleLoockupCosmosService lookupCosmosService;
    private readonly LookupOptions lookupOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogCosmosService logCosmosService;

    public VehicleLookupService(
        IVehicleLoockupCosmosService lookupService,
        IServiceProvider services = null,
        ILogCosmosService logCosmosService = null,
        LookupOptions options = null
    )
    {
        this.lookupCosmosService = lookupService;
        this.lookupOptions = options;
        this.serviceProvider = services;
        this.logCosmosService = logCosmosService;
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
        var companyDataAggregate = await lookupCosmosService.GetAggregatedCompanyData(vin);

        VehicleEntryModel vehicle = new VehicleEntryEvaluator(companyDataAggregate).Evaluate();

        var data = new VehicleLookupDTO()
        {
            VIN = vin,
            IsAuthorized = new VehicleAuthorizationEvaluator(companyDataAggregate).Evaluate(),
            PaintThicknessInspections = await new VehiclePaintThicknessEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(requestOptions.LanguageCode),
            Identifiers = new VehicleIdentifierEvaluator(companyDataAggregate).Evaluate(vehicle),
            VehicleSpecification = await new VehicleSpecificationEvaluator(lookupCosmosService).Evaluate(vehicle),
            ServiceHistory = await new VehicleServiceHistoryEvaluator(companyDataAggregate, lookupOptions, this.serviceProvider).Evaluate(requestOptions.LanguageCode),
            SSC = new VehicleSSCEvaluator(companyDataAggregate).Evaluate(),
            NextServiceDate = companyDataAggregate.Invoices?.OrderByDescending(x => x.InvoiceDate).FirstOrDefault()?.NextServiceDate,
            Accessories = await new VehicleAccessoriesEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(requestOptions.LanguageCode),
            SaleInformation = await new VehicleSaleInformationEvaluator(companyDataAggregate, lookupOptions, serviceProvider, lookupCosmosService).Evaluate(requestOptions.LanguageCode),
        };

        data.Warranty = new WarrantyAndFreeServiceDateEvaluator(companyDataAggregate, lookupOptions)
            .Evaluate(vehicle, data.SaleInformation, requestOptions.IgnoreBrokerStock);

        var serviceItemsResult = await new VehicleServiceItemEvaluator(
            this.lookupCosmosService, companyDataAggregate, this.lookupOptions, this.serviceProvider
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
                data.Identifiers?.Brand
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
                    data.Identifiers?.Brand
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