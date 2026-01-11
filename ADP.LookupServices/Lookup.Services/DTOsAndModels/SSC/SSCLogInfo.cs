using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

public class SSCLogInfo
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public LookupSources? VehicleLookupSource { get; set; }

    /// <summary>
    /// The Brand that the user made the lookup for
    /// </summary>
    public long? LookupBrandID { get; set; }
    public long? CityID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public long? UserID { get; set; }
    public long? CityIntegrationID { get; set; }
    public long? CompanyIntegrationID { get; set; }
    public long? CompanyBranchIntegrationID { get; set; }
    public long? PortalUserID { get; set; }
    public long? TicketID { get; set; }
    public string TicketHashID { get; set; }
    public TMCSaRIIResponse TMCSaRIIResponse { get; set; }
}