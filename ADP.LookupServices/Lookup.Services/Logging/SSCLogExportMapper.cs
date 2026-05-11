using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

namespace ShiftSoftware.ADP.Lookup.Services.Logging;

/// <summary>
/// Maps a Cosmos <see cref="SSCLogCosmosModel"/> to a flat <see cref="SSCLogExportRow"/>
/// suitable for BI consumption. Encodes the single rule for which rows BI should see.
/// </summary>
public static class SSCLogExportMapper
{
    /// <summary>
    /// Returns the export row for the given cosmos document, or <c>null</c> if the document
    /// represents an intermediate state that BI should not see.
    /// </summary>
    public static SSCLogExportRow? FromCosmos(SSCLogCosmosModel model)
    {
        if (model is null)
            return null;

        // Intermediate state: the unauthorized lookup wrote the row but the second-stage
        // TMC call has not landed yet. Suppress until the row reaches a terminal status.
        if (model.SSCLookupStatus == SSCLookupStatuses.PendingTMCLookup)
            return null;

        return new SSCLogExportRow
        {
            Id = model.id,
            VIN = model.VIN,
            LookupDate = model.LookupDate,
            VehicleLookupSource = model.VehicleLookupSource,
            Authorized = model.Authorized,
            HasActiveWarranty = model.HasActiveWarranty,
            SSCLookupStatus = model.SSCLookupStatus,
            LookupBrandID = model.LookupBrandID,
            BrandID = model.BrandID,
            CityID = model.CityID,
            CompanyID = model.CompanyID,
            CompanyBranchID = model.CompanyBranchID,
            UserID = model.UserID,
            CityIntegrationID = model.CityIntegrationID,
            CompanyIntegrationID = model.CompanyIntegrationID,
            CompanyBranchIntegrationID = model.CompanyBranchIntegrationID,
            PortalUserID = model.PortalUserID,
            TicketID = model.TicketID,
            TicketHashID = model.TicketHashID,
            Name = model.Name,
            Phone = model.Phone,
            Email = model.Email,
            SSC = model.SSC,
            TMCErrorCode = model.TMCSaRIIResponse?.TMCErrorCode,
            TMCExceptionMessage = model.TMCSaRIIResponse?.ExceptionMessage,
        };
    }
}
