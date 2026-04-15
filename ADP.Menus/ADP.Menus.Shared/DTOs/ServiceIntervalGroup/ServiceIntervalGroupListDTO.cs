using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ServiceIntervalGroupListDTO : ShiftEntityListDTO
{
    [ServiceIntervalGroupHashId]
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
    public string LabourCode { get; set; } = default!;
    public string LabourDescription { get; set; } = default!;
}

