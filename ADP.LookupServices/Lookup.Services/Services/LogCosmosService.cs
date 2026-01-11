using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
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
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.PartLookupLogs
        );

        var log = new PartLookupLogCosmosModel
        {
            id = Guid.NewGuid(),
            LookupSource = logInfo?.LookupSource,
            LookupDate = DateTimeOffset.UtcNow,
            PartLookupResult = lookupResult,
            PartNumber = lookupResult.PartNumber,
            DistributorStockLookupQuantity = distributorStockLookupQuantity,
            CityID = logInfo?.CityID,
            CompanyID = logInfo?.CompanyID,
            CompanyBranchID = logInfo?.CompanyBranchID,
            UserID = logInfo?.UserID,
            CityIntegrationID = logInfo?.CityIntegrationID,
            CompanyBranchIntegrationID = logInfo?.CompanyBranchIntegrationID,
            CompanyIntegrationID = logInfo?.CompanyIntegrationID,
            PortalUserID = logInfo?.PortalUserID,
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
        IEnumerable<SscDTO> ssc, 
        string vin,
        bool isAuthorized, 
        bool hasActiveWarranty,
        long? brand)
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
            BrandID = brand,
            CityID = sscLogInfo?.CityID,
            CompanyID = sscLogInfo?.CompanyID,
            CompanyBranchID = sscLogInfo?.CompanyBranchID,
            UserID = sscLogInfo?.UserID,
            CityIntegrationID = sscLogInfo?.CityIntegrationID,
            CompanyBranchIntegrationID = sscLogInfo?.CompanyBranchIntegrationID,
            CompanyIntegrationID = sscLogInfo?.CompanyIntegrationID,
            PortalUserID = sscLogInfo?.PortalUserID,
            Name = sscLogInfo?.Name,
            Phone = sscLogInfo?.Phone,
            Email = sscLogInfo?.Email,
            LookupBrandID = sscLogInfo?.LookupBrandID,
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
        long? brand)
    {
        var container = client.GetContainer("Logs", "CustomerVehicleLookup");

        var log = new CustomerVehicleLookupLogCosmosModel
        {
            id = Guid.NewGuid(),
            VIN = vin,
            LookupDate = DateTimeOffset.UtcNow,
            CityID = logInfo.CityID,
            Name = logInfo.Name,
            Phone = logInfo.Phone,
            LookupBrandID = logInfo.LookupBrandID,
            VehicleLookupSource = logInfo.VehicleLookupSource,
            Authorized = isAuthorized,
            HasActiveWarranty = hasActiveWarranty,
            BrandID = brand
        };

        await container.CreateItemAsync(log, requestOptions: new ItemRequestOptions
        {
            EnableContentResponseOnWrite = false
        });

        return log.id;
    }
}