using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;

public class ServiceIntervalGroupDTO : ShiftEntityViewAndUpsertDTO
{
    [ServiceIntervalGroupHashId]
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string LabourCode { get; set; } = default!;

    [Required]
    public string LabourDescription { get; set; } = default!;
}

