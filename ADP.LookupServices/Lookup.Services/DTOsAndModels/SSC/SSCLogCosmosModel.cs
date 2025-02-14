using ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;
using System;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

public class SSCLogCosmosModel
{
    public Guid id { get; set; }
    public string VIN { get; set; } = default!;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public IEnumerable<SSCDTO>? SSC { get; set; }
    public DateTimeOffset LookupDate { get; set; }
    public SSCLookupStatuses SSCLookupStatus { get; set; }
    public TMCSaRIIResponse? TMCSaRIIResponse { get; set; }
    public LookupSources? VehicleLookupSource { get; set; }
    public bool Authorized { get; set; }
    public bool HasActiveWarranty { get; set; }

    /// <summary>
    /// The Brand that the user made the lookup for
    /// </summary>
    public Franchises? LookupBrand { get; set; }

    /// <summary>
    /// The Actual Vehicle Brand if it was found in the lookup service
    /// </summary>
    public Franchises? OfficialVehicleBrand { get; set; }
    public long? CityId { get; set; }
    public long? CompanyId { get; set; }
    public long? CompanyBranchId { get; set; }
    public long? UserId { get; set; }

    public long? CityIntegrationId { get; set; }
    public long? CompanyIntegrationId { get; set; }
    public long? CompanyBranchIntegrationId { get; set; }
    public long? PortalUserId { get; set; }

    public long? TicketID { get; set; }
    public string? TicketHashID { get; set; }
}
