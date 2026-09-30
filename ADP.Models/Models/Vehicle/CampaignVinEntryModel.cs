using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a per-VIN entry recorded against a service campaign.
/// Used by campaigns whose <see cref="Enums.ClaimableItemCampaignActivationTrigger"/> is
/// <see cref="Enums.ClaimableItemCampaignActivationTrigger.ManualVinEntry"/> — an admin tags a VIN
/// as eligible for a specific campaign (e.g., the 500th customer reward), and the evaluator activates
/// every <see cref="ServiceItemModel"/> in that campaign for the VIN at <see cref="RecordedDate"/>.
/// </summary>
[Docable]
public class CampaignVinEntryModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) that qualifies for the campaign.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The ID of the <see cref="ServiceCampaignModel">campaign</see> this entry qualifies the VIN for.
    /// Matched against <see cref="ServiceItemModel.CampaignID"/> by the evaluator.
    /// </summary>
    public long? CampaignID { get; set; }

    /// <summary>
    /// The unique reference of the parent campaign (mirror of <see cref="ServiceItemModel.CampaignUniqueReference"/>),
    /// useful for human-readable lookups and auditing.
    /// </summary>
    public string CampaignUniqueReference { get; set; }

    /// <summary>
    /// Optional menu/package code explicitly assigned to this VIN's activation.
    /// Overrides the catalog/model-derived display code. When service consumption is
    /// enabled, a nonblank code also restricts this activation's consumption to that package.
    /// A blank code preserves the catalog consumption rule.
    /// </summary>
    public string PackageCode { get; set; }

    /// <summary>Optional source audit warning when an approved import preserves a noncanonical identifier.</summary>
    public string SourceIdentifierWarning { get; set; }

    /// <summary>
    /// The date this entry was recorded. Used as the activation date for items with
    /// <see cref="Enums.ClaimableItemValidityMode.RelativeToActivation"/> validity.
    /// </summary>
    public DateTimeOffset RecordedDate { get; set; }

    /// <summary>
    /// Indicates whether this entry has been deleted (effectively revoking the VIN's eligibility).
    /// </summary>
    public bool IsDeleted { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.CampaignVinEntry;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }
}
