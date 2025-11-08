using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ServiceItemModel : IIntegrationProps
{
    public string id { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; }
    public Dictionary<string, string> Photo { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CampaignStartDate { get; set; }
    public DateTime CampaignEndDate { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public Dictionary<string, string> PrintoutTitle { get; set; }
    public Dictionary<string, string> PrintoutDescription { get; set; }
    public IEnumerable<Brands> Brands { get; set; }
    public IEnumerable<long?> BrandIDs { get; set; }
    public IEnumerable<long?> CountryIDs { get; set; }
    public IEnumerable<long?> CompanyIDs { get; set; }
    public int? ActiveFor { get; set; }
    public DurationType? ActiveForDurationType { get; set; }
    public long? MaximumMileage { get; set; }
    public string PackageCode { get; set; }
    public string UniqueReference { get; set; }

    //Will be null if costing type is 'Per Model'
    public decimal? FixedCost { get; set; }

    //Will be null if costing type is 'Fixed'
    public IEnumerable<ServiceItemCostModel> ModelCosts { get; set; }

    public long? CampaignID { get; set; }
    public Dictionary<string, string> CampaignName { get; set; }
    public string CampaignUniqueReference { get; set; }

    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }
    public ClaimableItemCampaignActivationTypes CampaignActivationType { get; set; }
    public ClaimableItemValidityMode ValidityMode { get; set; }
    public ClaimableItemClaimingMethod ClaimingMethod { get; set; }
    public ClaimableItemAttachmentFieldBehavior AttachmentFieldBehavior { get; set; }

    public long? VehicleInspectionTypeID { get; set; }
    public string IntegrationID { get; set; }
}