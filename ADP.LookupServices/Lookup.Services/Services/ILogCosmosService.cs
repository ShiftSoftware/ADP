using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public interface ILogCosmosService
{
    Task<Guid> LogPartLookupAsync(PartLookupLogInfo logInfo, PartLookupDTO lookupResult, int? distributorStockLookupQuantity);

    Task<Guid> LogSSCLookupAsync(SSCLogInfo? sscLogInfo,
        IEnumerable<SscDTO> ssc,
        string vin,
        bool isAuthorized,
        bool hasActiveWarranty,
        long? brand);

    Task<Guid> LogCustomerVehicleLookupAsync(
        CustomerVehicleLookupLogInfo logInfo,
        string vin,
        bool isAuthorized,
        bool hasActiveWarranty,
        long? brand);
}