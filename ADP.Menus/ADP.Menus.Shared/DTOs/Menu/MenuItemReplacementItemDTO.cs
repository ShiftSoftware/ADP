using ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuItemReplacementItemDTO 
{
    [ReplacementItemHashId]
    public string? ID { get; set; }

    public string Name { get; set; } = default!;

    public ReplacementItemType Type { get; set; }

    public bool AllowMultiplePartNumbers { get; set; }

    public decimal? StandaloneAllowedTime { get; set; }
    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    [StandaloneReplacementItemGroupHashId]
    public ShiftEntitySelectDTO? StandaloneReplacementItemGroup { get; set; } = default!;
}
