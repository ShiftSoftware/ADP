using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuListDTO : ShiftEntityListDTO
{
    [MenuHashId]
    public override string? ID { get; set; }
    public string BasicModelCode { get; set; } = default!;
    public string? VehilceModel { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string? BrandID { get; set; } = default!;

    public int VariantsCount { get; set; }
}
