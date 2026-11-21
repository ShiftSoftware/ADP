using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

/// <summary>
/// The rate of a claim currency to USD from a date on. <see cref="Rate"/> is units of
/// <see cref="Currency"/> per 1 USD, the direction used everywhere in the module. A new claim takes the
/// row with the latest <see cref="EffectiveDate"/> that has started and stores the rate on itself, so a
/// later row never changes an existing claim.
/// </summary>
[TemporalShiftEntity]
[Index(nameof(Currency), nameof(EffectiveDate))]
public class WarrantyCurrencyRate : ShiftEntity<WarrantyCurrencyRate>
{
    [Column(TypeName = "varchar")]
    [MaxLength(3)]
    public string Currency { get; set; } = default!;

    [Precision(18, 6)]
    public decimal Rate { get; set; }

    public DateTime EffectiveDate { get; set; }
}
