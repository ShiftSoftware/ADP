using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.BrandMapping;

public class BrandMappingListDTO : ShiftEntityListDTO
{
    [BrandMappingHashId]
    public override string? ID { get; set; }

    [Required]
    public string Code { get; set; } = default!;

    [Required]
    public string BrandAbbreviation { get; set; } = default!;

    [Required]
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string? BrandID { get; set; } = default!;
}



