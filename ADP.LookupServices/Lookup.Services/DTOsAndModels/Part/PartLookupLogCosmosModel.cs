using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class PartLookupLogCosmosModel
{
    public Guid id { get; set; }
    public string PartNumber { get; set; } = default!;
    public decimal? DistributorStockLookupQuantity { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public PartLookupDTO? PartLookupResult { get; set; }
    public DateTimeOffset LookupDate { get; set; }
    public LookupSources? LookupSource { get; set; }

    public long? CityId { get; set; }
    public long? CompanyId { get; set; }
    public long? CompanyBranchId { get; set; }
    public long? UserId { get; set; }

    public string? CityIntegrationId { get; set; }
    public string? CompanyIntegrationId { get; set; }
    public string? CompanyBranchIntegrationId { get; set; }
    public string? PortalUserId { get; set; }
}
