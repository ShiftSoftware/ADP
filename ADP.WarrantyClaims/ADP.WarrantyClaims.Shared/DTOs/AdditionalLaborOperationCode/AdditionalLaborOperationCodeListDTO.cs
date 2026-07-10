using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.AdditionalLaborOperationCode;

[ShiftEntityKeyAndName(nameof(ID), nameof(Code))]
public class AdditionalLaborOperationCodeListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string Code { get; set; } = default!;
    public decimal Time { get; set; }
    public string? Description { get; set; }
}
