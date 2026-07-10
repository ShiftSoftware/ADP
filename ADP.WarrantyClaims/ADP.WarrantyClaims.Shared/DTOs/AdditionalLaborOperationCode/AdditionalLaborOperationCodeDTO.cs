using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.AdditionalLaborOperationCode;

public class AdditionalLaborOperationCodeDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public string Code { get; set; } = default!;

    [Required]
    public decimal Time { get; set; }

    public string? Description { get; set; }
}
