using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRateMapping;

public class LabourRateMappingDTO : ShiftEntityViewAndUpsertDTO
{
    [LabourRateMappingHashId]
    public override string? ID { get; set; }

    [Required]
    public decimal? LabourRate { get; set; }

    [Required]
    public string Code { get; set; } = default!;

    [Required]
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public ShiftEntitySelectDTO Brand { get; set; } = default!;
}

