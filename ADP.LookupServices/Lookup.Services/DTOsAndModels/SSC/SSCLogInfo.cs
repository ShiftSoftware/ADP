using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

public class SSCLogInfo
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public LookupSources? VehicleLookupSource { get; set; }

    /// <summary>
    /// The Brand that the user made the lookup for
    /// </summary>
    public Brands? LookupBrand { get; set; }

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

    public TMCSaRIIResponse? TMCSaRIIResponse { get; set; }
}
