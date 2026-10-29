using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

/// <summary>
/// A service contract: menu services sold for one VIN, valid from <see cref="StartDate"/> to
/// <see cref="EndDate"/>. Moved here from the original host application as an empty placeholder
/// (Phase 2 Slice 5) because ItemClaim FKs it AND its ID participates in both the claim UniqueHash and
/// the Cosmos doc-id formats (see D18); the columns came later. The table keeps its name
/// (dbo.ClaimableItemContract + History in the original host).
/// </summary>
/// <remarks>
/// Draft → issued → cancelled. Only a draft is edited or deleted; <see cref="IsCancelled"/> is set by the
/// cancel action, never by a save.
/// </remarks>
[TemporalShiftEntity]
public class ClaimableItemContract :
    ShiftEntity<ClaimableItemContract>,
    IEntityHasDraft<ClaimableItemContract>,
    IEntityHasCompany<ClaimableItemContract>,
    IEntityHasCompanyBranch<ClaimableItemContract>,
    IEntityHasCountry<ClaimableItemContract>
{
    public string VIN { get; set; } = default!;

    public string? Katashiki { get; set; }
    public string? BasicModelCode { get; set; }
    public string? Variant { get; set; }
    public string? ModelCode { get; set; }
    public string? ModelDescription { get; set; }
    public long? BrandID { get; set; }

    public string CustomerName { get; set; } = default!;
    public string? CustomerPhone { get; set; }

    public ClaimableItemContractType ContractType { get; set; }

    public long Mileage { get; set; }
    public DateTime StartDate { get; set; }
    public int Duration { get; set; }
    public ClaimableItemContractDurationUnit DurationUnit { get; set; }
    public DateTime EndDate { get; set; }

    [Precision(18, 4)]
    public decimal TransferRate { get; set; }

    public string? MenuLanguage { get; set; }

    [Precision(18, 4)]
    public decimal PriceIncrement { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    [Precision(18, 2)]
    public decimal TotalWithIncrement { get; set; }

    public bool IsDraft { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? IssuedDate { get; set; }
    public long? IssuedByUserID { get; set; }

    public bool IsCancelled { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? CancelledDate { get; set; }
    public long? CancelledByUserID { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>The country whose menu prices the contract uses.</summary>
    public long? CountryID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }

    public virtual ICollection<ClaimableItemContractLine> Lines { get; set; } = new HashSet<ClaimableItemContractLine>();
}
