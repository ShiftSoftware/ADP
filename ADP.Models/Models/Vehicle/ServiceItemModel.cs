using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a claimable service item offered through a <see cref="ServiceCampaignModel">Service Campaign</see>.
/// Service items define what services are available to vehicles, including validity periods, costs (fixed or per-model), and campaign activation rules.
/// </summary>
[Docable]
public class ServiceItemModel : IIntegrationProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The multilingual name of the service item (keyed by language code).
    /// </summary>
    public Dictionary<string, string> Name { get; set; }

    /// <summary>
    /// The multilingual photo URLs for the service item (keyed by language code).
    /// </summary>
    public Dictionary<string, string> Photo { get; set; }

    /// <summary>
    /// Indicates whether this service item has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The start date of the campaign that this service item belongs to.
    /// </summary>
    public DateTime CampaignStartDate { get; set; }

    /// <summary>
    /// The end date of the campaign that this service item belongs to.
    /// </summary>
    public DateTime CampaignEndDate { get; set; }

    /// <summary>
    /// The date from which this service item becomes valid for claiming.
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// The date after which this service item is no longer valid for claiming.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// The multilingual printout title (keyed by language code).
    /// </summary>
    public Dictionary<string, string> PrintoutTitle { get; set; }

    /// <summary>
    /// The multilingual printout description (keyed by language code).
    /// </summary>
    public Dictionary<string, string> PrintoutDescription { get; set; }

    /// <summary>
    /// The brand IDs this service item is available for.
    /// </summary>
    public IEnumerable<long?> BrandIDs { get; set; }

    /// <summary>
    /// The country IDs this service item is available in.
    /// </summary>
    public IEnumerable<long?> CountryIDs { get; set; }

    /// <summary>
    /// The company IDs this service item is available for.
    /// </summary>
    public IEnumerable<long?> CompanyIDs { get; set; }

    /// <summary>
    /// The duration for which this service item remains active after activation.
    /// </summary>
    public int? ActiveFor { get; set; }

    /// <summary>
    /// The unit of time for the <see cref="ActiveFor"/> duration (e.g., Days, Months, Years).
    /// </summary>
    public DurationType? ActiveForDurationType { get; set; }

    /// <summary>
    /// The maximum mileage up to which this service item is valid. Used in sequential validity calculations.
    /// </summary>
    public long? MaximumMileage { get; set; }

    /// <summary>
    /// Identifies the item's role in the service program. This catalog-only classification is
    /// orthogonal to whether an evaluated item is free or paid. Reward items keep all normal
    /// service-item lifecycle behavior but do not define the base scheduled-service mileage cap.
    /// </summary>
    public ServiceItemProgramRole ProgramRole { get; set; } = ServiceItemProgramRole.ScheduledService;

    /// <summary>
    /// The package code that groups related service items together.
    /// </summary>
    public string PackageCode { get; set; }

    /// <summary>
    /// A unique reference identifier for this service item.
    /// </summary>
    public string UniqueReference { get; set; }

    /// <summary>
    /// The fixed cost for this service item. Will be null if costing type is 'Per Model'.
    /// </summary>
    public decimal? FixedCost { get; set; }

    /// <summary>
    /// Per-model <see cref="ServiceItemCostModel">costs</see> for this service item. Will be null if costing type is 'Fixed'.
    /// </summary>
    public IEnumerable<ServiceItemCostModel> ModelCosts { get; set; }

    /// <summary>
    /// The ID of the <see cref="ServiceCampaignModel">campaign</see> this service item belongs to.
    /// </summary>
    public long? CampaignID { get; set; }

    /// <summary>
    /// The multilingual name of the parent campaign (keyed by language code).
    /// </summary>
    public Dictionary<string, string> CampaignName { get; set; }

    /// <summary>
    /// The unique reference of the parent campaign.
    /// </summary>
    public string CampaignUniqueReference { get; set; }

    /// <summary>
    /// What triggers the campaign to activate (e.g., on vehicle sale or manually).
    /// </summary>
    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }

    /// <summary>
    /// The activation type for the campaign (e.g., Automatic, Manual).
    /// </summary>
    public ClaimableItemCampaignActivationTypes CampaignActivationType { get; set; }

    /// <summary>
    /// How the validity period is calculated (e.g., relative to activation or fixed dates).
    /// </summary>
    public ClaimableItemValidityMode ValidityMode { get; set; }

    /// <summary>
    /// The method used to claim this service item.
    /// </summary>
    public ClaimableItemClaimingMethod ClaimingMethod { get; set; }

    /// <summary>
    /// How attachment fields behave when claiming this service item.
    /// </summary>
    public ClaimableItemAttachmentFieldBehavior AttachmentFieldBehavior { get; set; }

    /// <summary>
    /// The vehicle inspection type required for claiming this service item, if any.
    /// </summary>
    public long? VehicleInspectionTypeID { get; set; }

    /// <summary>
    /// An external identifier used for system-to-system integration.
    /// </summary>
    public string IntegrationID { get; set; }

    /// <summary>
    /// Optional declarative predicates that must match the vehicle lookup data before this
    /// service item is offered. This is a closed contract, not a general query language: the
    /// lookup evaluator supports only documented field paths and fails closed for every other path.
    /// </summary>
    public IEnumerable<ServiceItemEligibilityConditionModel> EligibilityConditions { get; set; }
}

/// <summary>
/// A declarative eligibility predicate evaluated against data exposed by the vehicle lookup.
/// </summary>
public class ServiceItemEligibilityConditionModel
{
    /// <summary>
    /// The fully-qualified vehicle lookup field path to evaluate. Supported paths are defined
    /// by the lookup evaluator; this model does not imply arbitrary vehicle lookup traversal.
    /// </summary>
    public string Field { get; set; }

    /// <summary>How the configured values are combined across the scoped field values.</summary>
    public ServiceItemEligibilityConditionOperator Operator { get; set; }

    /// <summary>
    /// How each configured value is matched against a scoped field value. Defaults to exact
    /// matching so catalogs created before this property was added keep their existing behavior.
    /// </summary>
    public ServiceItemEligibilityConditionValueMatch ValueMatch { get; set; } =
        ServiceItemEligibilityConditionValueMatch.Exact;

    /// <summary>The values required by the comparison.</summary>
    public IEnumerable<string> Values { get; set; }

    /// <summary>Optional selection scope for collection-based fields.</summary>
    public ServiceItemEligibilityConditionScope Scope { get; set; }
}

/// <summary>Defines how a collection-backed condition selects source entries.</summary>
public class ServiceItemEligibilityConditionScope
{
    /// <summary>The collection selection strategy.</summary>
    public ServiceItemEligibilityConditionSelection Selection { get; set; }

    /// <summary>The number of latest entries that take part in the comparison.</summary>
    public int? Count { get; set; }
}

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum ServiceItemEligibilityConditionOperator
{
    ContainsAll,
    Equals
}

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum ServiceItemEligibilityConditionValueMatch
{
    Exact = 0,
    EndsWith = 1
}

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum ServiceItemEligibilityConditionSelection
{
    Latest
}
