using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

[TemporalShiftEntity]
public class ClaimableItem : ShiftEntity<ClaimableItem>, IEntityHasUniqueHash<ClaimableItem>, IShiftEntityReplication
{
    public string Name { get; set; } = default!;
    public string? PrintoutTitle { get; set; }
    public string? PrintoutDescription { get; set; }
    public long? MaximumMileage { get; set; }
    public string? UniqueReference { get; set; }
    public string? PackageCode { get; set; }
    public ClaimableItemValidityMode ValidityMode { get; set; }
    public ClaimableItemClaimingMethod ClaimingMethod { get; set; }
    public int ActiveFor { get; set; }
    public DurationType ActiveForDurationType { get; set; }
    public ClaimableItemCostingType CostingType { get; set; }
    public ClaimableItemAttachmentFieldBehavior AttachmentFieldBehavior { get; set; }
    public decimal? FixedCost { get; set; }
    public string Costs { get; set; } = default!;
    public long? CampaignID { get; set; }
    public virtual Campaign? Campaign { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }

    public string? CalculateUniqueHash()
    {
        return UniqueReference;
    }
}
