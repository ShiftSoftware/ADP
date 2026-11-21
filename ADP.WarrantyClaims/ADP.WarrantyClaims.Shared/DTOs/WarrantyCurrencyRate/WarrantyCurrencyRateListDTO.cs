using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyCurrencyRate;

public class WarrantyCurrencyRateListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string Currency { get; set; } = default!;
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }
}
