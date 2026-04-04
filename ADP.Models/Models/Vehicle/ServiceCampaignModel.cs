using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a service campaign that offers <see cref="ServiceItemModel">Service Items</see> to vehicles.
/// Campaigns define the time period, target brands/countries/companies, and activation rules for their service items.
/// </summary>
[Docable]
public class ServiceCampaignModel
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The unique identifier for this campaign.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The multilingual name of the campaign (keyed by language code).
    /// </summary>
    public Dictionary<string, string> Name { get; set; }

    /// <summary>
    /// A unique reference identifier for this campaign.
    /// </summary>
    public string UniqueReference { get; set; }

    /// <summary>
    /// The date from which this campaign is active.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The date after which this campaign expires.
    /// </summary>
    public DateTime ExpireDate { get; set; }

    /// <summary>
    /// The brand IDs this campaign targets.
    /// </summary>
    public IEnumerable<long?> BrandIDs { get; set; }

    /// <summary>
    /// The country IDs this campaign is available in.
    /// </summary>
    public IEnumerable<long?> CountryIDs { get; set; }

    /// <summary>
    /// The company IDs this campaign is available for.
    /// </summary>
    public IEnumerable<long?> CompanyIDs { get; set; }

    /// <summary>
    /// What triggers this campaign to activate (e.g., on vehicle sale or manually).
    /// </summary>
    public ClaimableItemCampaignActivationTrigger ActivationTrigger { get; set; }

    /// <summary>
    /// The activation type for this campaign (e.g., Automatic, Manual).
    /// </summary>
    public ClaimableItemCampaignActivationTypes ActivationType { get; set; }

    /// <summary>
    /// The vehicle inspection type required for this campaign, if any.
    /// </summary>
    public long? VehicleInspectionTypeID { get; set; }
}