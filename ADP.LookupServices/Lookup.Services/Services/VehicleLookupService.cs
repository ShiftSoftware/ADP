using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
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
    private readonly IVehicleLookupStorageService vehicleLookupStorageService;
    private readonly LookupOptions lookupOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogCosmosService logCosmosService;

    public VehicleLookupService(
        IVehicleLookupStorageService vehicleLookupStorageService,
        IServiceProvider services = null,
        ILogCosmosService logCosmosService = null,
        LookupOptions options = null
    )
    {
        this.vehicleLookupStorageService = vehicleLookupStorageService;
        this.lookupOptions = options;
        this.serviceProvider = services;
        this.logCosmosService = logCosmosService;
    }

    public async Task<CompanyDataAggregateModel> GetAggregatedCompanyDataAsync(string vin)
    {
        return await vehicleLookupStorageService.GetAggregatedCompanyData(vin);
    }

    public async Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyDataAsync(IEnumerable<string> vins, IEnumerable<string> itemTypes)
    {
        return await vehicleLookupStorageService.GetAggregatedCompanyData(vins, itemTypes);
    }

    public async Task<VehicleLookupDTO> LookupAsync(string vin)
    {
        return await this.LookupAsync(vin, new VehicleLookupRequestOptions());
    }

    public async Task<IEnumerable<VehicleLookupDTO>> LookupAsync(IEnumerable<string> vins)
    {
        return await this.LookupAsync(vins, new VehicleLookupRequestOptions());
    }

    public async Task<IEnumerable<VehicleLookupDTO>> LookupAsync(IEnumerable<string> vins, VehicleLookupRequestOptions requestOptions)
    {
        if (requestOptions is null)
            requestOptions = new VehicleLookupRequestOptions();

        if (vins is null)
            return Enumerable.Empty<VehicleLookupDTO>();

        var normalizedInputOrder = vins
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal) ?? [];

        if (!normalizedInputOrder.Any())
            return Enumerable.Empty<VehicleLookupDTO>();

        var aggregates = await vehicleLookupStorageService.GetAggregatedCompanyDataForBulkLookupAsync(normalizedInputOrder);

        var result = new List<VehicleLookupDTO>();

        foreach (var aggregate in aggregates ?? Enumerable.Empty<CompanyDataAggregateModel>())
        {
            var vinKey = NormalizeVin(aggregate?.VIN);
            if (string.IsNullOrWhiteSpace(vinKey) || aggregate is null)
                continue;

            result.Add(await LookupFromAggregateAsync(vinKey, aggregate, requestOptions, disableLogs: true));
        }

        return result;
    }

    public async Task<VehicleLookupDTO> LookupAsync(string vin, VehicleLookupRequestOptions requestOptions)
    {
        if (requestOptions is null)
            requestOptions = new VehicleLookupRequestOptions();

        // Get all items related to the VIN from the cosmos container
        var companyDataAggregate = await vehicleLookupStorageService.GetAggregatedCompanyData(vin);
        return await LookupFromAggregateAsync(vin, companyDataAggregate, requestOptions, disableLogs: false);
    }

    /// <summary>
    /// Builds the Paint Thickness Certificate for a VIN: the latest PDI paint-thickness inspection taken
    /// strictly before the distributor's sale invoice date, plus vehicle header information.
    /// Returns <c>null</c> when there is no distributor invoice date or no qualifying PDI inspection.
    /// </summary>
    public async Task<PaintThicknessCertificateModel> GetPaintThicknessCertificateAsync(string vin, string languageCode)
    {
        var companyDataAggregate = await vehicleLookupStorageService.GetAggregatedCompanyData(vin);

        if (companyDataAggregate is null)
            return null;

        return await new PaintThicknessCertificateEvaluator(companyDataAggregate, lookupOptions, serviceProvider, vehicleLookupStorageService)
            .Evaluate(languageCode);
    }

    private async Task<VehicleLookupDTO> LookupFromAggregateAsync(
        string vin,
        CompanyDataAggregateModel companyDataAggregate,
        VehicleLookupRequestOptions requestOptions,
        bool disableLogs)
    {
        if (requestOptions is null)
            requestOptions = new VehicleLookupRequestOptions();

        VehicleEntryModel vehicle = new VehicleEntryEvaluator(companyDataAggregate, lookupOptions).Evaluate();

        // The activation-aware owner (company/country/region/branch), resolved once and passed to
        // the ownership-sensitive evaluators (Sale Information, Service Items) so they agree on the
        // owning dealer even when the activating company's entry has not synced yet.
        VehicleOwnership ownership = new VehicleOwnershipEvaluator(companyDataAggregate).Evaluate(vehicle);

        var data = new VehicleLookupDTO()
        {
            VIN = vin,
            IsAuthorized = new VehicleAuthorizationEvaluator(companyDataAggregate).Evaluate(),
            PaintThicknessInspections = await new VehiclePaintThicknessEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(requestOptions.LanguageCode),
            PaintThicknessCertificateAvailable = new PaintThicknessCertificateEvaluator(companyDataAggregate, lookupOptions, serviceProvider).EvaluateAvailability(),
            Identifiers = new VehicleIdentifierEvaluator(companyDataAggregate).Evaluate(vehicle),
            VehicleSpecification = await new VehicleSpecificationEvaluator(vehicleLookupStorageService).Evaluate(vehicle, requestOptions),
            ServiceHistory = await new VehicleServiceHistoryEvaluator(companyDataAggregate, lookupOptions, this.serviceProvider).Evaluate(requestOptions.LanguageCode, requestOptions.VehicleServiceHistoryConsistencyLevel),
            SSC = new VehicleSSCEvaluator(companyDataAggregate).Evaluate(),
            NextServiceDate = companyDataAggregate.LaborLines?.Max(x => x.NextServiceDate),
            Accessories = await new VehicleAccessoriesEvaluator(companyDataAggregate, lookupOptions, serviceProvider).Evaluate(requestOptions.LanguageCode),
            SaleInformation = await new VehicleSaleInformationEvaluator(companyDataAggregate, lookupOptions, serviceProvider, vehicleLookupStorageService).Evaluate(vehicle, ownership, requestOptions),
        };

        // The certificate's signed public URLs (one per language the host supports): produced
        // only when the certificate is available, the endpoint opted in (its server-side
        // permission check), and the host wired a resolver. An empty result stays null so
        // consumers have a single "no print offered" signal.
        if (data.PaintThicknessCertificateAvailable
            && requestOptions.GeneratePaintThicknessCertificateUrls
            && lookupOptions.PaintThicknessCertificateUrlsResolver is not null)
        {
            var certificateUrls = await lookupOptions.PaintThicknessCertificateUrlsResolver(
                new LookupOptionResolverModel<string>(vin, requestOptions.LanguageCode, serviceProvider));

            if (certificateUrls is not null && certificateUrls.Count > 0)
                data.PaintThicknessCertificateUrls = certificateUrls;
        }

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

        var traceCollector = requestOptions.TraceServiceItemEvaluation
            ? new ServiceItemTraceCollector(vin)
            : ServiceItemTraceCollector.Disabled;

        var serviceItemEvaluator = new VehicleServiceItemEvaluator(
            this.vehicleLookupStorageService, companyDataAggregate, this.lookupOptions, this.serviceProvider
        )
        { Trace = traceCollector };

        var serviceItemsResult = await serviceItemEvaluator.Evaluate(
            vehicle,
            ownership,
            data.Warranty?.FreeServiceStartDate,
            requestOptions.LanguageCode,
            data.SaleInformation?.Broker
        );

        data.ServiceItems = serviceItemsResult.serviceItems;

        if (requestOptions.TraceServiceItemEvaluation)
            data.ServiceItemTrace = traceCollector.Build();

        if (data.Warranty is not null)
        {
            // ActivationIsRequired (company-agnostic, consumed by reporting) keeps its legacy gate on a
            // resolved warranty start date.
            if (data.Warranty.WarrantyStartDate is not null)
                data.Warranty.ActivationIsRequired = serviceItemsResult.activationRequired;

            // ActivationStatus (company-scoped, drives the UI) is independent of the warranty start date:
            // tenants that don't default the warranty start to the invoice date
            // (WarrantyStartDateDefaultsToInvoiceDate = false) still leave it null for not-yet-activated
            // vehicles, and those are exactly the vehicles that need an activation / blocked affordance.
            data.Warranty.ActivationStatus = new ActivationStatusEvaluator(companyDataAggregate, lookupOptions)
                .Evaluate(serviceItemsResult.activationRequired, requestOptions.RequestingCompanyID);
        }

        if (!disableLogs && requestOptions.InsertSSCLog)
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

        if (!disableLogs && requestOptions.InsertCustomerVehcileLookupLog)
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

    private static string NormalizeVin(string vin)
    {
        return vin?.Trim()?.ToUpperInvariant();
    }

    public async Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync()
    {
        return await vehicleLookupStorageService.GetAllVehicleModelsAsync();
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki)
    {
        return await vehicleLookupStorageService.GetVehicleModelsByKatashikiAsync(katashiki);
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant)
    {
        return await vehicleLookupStorageService.GetVehicleModelsByVariantAsync(variant);
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin)
    {
        return await vehicleLookupStorageService.GetVehicleModelsByVinAsync(vin);
    }
}
