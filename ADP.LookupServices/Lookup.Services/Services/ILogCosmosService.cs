using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public interface ILogCosmosService
{
    Task<Guid> LogPartLookupAsync(PartLookupLogInfo logInfo, PartLookupDTO lookupResult, int? distributorStockLookupQuantity);

    Task<Guid> LogSSCLookupAsync(SSCLogInfo? sscLogInfo,
        IEnumerable<SSCDTO> ssc,
        string vin,
        bool isAuthorized,
        bool hasActiveWarranty,
        Brands? brand);

    Task<Guid> LogCustomerVehicleLookupAsync(
        CustomerVehicleLookupLogInfo? logInfo,
        string vin,
        bool isAuthorized,
        bool hasActiveWarranty,
        Brands? brand);
}