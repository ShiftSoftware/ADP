using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model.Flags;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

public class SSCLogCosmosModel :
    IEntityHasCompany<SSCLogCosmosModel>,
    IEntityHasCity<SSCLogCosmosModel>,
    IEntityHasCompanyBranch<SSCLogCosmosModel>
{
    public Guid id { get; set; }
    public string VIN { get; set; } = default!;
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public IEnumerable<SscDTO> SSC { get; set; }
    public DateTimeOffset LookupDate { get; set; }
    public SSCLookupStatuses SSCLookupStatus { get; set; }
    public TMCSaRIIResponse TMCSaRIIResponse { get; set; }
    public LookupSources? VehicleLookupSource { get; set; }
    public bool Authorized { get; set; }
    public bool HasActiveWarranty { get; set; }

    /// <summary>
    /// The Brand that the user made the lookup for
    /// </summary>
    public Brands? LookupBrand { get; set; }

    /// <summary>
    /// The Actual Vehicle Brand if it was found in the lookup service
    /// </summary>
    public Brands? OfficialVehicleBrand { get; set; }
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
}