using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyLaborRate;

public class WarrantyLaborRateListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string Currency { get; set; } = default!;
    public decimal LaborRate { get; set; }
    public decimal LaborRateDistributor { get; set; }
    public DateTime EffectiveDate { get; set; }
}
