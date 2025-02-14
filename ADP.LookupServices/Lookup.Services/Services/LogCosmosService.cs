using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class LogCosmosService : ILogCosmosService
{
    private readonly CosmosClient client;

    public LogCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    public async Task<Guid> LogPartLookupAsync(PartLookupLogInfo? logInfo, PartLookupDTO lookupResult, int? distributorStockLookupQuantity)
    {
        //Add the log to cosmos
        var container = client.GetContainer("Logs", "PartLookup");

        var log = new PartLookupLogCosmosModel
        {
            id = Guid.NewGuid(),
            LookupSource = logInfo?.LookupSource,
            LookupDate = DateTimeOffset.UtcNow,
            PartLookupResult = lookupResult,
            PartNumber = lookupResult.PartNumber,
            DistributorStockLookupQuantity = distributorStockLookupQuantity,
            CityId = logInfo?.CityId,
            CompanyId = logInfo?.CompanyId,
            CompanyBranchId = logInfo?.CompanyBranchId,
            UserId = logInfo?.UserId,
            CityIntegrationId = logInfo?.CityIntegrationId,
            CompanyBranchIntegrationId = logInfo?.CompanyBranchIntegrationId,
            CompanyIntegrationId = logInfo?.CompanyIntegrationId,
            PortalUserId = logInfo?.PortalUserId,
            Name = logInfo?.Name,
            Phone = logInfo?.Phone,
            Email = logInfo?.Email
        };

        await container.CreateItemAsync(log, requestOptions: new ItemRequestOptions
        {
            EnableContentResponseOnWrite = false
        });

        return log.id;
    }

    public async Task<Guid> LogSSCLookupAsync(SSCLogInfo? sscLogInfo,
        IEnumerable<SSCDTO> ssc, 
        string vin,
        bool isAuthorized, 
        bool hasActiveWarranty,
        Franchises? brand)
    {
        var container = client.GetContainer("Logs", "SSC");

        var log = new SSCLogCosmosModel
        {
            id = Guid.NewGuid(),
            VIN = vin,
            VehicleLookupSource = sscLogInfo?.VehicleLookupSource,
            SSC = ssc,
            LookupDate = DateTimeOffset.UtcNow,
            Authorized = isAuthorized,
            HasActiveWarranty = hasActiveWarranty,
            OfficialVehicleBrand = brand,
            CityId = sscLogInfo?.CityId,
            CompanyId = sscLogInfo?.CompanyId,
            CompanyBranchId = sscLogInfo?.CompanyBranchId,
            UserId = sscLogInfo?.UserId,
            CityIntegrationId = sscLogInfo?.CityIntegrationId,
            CompanyBranchIntegrationId = sscLogInfo?.CompanyBranchIntegrationId,
            CompanyIntegrationId = sscLogInfo?.CompanyIntegrationId,
            PortalUserId = sscLogInfo?.PortalUserId,
            Name = sscLogInfo?.Name,
            Phone = sscLogInfo?.Phone,
            Email = sscLogInfo?.Email,
            LookupBrand = sscLogInfo?.LookupBrand,
            TicketID = sscLogInfo?.TicketID,
            TicketHashID = sscLogInfo?.TicketHashID,
            TMCSaRIIResponse = sscLogInfo?.TMCSaRIIResponse,
        };

        if (isAuthorized)
            log.SSCLookupStatus = (ssc is not null && ssc.Any(x => !x.Repaired)) ? SSCLookupStatuses.RecallExists : SSCLookupStatuses.NoRecall;
        else
            log.SSCLookupStatus = SSCLookupStatuses.PendingTMCLookup;

        await container.CreateItemAsync(log, requestOptions: new ItemRequestOptions
        {
            EnableContentResponseOnWrite = false
        });

        return log.id;
    }

    public async Task<Guid> LogCustomerVehicleLookupAsync(CustomerVehicleLookupLogInfo? logInfo,
        string vin,
        bool isAuthorized,
        bool hasActiveWarranty,
        Franchises? brand)
    {
        var container = client.GetContainer("Logs", "CustomerVehicleLookup");

        var log = new CustomerVehicleLookupLogCosmosModel
        {
            id = Guid.NewGuid(),
            VIN = vin,
            LookupDate = DateTimeOffset.UtcNow,
            CityId = logInfo.CityId,
            Name = logInfo.Name,
            Phone = logInfo.Phone,
            LookupBrand = logInfo.LookupBrand,
            VehicleLookupSource = logInfo.VehicleLookupSource,
            Authorized = isAuthorized,
            HasActiveWarranty = hasActiveWarranty,
            OfficialVehicleBrand = brand
        };

        await container.CreateItemAsync(log, requestOptions: new ItemRequestOptions
        {
            EnableContentResponseOnWrite = false
        });

        return log.id;
    }
}
