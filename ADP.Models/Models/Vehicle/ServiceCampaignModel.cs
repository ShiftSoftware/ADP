using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ServiceCampaignModel
{
    public string id { get; set; }
    public long ID { get; set; }
    public Dictionary<string, string> Name { get; set; }
    public string UniqueReference { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpireDate { get; set; }
    public IEnumerable<long?> BrandIDs { get; set; }
    public IEnumerable<long?> CountryIDs { get; set; }
    public IEnumerable<long?> CompanyIDs { get; set; }
    public ClaimableItemCampaignActivationTrigger ActivationTrigger { get; set; }
    public ClaimableItemCampaignActivationTypes ActivationType { get; set; }

    public long? VehicleInspectionTypeID { get; set; }
}