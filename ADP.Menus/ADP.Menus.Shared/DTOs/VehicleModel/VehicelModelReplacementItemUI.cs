using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class VehicelModelReplacementItemUI
{
    public string? ID { get; set; }

    public string Name { get; set; } = default!;

    public ReplacementItemType Type { get; set; }

    public bool AllowMultiplePartNumbers { get; set; }

    public List<ReplacementItemDefaultPartDTO> DefaultParts { get; set; } = new();

    public string StandaloneOperationCode { get; set; } = default!;
    public string StandaloneLabourCode { get; set; } = default!;

    public decimal? StandaloneAllowedTime { get; set; }
    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    public ShiftEntitySelectDTO? StandaloneReplacementItemGroup { get; set; } = default!;

    public bool IsSelected { get; set; }
}
