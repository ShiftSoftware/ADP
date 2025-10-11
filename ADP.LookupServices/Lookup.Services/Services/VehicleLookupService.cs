using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
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
        LookupOptions options = null)
    {
        this.lookupCosmosService = lookupService;
        this.lookupOptions = options;
        this.serviceProvider = services;
        this.logCosmosService = logCosmosService;
    }

    public async Task<VehicleLookupDTO> LookupAsync(
        string vin,
        string languageCode = null,
        bool ignoreBrokerStock = false,
        bool insertSSCLog = false,
        SSCLogInfo sscLogInfo = null,
        bool insertCustomerVehcileLookupLog = false,
        CustomerVehicleLookupLogInfo customerVehicleLookupLogInfo = null
    )
    {
        // Get all items related to the VIN from the cosmos container
        var companyDataAggregate = await lookupCosmosService.GetAggregatedCompanyData(vin);

        VehicleEntryModel vehicle = new VehicleEntryEvaluator(companyDataAggregate).Evaluate();

        var data = new VehicleLookupDTO()
        {
            VIN = vin,
            IsAuthorized = new VehicleAuthorizationEvaluator(companyDataAggregate).Evaluate(),
            PaintThicknessInspections = await new VehiclePaintThicknessEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(languageCode),
            Identifiers = new VehicleIdentifierEvaluator(companyDataAggregate).Evaluate(vehicle),
            VehicleSpecification = await new VehicleSpecificationEvaluator(lookupCosmosService).Evaluate(vehicle),
            ServiceHistory = await new VehicleServiceHistoryEvaluator(companyDataAggregate, lookupOptions, this.serviceProvider).Evaluate(languageCode),
            SSC = new VehicleSSCEvaluator(companyDataAggregate).Evaluate(),
            NextServiceDate = companyDataAggregate.Invoices?.OrderByDescending(x => x.InvoiceDate).FirstOrDefault()?.NextServiceDate,
            Accessories = await new VehicleAccessoriesEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(languageCode),
            SaleInformation = await new VehicleSaleInformationEvaluator(companyDataAggregate, lookupOptions, serviceProvider, lookupCosmosService).Evaluate(languageCode),
        };

        data.Warranty = new WarrantyAndFreeServiceDateEvaluator(companyDataAggregate, lookupOptions)
            .Evaluate(vehicle, data.SaleInformation, ignoreBrokerStock);

        data.ServiceItems = await new VehicleServiceItemEvaluator(
            this.lookupCosmosService, companyDataAggregate, this.lookupOptions, this.serviceProvider
        ).Evaluate(
            vehicle,
            data.Warranty.FreeServiceStartDate,
            data.SaleInformation,
            languageCode
        );

        if (insertSSCLog)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    data.SSCLogId = await logCosmosService?.LogSSCLookupAsync(
                        sscLogInfo,
                        data.SSC,
                        vin,
                        data.IsAuthorized,
                        data.Warranty?.HasActiveWarranty ?? false,
                        data.Identifiers?.Brand
                    );
                }
                catch
                {

                }
            });
        }

        if (insertCustomerVehcileLookupLog)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await logCosmosService.LogCustomerVehicleLookupAsync(
                        customerVehicleLookupLogInfo,
                        vin,
                        data.IsAuthorized,
                        data.Warranty?.HasActiveWarranty ?? false,
                        data.Identifiers?.Brand
                    );
                }
                catch
                {

                }
            });
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