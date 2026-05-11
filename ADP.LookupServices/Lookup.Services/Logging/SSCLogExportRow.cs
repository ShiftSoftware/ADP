using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Logging;

/// <summary>
/// Flat, BI-friendly projection of an SSC log row. Produced by <see cref="SSCLogExportMapper"/>
/// from a <see cref="SSCLogCosmosModel"/> for persistence in a downstream sink (typically a SQL table).
/// </summary>
public class SSCLogExportRow
{
    public Guid Id { get; set; }
    public string VIN { get; set; } = default!;
    public DateTimeOffset LookupDate { get; set; }

    public LookupSources? VehicleLookupSource { get; set; }
    public bool Authorized { get; set; }
    public bool HasActiveWarranty { get; set; }
    public SSCLookupStatuses SSCLookupStatus { get; set; }

    public long? LookupBrandID { get; set; }
    public long? BrandID { get; set; }

    public long? CityID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public long? UserID { get; set; }
    public long? CityIntegrationID { get; set; }
    public long? CompanyIntegrationID { get; set; }
    public long? CompanyBranchIntegrationID { get; set; }
    public long? PortalUserID { get; set; }

    public long? TicketID { get; set; }
    public string? TicketHashID { get; set; }

    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public IEnumerable<SscDTO>? SSC { get; set; }

    public int? TMCErrorCode { get; set; }
    public string? TMCExceptionMessage { get; set; }
}
