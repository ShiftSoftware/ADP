using ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;

public class ReplacementItemListDTO : ShiftEntityListDTO
{
    [ReplacementItemHashId]
    public override string? ID { get; set; }

    public string Name { get; set; } = default!;

    public ReplacementItemType Type { get; set; }

    public bool AllowMultiplePartNumbers { get; set; }

    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string StandaloneOperationCode { get; set; } = default!;
    public string StandaloneLabourCode { get; set; } = default!;

    [StandaloneReplacementItemGroupHashId]
    public ShiftEntitySelectDTO? StandaloneReplacementItemGroup { get; set; } = default!;
}
