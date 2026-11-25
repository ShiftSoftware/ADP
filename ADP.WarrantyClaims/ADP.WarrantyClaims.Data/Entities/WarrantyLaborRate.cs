using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

/// <summary>
/// The labor rates per hour of the claims in one currency, from a date on. A new claim takes the row with the
/// latest <see cref="EffectiveDate"/> that has started and stores both rates on itself, so a later row never
/// changes an existing claim. A currency without a row takes its rate from the host
/// (<see cref="Shared.IWarrantyClaimCurrencyResolver.GetLaborRateAsync"/>).
/// </summary>
[TemporalShiftEntity]
[Index(nameof(Currency), nameof(EffectiveDate))]
public class WarrantyLaborRate : ShiftEntity<WarrantyLaborRate>
{
    /// <summary>The claim currency the row is for. USD has rows too.</summary>
    [Column(TypeName = "varchar")]
    [MaxLength(3)]
    public string Currency { get; set; } = default!;

    /// <summary>What the dealer is paid, in <see cref="Currency"/>.</summary>
    [Precision(18, 2)]
    public decimal LaborRate { get; set; }

    /// <summary>What the manufacturer is charged, in USD.</summary>
    [Precision(18, 2)]
    public decimal LaborRateDistributor { get; set; }

    public DateTime EffectiveDate { get; set; }
}
