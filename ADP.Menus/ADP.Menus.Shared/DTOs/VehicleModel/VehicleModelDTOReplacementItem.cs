using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class VehicleModelDTOReplacementItem
{
    [ReplacementItemHashId]
    public string ReplacementItemID { get; set; }

    public string Name { get; set; } = default!;

    public ReplacementItemType Type { get; set; }

    public bool AllowMultiplePartNumbers { get; set; }

    public List<ReplacementItemDefaultPartDTO> DefaultParts { get; set; } = new();

    public decimal? StandaloneAllowedTime { get; set; }
    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    public ShiftEntitySelectDTO? StandaloneReplacementItemGroup { get; set; } = default!;

    public bool IsSelected { get; set; }

    public VehicleModelDTOReplacementItem()
    {
    }

    public VehicleModelDTOReplacementItem(string replacementItemId)
    {
        ReplacementItemID = replacementItemId;
    }
}
