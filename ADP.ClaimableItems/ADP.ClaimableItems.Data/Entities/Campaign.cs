using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

[TemporalShiftEntity]
public class Campaign : ShiftEntity<Campaign>, IEntityHasUniqueHash<Campaign>, IShiftEntityReplication
{
    public string Name { get; set; } = default!;
    public string? UniqueReference { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpireDate { get; set; }
    public List<long> Brands { get; set; } = new();
    public List<long> Companies { get; set; } = new();
    public List<long> Countries { get; set; } = new();
    public ClaimableItemCampaignActivationTrigger ActivationTrigger { get; set; }
    public ClaimableItemCampaignActivationTypes ActivationType { get; set; }
    public virtual ICollection<ClaimableItem> ClaimableItems { get; set; } = new HashSet<ClaimableItem>();

    public string? CertificatePrintoutHeader { get; set; }
    public string? CertificatePrintoutBody { get; set; }
    public bool CertificatePrintoutSignStampVisibility { get; set; }
    public bool CertificatePrintoutKatashikiAndModelColumnVisibility { get; set; }

    public string? DistributorCertificateNumberPrefix { get; set; }

    // FK to the consumer-owned VehicleInspectionType (a cross-feature entity that stays in the consumer,
    // e.g. TCA Services). Kept as a plain nullable FK column with NO navigation — the relationship is
    // configured from the principal side by the consumer (VehicleInspectionType.Campaigns), which preserves
    // the existing FK constraint without this module depending on the consumer's type.
    public long? VehicleInspectionTypeID { get; set; }

    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }

    public string? CalculateUniqueHash()
    {
        return UniqueReference;
    }
}
