using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class VehicleModelListDTO : ShiftEntityListDTO
{
    [VehicleModelHashId]
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string? BrandID { get; set; } = default!;
}
