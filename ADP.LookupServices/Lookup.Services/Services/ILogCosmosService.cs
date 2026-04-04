using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// Service interface for writing audit log entries for part lookups, SSC lookups, and customer vehicle lookups.
/// </summary>
public interface ILogCosmosService
{
    /// <summary>Logs a part lookup operation with its result and stock lookup quantity.</summary>
    Task<Guid> LogPartLookupAsync(PartLookupLogInfo logInfo, PartLookupDTO lookupResult, int? distributorStockLookupQuantity);

    /// <summary>Logs an SSC (safety recall) lookup operation.</summary>
    Task<Guid> LogSSCLookupAsync(SSCLogInfo? sscLogInfo,
        IEnumerable<SscDTO> ssc,
        string vin,
        bool isAuthorized,
        bool hasActiveWarranty,
        long? brand);

    /// <summary>Logs a customer vehicle lookup operation.</summary>
    Task<Guid> LogCustomerVehicleLookupAsync(
        CustomerVehicleLookupLogInfo logInfo,
        string vin,
        bool isAuthorized,
        bool hasActiveWarranty,
        long? brand);
}