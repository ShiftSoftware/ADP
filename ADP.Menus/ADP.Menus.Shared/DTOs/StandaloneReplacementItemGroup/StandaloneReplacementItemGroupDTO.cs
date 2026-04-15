using ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class StandaloneReplacementItemGroupDTO : ShiftEntityViewAndUpsertDTO
{
    [StandaloneReplacementItemGroupHashId]
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string MenuCode { get; set; } = default!;

    [Required]
    public string LabourCode { get; set; } = default!;
}
