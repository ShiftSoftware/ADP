using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Flags;
using ShiftSoftware.ShiftEntity.Model.Replication;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

/// <summary>
/// The item-claim record. Moved from the original host application's Services.Data.Entities.ItemClaim (Phase 2 Slice 5,
/// empty-migration-diff gated — prod table dbo.ItemClaim + temporal history unchanged).
/// Implements the ADP.Cases <see cref="IClaim"/> engine contract (status transitions run through
/// the shared SharedClaimService).
/// </summary>
/// <remarks>
/// Deliberate differences from the original host implementation — relationships only, never columns:
/// <list type="bullet">
/// <item><c>VehicleInspectionResult</c> navigation dropped (consumer-owned entity); the plain
/// <see cref="VehicleInspectionResultID"/> column stays and the consumer configures the FK from its
/// side (host: <c>DB.OnModelCreating</c>). Flows that need inspection data (voucher printing, list
/// flattening) live in the consumer's derived repository.</item>
/// <item>Certificate navigations RETAINED, retyped to the ADP.Cases <see cref="Certificate"/>
/// (ClaimableItems.Data references Cases.Data; the reverse collections on Certificate are gone).</item>
/// <item><see cref="CalculateUniqueHash"/> intentionally EXCLUDES CampaignVinEntryID while the
/// Cosmos doc id includes it — prod asymmetry preserved bug-for-bug (D16).</item>
/// </list>
/// </remarks>
[TemporalShiftEntity]
public class ItemClaim :
    ShiftEntity<ItemClaim>,
    IClaim,
    IEntityHasCompany<ItemClaim>,
    IEntityHasCompanyBranch<ItemClaim>,
    IEntityHasUniqueHash<ItemClaim>,
    IShiftEntityReplication
{
    public string VIN { get; set; } = default!;
    public DateTimeOffset ClaimDate { get; set; }
    public string? JobNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? QRCode { get; set; }
    public string? ModelDescription { get; set; }
    public string? Katashiki { get; set; }
    public long CampaignID { get; set; }
    public virtual Campaign Campaign { get; set; } = default!;
    public long ClaimableItemID { get; set; }
    public virtual ClaimableItem ClaimableItem { get; set; } = default!;
    public long? VehicleInspectionResultID { get; set; }

    public long? CampaignVinEntryID { get; set; }
    public virtual CampaignVinEntry? CampaignVinEntry { get; set; }


    public long? ClaimableItemContractID { get; set; }
    public virtual ClaimableItemContract? ClaimableItemContract { get; set; }
    public decimal? Cost { get; set; }
    public string? PackageCode { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public long? ReimbursementCertificateID { get; set; }
    public virtual Certificate? ReimbursementCertificate { get; set; }
    public long? ContributionCertificateID { get; set; }
    public virtual Certificate? ContributionCertificate { get; set; }
    public int ClaimNumber { get; set; }

    public ClaimStatus? ClaimStatus { get; set; }
    public string? DistributorErrorMessage { get; set; }

    public string? Attachments { get; set; }
    public bool HasAttachment { get; set; }
    public DateTime? ProcessDate { get; set; }

    [NotMapped]
    public string? InvoiceNo { get; set; }

    [NotMapped]
    public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }

    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }

    public string? CalculateUniqueHash()
    {
        return $"{VIN.ToUpper()}-{CampaignID}-{ClaimableItemID}-{VehicleInspectionResultID}-{ClaimableItemContractID}";
    }

    public ItemClaim Clone()
    {
        return (ItemClaim) this.MemberwiseClone();
    }

    public string GetClaimIdentifier()
    {
        return ClaimNumber.ToString();
    }
}
