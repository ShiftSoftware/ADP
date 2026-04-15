using ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuReplacementItemDTO
{
    [ReplacementItemHashId]
    public string? ID { get; set; }

    public string Name { get; set; } = default!;

    public ReplacementItemType Type { get; set; }

    public bool AllowMultiplePartNumbers { get; set; }

    public long ReplacementItemVehicleModelID { get; set; }

    public List<ReplacementItemDefaultPartDTO> DefaultParts { get; set; } = new();

    public string StandaloneOperationCode { get; set; } = default!;
    public string StandaloneLabourCode { get; set; } = default!;

    public decimal StandaloneAllowedTime { get; set; }
    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    [StandaloneReplacementItemGroupHashId]
    public ShiftEntitySelectDTO? StandaloneReplacementItemGroup { get; set; } = default!;
}
