using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class PartLookupLogInfo
{
    public LookupSources LookupSource { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public long? CityId { get; set; }
    public long? CompanyId { get; set; }
    public long? CompanyBranchId { get; set; }
    public long? UserId { get; set; }

    public string? CityIntegrationId { get; set; }
    public string? CompanyIntegrationId { get; set; }
    public string? CompanyBranchIntegrationId { get; set; }
    public string? PortalUserId { get; set; }
}
