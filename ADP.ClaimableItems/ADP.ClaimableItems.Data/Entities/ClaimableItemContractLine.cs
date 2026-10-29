using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

/// <summary>
/// One menu service on a <see cref="ClaimableItemContract"/>: a snapshot of the menu line, priced when
/// the contract was written.
/// </summary>
[TemporalShiftEntity]
public class ClaimableItemContractLine : ShiftEntity<ClaimableItemContractLine>
{
    public long ClaimableItemContractID { get; set; }
    public virtual ClaimableItemContract ClaimableItemContract { get; set; } = default!;

    /// <summary>The menu line's stable identity; the menu code's text depends on the language.</summary>
    public string MenuLineKey { get; set; } = default!;
    public string? MenuCode { get; set; }
    public string? LabourCode { get; set; }
    public long? VariantID { get; set; }
    public string? VariantName { get; set; }
    public ClaimableItemContractLineType LineType { get; set; }
    public string? ServiceIntervalCode { get; set; }

    /// <summary>Kilometres, despite the name, as on the menu line.</summary>
    public int? ServiceIntervalValueInMeter { get; set; }

    public string? Description { get; set; }

    [Precision(18, 2)]
    public decimal LabourTotalPrice { get; set; }

    [Precision(18, 2)]
    public decimal PartsTotalPrice { get; set; }

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(18, 2)]
    public decimal Cost { get; set; }

    [Precision(18, 2)]
    public decimal PriceWithIncrement { get; set; }

    public int? Duration { get; set; }
    public ClaimableItemContractDurationUnit? DurationUnit { get; set; }
    public DateTime? ExpireDate { get; set; }

    public bool IsCancelled { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? CancelledDate { get; set; }
    public long? CancelledByUserID { get; set; }
    public string? CancelReason { get; set; }

    public int Sort { get; set; }
}
