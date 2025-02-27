using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class PartLookupLogCosmosModel
{
    public Guid id { get; set; }
    public string PartNumber { get; set; } = default!;
    public decimal? DistributorStockLookupQuantity { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public PartLookupDTO PartLookupResult { get; set; }
    public DateTimeOffset LookupDate { get; set; }
    public LookupSources? LookupSource { get; set; }
    public long? CityID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public long? UserID { get; set; }
    public string CityIntegrationID { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string CompanyBranchIntegrationID { get; set; }
    public string PortalUserID { get; set; }
}