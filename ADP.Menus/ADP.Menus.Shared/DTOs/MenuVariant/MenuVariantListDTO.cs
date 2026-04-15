using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;

public class MenuVariantListDTO : ShiftEntityListDTO
{
    [MenuVariantHashId]
    public override string? ID { get; set; }

    [MenuHashId]
    public string MenuID { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string MenuPrefix { get; set; } = default!;
    public string? MenuPostfix { get; set; }
    public decimal LabourRate { get; set; }
    public bool HasStandaloneItems { get; set; }
}
