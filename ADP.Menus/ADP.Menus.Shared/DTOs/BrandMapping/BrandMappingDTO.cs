using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.BrandMapping;

public class BrandMappingDTO : ShiftEntityViewAndUpsertDTO
{
    [BrandMappingHashId]
    public override string? ID { get; set; }

    [Required]
    public string Code { get; set; } = default!;

    [Required]
    public string BrandAbbreviation { get; set; } = default!;

    [Required]
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public ShiftEntitySelectDTO Brand { get; set; } = default!;
}



